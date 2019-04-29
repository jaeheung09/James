// Chat Monitor : you can set chat monitoring words and get alarms(sound, popup messages)
// for monitoring words, every word must be splitted by space or comma and belongs to Or conditions. You can set an And condition using (), which can be use together with Or condition words
using System;
using Turbo.Plugins.Default;
using System.Windows.Forms;
using SharpDX.DirectInput;
using System.Drawing;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Media;

namespace Turbo.Plugins.James
{
    public class ChatMonitorPlugin : BasePlugin, IKeyEventHandler, IChatLineChangedHandler
    {
	   private string[] ChatWatchListAnd = new string[5];
	   private string[] ChatWatchListOr = new string[5];
	   private string[,] regExps = new string[5, 2] { {"^\\d.+(?=\\[\\|H)", ""}, {"]\\|h", "]"}, {"]\\|H.*\\|h", "]"}, {"\\|H.*\\d\\|h", ""}, {"\\|h", ""} };	// replace internal chat messages with user friendly ones
	   private bool InputOK;
	   private string savedValue;
	   private static System.Timers.Timer ClickTimer;
	   private SoundPlayer ChatFind = new SoundPlayer();
	   private string whisperId = "±Ó¼Ó¸»:";	// replace it with your word, which may be "whisper:"

        public ChatMonitorPlugin()
        {
            Enabled = true;
            ChatFind.SoundLocation = "D:/Game/TurboD3/sounds/notification_1.wav";	// sound when finding conditions on chat
            ChatFind.LoadAsync();
        }

        public override void Load(IController hud)
        {
            base.Load(hud);
            InputOK = false;
        }

        public void OnChatLineChanged(string currentLine, string previousLine)
        {
			if (string.IsNullOrEmpty(currentLine)) return;

			//Hud.TextLog.Log("Chat",currentLine, true, true);	// for testing
			
			if (!InputOK) return;

			bool found = false;
			string chatLine = currentLine;

			if (ChatWatchListAnd[0] != "")
			{
				foreach (string x in ChatWatchListAnd)
				{
				    if (chatLine.Contains(x))
				    // if (chatLine.ToLower().Contains(x))
				    {
				    		found = true;
				    } else
				    {
				    		found = false;
				    		break;
				    }
				}
			}

			if (!found)
			{
			     if (ChatWatchListOr[0] != "")
			     {
					foreach (string x in ChatWatchListOr)
					{
					    if (chatLine.Contains(x))
					    // if (chatLine.ToLower().Contains(x))
					    {
					        found = true;
					        break;
					    }
					}
				}
			}

			if (found)
			{
				string output = chatLine;
				string re1, re2;
		          for (int i = 0; i <= regExps.GetUpperBound(0); i++)
		          {
		          		re1 = regExps[i, 0];
		            	re2 = regExps[i, 1];
					output = Regex.Replace(output, re1, re2);
				}

				var pTitle = "°¨½ÃµÈ Ã¤ÆÃ ³»¿ë";		// Chat filtered
				var pDuration = 10000;		// 10 secs
				Hud.RunOnPlugin<PopupMsgPlugin>(plugin =>
                	{
					plugin.Show(output, pTitle, pDuration, "", PopupMsgPlugin.EnumPopupDecoratorToUse.Chat1);
                     });
				//Console.Beep(900, 500);
				ChatFind.PlaySync();
				Hud.Sound.Speak("Ã¤ÆÃÃ¢¿¡ Ã£´Â ´Ü¾î µîÀå!");		// Words show up on the chat box
			}
	   }

	   public void DoClick(Object source, System.Timers.ElapsedEventArgs e)
	   {
               Cursor.Position = new Point(Hud.Window.Size.Width / 2, Hud.Window.Size.Height / 2 - 30);
	          Process.Start("D:\\Game\\click.exe");
	   }
	   
