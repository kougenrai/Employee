using System;
using System.Windows;
using System.Windows.Media;

namespace Employee {
    public class Checkinout {
        private static string DateTimeFormat => "yyyy-MM-dd";

        // 一天固定工作时间，单位（秒）8小时
        private static int FixedWorkTimeSecond => 8 * 3600;
        private static int FixedCoreWorkTimeSecond => 4 * 3600;

        private string m_checkin = null;
        private string m_checkout = null;

        public string Checkin {
            get => m_checkin;
            set {
                m_checkin = value;
                init();
            }
        }

        public string Checkout {
            get => m_checkout;
            set {
                m_checkout = value;
                init();
            }
        }

        public TimeSpan WorkTime { get; private set; }
        public TimeSpan FreeTime { get; private set; }
        public TimeSpan TotalFreeTime { get; set; }
        public string TotalFreeTimeText {
            get {
                double totalSeconds = TotalFreeTime.TotalSeconds;
                double hours = Math.Floor(Math.Abs(totalSeconds) / 3600);
                double minutes = Math.Floor((Math.Abs(totalSeconds) - hours * 3600) / 60);
                double seconds = Math.Abs(totalSeconds) % 60;
                return string.Format("{0}{1:D2}:{2:D2}:{3:D2}",
                    totalSeconds > 0 ? string.Empty : "-",
                    (int)hours, (int)minutes, (int)seconds);
            }
        }

        public string Date {
            get {
                string date = null;
                if (!string.IsNullOrEmpty(Checkin)) {
                    DateTime dateTime = DateTime.Parse(Checkin);
                    switch (dateTime.DayOfWeek) {
                        case DayOfWeek.Sunday: { date = string.Format("{0}({1})", dateTime.ToString(DateTimeFormat), Properties.Resources.IDS_SUNDAY); } break;
                        case DayOfWeek.Monday: { date = string.Format("{0}({1})", dateTime.ToString(DateTimeFormat), Properties.Resources.IDS_MONDAY); } break;
                        case DayOfWeek.Tuesday: { date = string.Format("{0}({1})", dateTime.ToString(DateTimeFormat), Properties.Resources.IDS_TUESDAY); } break;
                        case DayOfWeek.Wednesday: { date = string.Format("{0}({1})", dateTime.ToString(DateTimeFormat), Properties.Resources.IDS_WEDNESDAY); } break;
                        case DayOfWeek.Thursday: { date = string.Format("{0}({1})", dateTime.ToString(DateTimeFormat), Properties.Resources.IDS_THURSDAY); } break;
                        case DayOfWeek.Friday: { date = string.Format("{0}({1})", dateTime.ToString(DateTimeFormat), Properties.Resources.IDS_FRIDAY); } break;
                        case DayOfWeek.Saturday: { date = string.Format("{0}({1})", dateTime.ToString(DateTimeFormat), Properties.Resources.IDS_SATURDAY); } break;
                    }
                }
                return date;
            }
        }

        public string CheckinTime {
            get {
                string time = null;
                if (!string.IsNullOrEmpty(Checkin)) {
                    time = DateTime.Parse(Checkin).ToString("HH:mm:ss");
                }
                return time;
            }
        }

        public string CheckoutTime {
            get {
                string time = null;
                if (!string.IsNullOrEmpty(Checkout)) {
                    time = DateTime.Parse(Checkout).ToString("HH:mm:ss");
                }
                return time;
            }
        }

        public bool Late { get; private set; }
        public bool LeaveEarly { get; private set; }
        public bool IsLastWorkDayOfMonth { get; set; }

        public Brush DateForeground => IsToday || IsLastWorkDayOfMonth ? Brushes.Black : Brushes.Black;
        public Brush CheckinForeground => Late ? Brushes.Red : DateForeground;
        public Brush CheckoutForeground => LeaveEarly ? Brushes.Red : DateForeground;
        public Brush WorkTimeForeground => WorkTime.TotalSeconds > FixedCoreWorkTimeSecond ? DateForeground : Brushes.Red;
        public Brush FreeTimeForeground => FreeTime.TotalHours > 0 ? DateForeground : Brushes.Red;
        public Brush TotalFreeTimeForeground => TotalFreeTime.TotalHours > 0 ? DateForeground : Brushes.Red;
        public FontWeight CheckinFontWeight => Late ? FontWeights.Bold : FontWeights.Normal;
        public FontWeight CheckoutFontWeight => LeaveEarly ? FontWeights.Bold : FontWeights.Normal;
        public FontWeight WorkTimeFontWeight => WorkTime.TotalSeconds > FixedCoreWorkTimeSecond ? FontWeights.Normal : FontWeights.Bold;
        public FontWeight FreeTimeFontWeight => FreeTime.TotalHours > 0 ? FontWeights.Normal : FontWeights.Bold;
        public FontWeight TotalFreeTimeFontWeight => TotalFreeTime.TotalHours > 0 ? FontWeights.Normal : FontWeights.Bold;

