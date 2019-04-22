// This plugin show your party members' GR 4player-related information and the current class's highest solo gr level
// ctrl+Numpad"/" : to build the ranking database <-- need to be done only once after you login D3 app
// Placing your mouse pointer in health-glove box : show various GR info from the other party members
using System;
using System.Linq;
using Turbo.Plugins.Default;
using System.Windows.Forms;
using SharpDX.DirectInput;
using System.Text.RegularExpressions;
using System.Net;
using System.Collections.Generic;
using System.Threading;

namespace Turbo.Plugins.James
{
    public class PartyMembersGR4PlayerInfoPlugin : BasePlugin, IKeyEventHandler, IInGameTopPainter
    {
        private string WebsiteUrl;
        private string koUrl = "https://kr.diablo3.com/ko/rankings/season/16/rift-team-4";		// 한국 시즌16 4인 대균 순위
        private string enUrl = "https://us.diablo3.com/en/rankings/season/16/rift-team-4";	// US Season16 4 PLAYER GR Ranking
        private string[,] GRiftRanking = new string[1000, 5];		// 1~1000 GR 4player BaTag1_1, Batag1_2, Ranking, Highest GRlevel, Class
        private string[,] Players = new string[4, 7];				// Players' BaTag1_1, Batag1_2,, Ranking, Highest GRlevel, Class, Ztag, soloHLevel
        private string [] pPlayers = new string [4];				// for checking the change of the party members
	   private WebClient webClient = new WebClient();
	   private static System.Timers.Timer AbortTimer;
	   private bool IsDownloaded;
	   private bool BeingDownloaded;
	   System.Threading.Thread t1;
	   CancellationTokenSource cts;
	   private TopLabelDecorator TitleDecorator { get; set; }
	   private TopLabelDecorator GRLevelDecorator { get; set; }
	   private TopLabelDecorator ContentDecorator { get; set; }
        private string TitleStr;
        private string GRLevelSpeedHybrid;
        private string GRLevelSpeedStandard;
        private string Battletags;
        private string Paragons;
        private string ZClasses;
        private string DPS;
        private string HighestSolos;
        private string GR4PRanking;
        private string GR4PLevel;
        private string GR4PClass;
        private string culture;
	   private int BaseX = 250;
	   private int BaseY = 100;
	   private int NextSpeak = 1000;
	   private bool IsPlayerChanged;

        public PartyMembersGR4PlayerInfoPlugin()
        {
            Enabled = true;
        }

        public override void Load(IController hud)
        {
            base.Load(hud);
            IsDownloaded = false;
            BeingDownloaded = false;
            PlayersArrayInitialization();

		// automatic url selection according language culture --- you don't have to change it
          // if your ranking website is different from ko/KR or en/US, you need to replace the webpate url in this source code with yours for yourself
            culture = System.Globalization.CultureInfo.CurrentCulture.ToString().Substring(0, 2);
            if (culture == "ko")
            	WebsiteUrl = koUrl;
            else
            	WebsiteUrl = enUrl;

            webClient.Encoding = System.Text.Encoding.UTF8;

            for (int i = 0; i < GRiftRanking.GetLength(0); i++)
		  {
		  	for (int j = 0; j < GRiftRanking.GetLength(1); j++)
			{
				GRiftRanking[i, j] = string.Empty;
			}
		  }

            for (int i = 0; i < pPlayers.GetLength(0); i++)
		  {
			pPlayers[i] = string.Empty;
		  }

		 TitleDecorator = new TopLabelDecorator(Hud)
	      {
	           TextFont = Hud.Render.CreateFont("consolas", 8, 220, 120, 255, 120, true, false, 255, 0, 0, 0, true),
	           TextFunc = () => TitleStr,
	      };

	      ContentDecorator = new TopLabelDecorator(Hud)
	      {
	           TextFont = Hud.Render.CreateFont("consolas", 8, 220, 255, 255, 255, true, false, 255, 0, 0, 0, true),
	      };

	      GRLevelSpeedHybrid = string.Empty;
	      GRLevelSpeedStandard = string.Empty;
	      GRLevelDecorator = new TopLabelDecorator(Hud)
	      {
	           BackgroundBrush = Hud.Render.CreateBrush(150, 0, 0, 0, 0),
	           BorderBrush = Hud.Render.CreateBrush(250, 0, 0, 0, 2),
	           TextFont = Hud.Render.CreateFont("consolas", 8, 220, 255, 255, 255, true, false, 255, 0, 0, 0, true),
	           TextFunc = () => ">> R Speedy GR Level : for Hybrid (" + GRLevelSpeedHybrid + "), for 4P Standard ("+GRLevelSpeedStandard+") <<",
	      };
        }

