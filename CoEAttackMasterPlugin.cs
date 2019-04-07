// plugin for COE attack preparation
// popup messages and beep & sound instruction of attack&preparation
// key to enable/disable the sound instruction : default Keys.NumPad9
using Turbo.Plugins.Default;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Windows.Forms;
using System.Media;

namespace Turbo.Plugins.James
{
    public class CoEAttackMasterPlugin : BasePlugin, IInGameTopPainter, IKeyEventHandler
    {
    	   public bool HideWhenUiIsHidden { get; set; }
        private static System.Timers.Timer aTimer;
        private static System.Timers.Timer bTimer;
        private static System.Timers.Timer CountTimer;
        private bool Alarm;
        private bool TimerStarted;
        private bool Speak;
        private int TimeLeftBeforeAttack = 4;	//  must be from 2 to 8 : default 4 (secs before attact)
        private BuffRuleCalculator RuleCalculator { get; set; }
        private int AlarmCount;

	   private SoundPlayer ReadyToAttack = new SoundPlayer();
	   
        public bool IsGuardianDead
        {
            get
            {
                if (Hud.Game.Monsters.Any(m => m.Rarity == ActorRarity.Boss && !m.IsAlive))
                    return true;

                return riftQuest != null && (riftQuest.QuestStepId == 5 || riftQuest.QuestStepId == 10 || riftQuest.QuestStepId == 34 || riftQuest.QuestStepId == 46);
            }
        }

        private IQuest riftQuest
        {
            get
            {
                return Hud.Game.Quests.FirstOrDefault(q => q.SnoQuest.Sno == 337492) ?? // rift
                       Hud.Game.Quests.FirstOrDefault(q => q.SnoQuest.Sno == 382695);   // gr
            }
        }

        public CoEAttackMasterPlugin()
        {
            Enabled = true;
            ReadyToAttack.SoundLocation = "sounds/notification_10.wav";
            ReadyToAttack.LoadAsync();
        }

        public override void Load(IController hud)
        {
            base.Load(hud);
            
		  HideWhenUiIsHidden = false;
		  TimerStarted = false;
            Speak = true;
            Alarm = true;
            AlarmCount = 0;

            if (TimeLeftBeforeAttack < 2 || TimeLeftBeforeAttack >8)
            	TimeLeftBeforeAttack = 4;

            RuleCalculator = new BuffRuleCalculator(Hud);

            RuleCalculator.Rules.Add(new BuffRule(430674) { IconIndex = 1, MinimumIconCount = 0, DisableName = true }); // 비전
            RuleCalculator.Rules.Add(new BuffRule(430674) { IconIndex = 2, MinimumIconCount = 0, DisableName = true }); // 냉기
            RuleCalculator.Rules.Add(new BuffRule(430674) { IconIndex = 3, MinimumIconCount = 0, DisableName = true }); // 화염
            RuleCalculator.Rules.Add(new BuffRule(430674) { IconIndex = 4, MinimumIconCount = 0, DisableName = true }); // 신성
            RuleCalculator.Rules.Add(new BuffRule(430674) { IconIndex = 5, MinimumIconCount = 0, DisableName = true }); // 번개
            RuleCalculator.Rules.Add(new BuffRule(430674) { IconIndex = 6, MinimumIconCount = 0, DisableName = true }); // 물리
            RuleCalculator.Rules.Add(new BuffRule(430674) { IconIndex = 7, MinimumIconCount = 0, DisableName = true }); // 독
        }

        private IEnumerable<BuffRule> GetCurrentRules(HeroClass heroClass)
        {
            for (int i = 1; i <= 7; i++)
            {
                switch (heroClass)
                {
                    case HeroClass.Barbarian: if (i == 1 || i == 4 || i == 7) continue; break;
                    case HeroClass.Crusader: if (i == 1 || i == 2 || i == 7) continue; break;
                    case HeroClass.DemonHunter: if (i == 1 || i == 4 || i == 7) continue; break;
                    case HeroClass.Monk: if (i == 1 || i == 7) continue; break;
                    case HeroClass.Necromancer: if (i == 1 || i == 3 || i == 4 || i == 5) continue; break;
                    case HeroClass.WitchDoctor: if (i == 1 || i == 4 || i == 5) continue; break;
                    case HeroClass.Wizard: if (i == 4 || i == 6 || i == 7) continue; break;
                }
                yield return RuleCalculator.Rules[i - 1];
            }
        }

