using LogoWindow;
using MahApps.Metro.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Employee {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow {
        private Cursor m_normal = null;
        private Cursor m_leftDown = null;
        private static string SDCC_FW_TRANSMISSION_MODE_KEY => "SDCCFwTransmissionMode";
        private static string LAST_QUERY_NAME_KEY => "LastQueryName";
        private static string PROXY_ADDRESS = "13.187.0.55";
        private static int PROXY_PORT = 8000;

        public MainWindow() {
            InitializeComponent();

            if (Profile.GetBoolean(SDCC_FW_TRANSMISSION_MODE_KEY).GetValueOrDefault()) {
                Downloader.SetProxy(PROXY_ADDRESS, PROXY_PORT);
                chkFWTran.IsChecked = true;
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

        private void Update() {
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
                    stringBuilder.AppendLine(string.Format("COPY \"{0}\" \"{1}\" /Y", tmpNew, System.Windows.Forms.Application.ExecutablePath));
                    stringBuilder.AppendLine(string.Format("DEL \"{0}\"", tmpNew));
                    stringBuilder.AppendLine(string.Format("DEL \"{0}\"", tmpBat));
                    File.WriteAllText(tmpBat, stringBuilder.ToString());
                    Process.Start("cmd.exe", string.Format("/c {0}", tmpBat)).WaitForExit();
                }
            }
        }

        private void UserControl_MouseMove(object sender, MouseEventArgs e) {
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

        private void UserControl_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            Cursor = m_leftDown;
        }

        private void UserControl_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            if (!ReferenceEquals(Cursor, m_normal)) {
                Cursor = m_normal;
            }
        }

        private void btnQuery_Click(object sender, RoutedEventArgs e) {
            ExecuteQuery();
        }

        private void ExecuteQuery() {
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
                    string maxDateKey = null;
                    while (hashtable.Count > 0) {
                        foreach (DictionaryEntry entry in hashtable) {
                            if (maxDateKey == null) {
                                maxDateKey = entry.Key as string;
                            } else {
                                if (entry.Key is string key) {
                                    if (maxDateKey.CompareTo(key) < 0) {
                                        maxDateKey = key;
                                    }
                                }
                            }
                        }
                        inoutDesc.Add(hashtable[maxDateKey] as Checkinout);
                        hashtable.Remove(maxDateKey);
                        maxDateKey = null;
                    }
                    if (inoutDesc.Count > 0) {
                        inoutDesc[0].IsLastWorkDayOfMonth = true;
                    }

                    ObservableCollection<Checkinout> inoutAsc = new ObservableCollection<Checkinout>();
                    for (int i = inoutDesc.Count - 1; i >= 0; i--) {
                        inoutAsc.Add(inoutDesc[i]);
                    }

                    string prevMonth = null;
                    Checkinout prevCheckinout = null;
                    double totalFreeTime = 0;
                    Stack<string> monthes = new Stack<string>();
                    foreach (Checkinout checkinout in inoutAsc) {
                        string month = DateTime.Parse(checkinout.Checkin).ToString("yyyyMM");
                        if (!month.Equals(prevMonth)) {
                            totalFreeTime = 0;
                            prevMonth = month;
                            if (prevCheckinout != null) {
                                prevCheckinout.IsLastWorkDayOfMonth = true;
                            }
                            monthes.Push(month);
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
                    ObservableCollection<Checkinout> inoutDescAllDays = new ObservableCollection<Checkinout>();
                    while (monthes.Count > 0) {
                        string m = monthes.Pop();
                        int year = int.Parse(m.Substring(0, 4));
                        int month = int.Parse(m.Substring(4, 2));
                        int days = DateTime.DaysInMonth(year, month);
                        string now = DateTime.Now.ToString("yyyyMMdd");
                        for (int i = days; i >= 1; i--) {
                            string day = string.Format("{0}{1:D2}{2:D2}", year, month, i);
                            if (now.CompareTo(day) >= 0) {
                                Checkinout thisCheckinout = null;
                                foreach (Checkinout checkinout in inoutDesc) {
                                    if (DateTime.Parse(checkinout.Checkin).ToString("yyyyMMdd").Equals(day)) {
                                        thisCheckinout = checkinout;
                                        inoutDesc.Remove(checkinout);
                                        break;
                                    }
                                }
                                if (thisCheckinout == null && inoutDescAllDays.Count > 0) {
                                    DateTime dt = new DateTime(year, month, i);
                                    switch (dt.DayOfWeek) {
                                        case DayOfWeek.Saturday:
                                        case DayOfWeek.Sunday:
                                            break;
                                        default:
                                            thisCheckinout = new Checkinout {
                                                Checkin = dt.ToString("yyyy/MM/dd HH:mm:ss")
                                            };
                                            break;
                                    }
                                }
                                if (thisCheckinout != null) {
                                    inoutDescAllDays.Add(thisCheckinout);
                                }
                            }
                        }
                    }
                    lvwEmployee.Dispatcher.Invoke(() => {
                        lvwEmployee.ItemsSource = inoutDescAllDays;
                        btnQuery.IsEnabled = true;
                        ring.Visibility = Visibility.Hidden;
                        txtStandby.Visibility = Visibility.Hidden;
                        ibBackground.Opacity = 0.1;
                    });
                });
            }
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e) {
            txtName.Focus();
        }

        private void txtName_PreviewKeyDown(object sender, KeyEventArgs e) {
            switch (e.Key) {
                case Key.Enter: { ExecuteQuery(); } break;
            }
        }

        private void chkFWTran_Checked(object sender, RoutedEventArgs e) {
            Downloader.SetProxy(PROXY_ADDRESS, PROXY_PORT);
            Profile.SetBoolean(SDCC_FW_TRANSMISSION_MODE_KEY, true);
        }

        private void chkFWTran_Unchecked(object sender, RoutedEventArgs e) {
            Downloader.ClearPorxy();
            Profile.SetBoolean(SDCC_FW_TRANSMISSION_MODE_KEY, false);
        }

        private void TextBlock_Loaded(object sender, RoutedEventArgs e) {
            DependencyObject o = VisualTreeHelper.GetParent(sender as DependencyObject);
            while (o != null) {
                if (o is Border border) {
                    if (border.DataContext is Checkinout checkinout) {
                        if (checkinout.Background != Brushes.Transparent) {
                            border.Background = checkinout.Background;
                        }
                    }
                    break;
                }
                o = VisualTreeHelper.GetParent(o);
            }
        }
    }
}
