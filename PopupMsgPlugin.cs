using System.Linq;
using System;
using Turbo.Plugins.Default;
using System.Collections.Generic;

namespace Turbo.Plugins.James
{
     public class PopupMsgPlugin: BasePlugin, IInGameWorldPainter
     {
        public enum EnumPopupDecoratorToUse { Default, Type1, Chat1, WebBB1 };
	   public TopLabelWithTitleDecorator PopupDecorator1 { get; set; }
	   public TopLabelWithTitleDecorator PopupDecorator2 { get; set; }
	   public int w, h, x, y, fixedX;
	   public bool pyFirst;

	   public class Popup : IQueueItem
        {
            public string Text { get; set; }
            public string Title { get; set; }
            public string Hint { get; set; }
            public DateTime QueuedOn { get; private set; }
            public TimeSpan LifeTime { get; private set; }
            public EnumPopupDecoratorToUse PopUpDecoratorTouse { get; private set; }

            public Popup(string text, string title, TimeSpan lifetime, string hint, EnumPopupDecoratorToUse popupDecoratorToUse = EnumPopupDecoratorToUse.Default)
            {
                this.Text = text;
                this.Title = title;
                this.LifeTime = lifetime;
                this.Hint = hint;
                this.QueuedOn = DateTime.Now;
                this.PopUpDecoratorTouse = popupDecoratorToUse;
            }
        }

        public void Show(string text, string title, int duration, string hint = null, EnumPopupDecoratorToUse popupDecoratorToUse = EnumPopupDecoratorToUse.Default)
        {
            	Hud.Queue.AddItem(new Popup(text, title, new TimeSpan(0, 0, 0, 0, duration), hint, popupDecoratorToUse));
        }

        public PopupMsgPlugin()
        {
            	Enabled = true;
        }

        public override void Load(IController hud)
        {
            	base.Load(hud);

			pyFirst = true;
			//Ystart = (int)(Hud.Window.Size.Height * 0.4f);
			
            	PopupDecorator1 = new TopLabelWithTitleDecorator(Hud)
            	{
	                BorderBrush = Hud.Render.CreateBrush(255, 180, 147, 109, -1),
	                BackgroundBrush = Hud.Render.CreateBrush(100, 0, 0, 0, 0),		// opacity
	                TextFont = Hud.Render.CreateFont("tahoma", 8, 255, 255, 255, 255, true, false, false),
	                TitleFont = Hud.Render.CreateFont("tahoma", 7, 255, 180, 147, 109, true, false, false),
           	 };

            	PopupDecorator2 = new TopLabelWithTitleDecorator(Hud)
            	{
	                BorderBrush = Hud.Render.CreateBrush(255, 180, 147, 109, -1),
	                BackgroundBrush = Hud.Render.CreateBrush(100, 0, 0, 50, 0),		// opacity
	                TextFont = Hud.Render.CreateFont("tahoma", 8, 255, 150, 255, 0, true, false, false),
	                TitleFont = Hud.Render.CreateFont("tahoma", 7, 255, 180, 147, 109, true, false, false),
           	 };
	   }

        public void PaintWorld(WorldLayer layer)
        {
             try					// minimap overlay exceptions happen sometimes, but no harm...so let it just pass
             {
             	  var cnt = 0;		// for type1~3 (B&W)
             	  var cnt1 = 0;		// for webbbs 1~3 (color)
             	  var cnt2 = 0;		// for chat1~3 (B&W)
			  foreach (Popup p in Hud.Queue.GetItems<Popup>())
            	  {
				w = p.Text.Length * 13;
				if (w > 1300)
				{
					w = 1300;							// max width
					p.Text = p.Text.Substring(100);		// cut it if too long
				}

				var deco = 1;
            	  	switch (p.PopUpDecoratorTouse)
               		{
            	  	     case EnumPopupDecoratorToUse.Default:
            	  	     		x = fixedX = Hud.Window.Size.Width / 2 - (int)(w / 2);
						h = 55;
						y = 280;
						break;
            	  	     case EnumPopupDecoratorToUse.Type1:
            	  	     		x = fixedX = Hud.Window.Size.Width / 2 - (int)(w / 2);
						h = 50;
						w = 80;
						y = 350 + (cnt * h);
						cnt++;
						break;
				     case EnumPopupDecoratorToUse.WebBB1:
						deco = 2;
						x = 50;
						h = 60;
						y = 250 + (cnt1 * h);
						cnt1++;
						break;
				     case EnumPopupDecoratorToUse.Chat1:
				     		x = 50;
				     		h = 60;
				    		y = 440 + (cnt2 * h);
				    		cnt2++;
						break;
				  }
				  if (deco == 1)
				  	PopupDecorator1.Paint(x, y , w, h, p.Text, p.Title);		// black&white
				  else
				  	PopupDecorator2.Paint(x, y , w, h, p.Text, p.Title);		// color
	            }
	        }
	        catch	
		   {
		     		Console.Beep(300, 100);
		   }
        }
    }
}