	   public void PlayersArrayInitialization()
	   {
            for (int i = 0; i < Players.GetLength(0); i++)
		  {
		  	for (int j = 0; j < Players.GetLength(1); j++)
			{
				Players[i, j] = string.Empty;
			}
		  }
	   }

        public void PaintTopInGame(ClipState clipState)
        {
        		if (clipState != ClipState.BeforeClip) return;
			if (!Hud.Game.Me.IsInTown) return;

			// Show the infomation only when the mouse cursor is within the healthBall area
			var uiRect = Hud.Render.GetUiElement("Root.NormalLayer.game_dialog_backgroundScreenPC.game_progressBar_healthBall").Rectangle;

			if (Hud.Window.CursorY < uiRect.Top || Hud.Window.CursorY > (uiRect.Top + uiRect.Height) || Hud.Window.CursorX < uiRect.Left || Hud.Window.CursorX > (uiRect.Left + uiRect.Width))
				return;

			PlayersArrayInitialization();

			var cnt = 0;
			foreach (var player in Hud.Game.Players.OrderBy(p => p.PortraitIndex))
			{
				Players[cnt, 0] = player.BattleTagAbovePortrait.ToString().Trim();
				cnt++;
			}
			// Check whether the members of the current party are changed
			for (int i = 0; i < Players.GetLength(0); i++)
			{
				if (Players[i, 0] !=pPlayers[i])
				{
					IsPlayerChanged = true;
					break;
				}
			}

			if (IsPlayerChanged)		// Do it only when the party members are changed or just bypass it and show the existing data
			{
				Battletags = string.Empty;
				Paragons = string.Empty;
				ZClasses = string.Empty;
				DPS = string.Empty;
				HighestSolos = string.Empty;
				GR4PRanking = string.Empty;
				GR4PLevel = string.Empty;
				GR4PClass = string.Empty;

				for (int i = 0; i < Players.GetLength(0); i++)
				{
					pPlayers[i] = Players[i, 0];
				}

				SearchGRLevel();		// put the party members' 4p data into array

				var pCnt = 0;
				foreach (var player in Hud.Game.Players.OrderBy(p => p.PortraitIndex))
				{
					string Battletag = Players[pCnt, 0];
					if (Players[pCnt, 1] != string.Empty)
						Battletag += "#"+Players[pCnt, 1];
					string Paragon = checked((int)player.CurrentLevelParagon).ToString();
					string Dps = string.Empty;
					if (player.Offense.SheetDps > 0f)
						Dps = ValueToString((long)player.Offense.SheetDps, ValueFormat.LongNumber).Trim();
					string ZClass = string.Empty;
					if (IsZDPS(player))
					{
					   	ZClass = "Z";
					   	Players[pCnt, 5] = "Z";
					}
					ZClass += player.HeroClassDefinition.Name;
					Players[pCnt, 6] = player.HighestHeroSoloRiftLevel.ToString();
					string HighestSolo = player.HighestHeroSoloRiftLevel.ToString().PadLeft(3);
					string GR4Pranking = Players[pCnt, 2];
					Match match = Regex.Match(Players[pCnt, 3], @"\d{1,}");	// extract numbers only
					string GR4Plevel = match.Value;
					string GR4Pclass = Players[pCnt, 4];

					pCnt++;

					if (culture != "ko")
						TitleStr = "이름               파라곤    직업        DPS    솔플   4P순위  4P레벨  4P직업";
					else
						TitleStr = "BattleTag          Paragon   Class      DPS    Solo   4PRank  4PLevel 4PClass";
					Battletags = (Battletags.Length == 0) ? Battletag : Battletags + Environment.NewLine + Battletag;
					Paragons = (Paragons.Length == 0) ? Paragon : Paragons + Environment.NewLine + Paragon;
					ZClasses = (ZClasses.Length == 0) ? ZClass : ZClasses + Environment.NewLine + ZClass;
					DPS = (DPS.Length == 0) ? Dps : DPS + Environment.NewLine + Dps;
					if (player.HighestHeroSoloRiftLevel == 0)
					{
						IsPlayerChanged = true;
						HighestSolo = "???".PadLeft(3);
					}
					HighestSolos = (HighestSolos.Length == 0) ? HighestSolo : HighestSolos + Environment.NewLine + HighestSolo;
					GR4PRanking = (GR4PRanking.Length == 0) ? GR4Pranking : GR4PRanking + Environment.NewLine + GR4Pranking;
					GR4PLevel = (GR4PLevel.Length == 0) ? GR4Plevel : GR4PLevel + Environment.NewLine + GR4Plevel;
					GR4PClass = (GR4PClass.Length == 0) ? GR4Pclass : GR4PClass + Environment.NewLine + GR4Pclass;

					// to recommend speedy hybrid GR level
					bool Success;
			    		int number;
			    		var tmpLevel = 0;
			    		cnt = 0;
			    		var zflag = false;
					for (int i = 0; i < Players.GetLength(0); i++)
					{
						if (Players[i, 5] != "Z")	//if not Z class
						{
							Success = Int32.TryParse(Players[i, 6], out number);		// Solo GR Highest level
							if (Success && number > 0 && number < 200)			// solo hightest GR level : 1~ 199
							{
								tmpLevel += number;
								cnt++;
							}
						} else
							zflag = true;
					}
					int tmpNo = 0;
					if (cnt == 0) cnt = 1;
					tmpNo = (int)(tmpLevel/cnt);
					int pCount = Hud.Game.Players.Count();
					// R level will depend on the number of the party members and the presence of a zclass in the party
					if (!zflag)	// if no zclass
						tmpNo -= (5 + (4 - pCount));
					if (tmpNo < 0) tmpNo = 0;
					GRLevelSpeedHybrid = UnitDigitRound(tmpNo).ToString();

					// to recommend speedy standard GR level
			    		tmpLevel = 0;
			    		cnt = 0;
					for (int i = 0; i < Players.GetLength(0); i++)
					{
						Success = Int32.TryParse(Players[i, 3], out number);		// 4P GR Highest level
						if (Success && number > 0 && number < 200)			// solo hightest GR level : 1~ 199
						{
							tmpLevel += number;
							cnt++;
						}
					}
					if (cnt == 0) cnt = 1;
					tmpNo = (int)(tmpLevel/cnt) - 10;
					if (tmpNo < 0) tmpNo = 0;
					GRLevelSpeedStandard = UnitDigitRound(tmpNo).ToString();
			    }
		     }

	          TitleDecorator.Paint(BaseX, BaseY-15, 350, 15, HorizontalAlign.Left);			// Title

	          	ContentDecorator.TextFunc = () => Battletags;
	          ContentDecorator.Paint(BaseX, BaseY, 50, 80, HorizontalAlign.Left);
	          ContentDecorator.TextFunc = () => Paragons;
	          ContentDecorator.Paint(BaseX+150, BaseY, 50, 80, HorizontalAlign.Left);
	          ContentDecorator.TextFunc = () => ZClasses;
	          ContentDecorator.Paint(BaseX+220, BaseY, 50, 80, HorizontalAlign.Left);
	          ContentDecorator.TextFunc = () => DPS;
	          ContentDecorator.Paint(BaseX+310, BaseY-5, 50, 90, HorizontalAlign.Left);
	          ContentDecorator.TextFunc = () => HighestSolos;
	          ContentDecorator.Paint(BaseX+370, BaseY, 30, 80, HorizontalAlign.Left);
	          ContentDecorator.TextFunc = () => GR4PRanking;
	          ContentDecorator.Paint(BaseX+430, BaseY, 30, 80, HorizontalAlign.Left);
	          ContentDecorator.TextFunc = () => GR4PLevel;
	          ContentDecorator.Paint(BaseX+490, BaseY, 30, 80, HorizontalAlign.Left);
	          ContentDecorator.TextFunc = () => GR4PClass;
	          ContentDecorator.Paint(BaseX+550, BaseY, 30, 80, HorizontalAlign.Left);

	          if (Hud.Game.NumberOfPlayersInGame > 0) GRLevelDecorator.Paint(BaseX, BaseY+100, 530, 35, HorizontalAlign.Center);	// Recommended GR Level for speedy game
        }