        public bool IsToday => (DateTime.Parse(Checkin).ToString("yyyyMMdd") == DateTime.Now.ToString("yyyyMMdd"));
        public Brush Background => IsToday || IsLastWorkDayOfMonth ? Brushes.LightGray : Brushes.Transparent;

        public override string ToString() {
            return string.Format("{0}, {1} - {2}, F:{3}， T:{4}",
                new string[] {
                    DateTime.Parse(Checkin).ToString("yyyy-MM-dd"),
                    CheckinTime,
                    CheckoutTime,
                    FreeTime.ToString(),
                    TotalFreeTimeText,
                });
        }

        public void init() {
            if (!string.IsNullOrEmpty(Checkin) && !string.IsNullOrEmpty(Checkout)) {
                DateTime actualIn = DateTime.Parse(Checkin);
                DateTime actualOut = DateTime.Parse(Checkout);

                DateTime _100000 = DateTime.Parse(actualIn.ToString(DateTimeFormat) + " 10:00:00");

                // 上午结束时间
                DateTime _115959 = DateTime.Parse(actualIn.ToString(DateTimeFormat) + " 11:59:59");

                // 下午开始时间
                DateTime _130000 = DateTime.Parse(actualIn.ToString(DateTimeFormat) + " 13:00:00");

                // 下午结束时间
                DateTime _145959 = DateTime.Parse(actualIn.ToString(DateTimeFormat) + " 14:59:59");

                // 如果第一次打卡时间在11:59:59 - 13:00:00之间，那么作为 13:00:00
                if (actualIn >= _115959 && actualIn <= _130000) {
                    actualIn = _130000;
                }

                // 如果最后一次打卡在11:59:59 - 13:00:00之间，那么作为11:59:59
                if (actualOut >= _115959 && actualOut <= _130000) {
                    actualOut = _115959;
                }

                // 最后一次打卡时间必须大于等于第一次打卡时间
                if (actualIn > actualOut) {
                    actualOut = actualIn;
                }

                // 如果上午请假半天，或者早退半天
                if (actualIn >= _130000 || actualOut <= _115959) {
                    // 目前没有需要处理的逻辑
                }

                // 计算一天工作时间，单位“秒”，注意getTime返回的是毫秒
                long workTime = unchecked((long)(actualOut - actualIn).TotalSeconds);

                // 如果这一天工作横跨中午，那么减去中午的1小时
                if (actualIn <= _130000 && actualOut >= _130000) {

                    // 减去3600秒中午时间
                    workTime -= 3600;
                }

                Late = false;
                LeaveEarly = false;

                // 如果第一次打卡在10:00:00 - 11:59:59之间，那么作为迟到
                if (actualIn >= _100000 && actualIn <= _115959) {
                    Late = true;
                }

                // 如果最后一次打卡在13:00:00 - 14:59:59之间，那么作为早退
                if (actualIn >= _130000 && actualIn <= _145959) {
                    Late = true;
                }

                // 如果最后一次打卡在10:00:00 - 11:59:59之间，那么作为早退
                if (actualOut >= _100000 && actualOut <= _115959) {
                    LeaveEarly = true;
                }

                // 如果最后一次打卡在13:00:00 - 14:59:59之间，那么作为早退
                if (actualOut >= _130000 && actualOut <= _145959) {
                    LeaveEarly = true;
                }

                // 计算一天的工作时间
                WorkTime = TimeSpan.FromSeconds(workTime);

                // 计算一天可利用的弹性时间
                FreeTime = TimeSpan.FromSeconds(workTime - FixedWorkTimeSecond);
            }
        }
    }
}