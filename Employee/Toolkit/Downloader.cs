using System;
using System.Diagnostics;
using System.IO;
using System.Net;

namespace Employee
{
    public class Downloader
    {
        public delegate bool DataReceivedEventHandler(byte[] data);
        public static event DataReceivedEventHandler DataReceived;
        private static WebProxy WebProxy { get; set; }
        public static void SetProxy(string host, int port)
        {
            SetProxy(host, port, null, null);
        }

        public static void SetProxy(string host, int port, string userName, string password)
        {
            if (!string.IsNullOrEmpty(host)) {
                WebProxy = new WebProxy(host, port) {
                    BypassProxyOnLocal = false
                };
                if (!string.IsNullOrEmpty(userName)) {
                    WebProxy.Credentials = new NetworkCredential(userName, password);
                }
            }
        }

        public static void ClearPorxy()
        {
            WebProxy = null;
        }

        public static string GetHtml(string url)
        {
            string html = null;
            try {
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                request.Proxy = WebProxy;
                using (WebResponse response = request.GetResponse()) {
                    using (Stream stream = response.GetResponseStream()) {
                        StreamReader reader = new StreamReader(stream);
                        html = reader.ReadToEnd();
                    }
                }
            } catch (Exception ex) {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
            return html;
        }

        public static byte[] DownloadData(string url)
        {
            byte[] data = null;
            using (MemoryStream m = new MemoryStream()) {
                long startPosition = 0;
                long remoteFileLength = GetHttpLength(url);
                if (remoteFileLength != 745) {
                    try {
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                        request.Proxy = WebProxy;
                        if (startPosition > 0) {
                            request.AddRange((int)startPosition);
                        }
                        Stream readStream = request.GetResponse().GetResponseStream();
                        byte[] buffer = new byte[8192];
                        int contentSize = readStream.Read(buffer, 0, buffer.Length);
                        long currPostion = startPosition;
                        while (contentSize > 0) {
                            currPostion += contentSize;
                            m.Write(buffer, 0, contentSize);
                            bool? @continue = DataReceived?.Invoke(buffer);
                            if (@continue.HasValue && !@continue.GetValueOrDefault()) {
                                break;
                            }
                            contentSize = readStream.Read(buffer, 0, buffer.Length);
                        }
                        readStream.Close();
                    } catch {
                    }
                }
                data = m.ToArray();
            }
            return data;
        }

        public static bool DownloadFile(string url, string localfile)
        {
            bool b = false;
            long httpLength = GetHttpLength(url);
            if (httpLength != 745) {
                FileStream fileStream = null;
                long startPosition = 0;
                if (File.Exists(localfile)) {
                    fileStream = File.OpenWrite(localfile);
                    startPosition = fileStream.Length;
                    if (startPosition >= httpLength) {
                        fileStream.Close();
                        fileStream = null;
                    } else {
                        fileStream.Seek(startPosition, SeekOrigin.Current);
                    }
                } else {
                    fileStream = new FileStream(localfile, FileMode.Create);
                    startPosition = 0;
                }
                if (fileStream != null) {
                    try {
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                        request.Proxy = WebProxy;
                        if (startPosition > 0) {
                            request.AddRange((int)startPosition);
                        }
                        Stream readStream = request.GetResponse().GetResponseStream();
                        byte[] buffer = new byte[8192];
                        int contentSize = readStream.Read(buffer, 0, buffer.Length);
                        long currPostion = startPosition;
                        while (contentSize > 0) {
                            currPostion += contentSize;
                            fileStream.Write(buffer, 0, contentSize);
                            bool? @continue = DataReceived?.Invoke(buffer);
                            if (@continue.HasValue && !@continue.GetValueOrDefault()) {
                                break;
                            }
                            contentSize = readStream.Read(buffer, 0, buffer.Length);
                        }
                        readStream.Close();
                        b = true;
                    } catch {
                    }
                    fileStream.Close();
                }
            }
            return b;
        }

        public static long GetHttpLength(string url)
        {
            long length = 0;
            try {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                HttpWebResponse rsp = (HttpWebResponse)req.GetResponse();
                if (rsp.StatusCode == HttpStatusCode.OK) {
                    length = rsp.ContentLength;
                }
                rsp.Close();
            } catch {
            }
            return length;
        }
    }
}
