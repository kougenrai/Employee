using SqlServer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Http;
using WebApplication.Models;

namespace WebApplication.Controllers
{
    public class CheckinoutController : ApiController
    {
        public static String Database30 => "AccessMSDataBase";
        public static String UserName30 => "zb";
        public static String Password30 => "Svc_2003";
        public static int Port30 => 1433;

        public static String Database201 => "integrated";
        public static String UserName201 => "zb";
        public static String Password201 => "Svc_2003";
        public static int Port201 => 50008;
        private static string HttpContext => "MS_HttpContext";
        private static string RemoteEndpointMessage => "System.ServiceModel.Channels.RemoteEndpointMessageProperty";
        private static string OwinContext => "MS_OwinContext";
        public static string RootPath {
            get {
                string AppPath = "";
                HttpContext HttpCurrent = System.Web.HttpContext.Current;
                if (HttpCurrent != null) {
                    AppPath = HttpCurrent.Server.MapPath("~");
                } else {
                    AppPath = AppDomain.CurrentDomain.BaseDirectory;
                    if (Regex.Match(AppPath, @"\\$", RegexOptions.Compiled).Success)
                        AppPath = AppPath.Substring(0, AppPath.Length - 1);
                }
                return AppPath;
            }
        }
        private static string Naxian60IPFilePath => string.Format(@"{0}Config\ip.txt", RootPath);
        private static string Naxian60IP {
            get {
                string ip = null;
                if (File.Exists(Naxian60IPFilePath)) {
                    ip = File.ReadAllText(Naxian60IPFilePath, Encoding.Default);
                }
                return ip;
            }
            set {
                if (!string.IsNullOrEmpty(value)) {
                    string directory = Path.GetDirectoryName(Naxian60IPFilePath);
                    Directory.CreateDirectory(directory);
                    File.WriteAllText(Naxian60IPFilePath, value);
                }
            }
        }
        // GET api/values
        public IEnumerable<Checkinout> Get()
        {
            return null;
        }

        [HttpGet]
        public IEnumerable<Checkinout> Get(string name)
        {
            List<Checkinout> checkinouts = new List<Checkinout>();
            checkinouts.AddRange(CheckInOutFromUserName30(name));
            checkinouts.AddRange(CheckInOutFromUserName201(name));
            return checkinouts.ToArray();
        }

        [HttpGet]
        public string UpdateClientIP(string ip)
        {
            if (string.IsNullOrEmpty(ip)) {
                Naxian60IP = GetClientIpAddress(Request).MapToIPv4().ToString();
            } else {
                Naxian60IP = ip;
            }
            return Naxian60IP;
        }

        [HttpGet]
        public string GetVersion(string version)
        {
            return "1.0.0.0";
        }

        public static IPAddress GetClientIpAddress(HttpRequestMessage request)
        {
            var ipString = GetClientIpString(request);
            IPAddress ipAddress = new IPAddress(0);
            if (IPAddress.TryParse(ipString, out ipAddress)) {
                return ipAddress;
            }

            return ipAddress;
        }

        public static string GetClientIpString(HttpRequestMessage request)
        {
            // Web-hosting. Needs reference to System.Web.dll
            if (request.Properties.ContainsKey(HttpContext)) {
                dynamic ctx = request.Properties[HttpContext];
                if (ctx != null) {
                    return ctx.Request.UserHostAddress;
                }
            }

            // Self-hosting. Needs reference to System.ServiceModel.dll. 
            if (request.Properties.ContainsKey(RemoteEndpointMessage)) {
                dynamic remoteEndpoint = request.Properties[RemoteEndpointMessage];
                if (remoteEndpoint != null) {
                    return remoteEndpoint.Address;
                }
            }

            // Self-hosting using Owin. Needs reference to Microsoft.Owin.dll. 
            if (request.Properties.ContainsKey(OwinContext)) {
                dynamic owinContext = request.Properties[OwinContext];
                if (owinContext != null) {
                    return owinContext.Request.RemoteIpAddress;
                }
            }

            return null;
        }

        public Checkinout[] CheckInOutFromUserName30(String userName)
        {
            String sql = "";
            sql += " SELECT CONVERT(NVARCHAR, MIN([event].eventTime), 120) as checkin, ";
            sql += "        CONVERT(NVARCHAR, MAX([event].eventTime), 120) as checkout";
            sql += " FROM [event], [employee]";
            sql += " WHERE [event].employeeNo = [employee].employeeNo";
            sql += "   AND [event].location IS NOT NULL";
            sql += "   AND [employee].fullName = '" + userName + "'";
            sql += " GROUP BY CONVERT(NVARCHAR(8), [event].eventTime, 112)";
            sql += " ORDER BY CONVERT(NVARCHAR(8), [event].eventTime, 112)";
            return ExecuteQueryAtNaxian60Server30(sql);
        }

        public Checkinout[] CheckInOutFromUserName201(String userName)
        {
            String sql = "";
            sql += " SELECT CONVERT(NVARCHAR, MIN(t1.entry_dt), 120) as checkin,  ";
            sql += " CONVERT(NVARCHAR, MAX(t1.entry_dt), 120) as checkout ";
            sql += " FROM hr_staff t2";
            sql += "      LEFT JOIN Agms_entryrec t1 ON t2.emp_no=t1.emp_no ";
            sql += "      AND t2.[name] LIKE N'%%%" + userName + "%'";
            sql += "          LEFT JOIN ctrller t3 ON t1.ctrl_id = t3.ctrl_id";
            sql += "          AND t1.link_id = t3.link_id ";
            sql += " WHERE t3.TakeAttn <> 0 ";
            sql += "   AND t1.reader in(1,2) ";
            sql += " GROUP BY CONVERT(NVARCHAR(8), t1.entry_dt, 112) ";
            sql += " ORDER BY CONVERT(NVARCHAR(8), t1.entry_dt, 112) ";
            return ExecuteQueryAtNaxian60Server201(sql);
        }

        private Checkinout[] ExecuteQueryAtNaxian60Server30(string sql)
        {
            Checkinout[] checkinouts = new Checkinout[] { };
            if (!string.IsNullOrEmpty(Naxian60IP)) {
                checkinouts = new Database(string.Format("{0},{1}", Naxian60IP, Port30),
                    UserName30, Password30, Database30).Get<Checkinout>(sql);
            }
            return checkinouts;
        }

        private Checkinout[] ExecuteQueryAtNaxian60Server201(string sql)
        {
            Checkinout[] checkinouts = new Checkinout[] { };
            if (!string.IsNullOrEmpty(Naxian60IP)) {
                checkinouts = new Database(string.Format("{0},{1}", Naxian60IP, Port201),
                UserName201, Password201, Database201).Get<Checkinout>(sql);
            }
            return checkinouts;
        }
    }
}
