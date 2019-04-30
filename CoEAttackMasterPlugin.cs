// plugin for COE attack preparation
// popup messages and beep & sound instruction of attack&preparation
// NumPad8 : toggle text notice, Numpad9 : toggle sound notice
using Turbo.Plugins.Default;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Windows.Forms;
using System.Media;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Turbo.Plugins.James
{
	public class CoEAttackMasterPlugin : BasePlugin, IInGameTopPainter, IKeyEventHandler
    	{
		private SoundPlayer ReadyToAttack = new SoundPlayer();
		public bool HideWhenUiIsHidden { get; set; }
		private static System.Timers.Timer aTimer;
		private static System.Timers.Timer bTimer;
		private static System.Timers.Timer CountTimer;
		private bool Alarm;
		private bool TimerStarted;
		private bool Speak, TextMsg;
		private int TimeLeftBeforeAttack = 4;	//  must be from 2 to 8 : default 4 (secs before attact)
		private BuffRuleCalculator RuleCalculator { get; set; }
		private int AlarmCount;
		private string culture;
		private bool SoundFileExist;

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
			
			string path = Assembly.GetExecutingAssembly().Location;
        		Match match = Regex.Match(path, @".+(?=plugins_.+)");
			if (match.Success)
			{
				path = match.Value.Replace("\\", "/");
				path += "/sounds/notification_10.wav";
				ReadyToAttack.SoundLocation = path;
				ReadyToAttack.LoadAsync();
				SoundFileExist = true;
			} else
			{
				Hud.Sound.Speak("No such wave file in the directory!");
				SoundFileExist = false;
			}
		}

        	public override void Load(IController hud)
        	{
			base.Load(hud);
			
			culture = System.Globalization.CultureInfo.CurrentCulture.ToString().Substring(0, 2);
			
			HideWhenUiIsHidden = false;
			TimerStarted = false;
			Speak = true;
			TextMsg = true;
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
        		if (Hud.Input.IsKeyDown(Keys.NumPad8))			// key to text attack instructions
        	  	{
        	  		Console.Beep(200, 150);
        	  		if (TextMsg)
        	  		{
        	  			if (culture == "ko")
        	  				Hud.Sound.Speak("문자 안내 끝!");
        	  			else
        	  				Hud.Sound.Speak("Text guide stops!");
        	  			TextMsg = false;
        	  		} else
        	  		{
        	  			if (culture == "ko")
        	  				Hud.Sound.Speak("문자 안내 시작!");
        	  			else
        	  				Hud.Sound.Speak("Text guide starts!");
        	  			TextMsg = true;
        	  		}
        	  	}

        		if (Hud.Input.IsKeyDown(Keys.NumPad9))			// key to speak attack instructions
        		//if (Control.ModifierKeys == Keys.Control && Hud.Input.IsKeyDown(Keys.NumPad9))
        	  	{
        	  		Console.Beep(200, 150);
        	  		if (Speak)
        	  		{
        	  			if (culture == "ko")
        	  				Hud.Sound.Speak("소리 안내 끝!");
        	  			else
        	  				Hud.Sound.Speak("Sound guide stops!");
        	  			Speak = false;
        	  		} else
        	  		{
        	  			if (culture == "ko")
        	  				Hud.Sound.Speak("소리 안내 시작!");
        	  			else
        	  				Hud.Sound.Speak("Sound guide starts!");
        	  			Speak = true;
        	  		}
        	  	}
        	}

        	public void PaintTopInGame(ClipState clipState)
        	{
	        	if (clipState != ClipState.BeforeClip) return;
	        	if (HideWhenUiIsHidden && Hud.Render.UiHidden) return;
        	
        	try
        	{
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
        	catch {}
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

			string msg = string.Empty;
			
		     if (Speak || TextMsg)
		     {
		     		if (Speak)
		     		{
			     		if (SoundFileExist) ReadyToAttack.PlaySync();
			     		if (culture == "ko")
			     			msg = "공격 준비!";
			     		else
			     			msg = "Ready to attack!";
			     		Hud.Sound.Speak(msg);
				}
				if (TextMsg)
				{
					string msgTitle = string.Empty;
					if (culture == "ko")
					{
						msg = ">>> 공격 준비 <<<";
						msgTitle = " 준 비 !";
					} else
					{
						msg = ">>> Ready to attack <<<";
						msgTitle = "Preparation!";
					}
						
					Hud.RunOnPlugin<PopupMsgPlugin>(plugin =>
	                	{
					   	plugin.Show(msg, msgTitle, (TimeLeftBeforeAttack-1)*1000, "", PopupMsgPlugin.EnumPopupDecoratorToUse.Default);
	                     });
	                }
			}
		}

	   	private void CoEAttack(Object source, System.Timers.ElapsedEventArgs e)
        	{
        		CoEAttack_Action();
       	 }

        	private void CoEAttack_Action()
        	{
        		string msg = string.Empty;
			if (Speak)
			{
				Console.Beep(300, 120);
				CountTimer = new System.Timers.Timer();
				CountTimer.Interval = 1000;		// every second
				CountTimer.Elapsed += Counter;
				CountTimer.AutoReset = true;
				CountTimer.Enabled = true;

				if (culture == "ko")
					msg = "공격!";
				else
					msg = "Attack!";
				Hud.Sound.Speak(msg);
			}
			if (TextMsg)
			{
				string msgTitle = string.Empty;
				if (culture == "ko")
				{
					msg = ">>> 공 격 !!! <<<";
					msgTitle = "* 지금 좋아! *";
				} else
				{
					msg = ">>> Attack !!! <<<";
					msgTitle = "* Now Good! *";
				}			
				Hud.RunOnPlugin<PopupMsgPlugin>(plugin =>
                	{
				   	plugin.Show(msg, msgTitle, 4000, "", PopupMsgPlugin.EnumPopupDecoratorToUse.Default);
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
	           	TextMsg = true;
	           	Alarm = true;
	           	AlarmCount = 0;
            	}
        	}
    	}
}