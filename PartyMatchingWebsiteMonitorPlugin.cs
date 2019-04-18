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
using System.Collections;
using System.Threading;

namespace Turbo.Plugins.James
{
    public class PartyMatchingWebsiteMonitorPlugin : BasePlugin, IKeyEventHandler
    {
        // 아래 세 개의 url 중에서 본인의 원하는 것만 사용하고 나머지는 코멘트 처리하시면 됩니다. (시즌 이외는 상세 확인은 안 해봤지만 문제 있으면 알려주세요.)
        private string WebsiteUrl { get; set; }
        //private string WebsiteUrl = "http://www.inven.co.kr/board/diablo3/4738?category=%EB%AA%A8%EC%A7%91%EC%A4%91"; // 스텐파티모집
	   //private string WebsiteUrl = "http://www.inven.co.kr/board/diablo3/4623";	//하드코어 파티 모집
	   private string Url_party = "http://www.inven.co.kr/board/diablo3/4622?category=%EB%AA%A8%EC%A7%91%EC%A4%91"; // 인벤디아3 시즌파티모집[모집중]
	   private string Url_bus = "http://www.inven.co.kr/board/diablo3/3018?come_idx=3018&category=%EB%B2%84%EC%8A%A4(%EC%8B%9C%EC%A6%8C)"; // 시즌 버스모집
	   private string[] ChatWatchListAnd = new string[5];
	   private string[] ChatWatchListOr = new string[5];
	   private string[] WebBBList = new string[3];		// 인벤 모집 광고 내용
	   private string[] WebDate = new string[3];		// 광고 포스팅 시간
	   private string[] WebHREF = new string[3];		// 광고자 배택이 들어있는 웹페이지 주소
	   private string[,] WebAds = new string[3, 3];		// (광고내용, 올린 시간, 배틀태그) * 3개 - 3개 이상은 현실적으로 사용되지 않음
	   private bool InputOK;
	   private string savedValue;
	   private string oldValue;
	   private int ChatPopupNo;
	   private SoundPlayer ChatFind = new SoundPlayer();
	   private WebClient webClient = new WebClient();
	   private static System.Timers.Timer WebBBSearchTimer;
	   private static System.Timers.Timer ClickTimer;
	   private int WebBBSearchInterval = 7000;		//Intervval 시간 마다 인벤 검색
	   private string BaTag;
	   private bool InputChanged;
	   private int checkedrButton = 1;

        public PartyMatchingWebsiteMonitorPlugin()
        {
            Enabled = true;
            ChatFind.SoundLocation = "D:/Game/TurboD3/sounds/notification_1.wav";	// sound when finding conditions on the website
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
            WebsiteUrl = Url_party;
            webClient.Encoding = System.Text.Encoding.UTF8;
            // ListView 내용 초기화
            for (int i = 0; i < WebAds.GetLength(0); i++)
		  {
		  	for (int j = 0; j < WebAds.GetLength(1); j++)
			{
				WebAds[i, j] = string.Empty;
			}
		  }
        }