        public void OnKeyEvent(IKeyEvent keyEvent)
        {
        		if (Hud.Input.IsKeyDown(Keys.NumPad9))			// key to speak attack instructions
        	  	{
        	  		if (Speak)
        	  		{
        	  			Hud.Sound.Speak("Attack Instructions end!");
        	  			//Hud.Sound.Speak("공격 안내 끝!");
        	  			Speak = false;
        	  		} else
        	  		{
        	  			Hud.Sound.Speak("Attack Instructions start!");
        	  			//Hud.Sound.Speak("공격 안내 시작!");
        	  			Speak = true;
        	  		}
        	  	}
        }

        public void PaintTopInGame(ClipState clipState)
        {
            if (clipState != ClipState.BeforeClip) return;
            if (HideWhenUiIsHidden && Hud.Render.UiHidden) return;
            if (Hud.Game.IsInTown ||  Hud.Game.Me.IsDead || IsGuardianDead)
            {
            	if (TimerStarted)
            	{
            		aTimer.Enabled = false;
            		bTimer.Enabled = false;
            		CountTimer.Enabled = false;
            		TimerStarted = false;
            		Alarm = true;
            	}
            	return;
            }

            var player = Hud.Game.Me;
            var buff = player.Powers.GetBuff(430674);	//CoE Buff
            if ((buff == null) || (buff.IconCounts[0] <= 0)) return;

            var classSpecificRules = GetCurrentRules(player.HeroClassDefinition.HeroClass);

            RuleCalculator.CalculatePaintInfo(player, classSpecificRules);

            if (RuleCalculator.PaintInfoList.Count == 0) return;
            if (!RuleCalculator.PaintInfoList.Any(info => info.TimeLeft > 0)) return;

            var highestElementalBonus = player.Offense.HighestElementalDamageBonus;

            for (int i = 0; i < RuleCalculator.PaintInfoList.Count; i++)	// RuleCalculator.PaintInfoList.Count = 4
            {
                var info = RuleCalculator.PaintInfoList[0];
                if (info.TimeLeft <= 0)
                {
                    RuleCalculator.PaintInfoList.RemoveAt(0);
                    RuleCalculator.PaintInfoList.Add(info);
                }
                else break;
            }

            for (int orderIndex = 0; orderIndex < RuleCalculator.PaintInfoList.Count; orderIndex++)
            {
                var info = RuleCalculator.PaintInfoList[orderIndex];
                var best = false;
                switch (info.Rule.IconIndex)
                {
                    case 1: best = player.Offense.BonusToArcane == highestElementalBonus; break;
                    case 2: best = player.Offense.BonusToCold == highestElementalBonus; break;
                    case 3: best = player.Offense.BonusToFire == highestElementalBonus; break;
                    case 4: best = player.Offense.BonusToHoly == highestElementalBonus; break;
                    case 5: best = player.Offense.BonusToLightning == highestElementalBonus; break;
                    case 6: best = player.Offense.BonusToPhysical == highestElementalBonus; break;
                    case 7: best = player.Offense.BonusToPoison == highestElementalBonus; break;
                }
                if (best && orderIndex > 0)
                {
                    info.TimeLeft = (orderIndex - 1) * 4 + RuleCalculator.PaintInfoList[0].TimeLeft;

                } else
                	info.TimeLeftNumbersOverride = false;

		      if ((info.TimeLeft == TimeLeftBeforeAttack) && Alarm && best)
                {
                	TimerStarted = true;
                	
                	if (orderIndex == 0)
                	{
                		AlarmCount = 0;
                		CoEAttack_Action();
                	} else if (orderIndex == 1)
                	{	
					// Timer Start
					aTimer = new System.Timers.Timer();
					aTimer.Elapsed += CoEReady;
					aTimer.AutoReset = true;
					aTimer.Enabled = true;
					switch (Hud.Game.Me.HeroClassDefinition.HeroClass)
	               		{
	               	    		case HeroClass.Monk:
	               	    			 aTimer.Interval = 20000;		// 5 elements *4 secs
	               	    			 break;
	               	    		case HeroClass.Necromancer:
	                	    		 aTimer.Interval = 12000;		// 3 elements
	               	    			 break;
	               	    		case HeroClass.WitchDoctor:
	               	    		case HeroClass.Wizard:
	               	    		case HeroClass.Crusader:
	               	    		case HeroClass.Barbarian:
	               	    		case HeroClass.DemonHunter:
	               	    			 aTimer.Interval = 16000;		// 4 elements
	               	    			 break;
	               	    	}
					// timer End
					
					CoEReady_Action();
                		Alarm = false;
                	}
               }
            }
        }

