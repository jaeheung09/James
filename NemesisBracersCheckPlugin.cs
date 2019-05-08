//Check players with nemesis bracers before entering a new game and warning notice if necessary
using System;
using Turbo.Plugins.Default;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace Turbo.Plugins.James
{
	public class NemesisBracersCheckPlugin : BasePlugin, IInGameWorldPainter
    	{
		private static System.Timers.Timer CheckPixelTimer;
        	private WorldDecoratorCollection WarningMessageDecorator { get; set; }
        	private WorldDecoratorCollection NoticeMessageDecorator { get; set; }
        	private TopLabelWithTitleDecorator NemesisPlayerDecorator { get; set; }
        	private WorldDecoratorCollection keysDecoratorBad { get; set; }
        	private WorldDecoratorCollection keysDecoratorWarning { get; set; }
        	private WorldDecoratorCollection keysDecoratorOk { get; set; }
        	private const int keysOK = 30;
        	private const int keysBad = 10;
        	private int w, h, x, y;
        	private bool IsGRiftDialog;
        	private bool ScanPlayer;
        	private string WarningMsg;
		private string NemStr;
		private int NemesisCount;
		private int interval;
		private string obeliskText;
		private int keys;
		private string culture;


        	public NemesisBracersCheckPlugin()
        	{
        		Enabled = true;
		}

        	public override void Load(IController hud)
        	{
			base.Load(hud);

			culture = System.Globalization.CultureInfo.CurrentCulture.ToString().Substring(0, 2);
			IsGRiftDialog = false;
			ScanPlayer = true;
			keys = 0;

			WarningMessageDecorator = new WorldDecoratorCollection(
			new GroundLabelDecorator(Hud)
			{
				BackgroundBrush = Hud.Render.CreateBrush(0, 0, 0, 0, 0),
				TextFont = Hud.Render.CreateFont("tahoma", 20, 255, 255, 0, 0, true, true, true),
			});

			NoticeMessageDecorator = new WorldDecoratorCollection(
			new GroundLabelDecorator(Hud)
			{
				BackgroundBrush = Hud.Render.CreateBrush(0, 0, 0, 0, 0),
				TextFont = Hud.Render.CreateFont("tahoma", 20, 255, 0, 255, 0, true, true, true),
			});

			NemesisPlayerDecorator = new TopLabelWithTitleDecorator(hud)
			{
				BorderBrush = hud.Render.CreateBrush(255, 180, 147, 109, -1),
				BackgroundBrush = hud.Render.CreateBrush(100, 0, 0, 0, 0),	//200
				TextFont = hud.Render.CreateFont("tahoma", 8, 255, 255, 255, 255, true, false, false),
				TitleFont = hud.Render.CreateFont("tahoma", 6, 255, 180, 147, 109, true, false, false),
			};

            	keysDecoratorOk = new WorldDecoratorCollection(
                	new GroundLabelDecorator(Hud)
	                {
	                    BackgroundBrush = Hud.Render.CreateBrush(190, 0, 122, 26, 0),
	                    BorderBrush = Hud.Render.CreateBrush(255, 255, 255, 255, 1),
	                    TextFont = Hud.Render.CreateFont("tahoma", 7f, 255, 255, 255, 255, false, false, false),
	                }
           	);

            	keysDecoratorBad = new WorldDecoratorCollection(
	                new GroundLabelDecorator(Hud)
	                {
	                    BackgroundBrush = Hud.Render.CreateBrush(190, 122, 0, 0, 0),
	                    BorderBrush = Hud.Render.CreateBrush(255, 255, 255, 255, 1),
	                    TextFont = Hud.Render.CreateFont("tahoma", 7f, 255, 255, 255, 255, true, false, false),
	                }
            	);

            	keysDecoratorWarning = new WorldDecoratorCollection(
	                new GroundLabelDecorator(Hud)
	                {
	                    BackgroundBrush = Hud.Render.CreateBrush(190, 178, 110, 0, 0),
	                    BorderBrush = Hud.Render.CreateBrush(255, 255, 255, 255, 1),
	                    TextFont = Hud.Render.CreateFont("tahoma", 7f, 255, 255, 255, 255, false, false, false),
	                }
            	);

		     CheckPixelTimer = new System.Timers.Timer();
			CheckPixelTimer.Interval = 500;		// edit line filtering interval
			CheckPixelTimer.Elapsed += CheckScreenPixel;
			CheckPixelTimer.AutoReset = true;
			CheckPixelTimer.Enabled = true;
		}

	   	public void CheckScreenPixel(Object source, System.Timers.ElapsedEventArgs e)
        	{
        		if (!Hud.Game.IsInTown) return;

			ScanPlayer = true;
			
		  	keys = (int)Hud.Game.Me.Materials.GreaterRiftKeystone;
		  	if (Hud.Render.GetUiElement("Root.NormalLayer.rift_dialog_mainPage.LayoutRoot.RiftTierLevelCombo").Visible)
		  	{
		  		if (Hud.Sound.LastSpeak.TimerTest(interval))
		  		{
		  			string tmp;
		  			if (culture == "ko")
		  				tmp = "현재 균열석 " + keys + "개 보유중 입니다.";
		  			else
		  				tmp = "You have " + keys + " rift keystone.";
		  			Hud.Sound.Speak(tmp);
		  			interval = 15000;
		  		}
		  	} else
		  		interval = 1000;
		  		
		  	IsGRiftDialog = false;
			if (Hud.Window.Size.Width == 1920 && Hud.Window.Size.Height == 1080)
			{
	        		// Check Grift dialog popup windwow invoked by another player
	            	Color pixelColor = GetScreenPixel(1006, 766);		// Check Green
				if (pixelColor.R.Equals(0x00) && pixelColor.G.Equals(0xDE) && pixelColor.B.Equals(0x00))		// 0, 222, 0
					IsGRiftDialog = true;
			}
        	}

		public void PaintWorld(WorldLayer layer)
		{
			if (!Hud.Game.IsInTown) return;

			var me = Hud.Game.Me;

			if (ScanPlayer)
			{
				ScanPlayer = false;

				string NemPlayer = string.Empty;

				NemesisCount = 0;
				foreach (var player in Hud.Game.Players.OrderBy(p => p.PortraitIndex))
				{
					if (player == null) continue;

					var Nemesis = player.Powers.GetBuff(318820);		// Nemesis Bracers

					if (Nemesis != null && Nemesis.Active)
					{
						NemesisCount++;
						if (NemesisCount > 1)		// more than one player
						NemPlayer += ", ";
						NemPlayer += player.BattleTagAbovePortrait;
					}
				}

				NemStr = NemesisCount.ToString();
				if (NemesisCount > 0)
					NemStr += " -> " + NemPlayer;

				w = NemStr.Length * 12; // 12 pixel per letter (FYI, I'm using Hangul, which is Korean language)
				if (w < 150) w = 150;
				h = 55;
				x = Hud.Window.Size.Width / 2 - 480;
				y = 20;
			}

			var tmp = (culture == "ko") ? "천벌 손목" : "Nemesis Bracers";
			NemesisPlayerDecorator.Paint(x, y , w, h, NemStr, tmp);
			if (Hud.Render.GetUiElement("Root.NormalLayer.rift_dialog_mainPage").Visible || IsGRiftDialog)
			{
				if (NemesisCount == 0)
				{
					WarningMsg = (culture == "ko") ? "천벌 착용자 없음!" : "No one with Nemesis Bracers!";
					WarningMessageDecorator.Paint(layer, null, me.FloorCoordinate.Offset(0, 0, 15), WarningMsg);
					if(Hud.Sound.LastSpeak.TimerTest(5000))
					{
						Hud.Sound.Speak(WarningMsg);
						Console.Beep(900, 200);
					}
				} else if (NemesisCount > 1)
				{
					WarningMsg = (culture == "ko") ? "천벌 착용자 2명 이상 -> " : "More than one with Nemesis -> ";
					WarningMsg += NemesisCount.ToString("0");
					NoticeMessageDecorator.Paint(layer, null, me.FloorCoordinate.Offset(0, 0, 15), WarningMsg);
				}
			}
			
			var obeliskPos = Hud.Game.Actors.Where(x => x.SnoActor.Sno == ActorSnoEnum._x1_openworld_lootrunobelisk_b && x.IsOnScreen);
			obeliskText = "" + keys;
			foreach (var actor in obeliskPos)
			{
				if (keys <= keysBad)
					keysDecoratorBad.Paint(layer, actor, actor.FloorCoordinate, obeliskText);
				else if (keys > keysBad && keys < keysOK)
					keysDecoratorWarning.Paint(layer, actor, actor.FloorCoordinate, obeliskText);
				else 	//if (keys >= keysOK)
					keysDecoratorOk.Paint(layer, actor, actor.FloorCoordinate, obeliskText);
			}			
		}

        	public Color GetScreenPixel(int x, int y)
        	{
			Bitmap screenPixel = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
			Graphics screenGraph = Graphics.FromImage(screenPixel);
			screenGraph.CopyFromScreen(x, y, 0, 0, SystemInformation.VirtualScreen.Size, CopyPixelOperation.SourceCopy);
			screenGraph.Dispose();
			return screenPixel.GetPixel(0, 0);
		}
    	}
}
