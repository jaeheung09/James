using System.Linq;
using System;
using Turbo.Plugins.Default;
using System.Collections.Generic;

namespace Turbo.Plugins.James
{
     public class PopupMsgPlugin: BasePlugin, IInGameWorldPainter
     {
        public enum EnumPopupDecoratorToUse { Default, Type1, Type2, Type3, Chat1, Chat2, Chat3, WebBB1, WebBB2, WebBB3 };
	   public TopLabelWithTitleDecorator PopupDecorator1 { get; set; }
	   public TopLabelWithTitleDecorator PopupDecorator2 { get; set; }
	   public int w, h, x, y;

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
             try
             {
			  foreach (Popup p in Hud.Queue.GetItems<Popup>().Take(9))
            	  {
            	  	w = p.Text.Length * 13;
				h = 60;
				switch (p.PopUpDecoratorTouse)
               		{
               			case EnumPopupDecoratorToUse.Default:
               			case EnumPopupDecoratorToUse.Type1:
               			case EnumPopupDecoratorToUse.Type2:
               			case EnumPopupDecoratorToUse.Type3:
						x = Hud.Window.Size.Width / 2 - (int)(w / 2);
						break;
					default:
						x = 50;
						break;
				}
				if (w < 65) w = 65;
				if (w > 1300)
				{
					w = 1300;
					p.Text = p.Text.Substring(100);
				}				
				y = 250;

				var deco = 1;
            	  	switch (p.PopUpDecoratorTouse)
               		{
            	  	     case EnumPopupDecoratorToUse.Default:
				     		if (w < 100) w = 100;
						h = 50;
						y = 280;
						break;
				     case EnumPopupDecoratorToUse.Type1:
						break;
				     case EnumPopupDecoratorToUse.Type2:
						y += h;
						break;
				     case EnumPopupDecoratorToUse.Type3:
						y += (h * 2);
						break;
				     case EnumPopupDecoratorToUse.WebBB1:
						deco = 2;
						break;
				     case EnumPopupDecoratorToUse.WebBB2:
						y +=  h;
						deco = 2;
						break;
				     case EnumPopupDecoratorToUse.WebBB3:
						y += (h * 2);
						deco = 2;
						break;						
				     case EnumPopupDecoratorToUse.Chat1:
				    		 y += (h * 3);
						break;
				     case EnumPopupDecoratorToUse.Chat2:
				    		 y += (h * 4);
						break;
				     case EnumPopupDecoratorToUse.Chat3:
						y += (h * 5);
						break;
				  }
				  if (deco == 1)
				  	PopupDecorator1.Paint(x, y , w, h, p.Text, p.Title);		// black&white
				  else
				  	PopupDecorator2.Paint(x, y , w, h, p.Text, p.Title);		// color
	            }
	        }
	        catch		//(Exception ex)
		   {
		     		//throw;
		   }
        }
    }
}