using System;
using System.Diagnostics;
using System.IO;
using System.Net;

namespace UpdateClientIP
{
    public class Downloader
    {
        public delegate bool DataReceivedEventHandler(byte[] data);
        public static event DataReceivedEventHandler DataReceived;

        public static string GetHtml(string url)
        {
            string html = null;
            try {
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
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
                        HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(url);
                        if (startPosition > 0) {
                            myRequest.AddRange((int)startPosition);
                        }
                        Stream readStream = myRequest.GetResponse().GetResponseStream();
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
                        HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
                        if (startPosition > 0) {
                            webRequest.AddRange((int)startPosition);
                        }
                        Stream readStream = webRequest.GetResponse().GetResponseStream();
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
