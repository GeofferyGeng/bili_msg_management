using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing;
using System.IO.IsolatedStorage;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using System.Threading;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Configuration;
using System.Text.RegularExpressions;

namespace bili_msg_management
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<string> _messageQueue = new ObservableCollection<string>();
        public static string COOKIE { set; get; }
        public static string CSRF { set; get; }
        public MainWindow()
        {
            InitializeComponent();
            log.DataContext = _messageQueue;

    

        }

        private void get_bili_msg_click(object sender, RoutedEventArgs e)
        {
            try
            {
                string c = bcookietxt.Text;
                Dictionary<string, string> ccc = GetRequestParameters(c);
                CSRF = ccc["bili_jct"];
                COOKIE = bcookietxt.Text;
                logging("get cookie successfully!");
            }
            catch (Exception)
            {
                logging("get cookie failed！");
            }
            if (CSRF != null)
            {
                get_bili_info();
                get_bili_msg();
            }

        }

        private static Dictionary<string, string> GetRequestParameters(string row)
        {
            if (string.IsNullOrEmpty(row)) return null;
            var kvs = Regex.Split(row, ";");
            if (kvs == null || kvs.Count() <= 0) return null;

            return kvs.ToDictionary(e => Regex.Split(e, "=")[0].Replace(" ",""), e => Regex.Split(e, "=")[1].Replace(" ", ""));
        }

        /// <summary>
        ///  copy function
        /// </summary>
        TextBlock tbCell = null;
        private void ListView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbCell = e.OriginalSource as TextBlock;
        }
        private void OnCopy(object sender, System.Windows.RoutedEventArgs e)
        {
            if (tbCell != null)
                Clipboard.SetText(tbCell.Text);
        }


        /// <summary>
        /// msgObject 
        /// </summary>
        class msgObject
        {
            public int num { set; get; }
            public int uid { set; get; }
            public string stat { set; get; }
            public string uname { set; get; }
            public string isfollower { set; get; }
            public string utime { set; get; }
            public string msg { set; get; }
            public msgObject(int num, string stat, int uid, string uname, string isfollower, string utime, string msg)
            {
                this.num = num;
                this.stat = stat;
                this.uid = uid;
                this.uname = uname;
                this.isfollower = isfollower;
                this.utime = utime;
                this.msg = msg;
            }
        }


        /// <summary>
        ///  ViewList
        /// </summary>
        List<string> ls = new List<string>();//存放选中项
        List<string> unreadls = new List<string>();//存放未读选中项
        List<string> alllist = new List<string>();//存放未读选中项
        Dictionary<string, msgObject> msgdictionary = new Dictionary<string, msgObject>();//存放所有消息

        private void cbxxxx_Clicked(object sender, RoutedEventArgs e)
        {

            CheckBox cb_Selected = sender as CheckBox;
            string strTag = cb_Selected.Tag.ToString();
            if (cb_Selected.IsChecked == true)
            {
                ls.Add(strTag);
                logging(strTag + "is checked!");
            }
            else if (cb_Selected.IsChecked == false)
            {
                ls.Remove(strTag);
                logging(strTag + "is unchecked!");
            }
            
        }




        /// <summary>
        /// get info
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void get_bili_info()
        {
            string url1 = "https://api.bilibili.com/x/space/myinfo";
            string resq1 = GetHttpResponse(url1, 1000);
            JObject obj1 = JObject.Parse(resq1);
            string uname = obj1["data"]["name"].ToString();
            tid.Text = uname;
            string picurl = obj1["data"]["face"].ToString();

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(picurl);
            WebResponse response = request.GetResponse();
            System.Drawing.Image face1 = System.Drawing.Image.FromStream(response.GetResponseStream());
            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(face1);
            IntPtr hBitmap = bmp.GetHbitmap();
            System.Windows.Media.ImageSource WpfBitmap = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            face.Source = WpfBitmap;


            string url2 = "https://api.bilibili.com/x/web-interface/nav/stat";
            string resq2 = GetHttpResponse(url2, 1000);
            JObject obj2 = JObject.Parse(resq2);
            string flnum = obj2["data"]["following"].ToString();
            tfl.Text = flnum;
            string fansnum = obj2["data"]["follower"].ToString();
            tfans.Text = fansnum;

            string url3 = "https://api.vc.bilibili.com/session_svr/v1/session_svr/single_unread?unread_type=0&build=0&mobi_app=web";
            string resq3 = GetHttpResponse(url3, 1000);
            JObject obj3 = JObject.Parse(resq3);
            string msgnum = ((int)obj3["data"]["follow_unread"] + (int)obj3["data"]["unfollow_unread"]).ToString();
            tmsgn.Text = msgnum;

            logging("name:"+ uname);
            logging("following:"+flnum);
            logging("follower:"+fansnum);
            logging("unread msg:" + msgnum);

        }

        
        /// <summary>
        /// get messages
        /// </summary>
        private void get_bili_msg()
        {
            msglistview.Items.Clear();

            int unreadcount=0;
            TimeSpan tsnow = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            string endtime = Convert.ToInt64(tsnow.TotalMilliseconds).ToString()+"000";

            string url3 = "https://api.vc.bilibili.com/session_svr/v1/session_svr/single_unread?unread_type=0&build=0&mobi_app=web";
            string resq3 = GetHttpResponse(url3, 1000);
            JObject obj3 = JObject.Parse(resq3);
            int msgnum = (int)obj3["data"]["follow_unread"] + (int)obj3["data"]["unfollow_unread"];

            bool iscontinue = true;


            int j = 0;
            bool b1 = true;
            if (advanunreadmsg.IsChecked == true) b1 = unreadcount != msgnum;
            else b1 = true;
            while (b1 && iscontinue)
            {
                string urlmsg = "https://api.vc.bilibili.com/session_svr/v1/session_svr/get_sessions?session_type=1&group_fold=1&unfollow_fold=" + Convert.ToInt32(advanfanmsg.IsChecked).ToString() + "&sort_rule=2&end_ts=" + endtime;
                string resq1 = GetHttpResponse(urlmsg, 1000);
                JObject obj1 = JObject.Parse(resq1);
                string sessionlist = obj1["data"]["session_list"].ToString();
                int sessionlen = obj1["data"]["session_list"].Count();

                string allid = "";
                for (int i = 0; i < sessionlen; i++)
                {
                    string talkid = obj1["data"]["session_list"][i]["talker_id"].ToString();
                    allid += talkid;
                    allid += ",";
                }
                string unameurl = "https://api.vc.bilibili.com/account/v1/user/infos?uids=" + allid.Substring(0, allid.Length - 1);
                string resq2 = GetHttpResponseWithoutCookie(unameurl, 1000);
                JObject obj2 = JObject.Parse(resq2);
                Dictionary<int, string> unamels = new Dictionary<int, string>();
                for (int i = 0; i < sessionlen; i++)
                {
                    unamels.Add(int.Parse(obj2["data"][i]["mid"].ToString()), obj2["data"][i]["uname"].ToString());
                }

                for (int i = 0; i < sessionlen; i++)
                {
                    int number = j * 20 + i + 1;
                    string stat = obj1["data"]["session_list"][i]["unread_count"].ToString().Replace("0", "");
                    int talkid = int.Parse(obj1["data"]["session_list"][i]["talker_id"].ToString());
                    if (Convert.ToBoolean(int.Parse(obj1["data"]["session_list"][i]["unread_count"].ToString()))) { unreadcount += 1; unreadls.Add(talkid.ToString()); }
                    string uname = unamels[talkid];
                    string isfollower = Convert.ToBoolean(int.Parse(obj1["data"]["session_list"][i]["is_follow"].ToString())).ToString().Replace("False", "");
                    string jsTimeStamp = obj1["data"]["session_list"][i]["session_ts"].ToString();
                    System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1)); // 当地时区
                    DateTime dt = startTime.AddMilliseconds(long.Parse(jsTimeStamp.Substring(0, 13)));
                    string dtime = dt.ToString("yyyy-MM-dd HH:mm:ss");
                    //string content = obj1["data"]["session_list"][i]["last_msg"]["content"].ToString().Replace("{\"content\":\"", "").Replace("{ \"content\": \"", "").Replace("\\\\", "\\");
                    //string conconten = content.Substring(0, content.Length - 2);
                    string content = obj1["data"]["session_list"][i]["last_msg"]["content"].ToString();
                    JObject obj22 = JObject.Parse(content);
                    string conconten;
                    try
                    {
                        conconten = obj22["content"].ToString().Replace("\n", "");
                    }
                    catch (System.NullReferenceException)
                    {
                        conconten = content.Replace("\n", "");
                    }


                    bool isoutput = true;
                    if (advantimeset.IsChecked == true)
                    {
                        if (startdata.SelectedDate != null && enddata.SelectedDate != null)
                        {
                            TimeSpan st = (TimeSpan)(startdata.SelectedDate - new DateTime(1970, 1, 1, 0, 0, 0, 0));
                            string sst = Convert.ToInt64(st.TotalMilliseconds).ToString() + "000";
                            TimeSpan et = (TimeSpan)(enddata.SelectedDate - new DateTime(1970, 1, 1, 0, 0, 0, 0));
                            string eet = Convert.ToInt64(et.TotalMilliseconds).ToString() + "000";
                            if (Convert.ToInt64(jsTimeStamp) < Convert.ToInt64(sst) || Convert.ToInt64(jsTimeStamp) > Convert.ToInt64(eet)) isoutput = false;
                        }
                    }
                    if (advanfanmsg.IsChecked == true)
                    {
                        if (isfollower != "True") isoutput = false;
                    }
                    if (advanunreadmsg.IsChecked == true)
                    {
                        string unr = Convert.ToBoolean(int.Parse(obj1["data"]["session_list"][i]["unread_count"].ToString())).ToString();
                        if (unr != "True") isoutput = false;
                    }
                    if (isoutput)
                    {
                        msglistview.Items.Add(new msgObject(number, stat, talkid, uname, isfollower, dtime, conconten));
                        msgdictionary[talkid.ToString()] = new msgObject(number, stat, talkid, uname, isfollower, dtime, conconten);
                        alllist.Add(talkid.ToString());
                        logging("load " + talkid.ToString() + " " + uname);
                    }

                    endtime = jsTimeStamp;

                }
                j += 1;
                iscontinue = Convert.ToBoolean(int.Parse(obj1["data"]["has_more"].ToString()));
                System.Threading.Thread.Sleep(20);
            }

        }

        //get方法
        public static string GetHttpResponse(string url, int Timeout)
        {

            string cookie = COOKIE;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.ContentType = "text/html;charset=UTF-8";
            request.Headers["cookie"] = cookie;
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.132 Safari/537.36";

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();
            return retString;
        }
        //public static string PostHttpResponse(string url, Dictionary<string, string> parameters, int Timeout)
        //{

        //    string cookie = COOKIE;
        //    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        //    request.Method = "POST";
        //    request.ContentType = "text/html;charset=UTF-8";
        //    request.Headers["cookie"] = cookie;
        //    request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.132 Safari/537.36";
            

        //    if (!(parameters == null || parameters.Count == 0))
        //    {
        //        StringBuilder buffer = new StringBuilder();
        //        int i = 0;
        //        foreach (string key in parameters.Keys)
        //        {
        //            if (i > 0)
        //            {
        //                buffer.AppendFormat("&{0}={1}", key, parameters[key]);
        //            }
        //            else
        //            {
        //                buffer.AppendFormat("{0}={1}", key, parameters[key]);
        //            }
        //            i++;
        //        }
        //        byte[] data = Encoding.UTF8.GetBytes(buffer.ToString());
        //        using (Stream reqStream = request.GetRequestStream())
        //        {
        //            reqStream.Write(data, 0, data.Length);
        //        }
        //    }

        //    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        //    Stream myResponseStream = response.GetResponseStream();
        //    StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
        //    string retString = myStreamReader.ReadToEnd();
        //    myStreamReader.Close();
        //    myResponseStream.Close();
        //    return retString;
        //}
        public static string GetHttpResponseWithoutCookie(string url, int Timeout)
        {
            //string cookie = COOKIE;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.ContentType = "text/html;charset=UTF-8";
            //request.Headers["cookie"] = cookie;
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.132 Safari/537.36";

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();
            return retString;
        }

        /// <summary>
        /// show message detail
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void show_msg_detail(object sender, RoutedEventArgs e)
        {
            Button cb_Selected = sender as Button;
            string strTag = cb_Selected.Tag.ToString();
            string sessionurl = "https://api.vc.bilibili.com/svr_sync/v1/svr_sync/fetch_session_msgs?talker_id="+ strTag + "&session_type=1&size=20";
            string resq1 = GetHttpResponse(sessionurl, 1000);
            JObject obj1 = JObject.Parse(resq1);
            int msgnum = obj1["data"]["messages"].Count();


            string allid = obj1["data"]["messages"][0]["receiver_id"].ToString()+","+ obj1["data"]["messages"][0]["sender_uid"].ToString();
            string unameurl = "https://api.vc.bilibili.com/account/v1/user/infos?uids=" + allid;
            string resq2 = GetHttpResponseWithoutCookie(unameurl, 1000);
            JObject obj2 = JObject.Parse(resq2);
            Dictionary<int, string> unamels = new Dictionary<int, string>();
            for (int i = 0; i < 2; i++)
            {
                unamels.Add(int.Parse(obj2["data"][i]["mid"].ToString()), obj2["data"][i]["uname"].ToString());
            }

            string display = "";
            for (int i = 0; i < msgnum; i++)
            {
                string jsTimeStamp = obj1["data"]["messages"][msgnum-1-i]["timestamp"].ToString();
                System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1)); // 当地时区
                DateTime dt = startTime.AddSeconds(long.Parse(jsTimeStamp));
                string dtime = dt.ToString("yyyy-MM-dd HH:mm:ss");

                display += unamels[int.Parse(obj1["data"]["messages"][msgnum - 1 - i]["sender_uid"].ToString())]+ "-->"+ unamels[int.Parse(obj1["data"]["messages"][msgnum - 1 - i]["receiver_id"].ToString())]+"   " + dtime + "\n";

                string mmsg = obj1["data"]["messages"][msgnum - 1 - i]["content"].ToString();
                string conconten;
                try
                {
                    JObject obj11 = JObject.Parse(mmsg);
                    conconten = obj11["content"].ToString();
                }
                catch(Exception)
                {
                    conconten = mmsg;
                }
                display += conconten;
                display += "\n===========================================\n";

            }
            msgdetail msgwindow = new msgdetail(display);
            msgwindow.Show();
        }

        private void set_checked_read(object sender, RoutedEventArgs e)
        {
            int loopi = ls.Count();
            string url = "https://api.vc.bilibili.com/session_svr/v1/session_svr/update_ack?talker_id=33271826&session_type=1&ack_seqno=1&build=0&mobi_app=web&csrf_token=f3a30df5edad95806cd2059b2c24805b&csrf=f3a30df5edad95806cd2059b2c24805b";
            for (int i = 0; i < loopi; i++)
            {
                string geturl = "https://api.vc.bilibili.com/session_svr/v1/session_svr/update_ack?";
                Dictionary<string, string> pdata = new Dictionary<string, string>();
                pdata["talker_id"] = ls[i];
                pdata["session_type"] = "1";
                pdata["ack_seqno"] = "1";
                pdata["csrf_token"] = CSRF;
                pdata["csrf"] = CSRF;
                pdata["build"] = "0";
                pdata["mobi_app"] = "web";
                string ppp = "";
                int ii = 0;
                foreach (string key in pdata.Keys)
                {
                    if (ii > 0)
                    {
                        ppp += "&";
                        ppp += key;
                        ppp += "=";
                        ppp += pdata[key];
                    }
                    else
                    {
                        ppp += key;
                        ppp += "=";
                        ppp += pdata[key];
                    }
                    ii++;
                }
                geturl += ppp;
                string resq1 = GetHttpResponse(geturl, 1000);
                JObject obj1 = JObject.Parse(resq1);
                if (obj1["code"].ToString() != "0") {MessageBox.Show("Something is wrong with No." + ls[i]); logging("Something is wrong with No." + ls[i]); }
                else logging(ls[i]+"had been read");
                
            }

           }
        private void set_unread_read(object sender, RoutedEventArgs e)
        {
            int loopi = unreadls.Count();
            string url = "https://api.vc.bilibili.com/session_svr/v1/session_svr/update_ack?talker_id=33271826&session_type=1&ack_seqno=1&build=0&mobi_app=web&csrf_token=f3a30df5edad95806cd2059b2c24805b&csrf=f3a30df5edad95806cd2059b2c24805b";
            for (int i = 0; i < loopi; i++)
            {
                string geturl = "https://api.vc.bilibili.com/session_svr/v1/session_svr/update_ack?";
                Dictionary<string, string> pdata = new Dictionary<string, string>();
                pdata["talker_id"] = unreadls[i];
                pdata["session_type"] = "1";
                pdata["ack_seqno"] = "1";
                pdata["csrf_token"] = CSRF;
                pdata["csrf"] = CSRF;
                pdata["build"] = "0";
                pdata["mobi_app"] = "web";
                string ppp = "";
                int ii = 0;
                foreach (string key in pdata.Keys)
                {
                    if (ii > 0)
                    {
                        ppp += "&";
                        ppp += key;
                        ppp += "=";
                        ppp += pdata[key];
                    }
                    else
                    {
                        ppp += key;
                        ppp += "=";
                        ppp += pdata[key];
                    }
                    ii++;
                }
                geturl += ppp;
                string resq1 = GetHttpResponse(geturl, 1000);
                JObject obj1 = JObject.Parse(resq1);
                if (obj1["code"].ToString() != "0") { MessageBox.Show("Something is wrong with No." + unreadls[i]); logging("Something is wrong with No." + unreadls[i]); }
                else logging(unreadls[i] + "had been read");
            }
        }

        private void show_info_author(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("本工具由@文无华制作，在Github开源 https://github.com/GeofferyGeng/bili_msg_management； \n 版本：0.1.1  2021.06.26 ","Information");
        }

        private void output_checked(object sender, RoutedEventArgs e)
        {
            if (ls.Count == 0)
            {
                return;
            }
            //check save file exist
            string cren = Environment.CurrentDirectory;
            string fullPath = System.IO.Path.Combine(cren,"导出" + ".txt");
            FileInfo fi = new FileInfo(fullPath);
            if (!fi.Directory.Exists)
            {
                fi.Directory.Create();
            }
            using (var outfile = new StreamWriter(System.IO.Path.Combine(cren, "导出" + ".txt"), true))
            {
                outfile.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            for (int i = 0; i < ls.Count; i++)
            {
                string uid = msgdictionary[ls[i]].uid.ToString();
                string uname = msgdictionary[ls[i]].uname.ToString();
                string msg = msgdictionary[ls[i]].msg.ToString();
                string utime = msgdictionary[ls[i]].utime.ToString();
                using (var outfile = new StreamWriter(System.IO.Path.Combine(cren, "导出"  + ".txt"), true))
                {
                    outfile.WriteLine(uid+"   "+uname+ "   "+utime + "   "+msg);
                    outfile.WriteLine("---------------------------");
                }
                logging("output "+ls[i] + " successfully");
            }
            

        }
        private void output_all(object sender, RoutedEventArgs e)
        {
            if (msgdictionary.Count == 0)
            {
                return;
            }
            //check save file exist
            string cren = Environment.CurrentDirectory;
            string fullPath = System.IO.Path.Combine(cren, "导出" + ".txt");
            FileInfo fi = new FileInfo(fullPath);
            if (!fi.Directory.Exists)
            {
                fi.Directory.Create();
            }
            using (var outfile = new StreamWriter(System.IO.Path.Combine(cren, "导出" + ".txt"), true))
            {
                outfile.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            for (int i = 0; i < msgdictionary.Count; i++)
            {
                string uid = msgdictionary[alllist[i]].uid.ToString();
                string uname = msgdictionary[alllist[i]].uname.ToString();
                string msg = msgdictionary[alllist[i]].msg.ToString();
                string utime = msgdictionary[alllist[i]].utime.ToString();
                using (var outfile = new StreamWriter(System.IO.Path.Combine(cren, "导出" + ".txt"), true))
                {
                    outfile.WriteLine(uid + "   " + uname + "   " + utime + "   " + msg);
                    outfile.WriteLine("---------------------------");
                }
                logging("output " + alllist[i] + " successfully");
            }
        }


        public void logging(string text)
        {
            if (log.Dispatcher.CheckAccess())
            {

                lock (_messageQueue)
                {
                    if (_messageQueue.Count >= 100)
                    {
                        _messageQueue.RemoveAt(0);
                    }

                    _messageQueue.Add(DateTime.Now.ToString("T") + " : " + text);


                }
                if (true)
                {

                    try
                    {
                        //var path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                        //path = System.IO.Path.Combine(path, "log");
                        //Directory.CreateDirectory(path);
                        //string datatime = DateTime.Now.ToString("T");
                        //using (
                        //    var outfile =
                        //        new StreamWriter(System.IO.Path.Combine(path, DateTime.Now.ToString("yyyy-MM-dd") + ".txt"), true)
                        //)
                        //{
                        //    outfile.WriteLine(datatime + " : " + text);
                        //}

                        log.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " : " + text + Environment.NewLine);
                        if (IsVerticalScrollBarAtBottom)
                        {
                            this.log.ScrollToEnd();
                        }
                    }
                    catch (Exception)
                    {
                        log.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " : Something is wrong! " + Environment.NewLine);
                    }
                }
            }
            else
            {
                //log.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => logging(text)));
            }
        }
        public bool IsVerticalScrollBarAtBottom
        {
            get
            {
                bool atBottom = false;

                this.log.Dispatcher.Invoke((Action)delegate
                {

                    double dVer = this.log.VerticalOffset;       //获取竖直滚动条滚动位置
                    double dViewport = this.log.ViewportHeight;  //获取竖直可滚动内容高度
                    double dExtent = this.log.ExtentHeight;      //获取可视区域的高度

                    if (dVer + dViewport >= dExtent)
                    {
                        atBottom = true;
                    }
                    else
                    {
                        atBottom = false;
                    }
                });

                return atBottom;
            }
        }

    }
}