        public void WebBBListSearch(Object source, System.Timers.ElapsedEventArgs e)
        {
			if (!InputOK) return;

			WebBBSearchTimer.Interval = WebBBSearchInterval;
			for (int i = 0; i < WebBBList.GetLength(0); i++)
			{
				WebBBList[i] = string.Empty;
			}
			string WebBBStr = webClient.DownloadString(WebsiteUrl);
			string filteredStr = string.Empty;

			Match match = Regex.Match(WebBBStr, @"(?<='bbsNo'>).+(?=</TD><)");	// 모집 내용이 추가 되었는지 페이지 가장 첫 bbsNo로 확인
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

			// 모집 글 내용 3개 추출 : 현재는 3개. 그 이상은 광고 시간이 지나서 별 의미가 없음
			for (int i = 0; i <  WebBBList.GetLength(0); i++)
			{
				if (i == 0)
					match = Regex.Match(WebBBStr, @"(?<=bbsNo.+\]).+(?=</A)");
				else
					match = match.NextMatch();
				if (match.Success)
				{
					WebBBList[i] = Regex.Replace(match.Value, @"&nbsp;&nbsp;", string.Empty).Trim();
				} else
					break;
			}

			// 모집 글 Date 추출
			for (int i = 0; i <  WebDate.GetLength(0); i++)
			{
				if (i == 0)
					match = Regex.Match(WebBBStr, @"(?<='date'>).+(?=&nbsp;)");
				else
					match = match.NextMatch();

				if (match.Success)
				{
					WebDate[i] = match.Value;
				} else
					break;
			}

			// 모집 글 연결 HREF(웹 주소) 추출 (조건에 맞는 광고글의 배택이 들어있는 하위 웹 페이지 주소)
			for (int i = 0; i <  WebBBList.GetLength(0); i++)
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
			// 인벤 모집 광고글이 유저가 입력한 검색 조건에 부합한지 확인하는 작업
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
					var pTitle = "인벤 D3 시즌파티/버스 모집";
					var pDuration = WebBBSearchInterval;
					var tmp = chatLine.Trim() + "("+WebDate[cnt]+")";
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
						Hud.Sound.Speak("인벤 시즌 파티나 버스 모집 확인!");		// Words show up on the chat box

					found = false;
				}
				cnt++;
			}
	     }

		// 인벤 시즌파티찾기 계시판 광고글이 조건에 맞으면 실제 올린 사람의 배택이 들어있는 하위 웹페이지로 들어가서 배택을 가져옴
		public void GetBattleTag(int Aindex)
		{
			string WebBBStr = webClient.DownloadString(WebHREF[Aindex]);
			Match match = Regex.Match(WebBBStr, @"(?<=""description"" content="").+\d{4,}");	// BattleTag 추출
			if (match.Success)
			{
				//Console.Beep(200, 120);
				string output = Regex.Replace(match.Value, @".+\.", string.Empty);
				output = Regex.Replace(output, @" ", string.Empty).Trim();
				WebAds[Aindex, 1] = SubstringReverse(output, 1, 20);
			} else
			{
				match = Regex.Match(WebBBStr, @"(?<=""description"" content="").+(?="" />)");	// BattleTag가 없으면 내용 추출
				if (match.Success)
				{
					WebAds[Aindex, 1] = match.Value.Substring(20);	// 배택이 없으면 내용 중 첫 20자 보여줌
				} else
					Console.Beep(1000, 300);		// exception
			}					
			return;
		}

		public static string SubstringReverse(string str, int reverseIndex, int length)
		{
		    return string.Join("", str.Reverse().Skip(reverseIndex - 1).Take(length).Reverse());
		}
		
		// 인벤 모집광고 검색 결과 보여주는 ListView 폼 및 내용 작성
		public DialogResult listView_Doit(string title, string content)
		{
			var ForceReturn = true;
			for (int i = 0; i < WebAds.GetLength(0); i++)
			{
				if (WebAds[i, 0] != string.Empty)
				{
					ForceReturn = false;
					break;
				}
			}
			if (ForceReturn)
			{
				Hud.Sound.Speak("보여줄 광고 내용이 없습니다!");
				Console.Beep(500, 250);
				return DialogResult.Cancel;
			}
			Form form = new Form();
			Label label = new Label();
			ListView listView = new ListView();
			form.ClientSize = new Size(490, 190);
			listView.Bounds = new Rectangle(new Point(20,40), new Size(450,100));
			//listView.Sorting = SortOrder.Descending;
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

		     label.SetBounds(20, 17, 450, 20);	//(int x, int y, int width, int height);
		     buttonOk.SetBounds(20, 155, 200, 20);
		     buttonCancel.SetBounds(270, 155, 200, 20);

			listView.Name = "인벤 D3 파티/버스 모집글 검색 결과";
			// Select the item and subitems when selection is made.
			listView.FullRowSelect = true;
			listView.BackColor = Color.Black;
			listView.ForeColor = Color.FromArgb(0, 255, 0);
			listView.View = View.Details;
			//listView.Font = new Font("Arial", 10, FontStyle.Bold);
			form.Controls.AddRange(new Control[] { label, buttonOk, buttonCancel, listView });

			listView.BeginUpdate();

			 //Add columns to the ListView:
			listView.Columns.Add(">>> 시즌 파티/버스 모집 광고 [모집중] <<<", 300, HorizontalAlignment.Center);
			listView.Columns.Add(">>> BattleTag <<<", 150, HorizontalAlignment.Center);
			listView.OwnerDraw = false;
			// 보여줄 인벤 모집광고 내용 정렬 및 중복 제거
			int loopcnt = WebAds.GetLength(0)-1;
			try {
			for (int i = 0; i < WebAds.GetLength(0)-1; i++)
	          {
		          for (int j = 0; j < loopcnt; j++)
		          {
		          		int c = string.Compare(WebAds[j, 0], WebAds[j+1, 0]);
		               if (c == -1)	// 전자가 후자보다 작으면
		               {
						string temp1 = WebAds[j, 0];
						string temp2 = WebAds[j, 1];
						WebAds[j, 0] = WebAds[j+1, 0];
						WebAds[j, 1] = WebAds[j+1, 1];
						WebAds[j+1, 0] = temp1;
						WebAds[j+1, 1] = temp2;
		               } if (c == 0)		// 비교 대상이 같으면
		               {
		               		c = string.Compare(WebAds[j, 1], WebAds[j+1, 1]);
		               		if (c == 0)
		               		{
		               			WebAds[j+1, 0] = string.Empty;
	                	     		WebAds[j+1, 1] = string.Empty;
						}
		               }
		          }
		          loopcnt--;
		          if (loopcnt < 1) break;
		      }
		      }
		      catch {}
		      
			// 조건에 맞는 광고글 내용 및 올린 시간과 올린 사람 배택을 listView에 item 추가
			for (int i = 0; i <= WebAds.GetUpperBound(0); i++)	// WebAds.GetLength(0)와 같음
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
            if (Hud.Input.IsKeyDown(Keys.NumPad2))	// 인벤 모집 광고 검색 조건 입력 창 호출
            {
			string value = string.Empty;
			string output = string.Empty;
			// And 및 Or 검색 조건 변수 초기화
			for (int i = 0; i < ChatWatchListOr.Length; i++ )
			{
				ChatWatchListOr[i] = string.Empty;
			}
			for (int i = 0; i < ChatWatchListAnd.Length; i++ )
			{
				ChatWatchListAnd[i] = string.Empty;
			}
			if (InputOK)	// 이 전에 검색 입력을 한 상태라면
				value = savedValue;

			// 일정 시간 후 폼 자동 활성화
			ClickTimer = new System.Timers.Timer();
			ClickTimer.Interval = 50;
			ClickTimer.Elapsed += DoClick;
			ClickTimer.AutoReset = false;
			ClickTimer.Enabled = true;

			if(InputBox("인벤 D3 모집 광고 검색어", "Or : comma/space, And : ( Or )", ref value) == DialogResult.OK)
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
					for (int i = 0; i < WebAds.GetLength(0); i++) // 입력 내용이 바뀌면 인벤 모집광고 내용 및 배택 Array 초기화
		         		{
			        		WebAds[i, 0] = string.Empty;
		                	WebAds[i, 1] = string.Empty;
					}
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

				// 인벤 파티모집글 페이지를 일정 주기로 계속 모니터함
				WebBBSearchTimer = new System.Timers.Timer();
				WebBBSearchTimer.Interval = 1000;		// first in 1 sec and then search Web bulletin boards every WebBBSearchInterval
				WebBBSearchTimer.Elapsed += WebBBListSearch;
				WebBBSearchTimer.AutoReset = true;
				WebBBSearchTimer.Enabled = true;
			 }
             }
            if (Hud.Input.IsKeyDown(Keys.NumPad4))	// 인벤 광고 내용 및 배택 보여주는 ListView 호출
            {
            	ClickTimer = new System.Timers.Timer();
			ClickTimer.Interval = 50;
			ClickTimer.Elapsed += DoClick;
			ClickTimer.AutoReset = false;
			ClickTimer.Enabled = true;

            	if(listView_Doit("인벤 시즌파티/버스 모집", "선택하면 광고자 배틀태그 클립보드에 자동 복사(친추 때 ctrl_v)") == DialogResult.OK)
            	{
            		Clipboard.SetText(BaTag);
            		// 자동으로 친구 추가 페이지로 이동 및 배틀 태그 자동 복사해 넣기
            		SendKeys.SendWait("+(i)");	// 친구 창 단축키
			Thread.Sleep(500);
            		Cursor.Position = new Point(1560, 900);	//친구 창 화면에서 "친구 추구" 버튼의 위치
	          	Process.Start("D:\\Game\\click.exe");	// Just click the button
	          	Thread.Sleep(500);						// 0.7초 쉬어라
	          	SendKeys.SendWait("^(v)");				// 클립보드 내용을 빈칸에 복사해 넣어라
            		Hud.Sound.Speak("친구 추가 요청을 보내세요!");
            		//Hud.Sound.Speak("해당 배틀테그가 클립보드에 복사되었습니다!");
            	}
            }
          }

		// 인벤 모집광고 검색 조건 입력 폼
		public DialogResult InputBox(string title, string content, ref string value)
		{
		    Form form = new Form();
		    Label label = new Label();
		    GroupBox groupBox = new GroupBox();
		    RadioButton radioButton1 = new RadioButton();
		    RadioButton radioButton2 = new RadioButton();
		    TextBox textBox = new TextBox();
		    Button buttonOk = new Button();
		    Button buttonCancel = new Button();

		    form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel, radioButton1, radioButton2, groupBox });
		    form.FormBorderStyle = FormBorderStyle.FixedDialog;
		    form.StartPosition = FormStartPosition.CenterScreen;
		    form.MaximizeBox = false;
		    form.MinimizeBox = false;
		    form.TopMost = true;
		    form.AcceptButton = buttonOk;
		    form.CancelButton = buttonCancel;

    		    groupBox.Text = "모니터링할 인벤 웹 페이지";

		    radioButton1.Text = "시즌파티";
		    radioButton1.Name = "1";
		    radioButton1.CheckedChanged += new EventHandler(radioButton_CheckedChanged);
		    
		    radioButton2.Text = "시즌버스";
		    radioButton2.Name = "2";
		    radioButton2.CheckedChanged += new EventHandler(radioButton_CheckedChanged);
		    
		    if (checkedrButton == 1)
		    		radioButton1.Checked = true;
		    else
		    		radioButton2.Checked = true;
		    
		    form.Text = title;
		    label.Text = content;
		    textBox.Text = value;
		    buttonOk.Text = "OK";
		    buttonCancel.Text = "Cancel";

		    buttonOk.DialogResult = DialogResult.OK;
		    buttonCancel.DialogResult = DialogResult.Cancel;

		    form.ClientSize = new Size(250, 170);		// 250, 100
		    groupBox.Location = new Point(20, 17);
		    groupBox.Size = new Size(200, 50);
		    radioButton1.Location = new Point(30, 37);
		    radioButton1.Size = new Size(90, 20);		    
		    radioButton2.Location = new Point(140, 37);
		    radioButton2.Size = new Size(90, 20);		    
		    label.SetBounds(20, 75, 210, 20);	//(int x, int y, int width, int height);
		    textBox.SetBounds(20, 100, 210, 20);
		    buttonOk.SetBounds(20, 130, 90, 20);
		    buttonCancel.SetBounds(140, 130, 90, 20);

		    DialogResult dialogResult = form.ShowDialog();
		    value = textBox.Text;

		    return dialogResult;
		}
		
		public void radioButton_CheckedChanged(object sender, EventArgs e)
		{
		    RadioButton rbutton = sender as RadioButton;
		
		    if (rbutton == null)
		    {
		        Console.Beep(900, 200);	// exception
		        return;
		    }
		
		    if (rbutton.Checked)
		    {
		        if (rbutton.Name == "1")
		        {
		        	WebsiteUrl = Url_party;
		        	checkedrButton = 1;
		        } else
		        {
		        	WebsiteUrl = Url_bus;
		        	checkedrButton = 2;
		        }
		    }
		}
   }
}