	   // Determining whether the player is a zclass
        private bool IsZDPS(IPlayer player)
        {
	         int Points = 0;

	         var IllusoryBoots = player.Powers.GetBuff(318761);
	         if (IllusoryBoots == null || !IllusoryBoots.Active) {} else {Points++;}

	         var LeoricsCrown = player.Powers.GetBuff(442353);
	         if (LeoricsCrown == null || !LeoricsCrown.Active) {} else {Points++;}

	         var EfficaciousToxin = player.Powers.GetBuff(403461);
	         if (EfficaciousToxin == null || !EfficaciousToxin.Active) {} else {Points++;}

	         var OculusRing = player.Powers.GetBuff(402461);
	         if (OculusRing == null || !OculusRing.Active) {} else {Points++;}

	         var ZodiacRing = player.Powers.GetBuff(402459);
	         if (ZodiacRing == null || !ZodiacRing.Active) {} else {Points++;}

	         if (player.Offense.SheetDps < 500000f) Points++;
	         if (player.Offense.SheetDps > 1500000f) Points--;

	         if (player.Defense.EhpMax > 80000000f) Points++;

	         var ConventionRing = player.Powers.GetBuff(430674);
	         if (ConventionRing == null || !ConventionRing.Active) {} else {Points--;}

	         var Stricken = player.Powers.GetBuff(428348);
	         if (Stricken == null || !Stricken.Active) {} else {Points--;}

	         if (Points >= 4)
	         		return true;
	         else
	         		return false;

        }

