//Throwing out rare items out of your inventory out of town
using System.Linq;
using System;
using Turbo.Plugins.Default;
using System.Windows.Forms;
using SharpDX.DirectInput;
using System.Threading;
using System.Drawing;
using System.Diagnostics;

namespace Turbo.Plugins.James
{
    public class ThrowingOutRareItemsPlugin: BasePlugin, IKeyEventHandler, IInGameWorldPainter
    {
    	   public int itemX, itemY;
    	   public int BaseX, BaseY;
    	   public IKeyEvent PressKeyEvent { get; set; }

        public ThrowingOutRareItemsPlugin()
        {
            	Enabled = true;
            	BaseX = 1425;	// the positon of first item box in inventory window (1920 X 1080)
        		BaseY = 580;     // if you're not using 1920 x 1080, you should get the x, y positon of  the center of the first item box of your inventory window
        }

        public override void Load(IController hud)
        {
            	base.Load(hud);
            	PressKeyEvent = Hud.Input.CreateKeyEvent(true, Key.F1, false, false, false);
            	Process.Start("D:\\Game\\clickDrag.exe");		// Preload for quick processing later
	   }

        public void PaintWorld(WorldLayer layer)
        {
			if (Hud.Game.IsInTown) return;
        }

        public void OnKeyEvent(IKeyEvent keyEvent)
        {
            if (keyEvent.IsPressed && PressKeyEvent.Matches(keyEvent))
	       {
	       		Console.Beep(900, 200);
	       		//Process.Start("D:\\Game\\clickDrag.exe");		// Preload for quick processing later
	       		DumpingRareItemsInInventory();
		  }
        }

	   private void DumpingRareItemsInInventory()
	   {
	   		Hud.Sound.Speak("Dumping Rare Items!");
	   		if (!Hud.Inventory.InventoryMainUiElement.Visible)
	   			SendKeys.SendWait("1");		// the Key to open your inventory window

	   		Thread.Sleep(300);

			var items = Hud.Game.Items.Where(x => x.Location != ItemLocation.Merchant && x.Location != ItemLocation.Floor);
			if (items.Count() == 0) return;

            	foreach (var item in items)
           	{
               		if (item.Location == ItemLocation.Inventory)
               		{
               			if ((item.InventoryX < 0) || (item.InventoryY < 0)) continue;

               			if (item.IsRare)
               			{
	               			itemX = item.InventoryX;
			  		  	itemY = item.InventoryY;
			  		  	MoveCursorInventory(itemX, itemY);
			  		  	Thread.Sleep(300);
	                	}
		  		}
               }
        }

        private void MoveCursorInventory(int X, int Y)
        {
        		itemX = BaseX + (50 * X);
        		itemY = BaseY + (50 * Y);
        		Cursor.Position = new Point(itemX, itemY);
               var CursorPos = itemX.ToString("0") + "," + itemY.ToString("0");
	          Process.Start("D:\\Game\\clickDrag.exe", CursorPos);		     	
        }
	}
}