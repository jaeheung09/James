// 디아3인벤 시즌파티모집 계시판에서 찾고자 하는 단어가 나타나면 해당 광고 내용을 알려주고 Numpad4를 누르면 광고 내용과 배틀태그를 list 형태로 보여주며 선택하면 해당 배택을 클립보드에 자동 복사하여 친추시 ctrl_v만 누르면 배택이 자동 복사됨
// Alarm on finding the filtered words(or conditions) on a website bulletin board for a want ad of party matching and auto clipboard copy of the BattleTag so that "Add friend" can be done easily

using System;
using System.Linq;
using Turbo.Plugins.Default;
using System.Windows.Forms;
using SharpDX.DirectInput;
using System.Drawing;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Media;
using System.Net;

namespace Turbo.Plugins.James
{
    public class PartyMatchingWebsiteMonitorPlugin : BasePlugin, IKeyEventHandler
    {
        // 아래 세 개의 url 중에서 본인의 원하는 것만 사용하고 나머지는 코멘트 처리하시면 됩니다. (시즌 이외는 상세 확인은 안 해봤지만 문제 있으면 알려주세요.)
        private string WebsiteUrl = "http://www.inven.co.kr/board/diablo3/4622?category=%EB%AA%A8%EC%A7%91%EC%A4%91"; // 인벤디아3 시즌파티모집[모집중]
        //private string WebsiteUrl = "http://www.inven.co.kr/board/diablo3/4738?category=%EB%AA%A8%EC%A7%91%EC%A4%91"; // 스텐파티모집
	   //private string WebsiteUrl = "http://www.inven.co.kr/board/diablo3/4623";	//하드코어 파티 모집
	   private string[] ChatWatchListAnd = new string[5];
	   private string[] ChatWatchListOr = new string[5];
	   private string[] WebBBList = new string[3];
	   private string[] WebHREF = new string[3];
	   private string[,] WebAds = new string[3, 2];		// (광고내용, 배틀태그) * 3개
	   private bool InputOK;
	   private string savedValue;
	   private string oldValue;
	   private int ChatPopupNo;
	   private SoundPlayer ChatFind = new SoundPlayer();
	   private WebClient webClient = new WebClient();
	   private static System.Timers.Timer WebBBSearchTimer;
	   private static System.Timers.Timer ClickTimer;
	   private int WebBBearchInterval = 7000;		//5초마다 인벤 검색
	   private string BaTag;
	   private bool InputChanged;

        public PartyMatchingWebsiteMonitorPlugin()
        {
            Enabled = true;
            ChatFind.SoundLocation = "D:/Game/TurboD3/sounds/notification_1.wav";	// sound when finding conditions on chat
            ChatFind.LoadAsync();
            BaTag = string.Empty;
        }

        public override void Load(IController hud)
        {
            base.Load(hud);
            InputOK = false;
            InputChanged = false;
            ChatPopupNo = 0;
            oldValue = string.Empty;
            webClient.Encoding = System.Text.Encoding.UTF8;
        }

