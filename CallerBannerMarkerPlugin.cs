// This plugin helps to identify who called in a party. The call's mainly because of pool of reflextion, bandit shrine, rainbow room, and so forth
// the information from the chat lines : ex) [party][player]:11, [party][<clan>player]:11...
using System;
using Turbo.Plugins.Default;
using System.Windows.Forms;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SharpDX.DirectInput;
using System.Drawing;

namespace Turbo.Plugins.James
{
    public class CallerBannerMarkerPlugin : BasePlugin, IInGameWorldPainter, IChatLineChangedHandler		//, IKeyEventHandler
    {
    	   //public IKeyEvent PressKeyEvent { get; set; }
        private WorldDecoratorCollection CallerSignDecorator { get; set; }
        private WorldDecoratorCollection PlayerLabelDecorator { get; set; }
        private TopLabelWithTitleDecorator CallerPopupDecorator { get; set; }
        private TopLabelDecorator TagDecorator { get; set; }
	   private readonly HashSet<ActorSnoEnum> _bannersSnoList = new HashSet<ActorSnoEnum>();
	   private bool BannerShow { get; set; }
        private string [] CallSign = new string [] {"11", "깃", "오세요"};	// *** change or add your own caller's conventional phrases ***
        private string Caller { get; set; }
        private string partyId = "[파티]";	// *** [party][<clan>player] <-- replace "[파티]" with an accurate string for the party in your language
	   private static System.Timers.Timer aTimer;		// for deleteing the ready-made caller markerafter a period of time : 15 secs (default)
        private int w, h, x, y;
        
        public CallerBannerMarkerPlugin()
        {
            Enabled = true;
            BannerShow = false;
            Caller = "*";
        }

        public override void Load(IController hud)
        {
            base.Load(hud);

            CallerSignDecorator = new WorldDecoratorCollection(
                new GroundCircleDecorator(Hud)
                {
                    Brush = Hud.Render.CreateBrush(255, 0, 255, 0, 20)
                });
                
            PlayerLabelDecorator = new WorldDecoratorCollection(
                new GroundLabelDecorator(Hud)
                {
                    TextFont = Hud.Render.CreateFont("tahoma", 13.0f, 255, 0, 255, 0, false, false, false),
                    BackgroundBrush = Hud.Render.CreateBrush(255, 0, 0,0, 0)
                });
                
            TagDecorator = new TopLabelDecorator(Hud)
            {
                TextFont = Hud.Render.CreateFont("tahoma", 8, 200, 255, 255, 255, true, false, false),
                BackgroundTexture1 = hud.Texture.ButtonTextureBlue,
            };

            CallerPopupDecorator = new TopLabelWithTitleDecorator(hud)
            {
                BorderBrush = hud.Render.CreateBrush(255, 180, 147, 109, -1),
                BackgroundBrush = hud.Render.CreateBrush(200, 0, 0, 0, 0),
                TextFont = hud.Render.CreateFont("tahoma", 8, 255, 255, 255, 255, true, false, false),
                TitleFont = hud.Render.CreateFont("tahoma", 6, 255, 180, 147, 109, true, false, false),
            };
            
            //PressKeyEvent = Hud.Input.CreateKeyEvent(true, Key.Divide, false, false, false);
            
            _bannersSnoList.Add(ActorSnoEnum._banner_player_1);
            _bannersSnoList.Add(ActorSnoEnum._banner_player_2);
            _bannersSnoList.Add(ActorSnoEnum._banner_player_3);
            _bannersSnoList.Add(ActorSnoEnum._banner_player_4);
            _bannersSnoList.Add(ActorSnoEnum._banner_player_1_act2);
            _bannersSnoList.Add(ActorSnoEnum._banner_player_2_act2);
            _bannersSnoList.Add(ActorSnoEnum._banner_player_3_act2);
            _bannersSnoList.Add(ActorSnoEnum._banner_player_4_act2);
            _bannersSnoList.Add(ActorSnoEnum._banner_player_1_act5);
            _bannersSnoList.Add(ActorSnoEnum._banner_player_2_act5);
            _bannersSnoList.Add(ActorSnoEnum._banner_player_3_act5);
            _bannersSnoList.Add(ActorSnoEnum._banner_player_4_act5);
        }

