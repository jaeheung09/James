using System.Collections.Generic;
using Turbo.Plugins.Default;
using System.Linq;
using System;
using System.Windows.Forms;

namespace Turbo.Plugins.James
{

    public class PylonActiveTimerPlugin : BasePlugin, IInGameWorldPainter
    {
        public short PopupNo { get; set; }
        private static System.Timers.Timer Remaining1Timer;
        private static System.Timers.Timer Remaining2Timer;
        private static System.Timers.Timer Remaining3Timer;
        private int AlarmCount1, AlarmCount2, AlarmCount3;
        private string pName1, pName2, pName3;
        private string pTitle1, pTitle2, pTitle3;
        private int pDuration1, pDuration2, pDuration3;

        public class Buff
        {
            public bool Displayed { get; set; }
            public uint SNO { get; set; }
            public int Icon { get; set; }
            public string Name { get; set; }
            public string Hint { get; set; }
            public string Title { get; set; }
            public int Duration { get; set; }

            public Buff(uint sno, int icon, string name, string hint, string title, int duration)
            {
                this.SNO = sno;
                this.Icon = icon;
                this.Displayed = false;
                this.Name = name;
                this.Title = title;
                this.Duration = duration;
            }
        }

        public List<Buff> BuffsToWatch { get; set; }

        public PylonActiveTimerPlugin()
        {
            Enabled = true;
        }

        public override void Load(IController hud)
        {
            base.Load(hud);
            PopupNo = 1;
            AlarmCount1 = 0;
            AlarmCount2 = 0;
            AlarmCount3 = 0;
            
            BuffsToWatch = new List<Buff>();
            /*
            BuffsToWatch.Add(new Buff(262935, 0, "능력", "", "Active", 30000));	// Power
            BuffsToWatch.Add(new Buff(266258, 0, "재감", "", "Active", 30000));	// Channeling
            BuffsToWatch.Add(new Buff(266254, 0, "보호", "", "Active", 60000));	// Shield
		  BuffsToWatch.Add(new Buff(263029, 0, "도관", "", "Active", 30000));	// rift conduit
		  BuffsToWatch.Add(new Buff(403404, 0, "도관", "", "Active", 30000));	// gr conduit
		  BuffsToWatch.Add(new Buff(266271, 0, "속도", "", "Active", 60000));	// Speed
		  */
            BuffsToWatch.Add(new Buff(262935, 0, "PW", "", "Active", 30000));
            BuffsToWatch.Add(new Buff(266258, 0, "CH", "", "Active", 30000));
            BuffsToWatch.Add(new Buff(266254, 0, "SH", "", "Active", 60000));
		  BuffsToWatch.Add(new Buff(263029, 0, "CO", "", "Active", 30000)); // rift
		  BuffsToWatch.Add(new Buff(403404, 0, "CO", "", "Active", 30000)); //gr
		  BuffsToWatch.Add(new Buff(266271, 0, "SP", "", "Active", 60000));  
        }

        public void PaintWorld(WorldLayer layer)
        {
            foreach (Buff buff in BuffsToWatch)
            {
                if (Hud.Game.Me.Powers.BuffIsActive(buff.SNO, buff.Icon))
                {
                    if (!buff.Displayed)
                    {
                    		switch(PopupNo)		// 3 popups available for the pylons only
                    		{
                    			case (1):
								pDuration1 = buff.Duration;
								pName1 = buff.Name;
								pTitle1 = buff.Title;
		                              	Remaining1Timer = new System.Timers.Timer();
								Remaining1Timer.Interval = 1000;		// every second
								Remaining1Timer.Elapsed += Counter1;
								Remaining1Timer.AutoReset = true;
								Remaining1Timer.Enabled = true;
							break;
						case (2):
								pDuration2 = buff.Duration;
								pName2 = buff.Name;
								pTitle2 = buff.Title;
		                              	Remaining2Timer = new System.Timers.Timer();
								Remaining2Timer.Interval = 1000;
								Remaining2Timer.Elapsed += Counter2;
								Remaining2Timer.AutoReset = true;
								Remaining2Timer.Enabled = true;
							break;
						case (3):
								pDuration3 = buff.Duration;
								pName3 = buff.Name;
								pTitle3 = buff.Title;
		                              	Remaining3Timer = new System.Timers.Timer();
								Remaining3Timer.Interval = 1000;
								Remaining3Timer.Elapsed += Counter3;
								Remaining3Timer.AutoReset = true;
								Remaining3Timer.Enabled = true;
							break;
				   }

				   PopupNo++;
				   if (PopupNo > 3) PopupNo = 1;

                        buff.Displayed = true;
                    }
                }
                else
                {
                    buff.Displayed = false;
                }
            }
        }

 	   private void Counter1(Object source, System.Timers.ElapsedEventArgs e)
        {
        		AlarmCount1++;
        		var tmpCnt = (int)(pDuration1 / 1000);
        		if (AlarmCount1 >= tmpCnt)
        		{
        			Remaining1Timer.Enabled = false;
        			AlarmCount1 = 0;
        		} else
        		{
        			var tmp = pName1 + " " + (tmpCnt - AlarmCount1).ToString();
				Counter_Action(tmp, pTitle1, 1000, 1);
			}
        }

 	   private void Counter2(Object source, System.Timers.ElapsedEventArgs e)
        {
        		AlarmCount2++;
        		var tmpCnt = (int)(pDuration2 / 1000);
        		if (AlarmCount2 >= tmpCnt)
        		{
        			Remaining2Timer.Enabled = false;
        			AlarmCount2 = 0;
        		} else
        		{
        			var tmp = pName2 + " " + (tmpCnt - AlarmCount2).ToString();
				Counter_Action(tmp, pTitle2, 1000, 2);
			}
        }

 	   private void Counter3(Object source, System.Timers.ElapsedEventArgs e)
        {
        		AlarmCount3++;
        		var tmpCnt = (int)(pDuration3 / 1000);
        		if (AlarmCount3 >= tmpCnt)
        		{
        			Remaining3Timer.Enabled = false;
        			AlarmCount3 = 0;
        		} else
        		{
        			var tmp = pName3 + " " + (tmpCnt - AlarmCount3).ToString();
				Counter_Action(tmp, pTitle3, 1000, 3);
			}
        }
 	   private void Counter_Action(string Name, string Title, int Duration, int pType)
        {
        	Hud.RunOnPlugin<PopupMsgPlugin>(plugin =>
          {
          		switch(pType)
          		{
          			case(1):
					plugin.Show(Name, Title, Duration, "", PopupMsgPlugin.EnumPopupDecoratorToUse.Type1);
					break;
          			case(2):
					plugin.Show(Name, Title, Duration, "", PopupMsgPlugin.EnumPopupDecoratorToUse.Type2);
					break;
          			case(3):
					plugin.Show(Name, Title, Duration, "", PopupMsgPlugin.EnumPopupDecoratorToUse.Type3);
					break;
			}
		});
        }
    }
}