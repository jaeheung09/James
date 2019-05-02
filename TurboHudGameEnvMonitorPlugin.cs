// This plugin shows you current Cpu usuage, current Ram available, current Latency/Average Latency and TurboHUD real FPS
// Regarding FPS, it is different from of the D3 built-in function. This calculates real turboHUD's FPS, which is mostly the same but on occasion shows clear distinction from Blizzard's.
// if they meet the guideline you set, info text color will change. :bule(OK)->orange(Warning)->red(Bad)
// toggle Key to trigger this monitor : ctrl + NumPad.Subtract("-")
using System;
using System.Linq;
using Turbo.Plugins.Default;
using System.Windows.Forms;
using SharpDX.DirectInput;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Turbo.Plugins.James
{
    public class TurboHudGameEnvMonitorPlugin : BasePlugin, IKeyEventHandler, IInGameTopPainter, IInGameWorldPainter
    {
    		private PerformanceCounter Cpu = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        	private PerformanceCounter Ram = new PerformanceCounter("Memory", "Available MBytes");
        	private ProcessStartInfo cmd = new ProcessStartInfo();
		private Process process = new Process();
		private string chatEditLine = "Root.NormalLayer.chatentry_dialog_backgroundScreen.chatentry_content.chat_editline";
		private static System.Timers.Timer ReadEditLineTimer;
        	private static int lastTick;
        	private static int lastFrameRate;
        	private static int frameRate;
        	private static int lastTickWorld;
        	private static int lastFrameRateWorld;
        	private static int frameRateWorld;
        	private static int lastTickTop;
        	private static int lastFrameRateTop;
        	private static int frameRateTop;
        	private bool doFlag;
        	private static System.Timers.Timer MonitorTimer;
        	private TopLabelDecorator ContentOKDecorator { get; set; }
        	private TopLabelDecorator ContentWarningDecorator { get; set; }
        	private TopLabelDecorator ContentBadDecorator { get; set; }
        	private string MonitoredResource;
        	private int FrameRateKind;	// 1: PaintTopInGame with checking Clipstate, 2: Pure PaintTopInGame, 3. Pure PaintWorld, 4: all of them
        	private int savedKind;
        	// You can set them to your prefered values (OK: color blue green, Warning: orange, Bad: red)*****
        	private const int CpuWarning = 85;		// more than cpu using 90%
        	private const int CpuBad = 95;			// more than cpu using 95%
        	private const int GpuWarning = 70;		// more than gpu using 70%
        	private const int GpuBad = 90;			// more than cpu using 90%
        	private const int RamWarning = 200;		// less than 200 MB
        	private const int RamBad = 100;			// less than 100 MB
        	private const int LatencyWarning = 50;		// if current latency is more than 50 ms
        	private const int LatencyBad = 80;		// if cur latency is greater than 80 ms
        	private const int FPSWarning = 40;		// if current FPS is less than 40
        	private const int FPSBad = 20;			// if cur FPS is less than 20
		private const int RMInterval = 1000; 		// Resources monitoring interval 1 sec
        	//*********************************************************************************************************
		private int BaseX;
		private int BaseY;
		private int displayTag;	// ok, warning, bad
		private int FrameRate;
		private int FrameRateTop;
		private int FrameRateWorld;
		private int FPSWarningCnt;
		private string culture;
		public TopLabelWithTitleDecorator PlayerDecorator { get; set; }
		// private enum display { ok, warning, bad }

		public TurboHudGameEnvMonitorPlugin()
        	{
        		Enabled = true;
        	}

        	public override void Load(IController hud)
        	{
            	base.Load(hud);

			culture = System.Globalization.CultureInfo.CurrentCulture.ToString().Substring(0, 2);

		     ReadEditLineTimer = new System.Timers.Timer();
			ReadEditLineTimer.Interval = 500;		// edit line filtering interval
			ReadEditLineTimer.Elapsed += ReadEditLine;
			ReadEditLineTimer.AutoReset = true;
			ReadEditLineTimer.Enabled = true;
			
			cmd.FileName = "CMD.exe";
			cmd.WorkingDirectory = @"C:\Program Files\NVIDIA Corporation\NVSMI";
			cmd.WindowStyle = ProcessWindowStyle.Hidden;
			cmd.CreateNoWindow = true;
			cmd.UseShellExecute = false;
			cmd.RedirectStandardInput = true;
			cmd.RedirectStandardOutput = true;
			cmd.RedirectStandardError = true;
			process.EnableRaisingEvents = false;
			process.StartInfo = cmd;

            	doFlag = false;
            	FPSWarningCnt = 0;
            	FrameRateKind = 1;
            	savedKind = 0;
            	MonitoredResource = string.Empty;
            	BaseX = (int)(Hud.Window.Size.Width * 0.82f);
            	BaseY = (int)(Hud.Window.Size.Height * 0.90f);

			PlayerDecorator = new TopLabelWithTitleDecorator(hud)
			{
				BorderBrush = hud.Render.CreateBrush(255, 180, 147, 109, -1),
				BackgroundBrush = hud.Render.CreateBrush(100, 0, 0, 0, 0),	//200
				TextFont = hud.Render.CreateFont("tahoma", 8, 255, 255, 255, 255, true, false, false),
				TitleFont = hud.Render.CreateFont("tahoma", 6, 255, 180, 147, 109, true, false, false),
			};

		      ContentOKDecorator = new TopLabelDecorator(Hud)
		      {
		           TextFont = Hud.Render.CreateFont("consolas", 8, 220, 100, 255, 100, true, false, 255, 0, 0, 0, true),
		           TextFunc = () => MonitoredResource,
		      };

		      ContentWarningDecorator = new TopLabelDecorator(Hud)
		      {
		           TextFont = Hud.Render.CreateFont("consolas", 8, 220, 255, 150, 80, true, false, 255, 0, 0, 0, true),
		           TextFunc = () => MonitoredResource,
		      };

		      ContentBadDecorator = new TopLabelDecorator(Hud)
		      {
		           TextFont = Hud.Render.CreateFont("consolas", 8, 220, 255, 0, 0, true, false, 255, 0, 0, 0, true),
		           TextFunc = () => MonitoredResource,
		      };
	   	}
	   	
		public void ReadEditLine(Object source, System.Timers.ElapsedEventArgs e)
        	{
        		// chat edit line
        		if (!Hud.Render.GetUiElement(chatEditLine).Visible)
        			return;

			int tmp = 0;
			string defaultVal = string.Empty;
        		var lineStr = Hud.Render.GetUiElement(chatEditLine).ReadText(System.Text.Encoding.UTF8, false).Trim();
        		lineStr = lineStr.ToLower();
        		Match match = Regex.Match(lineStr, @"(?<=/fps ).+(?=/)");
			if (match.Success)	// in the edit line, should type "/fps n/" <- n is from 1 to 4.
			{
				if (Char.IsDigit(match.Value[0]))
				{
					tmp = Int32.Parse(match.Value);
					if (tmp < 1 || tmp > 4)
					{
						FrameRateKind = 1;			// default volume
						defaultVal = (culture == "ko") ? "기본값 " : "default value ";
					} else
						FrameRateKind = tmp;
				} else
				{
					if (Hud.Sound.LastSpeak.TimerTest(5000))
	        			{
	        				Console.Beep(300, 200);
	        				if (culture == "ko")
	        					Hud.Sound.Speak("FPS 종류 설정 에러!");
	        				else
	        					Hud.Sound.Speak("FPS kind setting error!");
	        			}
	        		}

				if (FrameRateKind != savedKind)
				{
					savedKind = FrameRateKind;
					
	        			if (culture == "ko")
	        				Hud.Sound.Speak("FPS가 " + defaultVal + Convert.ToString(FrameRateKind) + "으로 설정 되었습니다..");
	        			else
	        				Hud.Sound.Speak("Current FPS kind is set to " + defaultVal + Convert.ToString(FrameRateKind));
	        		}
        		}
        	}	   	

        	public void PaintWorld(WorldLayer layer)
        	{
        		if (doFlag)
        		{
        			if (FrameRateKind == 3 || FrameRateKind == 4)
        				FrameRateWorld = CalculateFrameRateWorld();
        		}
        	}

        	public void PaintTopInGame(ClipState clipState)
        	{
        		if (doFlag)
        		{
        			if (FrameRateKind == 1 || FrameRateKind == 4)
        			{
		        		if (clipState != ClipState.AfterClip) return;		// without this check, turboHUD generates frams more than VSync cap.
		        			FrameRate = CalculateFrameRate();	// count the frames for each layer while HUD is rendering the UI
		        	} else if (FrameRateKind == 2)
        				FrameRateTop = CalculateFrameRateWorld();

				switch (displayTag)
				{
					case 0:	// OK state
		          			ContentOKDecorator.Paint(BaseX, BaseY, 200, 100, HorizontalAlign.Left);
		          			break;
					case 1:	// Warning state
		          			ContentWarningDecorator.Paint(BaseX, BaseY, 200, 100, HorizontalAlign.Left);
		          			break;
					case 2:	// Bad state
		          			ContentBadDecorator.Paint(BaseX, BaseY, 200, 100, HorizontalAlign.Left);
		          			break;
		          	}
	          	}
        	}

		public void MonitoringResource(Object source, System.Timers.ElapsedEventArgs e)
        	{
        	     if (Hud.Render.GetUiElement("Root.NormalLayer.BattleNetCampaign_main.LayoutRoot.Menu.PlayGameButton").Visible)	// Game Menu
        		{
        			doFlag = false;
        			try {
					MonitorTimer.Enabled = false;
				}
				catch {}
			}
			
			int CpuUse = (int)(Cpu.NextValue());
			int RamUse = (int)(Ram.NextValue());
			int aLatency = (int)Hud.Game.AverageLatency;
			int cLatency = (int)Hud.Game.CurrentLatency;

        		process.Start();
        		process.StandardInput.Write("nvidia-smi --query-gpu=utilization.gpu --format=csv" + Environment.NewLine);
			process.StandardInput.Close();
			string result = process.StandardOutput.ReadToEnd();
			bool Success;
			int GpuUse = 0;
			var GpuText = string.Empty;
			Match match = Regex.Match(result, @"(\d){1,}(?= %)");
			if (match.Success)
			{
				Success = Int32.TryParse(match.Value, out GpuUse);
				if (Success && GpuUse > 0 && GpuUse <= 100)
					GpuText = "Gpu Usuage : " + Convert.ToString(GpuUse) + " %" + Environment.NewLine;
			}

			int CheckedFPS = 0;
			string FPSText = string.Empty;
			switch (FrameRateKind)
			{
				case 1:
					CheckedFPS = FrameRate;
					FPSText = "Frame_TClip: " + Convert.ToString(FrameRate);
					break;
				case 2:
					CheckedFPS = FrameRateTop;
					FPSText = "Frame_Top  : " + Convert.ToString(FrameRateTop);
					break;
				case 3:
					CheckedFPS = FrameRateWorld;
					FPSText = "Frame_World: " + Convert.ToString(FrameRateWorld);
					break;
				case 4:
					CheckedFPS = FrameRate;
					FPSText = "Frame_All  : " + Convert.ToString(FrameRate) + "/" + Convert.ToString(FrameRateTop) + "/" + Convert.ToString(FrameRateWorld);
					break;					
			}
			FPSText += " FPS";

			if (cLatency > LatencyWarning || CpuUse > CpuWarning || GpuUse > GpuWarning || RamUse < RamWarning || CheckedFPS < FPSWarning)
				displayTag = 1;	// warning
			else if (cLatency > LatencyBad || CpuUse > CpuBad || GpuUse > GpuBad || RamUse < RamBad || CheckedFPS < FPSBad)
			{
				Console.Beep(800, 200);	// Alarm if Bad state
				displayTag = 2;	// bad
			} else
				displayTag = 0;	// ok
							
			if (CheckedFPS < FPSBad && cLatency < LatencyWarning && CpuUse < CpuWarning && RamUse > RamWarning)
			{
				if (FPSWarningCnt++ > 5 && Hud.Sound.LastSpeak.TimerTest(4000))
				{
					FPSWarningCnt = 0;
					Console.Beep(800, 200);
					Hud.Sound.Speak("Low FPS warning! Check your background application and close it. particularly a web page!"); // "FPS가 비정상적으로 낮습니다. 백그라운드 프로그램을 확인하시고 필요시 종료하세요. 특히 웹페이지 조심!"
				}
			}
			var CpuText = "Cpu Usuage : " + Convert.ToString(CpuUse) + " %";
			var RamText = "Usable Ram : " + Convert.ToString(RamUse) + " MB";
			var LatencyText = "C/A Latency: " + Convert.ToString(cLatency) + "/" + Convert.ToString(aLatency) + " ms";
			MonitoredResource = CpuText + Environment.NewLine + GpuText + RamText + Environment.NewLine + LatencyText + Environment.NewLine + FPSText;
		}

		public static int CalculateFrameRate()
		{
			if (System.Environment.TickCount - lastTick >= RMInterval)		// count frames for a second
		     {
		     		lastFrameRate = frameRate;
		     		frameRate = 0;
		     		lastTick = System.Environment.TickCount;
		    	}
		     frameRate++;
		     return lastFrameRate;
		}

		public static int CalculateFrameRateTop()
		{
			if (System.Environment.TickCount - lastTickTop >= RMInterval)		// count frames for a second
		     {
		     		lastFrameRateTop = frameRateTop;
		     		frameRateTop = 0;
		     		lastTickTop = System.Environment.TickCount;
		    	}
		     frameRateTop++;
		     return lastFrameRateTop;
		}
		
		public static int CalculateFrameRateWorld()
		{
			if (System.Environment.TickCount - lastTickWorld >= RMInterval)		// count frames for a second
		     {
		     		lastFrameRateWorld = frameRateWorld;
		     		frameRateWorld = 0;
		     		lastTickWorld = System.Environment.TickCount;
		    	}
		     frameRateWorld++;
		     return lastFrameRateWorld;
		}

        	public void OnKeyEvent(IKeyEvent keyEvent)
        	{
            	if (Hud.Input.IsKeyDown(Keys.NumPad5))
            	{
            		Console.Beep(250, 150);
				doFlag = !doFlag;	// toggle display

				if (doFlag)
				{
					displayTag = 0;

					// Text decoration every interval (default 1 sec)
		            	MonitorTimer = new System.Timers.Timer();
					MonitorTimer.Interval = RMInterval;
					MonitorTimer.Elapsed += MonitoringResource;
					MonitorTimer.AutoReset = true;
					MonitorTimer.Enabled = true;
				} else
				{
					try {
						MonitorTimer.Enabled = false;
					}
					catch {}
				}
            	}
	   	}
	}
}