        public void OnKeyEvent(IKeyEvent keyEvent)
        {
            if (Hud.Input.IsKeyDown(Keys.NumPad1))
            {
			string value = "";
			string output = "";
			for (int i = 0; i < ChatWatchListOr.Length; i++ )
			{
				ChatWatchListOr[i] = string.Empty;
			}
			for (int i = 0; i < ChatWatchListAnd.Length; i++ )
			{
				ChatWatchListAnd[i] = string.Empty;
			}
			if (InputOK)
				value = savedValue;

			ClickTimer = new System.Timers.Timer();
			ClickTimer.Interval = 50;
			ClickTimer.Elapsed += DoClick;
			ClickTimer.AutoReset = false;
			ClickTimer.Enabled = true;

               //var CursorPos = (Hud.Window.Size.Width / 2).ToString("0") + "," + (Hud.Window.Size.Height / 2 - 30).ToString("0");
	          //Process.Start("D:\\Game\\click.exe", CursorPos);

			if(InputBox("Ã¤ÆÃ Ã¢ ¸ð´ÏÅÍ", "Or : comma/space, And : ( Or )", ref value) == DialogResult.OK)
			{
				Console.Beep(200, 120);
			     string sep = ", ";
			     value = value.Trim();
			     if (value == string.Empty)
			     {
			     		InputOK = false;
			     		return;
			     }

			     savedValue = value;
			     Match match = Regex.Match(savedValue, @"(?<=\().+(?=\))");		// extract "And" condition words
			     if (match.Success)
				{
					ChatWatchListAnd = match.Value.Split(sep.ToCharArray());
					for (int i = 0; i < ChatWatchListAnd.Length; i++ )
			     		{
						if (ChatWatchListAnd[i].Contains("/w"))
				     		{
				     			// |HOnlUserHdl:27e1a45-4433-3|h[±èÀçÈÆ]|h ´ÔÀÇ ±Ó¼Ó¸»: ³Ü
				     			ChatWatchListAnd[i] = whisperId;		// "±Ó¼Ó¸»:"
				     		}
					}
					output = Regex.Replace(value, @"\(.+\) ", "");	// delete And condition for Or processing
				} else
					output = value;

			     ChatWatchListOr = output.Split(sep.ToCharArray());
			     for (int i = 0; i < ChatWatchListOr.Length; i++ )
			     {
					if (ChatWatchListOr[i].Contains("/w"))
				     {
				     		ChatWatchListOr[i] = whisperId;		// "±Ó¼Ó¸»:" 
				     }
				}
			     InputOK = true;
			}
            }
        }

		public static DialogResult InputBox(string title, string content, ref string value)
		{
		    Form form = new Form();
		    Label label = new Label();
		    TextBox textBox = new TextBox();
		    Button buttonOk = new Button();
		    Button buttonCancel = new Button();

		    form.ClientSize = new Size(250, 100);
		    form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
		    form.FormBorderStyle = FormBorderStyle.FixedDialog;
		    form.StartPosition = FormStartPosition.CenterScreen;
		    form.MaximizeBox = false;
		    form.MinimizeBox = false;
		    form.TopMost = true;
		    form.AcceptButton = buttonOk;
		    form.CancelButton = buttonCancel;

		    form.Text = title;
		    label.Text = content;
		    textBox.Text = value;
		    buttonOk.Text = "OK";
		    buttonCancel.Text = "Cancel";

		    buttonOk.DialogResult = DialogResult.OK;
		    buttonCancel.DialogResult = DialogResult.Cancel;

		    label.SetBounds(20, 17, 210, 20);	//(int x, int y, int width, int height);
		    textBox.SetBounds(20, 40, 210, 20);
		    buttonOk.SetBounds(20, 70, 90, 20);
		    buttonCancel.SetBounds(140, 70, 90, 20);

		    DialogResult dialogResult = form.ShowDialog();
		    value = textBox.Text;

		    return dialogResult;
		}
   }
}