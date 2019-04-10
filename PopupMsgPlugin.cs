using System.Linq;
using System;
using Turbo.Plugins.Default;

namespace Turbo.Plugins.James
{
     public class PopupMsgPlugin: BasePlugin, IInGameWorldPainter
     {
        public enum EnumPopupDecoratorToUse { Default, Type1, Type2, Type3 };
	   public TopLabelWithTitleDecorator PopupDecorator { get; set; }
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
            	
            	PopupDecorator = new TopLabelWithTitleDecorator(Hud)
            	{
	                BorderBrush = Hud.Render.CreateBrush(255, 180, 147, 109, -1),
	                BackgroundBrush = Hud.Render.CreateBrush(100, 0, 0, 0, 0),		// opacity
	                TextFont = Hud.Render.CreateFont("tahoma", 8, 255, 255, 255, 255, true, false, false),
	                TitleFont = Hud.Render.CreateFont("tahoma", 6, 255, 180, 147, 109, true, false, false),
           	 };
	   }

        public void PaintWorld(WorldLayer layer)
        {
             try
             {    
			  foreach (Popup p in Hud.Queue.GetItems<Popup>().Take(3))	//13
            	  {
            	  	w = p.Text.Length * 13;
				if (w < 65) w = 65;
				h = 60;
				x = Hud.Window.Size.Width / 2 - (int)(w / 2);
				y = 350;
            	  	
            	  	switch (p.PopUpDecoratorTouse)
               		{
            	  	     case EnumPopupDecoratorToUse.Default:
				     		w = p.Text.Length * 12;
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
						y += h * 2;
						break;						
				  }
				  PopupDecorator.Paint(x, y , w, h, p.Text, p.Title);
	            }
	        }
	        catch		//(Exception ex)
		   {
		     		//throw;
		   }
        }
    }
}