        private void CoEReady(Object source, System.Timers.ElapsedEventArgs e)
        {
			CoEReady_Action();
	   }

	   private void CoEReady_Action()
	   {
			// attack timer
			bTimer = new System.Timers.Timer();
			bTimer.Interval = TimeLeftBeforeAttack * 1000;
			bTimer.Elapsed += CoEAttack;
			bTimer.AutoReset = false;
			bTimer.Enabled = true;

		     if (Speak)
		     {
		     		//Console.Beep(900, 200);
		     		ReadyToAttack.PlaySync();
		     		Hud.Sound.Speak("Ready to attack!");
			     	string text = "*** Ready to attack ***";
		     		//Hud.Sound.Speak("공격 준비!");
			     	//string text = "*** 공격 준비 ***";			     	
				Hud.RunOnPlugin<PopupMsgPlugin>(plugin =>
                	{ 
				   	plugin.Show(text, "Preparation!", TimeLeftBeforeAttack*1000, "", PopupMsgPlugin.EnumPopupDecoratorToUse.Default);
				   	//plugin.Show(text, " 준 비 !", TimeLeftBeforeAttack*1000, "", PopupInformPlugin.EnumPopupDecoratorToUse.Default);
                     });			     	
		     }

	   }

	   private void CoEAttack(Object source, System.Timers.ElapsedEventArgs e)
        {
        		CoEAttack_Action();
        }
        
        private void CoEAttack_Action()
        {
			if (Speak)
			{
				Console.Beep(300, 120);
				CountTimer = new System.Timers.Timer();
				CountTimer.Interval = 1000;		// every second
				CountTimer.Elapsed += Counter;
				CountTimer.AutoReset = true;
				CountTimer.Enabled = true;
				
				Hud.Sound.Speak("Attack!");
				string text = ">>> Attack !!! <<<";
				//Hud.Sound.Speak("공격!");
				//string text = ">>> 공 격 !!! <<<";
				Hud.RunOnPlugin<PopupMsgPlugin>(plugin =>
                	{
				   	plugin.Show(text, "* Now! *", 4000, "", PopupMsgPlugin.EnumPopupDecoratorToUse.Default);
				   	//plugin.Show(text, "* 지금 능력 최고! *", 4000, "", PopupInformPlugin.EnumPopupDecoratorToUse.Type2);
                     });
			}
        }

	   private void Counter(Object source, System.Timers.ElapsedEventArgs e)
        {
        		AlarmCount++;
        		if (AlarmCount > 3)
        		{
        			CountTimer.Enabled = false;
        			AlarmCount = 0;
        		} else
        			Console.Beep(300, 120);
        }
        
        public void OnNewArea(bool newGame, ISnoArea area)
        {
            if (newGame)
            {
			TimerStarted = false;
	           Speak = true;
	           Alarm = true;
	           AlarmCount = 0;
            }
        }
    }

}
