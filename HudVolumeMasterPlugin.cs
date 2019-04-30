// This plugin is to set turboHUD master volume.
// To set the volume, put your cursor on the chat edit line by pressing "Enter" and then "/volume n/" (n is from 0 to 100.
using System;
using Turbo.Plugins.Default;
using System.Text.RegularExpressions;

namespace Turbo.Plugins.James
{
    public class HudVolumeMasterPlugin : BasePlugin
    {
		private string chatEditLine = "Root.NormalLayer.chatentry_dialog_backgroundScreen.chatentry_content.chat_editline";
		private static System.Timers.Timer ReadEditLineTimer;
		private int MasterVolume;
		private string culture;

		public HudVolumeMasterPlugin()
        	{
        		Enabled = true;
        	}

        	public override void Load(IController hud)
        	{
            	base.Load(hud);

			Hud.Sound.VolumeMode = VolumeMode.Constant;
			MasterVolume = 80;
			culture = System.Globalization.CultureInfo.CurrentCulture.ToString().Substring(0, 2);

		     ReadEditLineTimer = new System.Timers.Timer();
			ReadEditLineTimer.Interval = 500;		// edit line filtering interval
			ReadEditLineTimer.Elapsed += ReadEditLine;
			ReadEditLineTimer.AutoReset = true;
			ReadEditLineTimer.Enabled = true;
	   	}

		public void ReadEditLine(Object source, System.Timers.ElapsedEventArgs e)
        	{
        		// chat edit line
        		if (!Hud.Render.GetUiElement(chatEditLine).Visible)
        			return;

			int tmp = 0;
			string defaultVal = string.Empty;
        		var lineStr = Hud.Render.GetUiElement(chatEditLine).ReadText(System.Text.Encoding.UTF8, false).Trim();	// if error, change "UTF8" with "Default"...not tested though
        		Match match = Regex.Match(lineStr, @"(?<=/volume ).+(?=/)");
			if (match.Success)	// in the edit line, should type "/volume n/" <- n is from 0 to 100.
			{
				if (Char.IsDigit(match.Value[0]))
				{
				//match = Regex.Match(lineStr, @"\d{1,}");	// extract a number
				//if (match.Success)
				//{
					tmp = Int32.Parse(match.Value);
					if (tmp < 0 || tmp > 100)
					{
						MasterVolume = 80;			// default volume
						defaultVal = (culture == "ko") ? "기본값 " : "default value ";
					} else
						MasterVolume = tmp;
				} else
				{
					if (Hud.Sound.LastSpeak.TimerTest(5000))
	        			{
	        				Console.Beep(300, 200);
	        				if (culture == "ko")
	        					Hud.Sound.Speak("허드 볼륨 설정 에러!");
	        				else
	        					Hud.Sound.Speak("Hud volume setting error!");
	        			}
	        		}

				Hud.Sound.ConstantVolume = MasterVolume; //0 .. 100

        			if (Hud.Sound.LastSpeak.TimerTest(5000))
        			{
        				if (culture == "ko")
        					Hud.Sound.Speak("허드 볼륨이 " + defaultVal + Convert.ToString(MasterVolume) + "으로 설정 되었습니다..");
        				else
        					Hud.Sound.Speak("Current Hud volume is set to " + defaultVal + Convert.ToString(MasterVolume));
        			}
        		}
        	}
	}
}