	   public void PaintWorld(WorldLayer layer)
	   {
	       var me = Hud.Game.Me;
		  if (me.IsInTown && BannerShow)
		  {
		       var meTag = me.BattleTagAbovePortrait;
		       var cnt = 0;
		       var PlayerId = 0;
		       var tmpPlayer = "*";
		       
	            foreach (var player in Hud.Game.Players)
	            {
	               cnt++;
	               if (player == null) continue;

	               tmpPlayer = player.BattleTagAbovePortrait;
	               if (tmpPlayer == meTag) return;		// if the caller is me, return
	               if (tmpPlayer == Caller)
	               {
	               		PlayerId = cnt;
	               		TagDecorator.TextFunc = () => tmpPlayer.ToString();
	                 	break;
	               }
	            }
	            
			 if (PlayerId == 0)						// No match - it's logically impossible.. but just for exceptions
			 {
			 	Console.Beep(900, 200);
			 	BannerShow = false;
			  	return;
			 }

	           bool IsCalled = false;
                var banners = Hud.Game.Actors.Where(x => _bannersSnoList.Contains(x.SnoActor.Sno));
            	 foreach (var banner in banners)
	           {
	                switch (banner.SnoActor.Sno)
	                {
	                    case ActorSnoEnum._banner_player_1:
	                    case ActorSnoEnum._banner_player_1_act2:
	                    case ActorSnoEnum._banner_player_1_act5:
	                        if (PlayerId == 1)
	                        		IsCalled = true;
	                        break;
	                    case ActorSnoEnum._banner_player_2:
	                    case ActorSnoEnum._banner_player_2_act2:
	                    case ActorSnoEnum._banner_player_2_act5:
	                        if (PlayerId == 2)
	                        		IsCalled = true;
	                        break;
	                    case ActorSnoEnum._banner_player_3:
	                    case ActorSnoEnum._banner_player_3_act2:
	                    case ActorSnoEnum._banner_player_3_act5:
	                        if (PlayerId == 3)
	                        		IsCalled = true;
	                        break;
	                    case ActorSnoEnum._banner_player_4:
	                    case ActorSnoEnum._banner_player_4_act2:
	                    case ActorSnoEnum._banner_player_4_act5:
	                        if (PlayerId == 4)
	                        		IsCalled = true;
	                        break;
	                    default:
	                        break;
	                }

		          if (IsCalled)
		          {
		                CallerSignDecorator.Paint(layer, banner, banner.FloorCoordinate.Offset(0, 0, 10), string.Empty);  // circle
		                PlayerLabelDecorator.Paint(layer, banner, banner.FloorCoordinate.Offset(0, 0, 13), $"Caller: {Caller}"); 
		                w = tmpPlayer.Length * 12;
				      if (w < 100) w = 100;
					 h = 50;
					 x = Hud.Window.Size.Width / 2 - 50;
					 y = 280;
					 CallerPopupDecorator.Paint(x, y , w, h, tmpPlayer, " Caller ");
					 
	                      IsCalled = false;
		          }
		      }
		  }
        }

        public void OnChatLineChanged(string currentLine, string previousLine)
        {
			if (string.IsNullOrEmpty(currentLine)) return;
                //Hud.TextLog.Log("Chat",currentLine, true, true);
                
			if (currentLine.Contains(partyId))
			{
				foreach (string sign in CallSign)
				{
				    if (currentLine.Contains(sign))
				    {
				    		Hud.Sound.Speak("Call for you!");		// 호출입니다.
				    		Match match = Regex.Match(currentLine, @"(?<=\> ).+(?=\])");	//[파티][<xxx>YYY]: 11
					     if (match.Success)
					        Caller = match.Value;
					     else
					     {
					    		match = Regex.Match(currentLine, @"(?<=(h\[)).+(?=\])");	//[파티][YYY]: 11.. internal chat line is different from being seen
					          if (match.Success)
					          		Caller = match.Value;
					     }
					     Hud.Sound.Speak(Caller);
					     BannerShow = true;
					}
				}

				// start the timer setting
				aTimer = new System.Timers.Timer();
				aTimer.Interval = 15000; 					// 15 secs
				aTimer.Elapsed += DeleteBannerShow;	// function to delete ready-made marker(circle) and popup after 10 secs
				aTimer.AutoReset = false;				// only once
				aTimer.Enabled = true;					// timer On/Off
				// end the timer setting
			}
	   }

	   private void DeleteBannerShow(Object source, System.Timers.ElapsedEventArgs e)
        {
			BannerShow = false;
        }

/*
        public void OnKeyEvent(IKeyEvent keyEvent)
        {
            if (keyEvent.IsPressed && PressKeyEvent.Matches(keyEvent))
            {
	        	//BannerShow = !BannerShow;
	        	//OnChatLineChanged("[파티] |HOnlUserHdl:46527f4-4433-3|h[아제]|h: 11", "");
	        	OnChatLineChanged("[파티] |HOnlUserHdl:c51417-4433-3|h[<쓰리고> 고양이]|h: 11", "");
	      }
        }
*/
   }
}