        public void WebBBListSearch(Object source, System.Timers.ElapsedEventArgs e)
        {
			if (!InputOK) return;

			for (int i = 0; i < 2; i++)
			{
				WebBBList[i] = string.Empty;
			}
			string WebBBStr = webClient.DownloadString(WebsiteUrl);
			string filteredStr = string.Empty;

			Match match = Regex.Match(WebBBStr, @"(?<='bbsNo'>).+(?=</TD><)");	// 모집 내용이 추가 되었는지 확인
			if (match.Success)
			{
				if (match.Value == oldValue)
				{
					if (!InputChanged)		// 계시판 내용이 안 바꼈어도 검색 단어가 바뀌면 계시판 다시 검색
						return;
					else
						InputChanged = false;	
				} else
					oldValue = match.Value;
			} else
			{
				Console.Beep(1000, 300);
				return;		//exception
			}
			
			// 모집 글 내용 3개 추출 : 3개 이상은 시간이 지나서 별 의미가 없음
			for (int i = 0; i < 3; i++)		// 3 matchings
			{
				if (i == 0)
					match = Regex.Match(WebBBStr, @"(?<=bbsNo.+;\[).+(?=</A)");
				else
					match = match.NextMatch();
				if (match.Success)
				{
					WebBBList[i] = "[" + Regex.Replace(match.Value, @"&nbsp;&nbsp;", string.Empty).Trim();
				} else
					break;
			}

			// 모집 글 연결 HREF 3개 추출 (조건에 맞는 광고글의 배택이 들어있는 하위 웹 페이지 주소)
			for (int i = 0; i < 3; i++)
			{
				if (i == 0)
					match = Regex.Match(WebBBStr, @"(?<=HREF="").+(?="">&nbsp;)");
				else
					match = match.NextMatch();

				if (match.Success)
				{
					WebHREF[i] = match.Value;
				} else
					break;
			}

			if (WebBBList[0] == string.Empty) return;

			bool found = false;
			var cnt = 0;
			foreach (string chatLine in WebBBList)
			{
				if (ChatWatchListAnd[0] != string.Empty)
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
				     if (ChatWatchListOr[0] != string.Empty)
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
					ChatPopupNo++;
					if (ChatPopupNo > 3) ChatPopupNo = 1;
					var pTitle = "인벤 시즌파티 모집";
					var pDuration = WebBBearchInterval;	
					var tmp = chatLine.Trim();
					WebAds[cnt, 0] = tmp;
					GetBattleTag(cnt);

					Hud.RunOnPlugin<PopupMsgPlugin>(plugin =>
	                	{
			          		switch(ChatPopupNo)
			          		{
			          			case(1):
								plugin.Show(tmp, pTitle, pDuration, "", PopupMsgPlugin.EnumPopupDecoratorToUse.WebBB1);
								break;
			          			case(2):
								plugin.Show(tmp, pTitle, pDuration, "", PopupMsgPlugin.EnumPopupDecoratorToUse.WebBB2);
								break;
			          			case(3):
								plugin.Show(tmp, pTitle, pDuration, "", PopupMsgPlugin.EnumPopupDecoratorToUse.WebBB3);
								break;
						}
	                     });
					ChatFind.PlaySync();
					if (Hud.Sound.LastSpeak.TimerTest(3000))
						Hud.Sound.Speak("인벤 시즌 파티 모집 확인!");		// Words show up on the chat box

					found = false;
				}
				cnt++;
			}
	     }

		// 인벤 시즌파티찾기 계시판 광고글이 조건에 맞으면 실제 올린 사람의 배택이 들어있는 하위 웹페이지로 들어가서 배택을 가져옴
		public void GetBattleTag(int index)
		{
			string WebBBStr = webClient.DownloadString(WebHREF[index]);
			Match match = Regex.Match(WebBBStr, @"(?<=""description"" content="").+\d{4,}");	// BattleTag 추출
			if (match.Success)
			{
				if (match.Value.Length < 20)
					WebAds[index, 1] = Regex.Replace(match.Value, @" ", string.Empty).Trim();
			} else
			{
				Console.Beep(1000, 300);
				return;		//exception
			}
		}

		public DialogResult listView_Doit(string title, string content)
		{
			Form form = new Form();
			Label label = new Label();
			ListView listView = new ListView();
			form.ClientSize = new Size(490, 190);
			listView.Bounds = new Rectangle(new Point(20,40), new Size(450,100));
			listView.View = View.Details;
			Button buttonOk = new Button();
		     Button buttonCancel = new Button();
			form.StartPosition = FormStartPosition.CenterScreen;
			form.MaximizeBox = false;
			form.MinimizeBox = false;
			form.TopMost = true;
			form.FormBorderStyle = FormBorderStyle.FixedDialog;
			form.AcceptButton = buttonOk;
		     form.CancelButton = buttonCancel;

			form.Text = title;
			label.Text = content;
			buttonOk.Text = "OK";
		     buttonCancel.Text = "Cancel";
		     buttonOk.DialogResult = DialogResult.OK;
		     buttonCancel.DialogResult = DialogResult.Cancel;

		     label.SetBounds(20, 17, 310, 20);	//(int x, int y, int width, int height);
		     buttonOk.SetBounds(20, 155, 200, 20);
		     buttonCancel.SetBounds(270, 155, 200, 20);

			listView.Name = "인벤 파티 모집글";
			// Select the item and subitems when selection is made.
			listView.FullRowSelect = true;
			form.Controls.AddRange(new Control[] { label, buttonOk, buttonCancel, listView });

			listView.BeginUpdate();

			//Creat columns:
			 ColumnHeader column1 = new ColumnHeader();
			 column1.Text = ">>> 파티 모집 광고 내용 <<<";
			 column1.Width = 300;
			 column1.TextAlign = HorizontalAlignment.Left;

			 ColumnHeader column2 = new ColumnHeader();
			 column2.Text = ">>> BattleTag <<<";
			 column2.Width = 150;
			 column2.TextAlign = HorizontalAlignment.Left;
			 //Add columns to the ListView:
			listView.Columns.Add(column1);
			listView.Columns.Add(column2);

			// 조건에 맞는 광고글 내용과 그 사람이 올린 배택을 list로 보여줌
			for (int i = 0; i <= WebAds.GetUpperBound(0); i++)
		     {
				if (WebAds[i, 0] != string.Empty)
				{
					listView.Items.Add(WebAds[i, 0]);
					listView.Items[i].SubItems.Add(WebAds[i, 1]);
				} else
				{
					
					WebAds[i, 1] = string.Empty;
				}	
			}

			listView.EndUpdate();
			listView.SelectedIndexChanged += new System.EventHandler(listView_SelectedIndexChanged);

			DialogResult dialogResult = form.ShowDialog();
		     return dialogResult;
		}

