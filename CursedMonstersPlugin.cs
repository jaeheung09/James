using System.Collections.Generic;
using System.Linq;
using System;
using Turbo.Plugins.Default;
namespace Turbo.Plugins.James
{
    public class CursedMonstersPlugin : BasePlugin, IInGameWorldPainter
    {
        public IFont TextFontFrailty { get; set; }
        //public IFont TextFontLeech { get; set; }
        //public IFont TextFontDecrepify { get; set; }
        private WorldDecoratorCollection CursedMonsterDecorator { get; set; }
        
        public CursedMonstersPlugin()
        {
            Enabled = true;
        }

        public override void Load(IController hud)
        {
            base.Load(hud);

            TextFontFrailty = Hud.Render.CreateFont("tahoma", 8, 255, 64, 224, 208, false, false, true);	// 距拳
            //TextFontLeech = Hud.Render.CreateFont("tahoma", 10, 255, 255, 0, 0, false, false, true);		// 积扁软荐
            //TextFontDecrepify = Hud.Render.CreateFont("tahoma", 10, 255, 64, 224, 208, false, false, true);	// 畴拳
            
            CursedMonsterDecorator = new WorldDecoratorCollection(
                new GroundLabelDecorator(Hud)
                {
                    TextFont = Hud.Render.CreateFont("tahoma", 10.5f, 255, 0, 255, 0, false, false, false),
                    BackgroundBrush = Hud.Render.CreateBrush(255, 0, 0,0, 0)
                });
        }

        public void PaintWorld(WorldLayer layer)
        {
            if (Hud.Render.UiHidden) return;
            if (Hud.Game.Me.HeroClassDefinition.HeroClass != HeroClass.Necromancer)
            	return;
            	
            var w1 = 30;
            var py = Hud.Window.Size.Height / 600;
            var monsters = Hud.Game.AliveMonsters.Where(x => x.IsAlive);

//            if (NecroMe)
//            {
	           var player = Hud.Game.Me;
                var FCount = 0;
	           var FNotCount = 0;
                foreach (var monster in monsters)
                {
                    var textFrailty = "";
                    //var textLeech = "";
                    //var textDecrepify = "";
                    var test = monster.GetAttributeValue(Hud.Sno.Attributes.Power_Buff_2_Visual_Effect_None, 471845);//471845	1	power: Frailty
                    if (test == -1)
                    {
                        textFrailty += "历林";
                        FNotCount ++; 
                    } else
                        FCount++;
                    /*
                    test = monster.GetAttributeValue(Hud.Sno.Attributes.Power_Buff_2_Visual_Effect_None, 471869);//471869	1	power: Leech
                    if (test == -1)
                    {
                        textLeech += "历林";
                    }
                    test = monster.GetAttributeValue(Hud.Sno.Attributes.Power_Buff_2_Visual_Effect_None, 471738);//471738	1	power: Decrepify
                    if (test == -1)
                    {
                        textDecrepify += "历林";
                    }
                    */
                    var layoutFrailty = TextFontFrailty.GetTextLayout(textFrailty);
                    //var layoutLeech = TextFontLeech.GetTextLayout(textLeech);
                    //var layoutDecrepify = TextFontDecrepify.GetTextLayout(textDecrepify);
                    var w = monster.CurHealth * w1 / monster.MaxHealth;
                    var monsterX = monster.FloorCoordinate.ToScreenCoordinate().X - w1 / 2;
                    var monsterY = monster.FloorCoordinate.ToScreenCoordinate().Y + py * 12;
                    var buffY = monsterY - 1;
                    var hpX = monsterX + 7;

                    TextFontFrailty.DrawText(layoutFrailty, hpX - 2, buffY);
                    //TextFontLeech.DrawText(layoutLeech, hpX + 6, buffY);
                    //TextFontDecrepify.DrawText(layoutDecrepify, hpX + 14, buffY);
                }
                CursedMonsterDecorator.Paint(layer, player, player.FloorCoordinate.Offset(-16f, -16f, 0), $"历林 : {FCount} | {FNotCount}");
//            }
        }
    }
}