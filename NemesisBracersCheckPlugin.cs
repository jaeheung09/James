//Check players with nemesis bracers before entering a new game and warning notice if necessary
using Turbo.Plugins.Default;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Turbo.Plugins.James
{
    public class NemesisBracersCheckPlugin : BasePlugin, IInGameWorldPainter
    {
        public WorldDecoratorCollection WarningMessageDecorator { get; set; }
        public WorldDecoratorCollection NoticeMessageDecorator { get; set; }
        public TopLabelWithTitleDecorator NemesisPlayerDecorator { get; set; }
        public int w, h, x, y;
        
        public NemesisBracersCheckPlugin()
        {
            Enabled = true;
        }
    
        public override void Load(IController hud)
        {
            base.Load(hud);

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
                BackgroundBrush = hud.Render.CreateBrush(200, 0, 0, 0, 0),
                TextFont = hud.Render.CreateFont("tahoma", 8, 255, 255, 255, 255, true, false, false),
                TitleFont = hud.Render.CreateFont("tahoma", 6, 255, 180, 147, 109, true, false, false),
            };            
        }

        public void PaintWorld(WorldLayer layer)
        {
        	  if (!Hud.Game.IsInTown) return;
        	  
            short NemesisCount = 0;
            var NemPlayer = "";
            
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
            
            var NemStr = NemesisCount.ToString("0");
            if (NemesisCount > 0) 
            	NemStr += " -> " + NemPlayer;
            	
		  w = NemStr.Length * 12; // 12 pixel per letter (FYI, I'm using Hangul, which is Korean language)
		  if (w < 150) w = 150;
		  h = 55;
		  x = Hud.Window.Size.Width / 2 - 480;
		  y = 20;
		  NemesisPlayerDecorator.Paint(x, y , w, h, NemStr, "Nemesis bracers");
		  
		  if (Hud.Render.GetUiElement("Root.NormalLayer.rift_dialog_mainPage").Visible || Hud.Render.GetUiElement("Root.NormalLayer.rift_dialog_mainPage.LayoutRoot.RiftTierLevelCombo").Visible)
		  {
		      var me = Hud.Game.Me;
		      if (NemesisCount == 0)
		      {
		  		var WarningMsg = "No Nemesis bracers!";
		  		WarningMessageDecorator.Paint(layer, null, me.FloorCoordinate.Offset(0, 0, 15), WarningMsg);
		  		
		  		if(Hud.Sound.LastSpeak.TimerTest(4000))
		  		{
					Hud.Sound.Speak(WarningMsg);
					Console.Beep(900, 200);
				}
			} else if (NemesisCount > 1)
			{
				var WarningMsg = "More than 1 Nemesis bracers -> " + NemesisCount.ToString("0");
		  		NoticeMessageDecorator.Paint(layer, null, me.FloorCoordinate.Offset(0, 0, 15), WarningMsg);
			}
		  }
        }
    }
}
