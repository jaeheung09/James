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
	   private int ChatPopupNo;
	   private SoundPlayer ChatFind = new SoundPlayer();
	   	   	   
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
            ChatPopupNo = 0;
        }

        public void OnChatLineChanged(string currentLine, string previousLine)
        {
			if (string.IsNullOrEmpty(currentLine)) return;
			
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

				ChatPopupNo++;
				if (ChatPopupNo > 3) ChatPopupNo = 1;
				var pTitle = "Censored Chat";		// 검열된 채팅 글
				var pDuration = 10000;		// 10 secs
				Hud.RunOnPlugin<PopupMsgPlugin>(plugin =>
                	{
		          		switch(ChatPopupNo)
		          		{
		          			case(1):
							plugin.Show(output, pTitle, pDuration, "", PopupMsgPlugin.EnumPopupDecoratorToUse.Chat1);
							break;
		          			case(2):
							plugin.Show(output, pTitle, pDuration, "", PopupMsgPlugin.EnumPopupDecoratorToUse.Chat2);
							break;
		          			case(3):
							plugin.Show(output, pTitle, pDuration, "", PopupMsgPlugin.EnumPopupDecoratorToUse.Chat3);
							break;
					}
                     });
				//Console.Beep(900, 500);
				ChatFind.PlaySync();
				Hud.Sound.Speak("Words you're looking for!");		// 채팅에 검열 단어 등장
			}
	   }

        public void OnKeyEvent(IKeyEvent keyEvent)
        {
            if (Hud.Input.IsKeyDown(Keys.NumPad1))
            {
               var CursorPos = (Hud.Window.Size.Width / 2).ToString("0") + "," + (Hud.Window.Size.Height / 2 - 30).ToString("0");
	          Process.Start("D:\\Game\\click.exe", CursorPos);

			string value = "";
			string output = "";
			if (InputOK)
				value = savedValue;

			if(InputBox("Chat Monitor", "Or : comma/space, And : ( Or )", ref value) == DialogResult.OK)
			{
			     string sep = ", ";
			     value = value.Trim();	// delete first/last blanks
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
					ChatWatchListAnd[i] = "OnlUserHd";		// whisper id = 귓속말
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
				     		ChatWatchListOr[i] = "OnlUserHd";		// whisper id = 귓속말
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