		private void listView_SelectedIndexChanged(object sender, EventArgs e)
		{
			ListView listView = (ListView) sender;

			if(listView.SelectedItems.Count == 0)
			    return;

			BaTag = listView.SelectedItems[0].SubItems[1].Text;
		}

	     public void DoClick(Object source, System.Timers.ElapsedEventArgs e)
	     {
               Cursor.Position = new Point(Hud.Window.Size.Width / 2, Hud.Window.Size.Height / 2 - 30);
	          Process.Start("D:\\Game\\click.exe");
	     }

         public void OnKeyEvent(IKeyEvent keyEvent)
         {
            if (Hud.Input.IsKeyDown(Keys.NumPad2))
            {
			string value = string.Empty;
			string output = string.Empty;
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

			if(InputBox("인벤 파티 모집 검색어", "Or : comma/space, And : ( Or )", ref value) == DialogResult.OK)
			{
				Console.Beep(200, 120);
			     string sep = ", ";
			     value = value.Trim();
			     if (value == string.Empty)
			     {
			     		InputOK = false;
			     		try {
						WebBBSearchTimer.Enabled = false;
						WebBBSearchTimer.AutoReset = false;
					}
					catch {}
			     		return;
			     } else if (savedValue != value)
			     {
			     		InputChanged = true;
				}
				
			     savedValue = value;
			     Match match = Regex.Match(savedValue, @"(?<=\().+(?=\))");		// extract "And" condition words
			     if (match.Success)
				{
					ChatWatchListAnd = match.Value.Split(sep.ToCharArray());
					output = Regex.Replace(value, @"\(.+\) ", string.Empty);	// delete And condition for Or processing
				} else
					output = value;

			     ChatWatchListOr = output.Split(sep.ToCharArray());
			     InputOK = true;

				WebBBSearchTimer = new System.Timers.Timer();
				WebBBSearchTimer.Interval = WebBBearchInterval;		// Search Web bulletin boards every 5 secs
				WebBBSearchTimer.Elapsed += WebBBListSearch;
				WebBBSearchTimer.AutoReset = true;
				WebBBSearchTimer.Enabled = true;
			 }
             }
            if (Hud.Input.IsKeyDown(Keys.NumPad4))
            {
            	ClickTimer = new System.Timers.Timer();
			ClickTimer.Interval = 50;
			ClickTimer.Elapsed += DoClick;
			ClickTimer.AutoReset = false;
			ClickTimer.Enabled = true;

            	if(listView_Doit("인벤 시즌파티 모집", "선택: 광고자 배틀태그 클립보드에 복사(친추 때 ctrl_v)") == DialogResult.OK)
            	{
            		Clipboard.SetText(BaTag);
            		Hud.Sound.Speak("해당 배틀테그가 클립보드에 복사되었습니다!");
            	}
            }
          }

		public DialogResult InputBox(string title, string content, ref string value)
		{
		    Form form = new Form();
		    Label label = new Label();
		    TextBox textBox = new TextBox();
		    Button buttonOk = new Button();
		    Button buttonCancel = new Button();

		    form.ClientSize = new Size(250, 100);		// 250, 100
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