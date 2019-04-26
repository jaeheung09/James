// This plugin shows you current Cpu usuage, current Ram available, current Latency/Average Latency and TurboHUD real FPS and combat-related info
// Regarding FPS, it is different from of the D3 built-in function. This calculates real turboHUD's FPS, which is mostly the same but on occasion shows clear distinction from Blizzard's.
// if they meet the guideline you set, info text color will change. :blue(OK)->orange(Warning)->red(Bad)
// toggle Key to trigger monitoring resources : ctrl + NumPad.Subtract("-")
// To start log, put your cursor on the chat edit line by pressing "Enter" and then "/combatlog n/" (n for log interval(sec)) or "/cancellog/" to cancel your log request.
// Logging will start right after you enter a (G)rift. it will be finished as soon as Boss is dead.
// Open and look at "CombatLog.txt" in the turboHUD log folder. (You can change the log file name in this source code
using System;
using System.Linq;
using Turbo.Plugins.Default;
using System.Windows.Forms;
using SharpDX.DirectInput;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Turbo.Plugins.James
{
    public class D3CombatLogPlugin : BasePlugin, IKeyEventHandler, IInGameTopPainter
    {
    		private PerformanceCounter Cpu = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        	private PerformanceCounter Ram = new PerformanceCounter("Memory", "Available MBytes");
        	private int LogInterval;
        	private static int lastTick;
        	private static int lastFrameRate;
        	private static int frameRate;
    		private int BossATick, StartTick;
        	private bool monitoring;
        	private static System.Timers.Timer MonitorTimer;
        	private static System.Timers.Timer ReadEditLineTimer;
        	private static System.Timers.Timer ScanMonstersTimer;
        	private TopLabelDecorator ContentOKDecorator { get; set; }
        	private TopLabelDecorator ContentWarningDecorator { get; set; }
        	private TopLabelDecorator ContentBadDecorator { get; set; }
        	private string MonitoredResource;
        	private string MonitoredLog;
        	// You can set them to your prefered values (OK: color blue green, Warning: orange, Bad: red)*****
        	private const string LogFile = "CombatLog";	// a text file in the TH log directory
        	private const int CpuWarning = 90;		// more than cpu using 90%
        	private const int CpuBad = 95;			// more than cpu using 95%
        	private const int RamWarning = 200;		// less than 200 MB
        	private const int RamBad = 100;			// less than 100 MB
        	private const int LatencyWarning = 50;	// if current latency is more than 50 ms
        	private const int LatencyBad = 80;		// if cur latency is 80 ms
        	private const int FPSWarning = 40;		// if current FPS is less than 40
        	private const int FPSBad = 20;			// if cur FPS is less than 20
        	private const int RMInterval = 1000; 		// Resources monitoring interval 1초
        	//*********************************************************************************************************
        	private string culture;
        	private string eliteNames;
        	private int mobNumbers;
		private int BaseX;
		private int BaseY;
		private int displayTag;	// ok, warning, bad
		private int FrameRate;
		private bool First;
		private bool Logging;
		private bool uiFirst;
		private bool aTimeStamp;
		private bool ScanMonsters;
		private bool IsGRift;
		private uint savedSno;
		private string BossTextUiName = "Root.NormalLayer.game_notify_dialog_backgroundScreen.game_text_line0";
		private string chatEditLine = "Root.NormalLayer.chatentry_dialog_backgroundScreen.chatentry_content.chat_editline";
		public TopLabelWithTitleDecorator PlayerDecorator { get; set; }
		// private enum display { ok, warning, bad }

	     public bool IsGuardianAlive
	     {
	     		get
	          {
	                return riftQuest != null && (riftQuest.QuestStepId == 3 || riftQuest.QuestStepId == 16);
	          }
	     }

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

		public class Buff
		{
			public bool Displayed { get; set; }
			public uint SNO { get; set; }
			public int Icon { get; set; }
			public string Name { get; set; }
			public string Hint { get; set; }
			public string Title { get; set; }
			public int Duration { get; set; }

			public Buff(uint sno, int icon, string name)
			{
				this.SNO = sno;
				this.Icon = icon;
				this.Name = name;
			}
		}
        	public List<Buff> BuffsToWatch { get; set; }

		public D3CombatLogPlugin()
        	{
        		Enabled = true;
        	}

        	public override void Load(IController hud)
        	{
            	base.Load(hud);

			culture = System.Globalization.CultureInfo.CurrentCulture.ToString().Substring(0, 2);

               if (culture == "ko")
               {
				BuffsToWatch = new List<Buff>();
				BuffsToWatch.Add(new Buff(262935, 0, "능력P"));
				BuffsToWatch.Add(new Buff(266258, 0, "재감P"));
				BuffsToWatch.Add(new Buff(266254, 0, "보호P"));
				BuffsToWatch.Add(new Buff(263029, 0, "도관P"));
				BuffsToWatch.Add(new Buff(403404, 0, "도관P"));
				BuffsToWatch.Add(new Buff(266271, 0, "속도P"));
			} else
			{
				BuffsToWatch = new List<Buff>();
				BuffsToWatch.Add(new Buff(262935, 0, "pPW"));
				BuffsToWatch.Add(new Buff(266258, 0, "pCH"));
				BuffsToWatch.Add(new Buff(266254, 0, "pSH"));
				BuffsToWatch.Add(new Buff(263029, 0, "pCO"));
				BuffsToWatch.Add(new Buff(403404, 0, "pCO"));
				BuffsToWatch.Add(new Buff(266271, 0, "pSP"));
			}

            	monitoring = false;
            	First = true;
            	Logging = false;
            	uiFirst = true;
            	aTimeStamp = false;
            	ScanMonsters = false;
            	IsGRift = false;
            	savedSno = 0;
            	LogInterval = 1000; 	// default 1 sec
            	MonitoredResource = string.Empty;
            	BaseX = (int)(Hud.Window.Size.Width * 0.82f);
            	BaseY = (int)(Hud.Window.Size.Height * 0.90f);

			PlayerDecorator = new TopLabelWithTitleDecorator(hud)
			{
				BorderBrush = hud.Render.CreateBrush(255, 180, 147, 109, -1),
				BackgroundBrush = hud.Render.CreateBrush(100, 0, 0, 0, 0),	//200
				TextFont = hud.Render.CreateFont("tahoma", 8, 255, 255, 255, 255, true, false, false),
				TitleFont = hud.Render.CreateFont("tahoma", 6, 255, 180, 147, 109, true, false, false),
			};

		     ContentOKDecorator = new TopLabelDecorator(Hud)
		     {
		           TextFont = Hud.Render.CreateFont("consolas", 8, 220, 100, 255, 100, true, false, 255, 0, 0, 0, true),
		           TextFunc = () => MonitoredResource,
		     };

		     ContentWarningDecorator = new TopLabelDecorator(Hud)
		     {
		           TextFont = Hud.Render.CreateFont("consolas", 8, 220, 255, 150, 80, true, false, 255, 0, 0, 0, true),
		           TextFunc = () => MonitoredResource,
		     };

		     ContentBadDecorator = new TopLabelDecorator(Hud)
		     {
		           TextFont = Hud.Render.CreateFont("consolas", 8, 220, 255, 0, 0, true, false, 255, 0, 0, 0, true),
		           TextFunc = () => MonitoredResource,
		    	};

		     ReadEditLineTimer = new System.Timers.Timer();
			ReadEditLineTimer.Interval = 500;		// edit line filtering interval
			ReadEditLineTimer.Elapsed += ReadEditLine;
			ReadEditLineTimer.AutoReset = true;
			ReadEditLineTimer.Enabled = true;
	   	}

		public void ReadEditLine(Object source, System.Timers.ElapsedEventArgs e)
        	{
        		// chat edit line : need a new log request every new game
        		if (!Hud.Render.GetUiElement(chatEditLine).Visible)
        			return;

        		var lineStr = Hud.Render.GetUiElement(chatEditLine).ReadText(System.Text.Encoding.UTF8, false).Trim();	// if error, change "UTF8" with "Default"...not tested though
        		Match match = Regex.Match(lineStr, @"(?<=/combatlog ).+(?=/)");
			if (match.Success)	// in the edit line, should type "/combatlog/ or /combatlong n/" <- the n is the log interval.
			{
				match = Regex.Match(lineStr, @"\d{1,}");	// extract a number
				if (match.Success)
				{
					int iVal = Int32.Parse(match.Value);
					if (iVal < 1 || iVal > 60)		// log interval must be between 1 sec and 60 secs
						iVal = 1;
					LogInterval = iVal * 1000;	// log interval default value :1 sec	
				} else
					LogInterval = 1000; 	// default 1 sec

				First = true;
        			if (!Logging)
        			{
        				Logging = true;
        				uiFirst = true;
					monitoringRS(true);		// collect data of computing resources
					
        				ScanMonstersTimer = new System.Timers.Timer();
					ScanMonstersTimer.Interval = LogInterval;	// allow scanning monsters once per second in "PaintTopInGame"
					ScanMonstersTimer.Elapsed += AllowScanMonsters;
					ScanMonstersTimer.AutoReset = true;
					ScanMonstersTimer.Enabled = true;

        				if (Hud.Sound.LastSpeak.TimerTest(5000))
        					if (culture == "ko")
        						Hud.Sound.Speak("전투 로그 요청이 접수되었습니다.");
        					else
        						Hud.Sound.Speak("your combat log request is received.");
        			} else
        			{
        				if (Hud.Sound.LastSpeak.TimerTest(5000))
        					if (culture == "ko")
        						Hud.Sound.Speak("로그 요청이 이미 접수된 상태입니다.");
        					else
        						Hud.Sound.Speak("Your log request is already received.");
        			}
        		} else if (lineStr.Equals("/cancellog/"))
        		{
        			if (Logging)
        			{
        		     		Logging = false;
        		     		if (Hud.Sound.LastSpeak.TimerTest(5000))
        		     			if (culture == "ko")
        						Hud.Sound.Speak("전투 로그 요청이 취소되었습니다.");
        					else
        						Hud.Sound.Speak("your log cancel request is canceled.");
        			} else
        			{
        				if (Hud.Sound.LastSpeak.TimerTest(5000))
        					if (culture == "ko")
        						Hud.Sound.Speak("로그 요청이 접수되지 않은 상태입니다.");
        					else
        						Hud.Sound.Speak("No log request exists..");
        			}
        		}
        	}

		// to reduce the use of the computing resources as much as possible
		public void AllowScanMonsters(Object source, System.Timers.ElapsedEventArgs e)
        	{
			ScanMonsters = true;
		}

        	public void PaintTopInGame(ClipState clipState)
        	{
        		if (clipState != ClipState.AfterClip) return;		// without checking this, turboHUD generates frams more than VSync cap.
        		if (!monitoring && !Logging)	return;			// if not both monitoring and logging, exit

			if (monitoring)
			{
	        		FrameRate = CalculateFrameRate();	// count the frames for each layer while HUD is rendering the UI
	
				switch (displayTag)
				{
					case 0:	// OK state
		          			ContentOKDecorator.Paint(BaseX, BaseY, 200, 100, HorizontalAlign.Left);
		          			break;
					case 1:	// Warning state
		          			ContentWarningDecorator.Paint(BaseX, BaseY, 200, 100, HorizontalAlign.Left);
		          			break;
					case 2:	// Bad state
		          			ContentBadDecorator.Paint(BaseX, BaseY, 200, 100, HorizontalAlign.Left);
		          			break;
		          	}
			}
			
	          	if (Logging)
	          	{
	          		if (ScanMonsters)
	          		{
		          		// get elite names in combat
					var monsters = Hud.Game.AliveMonsters.Where(x => (x.SnoMonster != null) && (x.NormalizedXyDistanceToMe <= 80) && (x.Rarity >= ActorRarity.Champion));
					mobNumbers = Hud.Game.AliveMonsters.Count(m => m.NormalizedXyDistanceToMe <= 80);
					
					eliteNames = string.Empty;
					string sname = string.Empty;
					foreach (var monster in monsters)
					{
						if (savedSno != monster.SnoMonster.Sno)
						{
							savedSno = monster.SnoMonster.Sno;
							if (culture == "ko")
								sname = monster.SnoMonster.NameLocalized;
							else
								sname = monster.SnoMonster.NameEnglish;
							eliteNames += sname + ", ";
						}
					}
				}

	          		if (IsGuardianDead)
	          		{
			      	Logging = false;
			      	uiFirst = true;
			      	string timeNow = DateTime.Now.ToString("HH:mm:ss");
					var titleContent = timeNow + ",  " + Convert.ToString((int)Hud.Game.RiftPercentage) + " %, " +
			          					   MonitoredLog + ", *Boss is terminated!!!*  Game finish time (";
			          	int GameTime = (System.Environment.TickCount - StartTick) / 1000;
			          	int bossKillTime = (System.Environment.TickCount - BossATick) / 1000;
			          	TimeSpan t = TimeSpan.FromSeconds(GameTime);
			          	var answer = string.Format("{0}m:{1:D2}s", t.Minutes, t.Seconds);
			          	titleContent += answer.ToString() + ")";
			          	if (IsGRift)
			          	{
				          	t = TimeSpan.FromSeconds(bossKillTime);
				          	answer = string.Format("{0}m:{1:D2}s", t.Minutes, t.Seconds);
				          	titleContent += ", Boss kill time (" + answer.ToString() + ")";
			          	}
			          	Hud.TextLog.Log(LogFile, titleContent, aTimeStamp, true);
			          	if (culture == "ko")
			          		Hud.Sound.Speak("로그 기록이 완료되었습니다!");
			          	else
			          		Hud.Sound.Speak("Combat Log is completed!");
			          		
			          	monitoringRS(false);
			          	Console.Beep(400, 200);
		          } else if (Hud.Game.SpecialArea == SpecialArea.Rift || Hud.Game.SpecialArea == SpecialArea.GreaterRift)
	          			combatLogging();

	          		ScanMonsters = false;
	          	}

        	}

		public void combatLogging()
		{
			string titleContent = string.Empty;

			if (First)
			{
				First = false;
				if (Hud.Sound.LastSpeak.TimerTest(3000))
					Hud.Sound.Speak("Log started!");
				StartTick = BossATick = System.Environment.TickCount;
				IsGRift = (Hud.Game.SpecialArea == SpecialArea.GreaterRift) ? true : false;
				
				string decoline = "---------------------------------------------------------------------------------------------------------------------------";
				string Today = DateTime.Now.ToString("MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture);
				string titleText = "*" + Environment.NewLine + decoline + Environment.NewLine;
				titleContent = string.Empty;
				var sArea = string.Empty;
				if (Hud.Game.SpecialArea == SpecialArea.Rift)
					sArea = (culture == "ko") ? "일균" : "Rift";
				else if (Hud.Game.SpecialArea == SpecialArea.GreaterRift)
					sArea = (culture == "ko") ? "대균" : "GRift";
				try {
        				titleContent = Today + ", Season"+ Convert.ToString(Hud.Game.Me.Hero.Season) + ", " +
        			                   Hud.Game.Me.BattleTagAbovePortrait + " (" + Convert.ToString(Hud.Game.Me.Hero.Level) + "), " +
        			                   Hud.Game.Me.HeroClassDefinition.Name.ToString() + ", " +
        			                   sArea + " (" + Convert.ToString(Hud.Game.Me.InGreaterRiftRank) + "), " +
        			                   "Paragon Level (" + Convert.ToString(Hud.Game.Me.Hero.ParagonLevel) + "), " +
        			                   "Hero Solo Highest GR Level (" + Convert.ToString(Hud.Game.Me.HighestHeroSoloRiftLevel) + ")" + Environment.NewLine +
        			                   "Time, Progress, CPU Usage, Usable Ram, Cur/Ave Latency, TH FrameRate : SnoArea, Combat-contents" + Environment.NewLine + decoline;
        			}
        			catch {}		// just in case...Hero.Season may not work with non-season characters
        			Hud.TextLog.Log(LogFile, titleText + titleContent, aTimeStamp, true);
			}

			if (ScanMonsters)		// log every second : refer to CalculateFrameRate()
			{
				string area = string.Empty;
				var tmpA = string.Empty;
				if (culture == "ko")
					tmpA = Hud.Game.Me.SnoArea.NameEnglish;
				else
					tmpA = Hud.Game.Me.SnoArea.NameLocalized;
				Match match = Regex.Match(tmpA, @"\d{1,}");
				if (match.Success)
				{
					if (culture == "ko")
						area = "지하 " + match.Value + "층";
					else
						area = "Run Level " + match.Value;
				}
				string timeNow = DateTime.Now.ToString("HH:mm:ss");
				titleContent = timeNow + ",  " + Convert.ToString((int)Hud.Game.RiftPercentage) + " %, " + MonitoredLog + " : " +area;
				if (Hud.Game.IsEliteOnScreen)
				{
					eliteNames = Regex.Replace(eliteNames, @", $", string.Empty);
					titleContent += ", Elite (" + eliteNames + ")";
					savedSno = 0;
				}
				if (Hud.Game.Me.InCombat)
					titleContent += ", In Combat (mobs: " + Convert.ToString(mobNumbers) + ")";	// number of the mobs around X yards (default 80)
				if (Hud.Game.Me.IsDead)
					titleContent += ", MeDead";
				foreach (Buff buff in BuffsToWatch)
	            	{
	                	if (Hud.Game.Me.Powers.BuffIsActive(buff.SNO, buff.Icon))
						titleContent += ", " + buff.Name;
	            	}

				if (Hud.Render.GetUiElement(BossTextUiName).Visible)	// for GR
		          	{
		          		if (uiFirst)
		          		{
		          			BossATick = System.Environment.TickCount;
		          			uiFirst = false;
		          		}
			          	titleContent +=  ", * Boss is alive! *";
		          	}
		          	Hud.TextLog.Log(LogFile, titleContent, aTimeStamp, true);
			}
		}

		public void MonitoringResource(Object source, System.Timers.ElapsedEventArgs e)
        	{
			int CpuUse = (int)(Cpu.NextValue());
			int RamUse = (int)(Ram.NextValue());
			int aLatency = (int)Hud.Game.AverageLatency;
			int cLatency = (int)Hud.Game.CurrentLatency;
			if (cLatency > LatencyWarning || CpuUse > CpuWarning || RamUse < RamWarning || FrameRate < FPSWarning)
				displayTag = 1;	// warning
			else if (cLatency > LatencyBad || CpuUse > CpuBad || RamUse < RamBad || FrameRate < FPSBad)
			{
				Console.Beep(800, 200);	// Alarm if in bad state
				displayTag = 2;	// bad
			} else
				displayTag = 0;	// ok

			var CpuVal = Convert.ToString(CpuUse);
			var CpuText = "Cpu Usuage : " + CpuVal + " %";
			var RamVal = Convert.ToString(RamUse);
			var RamText = "Usable Ram : " + RamVal + " MB";
			var LatencyVal = Convert.ToString(cLatency) + "/" + Convert.ToString(aLatency);
			var LatencyText = "C/A Latency: " + LatencyVal + " ms";
			//var FPSText =      Convert.ToString(FrameRate) + "/" + Convert.ToString(FrameRate) + " FPS";
			var FPSVal = Convert.ToString(FrameRate);
			var FPSText =      "FrameRate  : " + FPSVal + " FPS";
			MonitoredResource = CpuText + Environment.NewLine + RamText + Environment.NewLine + LatencyText + Environment.NewLine + FPSText;
			MonitoredLog = CpuVal + " %, " + RamVal + " MB, " + LatencyVal + " ms, " + FPSVal + " FPS";
		}

		public static int CalculateFrameRate()
		{
			if (System.Environment.TickCount - lastTick >= RMInterval)		// count frames for a second
		     {
		     		lastFrameRate = frameRate;
		     		frameRate = 0;
		     		lastTick = System.Environment.TickCount;
		    	}
		     frameRate++;
		     return lastFrameRate;
		}

        	public void OnKeyEvent(IKeyEvent keyEvent)
        	{
            	if (Control.ModifierKeys == Keys.Control && Hud.Input.IsKeyDown(Keys.Subtract))	// ctrl+NumPad.Subtract"-")
            	{
            		Console.Beep(250, 150);
				monitoring = !monitoring;	// toggle display

				if (Logging) return;
				if (monitoring)
				{
					displayTag = 0;
					monitoringRS(true);
				} else
					monitoringRS(false);
            	}
           }
           
           public void monitoringRS(bool go)
           {
           	if (go)
           	{
				// Text decoration every interval (default 1 sec)
		          	MonitorTimer = new System.Timers.Timer();
				MonitorTimer.Interval = RMInterval;
				MonitorTimer.Elapsed += MonitoringResource;
				MonitorTimer.AutoReset = true;
				MonitorTimer.Enabled = true;
			} else
			{
				try {
					MonitorTimer.Enabled = false;
				}
				catch {}
			}        
		}   
	}
}