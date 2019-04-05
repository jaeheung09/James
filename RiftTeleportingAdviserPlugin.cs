// This plugin helps you to choose which banner(whose player is in rfit) to teleport.
// It marks the banner of the player who went the deepest in rift now and has higher paragon level if more than 1 at the same place
using System;
using Turbo.Plugins.Default;
using System.Windows.Forms;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Turbo.Plugins.James
{
    public class RiftTeleportingAdviserPlugin : BasePlugin, IInGameWorldPainter
    {
        private WorldDecoratorCollection TelpoSignDecorator { get; set; }
        private WorldDecoratorCollection PlayerLabelDecorator { get; set; }
        private WorldDecoratorCollection PlayerTitleLabelDecorator { get; set; }
        private readonly HashSet<ActorSnoEnum> _bannersSnoList = new HashSet<ActorSnoEnum>();
	   private bool BannerShow { get; set; }
        private string myLocation { get; set; }

        public RiftTeleportingAdviserPlugin()
        {
            Enabled = true;
            BannerShow = false;
            myLocation = string.Empty;
        }

        public override void Load(IController hud)
        {
            base.Load(hud);

            TelpoSignDecorator = new WorldDecoratorCollection(
                new GroundCircleDecorator(Hud)
                {
                    Brush = Hud.Render.CreateBrush(255, 255, 255, 0, 20)
                });

            PlayerLabelDecorator = new WorldDecoratorCollection(
                new GroundLabelDecorator(Hud)
                {
                    TextFont = Hud.Render.CreateFont("tahoma", 7.0f, 255, 200, 150, 0, false, false, false),
                    BorderBrush = hud.Render.CreateBrush(255, 180, 147, 109, -1),
                    BackgroundBrush = Hud.Render.CreateBrush(255, 0, 0, 0, 0)
                });

            PlayerTitleLabelDecorator = new WorldDecoratorCollection(
                new GroundLabelDecorator(Hud)
                {
                    TextFont = Hud.Render.CreateFont("tahoma", 8.0f, 255, 0, 255, 0, false, false, false),
                    BorderBrush = hud.Render.CreateBrush(255, 180, 147, 109, -1),
                    BackgroundBrush = Hud.Render.CreateBrush(255, 0, 0, 0, 0)
                });

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
	       if (!me.IsInTown)	return;

	       var PlayerId = 0;
	       var PlayerAreaName = string.Empty;
	       var PlayerParagonLevel = 0;
	       var StoryNo = 0;
	       var cnt = 0;
	       var tmpValue = 0;
	       int tmp = 0;

	       foreach (var player in Hud.Game.Players)
	       {
	       	cnt++;
	       	if (player.IsInTown) continue;
	       	PlayerAreaName = player.SnoArea.NameLocalized;	// player.SnoArea.NameEnglish
	       	Match match = Regex.Match(PlayerAreaName, @"\d");

			if (match.Success)
			{
				tmpValue = Int32.Parse(match.Value);
				if (tmpValue > StoryNo)
				{
					tmp = checked((int)player.CurrentLevelParagon);
					PlayerParagonLevel = tmp;
					PlayerId = cnt;
				} else if (tmpValue == StoryNo)
				{
					if (player.CurrentLevelParagon > PlayerParagonLevel)
					{
						tmp = checked((int)player.CurrentLevelParagon);
						PlayerParagonLevel = tmp;
						PlayerId = cnt;
					}
				}
				if (tmpValue != StoryNo)
				StoryNo = tmpValue;
			}
	       }

		  if (PlayerId == 0) return;

		  cnt = 0;
	       foreach (var player in Hud.Game.Players)
	       {
	       	cnt++;
	           if (cnt == PlayerId)
	           {
			 	bool DrawIt = false;
		          var banners = Hud.Game.Actors.Where(x => _bannersSnoList.Contains(x.SnoActor.Sno));
		          foreach (var banner in banners)
			     {
			     		switch (banner.SnoActor.Sno)
			          {
			          		case ActorSnoEnum._banner_player_1:
			               case ActorSnoEnum._banner_player_1_act2:
			               case ActorSnoEnum._banner_player_1_act5:
			                    if (PlayerId == 1)
			                   		DrawIt = true;
			                    break;
			                case ActorSnoEnum._banner_player_2:
			                case ActorSnoEnum._banner_player_2_act2:
			                case ActorSnoEnum._banner_player_2_act5:
			                    if (PlayerId == 2)
			                    		DrawIt = true;
			                    break;
			                case ActorSnoEnum._banner_player_3:
			                case ActorSnoEnum._banner_player_3_act2:
			                case ActorSnoEnum._banner_player_3_act5:
			                    if (PlayerId == 3)
			                    		DrawIt = true;
			                    break;
			                case ActorSnoEnum._banner_player_4:
			                case ActorSnoEnum._banner_player_4_act2:
			                case ActorSnoEnum._banner_player_4_act5:
			                    if (PlayerId == 4)
			                    		DrawIt = true;
			                    break;
			           }

				      if (DrawIt)
				      {
				      	// var Hero = player.HeroName;		// wonder if this works in English, but not in Korean. It it works, no need the switch statement below
				          	switch (player.HeroClassDefinition.HeroClass)
              				{
               	    				case HeroClass.Monk:
               	    					Hero = "수도";
               	    					break;
               	    				case HeroClass.Wizard:
               	    					Hero = "법사";
               	    					break;
               	    				case HeroClass.Crusader:
               	    					Hero = "성전";
               	    					break;
               	    				case HeroClass.Barbarian:
               	    					Hero = "야만";
               	    					break;
               	    				case HeroClass.DemonHunter:
               	    					Hero = "악사";
               	    					break;
               	    				case HeroClass.WitchDoctor:
               	    					Hero = "부두";
               	    					break;
               	    				case HeroClass.Necromancer:
               	    					Hero = "강령";
               	    					break;
               	    			 }

				      	var pTitle = player.BattleTagAbovePortrait + " - " + Hero + "( P:" +  PlayerParagonLevel.ToString() + " | S:" + player.HighestHeroSoloRiftLevel.ToString() + " )";
				      	PlayerTitleLabelDecorator.Paint(layer, banner, banner.FloorCoordinate.Offset(0, 0, 8), $"{pTitle}");
				          PlayerLabelDecorator.Paint(layer, banner, banner.FloorCoordinate.Offset(0, 0, 6), $"{PlayerAreaName}");
						TelpoSignDecorator.Paint(layer, banner, banner.FloorCoordinate.Offset(0, 0, 3), string.Empty);  // circle
			                DrawIt = false;
			                break;
				       }
				  }
			   }
		    }
		}
   }
}