	   // 1의 자리에서 반올림
	   public int UnitDigitRound(int number)
	   {
	   		int tmp = (number + 5) / 10 * 10;
	   		return tmp;
	   }

	   // Built the GR ranking database : BattleTag, Ranking, Highest GRlevel, Class from Diablo 3 public website
	   public void BuildGRiftRanking()
	   {
	   		string tmpStr= string.Empty;
	   		if (BeingDownloaded)
	   		{
	   			if (culture == "ko")
	   				Hud.Sound.Speak("현재 다운받고 있습니다. 기다려 주세요.!");		// "Being downloaded. Please wait!"
	   			else
	   				Hud.Sound.Speak("Being downloaded. Please wait!");
	   			return;
	   		}
	   		try {
	   			BeingDownloaded = true;
	   			tmpStr = webClient.DownloadString(WebsiteUrl);		// download the D3 4player ranking page
	   		}
	   		catch { return; }

	   		BeingDownloaded = false;
	   		IsDownloaded = true;
			Console.Beep(300, 200);			// Alarm when finished downloading to check how long it takes

			Match match = Regex.Match(tmpStr, @"(?s)(?<=<tbody>).+(?=</tbody>)");	// extract the ranking-related info only
			if (match.Success)
			{
				BuildRankingDatabase(match.Value); 		// BattleTag, Ranking, Highest GRlevel, Class
				if (culture == "ko")
					Hud.Sound.Speak("자료 준비 완료!");		// "Data is ready!"
				else
					Hud.Sound.Speak("Data is ready!");
			} else
			{
				Console.Beep(500, 250);
				Hud.Sound.Speak("Extracting failure!");
			}
	   }

