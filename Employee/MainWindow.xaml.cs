﻿using LogoWindow;
using MahApps.Metro.Controls;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Employee
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private Cursor m_normal = null;
        private Cursor m_leftDown = null;
        private static string SDCC_FW_TRANSMISSION_MODE_KEY => "SDCCFwTransmissionMode";
        private static string LAST_QUERY_NAME_KEY => "LastQueryName";
        private static string PROXY_ADDRESS = "13.187.0.55";
        private static int PROXY_PORT = 8000;

        public MainWindow()
        {
            InitializeComponent();

            if (Profile.GetBoolean(SDCC_FW_TRANSMISSION_MODE_KEY).GetValueOrDefault()) {
                Downloader.SetProxy(PROXY_ADDRESS, PROXY_PORT);
            }
            txtName.Text = Profile.GetString(LAST_QUERY_NAME_KEY) ?? string.Empty;
            string version = FileVersionInfo.GetVersionInfo(System.Windows.Forms.Application.ExecutablePath).ProductVersion;
            string onlineVersion = Downloader.GetHtml("http://www.huangyuanlei.com/api/checkinout/?version=");
            if (!string.IsNullOrEmpty(onlineVersion)) {
                onlineVersion = onlineVersion.Replace("\"", string.Empty);
                if (version.CompareTo(onlineVersion) < 0) {
                    Update();
                }
            }

            YangyangWindow yangyang = new YangyangWindow();
            yangyang.ShowDialog();
            m_normal = new Cursor(new MemoryStream(Properties.Resources.Normal));
            m_leftDown = new Cursor(new MemoryStream(Properties.Resources.LeftDown));
            MouseMove += UserControl_MouseMove;
            PreviewMouseLeftButtonDown += UserControl_PreviewMouseLeftButtonDown;
            PreviewMouseLeftButtonUp += UserControl_PreviewMouseLeftButtonUp;
        }

        private void Update()
        {
            string tmpNew = Path.GetTempFileName();
            string tmpBat = Path.GetTempFileName();
            File.Delete(tmpNew);
            File.Delete(tmpBat);
            tmpBat += ".bat";
            if (Downloader.DownloadFile("http://www.huangyuanlei.com/employee.zip", tmpNew)) {
                string unzippedDirectory = Zip.Unzip(tmpNew);
                File.Delete(tmpNew);
                tmpNew = string.Format(@"{0}\Employee.exe", unzippedDirectory);
                if (File.Exists(tmpNew)) {
                    StringBuilder stringBuilder = new StringBuilder();
                    stringBuilder.AppendLine(string.Format("TASKKILL /F /IM:employee.exe"));
                    stringBuilder.AppendLine(string.Format("PING 127.0.0.1"));
                    stringBuilder.AppendLine(string.Format("COPY \"{0}\" \"{1}\"", tmpNew, System.Windows.Forms.Application.ExecutablePath));
                    stringBuilder.AppendLine(string.Format("DEL \"{0}\"", tmpNew));
                    stringBuilder.AppendLine(string.Format("DEL \"{0}\"", tmpBat));
                    File.WriteAllText(tmpBat, stringBuilder.ToString());
                    Process.Start("cmd.exe", string.Format("/c {0}", tmpBat)).WaitForExit();
                }
            }
        }

        private void UserControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) {
                if (!ReferenceEquals(Cursor, m_leftDown)) {
                    Cursor = m_leftDown;
                }
            } else if (e.LeftButton == MouseButtonState.Released) {
                if (!ReferenceEquals(Cursor, m_normal)) {
                    Cursor = m_normal;
                }
            }

            e.Handled = true;
        }

        private void UserControl_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Cursor = m_leftDown;
        }

        private void UserControl_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!ReferenceEquals(Cursor, m_normal)) {
                Cursor = m_normal;
            }
        }

        private void btnQuery_Click(object sender, RoutedEventArgs e)
        {
            ExecuteQuery();
        }

        private void ExecuteQuery()
        {
            string name = txtName.Text;
            if (!string.IsNullOrEmpty(name)) {
                btnQuery.IsEnabled = false;
                lvwEmployee.ItemsSource = null;
                ibBackground.Opacity = 0.3;
                ring.Visibility = Visibility.Visible;
                Task.Factory.StartNew(() => {
                    string url = string.Format("http://www.huangyuanlei.com/api/checkinout/?name={0}", HttpUtility.UrlEncode(name));
                    string html = Downloader.GetHtml(url);
                    Json<Checkinout> json = new Json<Checkinout>();
                    json.ParseArray(html);
                    Hashtable hashtable = new Hashtable();
                    foreach (Checkinout checkinout in json.ToArray()) {
                        string key = DateTime.Parse(checkinout.Checkin).ToString("yyyyMMdd");
                        if (hashtable.Contains(key)) {
                            Checkinout tmp = hashtable[key] as Checkinout;
                            if (tmp.Checkin.CompareTo(checkinout.Checkin) > 0) {
                                tmp.Checkin = checkinout.Checkin;
                            }
                            if (tmp.Checkout.CompareTo(checkinout.Checkout) < 0) {
                                tmp.Checkout = checkinout.Checkout;
                            }
                        } else {
                            hashtable[key] = checkinout;
                        }
                    }

                    ObservableCollection<Checkinout> inoutDesc = new ObservableCollection<Checkinout>();
                    string maxKey = null;
                    while (hashtable.Count > 0) {
                        foreach (DictionaryEntry entry in hashtable) {
                            if (maxKey == null) {
                                maxKey = entry.Key as string;
                            } else {
                                if (entry.Key is string key) {
                                    if (maxKey.CompareTo(key) < 0) {
                                        maxKey = key;
                                    }
                                }
                            }
                        }
                        inoutDesc.Add(hashtable[maxKey] as Checkinout);
                        hashtable.Remove(maxKey);
                        maxKey = null;
                    }

                    ObservableCollection<Checkinout> inoutAsc = new ObservableCollection<Checkinout>();
                    for (int i = inoutDesc.Count - 1; i >= 0; i--) {
                        inoutAsc.Add(inoutDesc[i]);
                    }

                    string prevMonth = null;
                    Checkinout prevCheckinout = null;
                    double totalFreeTime = 0;
                    foreach (Checkinout checkinout in inoutAsc) {
                        string month = DateTime.Parse(checkinout.Checkin).ToString("yyyyMM");
                        if (!month.Equals(prevMonth)) {
                            totalFreeTime = 0;
                            prevMonth = month;
                            if (prevCheckinout != null) {
                                prevCheckinout.IsLastWorkDayOfMonth = true;
                            }
                        }
                        totalFreeTime += checkinout.FreeTime.TotalSeconds;
                        checkinout.TotalFreeTime = TimeSpan.FromSeconds(totalFreeTime);
                        prevCheckinout = checkinout;
                    }
                    if (inoutDesc.Count > 0) {
                        Profile.SetString(LAST_QUERY_NAME_KEY, name);
                        txtStandby.Dispatcher.Invoke(() => {
                            ring.Visibility = Visibility.Hidden;
                            txtStandby.Visibility = Visibility.Visible;
                        });
                    }
                    Thread.Sleep(500);
                    lvwEmployee.Dispatcher.Invoke(() => {
                        lvwEmployee.ItemsSource = inoutDesc;
                        btnQuery.IsEnabled = true;
                        ring.Visibility = Visibility.Hidden;
                        txtStandby.Visibility = Visibility.Hidden;
                        ibBackground.Opacity = 0.1;
                    });
                });
            }
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            txtName.Focus();
        }

        private void txtName_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key) {
                case Key.Enter: { ExecuteQuery(); } break;
            }
        }

        private void chkFWTran_Checked(object sender, RoutedEventArgs e)
        {
            Downloader.SetProxy(PROXY_ADDRESS, PROXY_PORT);
            Profile.SetBoolean(SDCC_FW_TRANSMISSION_MODE_KEY, true);
        }

        private void chkFWTran_Unchecked(object sender, RoutedEventArgs e)
        {
            Downloader.ClearPorxy();
            Profile.SetBoolean(SDCC_FW_TRANSMISSION_MODE_KEY, false);
        }
    }
}