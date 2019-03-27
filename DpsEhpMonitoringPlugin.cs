//  dps, ehp real time Monitoring plugin by James
// F12: toggle Monitoring
using Turbo.Plugins.Default;
using System.Linq;
using SharpDX.DirectInput;

namespace Turbo.Plugins.James
{
    public class DpsEhpMonitoringPlugin : BasePlugin, IInGameTopPainter, IKeyEventHandler, INewAreaHandler
    {
        public TopLabelDecorator DpsLabelDecorator { get; set; }
	   public IKeyEvent PressKeyEvent { get; set; }		// Show or not F12
        private float OriginalSheetDPS;
        private float OriginalSheetEHP;
        private float DPSGap;
        private float EHPGap;
        private bool First;
        private bool Show;
        private string ShowCont;
        
        public DpsEhpMonitoringPlugin()
        {
            Enabled = true;
        }
        
        public override void Load(IController hud)
        {
            base.Load(hud);

		  OriginalSheetDPS = 0f;
		  OriginalSheetEHP = 0f;
            DPSGap = 0f;
            EHPGap = 0f;
            First = true;
            Show = true;
            ShowCont = "";
            PressKeyEvent = Hud.Input.CreateKeyEvent(true, Key.F12, false, false, false);

            DpsLabelDecorator = new TopLabelDecorator(Hud)
            {
                TextFont = Hud.Render.CreateFont("tahoma", 9, 255, 255, 255, 255, true, false, false),
                BackgroundTexture1 = hud.Texture.ButtonTextureBlue,
                BackgroundTexture2 = hud.Texture.BackgroundTextureBlue,
                BackgroundTextureOpacity2 = 0.5f,
                TextFunc = () => ShowCont,
                HintFunc = () => "Dps, Ehp Real time Monitoring"
            };
        }
        public void PaintTopInGame(ClipState clipState)
        {
        	  if (!Show || Hud.Game.IsInTown) return;
        	  
            if (Hud.Game.Me.Defense.EhpCur != OriginalSheetEHP || Hud.Game.Me.Damage.CurrentDps > 0d)
        	  {
        	  		if (First)
        	  		{
        	  			OriginalSheetDPS = Hud.Game.Me.Offense.SheetDps;
        	  			OriginalSheetEHP = Hud.Game.Me.Defense.EhpCur;
        	  			First = false;
        	  		}
        	  		var diff = Hud.Game.Me.Damage.CurrentDps - OriginalSheetDPS;
        	  		if (diff > 0f) 
        	  			DPSGap = (diff / OriginalSheetDPS) * 100f;
        	  		else
        	  			DPSGap = 0f;
        	  			
        	  		diff = Hud.Game.Me.Defense.EhpCur - OriginalSheetEHP;
        	  		if (diff > 0f) 
        	  			EHPGap = (diff / OriginalSheetEHP) * 100f;
        	  		else
        	  			EHPGap = 0f;        	  			
        	  } else
		         	return;

		  ShowCont = ValueToString(DPSGap, ValueFormat.LongNumber) + "%, D: " + ValueToString(Hud.Game.Me.Damage.CurrentDps, ValueFormat.LongNumber) + 
		              " | " + EHPGap.ToString("0.0") + "%, E: " + ValueToString(Hud.Game.Me.Defense.EhpCur, ValueFormat.ShortNumber);
		              
            var xPos = Hud.Window.Size.Width / 2 + 550;
            var yPos = 3;
            var bgWidth = ShowCont.Length * 12;
            //var bgWidth = Hud.Window.Size.Width * 0.20f;
            var bgHeight = Hud.Window.Size.Height * 0.03f;

            DpsLabelDecorator.Paint(xPos - (bgWidth / 2), yPos, bgWidth, bgHeight, HorizontalAlign.Center);
        }
        
        // if New Game the initialization
        public void OnNewArea(bool newGame, ISnoArea area)
        {
            if (newGame)
            {
            		Hud.Sound.Speak("New Game");
            		OriginalSheetDPS = 0;
			  	OriginalSheetEHP = 0;
	            	First = true;
            } 
        }
            
        // dps/ehp window show or not
        public void OnKeyEvent(IKeyEvent keyEvent)
        {
            if (keyEvent.IsPressed && PressKeyEvent.Matches(keyEvent))
            {
	    	    Show = !Show;
		    /*
	            if (Show)
	            	Show = false;
	            else
	            	Show = true;
		    */
            }
        }
    }
}