	   // put 1~1000 rankers' BattleTag, Ranking, GR Level, Class in the array
	   public void BuildRankingDatabase(string rankingStr)
	   {
	   		Match match = Regex.Match(rankingStr, @"(?<=profile/)(.+?)(?=/hero)");		// BattleTag
	   		for (int i = 0; i < GRiftRanking.GetLength(0); i++)
	   		{
	   			if (i >  0)
					match = match.NextMatch();

				if (match.Success)
				{
					var tmpVal = match.Value;
					Match match2 = Regex.Match(tmpVal, @".+(?=-.+)");		// left part of a BattleTag(letter)
					if (match2.Success)
					{
						GRiftRanking[i, 0] = match2.Value;
						match2 = Regex.Match(tmpVal, @"(?<=.+-).+");		// right part of a BattleTag (number)
						if (match2.Success)
							GRiftRanking[i, 1] = match2.Value;
					}
				} else
					break;
	   		}

	   		for (int i = 0; i < GRiftRanking.GetLength(0); i++)	// GR ranking 1~1000
	   		{
	   			if (i == 0)
					match = Regex.Match(rankingStr, @"(?<=data-raw=')(.+?)(?='>)");		// Ranking
				else
					match = match.NextMatch();

				if (match.Success)
				{
					GRiftRanking[i, 2] = match.Value;
				} else
					break;
	   		}

	   		for (int i = 0; i < GRiftRanking.GetLength(0); i++)	// GR ranking 1~1000
	   		{
	   			if (i == 0)
					match = Regex.Match(rankingStr, @"(?s)(?<=RiftLevel"" >)(.+?)(?=</td)");	// GR level
				else
					match = match.NextMatch();

				if (match.Success)
				{
					GRiftRanking[i, 3] = match.Value.Replace(Environment.NewLine, "");
				} else
					break;
	   		}

	   		for (int i = 0; i < GRiftRanking.GetLength(0); i++)	// GR ranking 1~1000
	   		{
	   			if (i == 0)
					match = Regex.Match(rankingStr, @"(?s)(?<=portraits/21/)(.+?)(?=.png)");		// class
				else
					match = match.NextMatch();

				if (match.Success)
				{
					var tmpP6 = match.Value.Replace("p6_", string.Empty);
					tmpP6 = Regex.Replace(tmpP6, @"_.+", string.Empty);
					GRiftRanking[i, 4] = tmpP6;
				} else
					break;
	   		}
	   }

	   // Search party members' GR 4player info in the ready-made array
        public void SearchGRLevel()
        {
            if (GRiftRanking[0, 0] == string.Empty)
            {
            	if (Hud.Sound.LastSpeak.TimerTest(NextSpeak))
            	{
            		NextSpeak = 10000;
            		if (culture == "ko")
            			Hud.Sound.Speak("데이터베이스가 구축되지 않았습니다!");
            		else
            			Hud.Sound.Speak("Databasae is not built yet!");
            	}
            	return;
            }

		  for (int i = 0; i < Players.GetLength(0); i++)
            {
            	if (Players[i, 0].Equals(string.Empty))
            		continue;
			for (int j = 0; j < GRiftRanking.GetLength(0); j++)
			{
				if (GRiftRanking[j, 0].Equals(Players[i, 0]))
				{
					Players[i, 1] = GRiftRanking[j, 1];		// BattleTag 1-2
					Players[i, 2] = GRiftRanking[j, 2];		// Ranking
	            		Players[i, 3] = GRiftRanking[j, 3];		// GR Level
	            		Players[i, 4] = GRiftRanking[j, 4];		// Class
	            		break;
	            	}
			}
		  }
	   }

        public void OnKeyEvent(IKeyEvent keyEvent)
        {
        	  // Builing the database needs to be done only once in game because the ranking does not change very often.
        	  // It may take time to download the ranking webpage. It depends on your computing/network environment though.
            if (Control.ModifierKeys == Keys.Control && Hud.Input.IsKeyDown(Keys.Divide))	// ctrl+Numpad("/")
            {
            	Console.Beep(250, 120);
            	t1 = new Thread(new ThreadStart(BuildGRiftRanking)); // just in case that it often takes long time to download the webpage
            	t1.Start();											  // start multi-threading
            	if (culture == "ko")
            		Hud.Sound.Speak("잠시 기다려 주세요!");
            	else
            		Hud.Sound.Speak("Please wait a moment!");

            	AbortTimer = new System.Timers.Timer();
			AbortTimer.Interval = 10000;		// aboart the thread after Interval
			AbortTimer.Elapsed += AbortThread;
			AbortTimer.AutoReset = false;
			AbortTimer.Enabled = true;
            }
        }

	   public void AbortThread(Object source, System.Timers.ElapsedEventArgs e)
	   {
	   		if (!IsDownloaded)
	   		{
	   			BeingDownloaded = false;
	   			IsDownloaded = false;
	   			if (culture == "ko")
	   				Hud.Sound.Speak("다운로드 에러. 잠시 후 다시 시도하세요!");
	   			else
	   				Hud.Sound.Speak("Download error. Please try again in a little while.");
	   			try {
	   			      if (cts != null)
					 {
					 	cts.Cancel();
		   				t1.Abort();	// aboart the thread (get the webpage and build the 4player GR ranking(1~1000) database
		   			}
		   		}
		   		catch {}
		   	}
		   	IsDownloaded = false;
	   }
   }
}