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
    public class TurboHudGameEnvMonitorPlugin : BasePlugin, IKeyEventHandler, IInGameTopPainter
    {
    		private PerformanceCounter Cpu = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        	private PerformanceCounter Ram = new PerformanceCounter("Memory", "Available MBytes");
        	private ProcessStartInfo cmd = new ProcessStartInfo();
		private Process process = new Process();
        	private static int lastTick;
        	private static int lastFrameRate;
        	private static int frameRate;
        	private bool doFlag;
        	private static System.Timers.Timer MonitorTimer;
        	private TopLabelDecorator ContentOKDecorator { get; set; }
        	private TopLabelDecorator ContentWarningDecorator { get; set; }
        	private TopLabelDecorator ContentBadDecorator { get; set; }
        	private string MonitoredResource;
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
		private const int mInterval = 1000; 		// Resources monitoring interval 1 sec
        	//*********************************************************************************************************
		private int BaseX;
		private int BaseY;
		private int displayTag;	// ok, warning, bad
		private int FrameRate;
		public TopLabelWithTitleDecorator PlayerDecorator { get; set; }
		// private enum display { ok, warning, bad }

		public TurboHudGameEnvMonitorPlugin()
        	{
        		Enabled = true;
        	}

        	public override void Load(IController hud)
        	{
            	base.Load(hud);

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

        	public void PaintTopInGame(ClipState clipState)
        	{
        		if (clipState != ClipState.AfterClip) return;		// without this check, turboHUD generates frams more than VSync cap.
        		if (!doFlag)	return;		// toggled by the hotkey

        		FrameRate = CalculateFrameRate();	// count the frames for each layer while HUD is rendering the UI

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

		public void MonitoringResource(Object source, System.Timers.ElapsedEventArgs e)
        	{
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
				if (Success && GpuUse > 0 && GpuUse < 100)
					GpuText = "Gpu Usuage : " + Convert.ToString(GpuUse) + " %";
			}

			if (cLatency > LatencyWarning || CpuUse > CpuWarning || GpuUse > GpuWarning || RamUse < RamWarning || FrameRate < FPSWarning)
				displayTag = 1;	// warning
			else if (cLatency > LatencyBad || CpuUse > CpuBad || GpuUse > GpuBad || RamUse < RamBad || FrameRate < FPSBad)
			{
				Console.Beep(800, 200);	// Alarm if Bad state
				displayTag = 2;	// bad
			} else
				displayTag = 0;	// ok
				
			var CpuText = "Cpu Usuage : " + Convert.ToString(CpuUse) + " %";
			var RamText = "Usable Ram : " + Convert.ToString(RamUse) + " MB";
			var LatencyText = "C/A Latency: " + Convert.ToString(cLatency) + "/" + Convert.ToString(aLatency) + " ms";
			//var FPSText =      Convert.ToString(FrameRate) + "/" + Convert.ToString(FrameRate) + " FPS";
			var FPSText =      "FrameRate  : " + Convert.ToString(FrameRate) + " FPS";
			MonitoredResource = CpuText + Environment.NewLine + GpuText + Environment.NewLine + RamText + Environment.NewLine + LatencyText + Environment.NewLine + FPSText;
		}

		public static int CalculateFrameRate()
		{
			if (System.Environment.TickCount - lastTick >= mInterval)		// count frames for a second
		     {
		     		lastFrameRate = frameRate;
		     		frameRate = 0;
		     		lastTick = System.Environment.TickCount;
		    	}
		     frameRate++;
		     return lastFrameRate;
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
					MonitorTimer.Interval = mInterval;
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