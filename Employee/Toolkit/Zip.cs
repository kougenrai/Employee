using ICSharpCode.SharpZipLib.Checksums;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Employee
{
    public static class Zip
    {
        public enum CompressLevel
        {
            Store = 0,
            Level1,
            Level2,
            Level3,
            Level4,
            Level5,
            Level6,
            Level7,
            Level8,
            Best
        }

        public static int Create(string zipFilePath, string[] files,
            string password = null, CompressLevel compressLevel = CompressLevel.Level1)
        {
            List<ZipFileEntry> fileEntries = new List<ZipFileEntry>();
            foreach (string file in files) {
                fileEntries.Add(new ZipFileEntry() {
                    FilePath = file,
                    RelativePath = null,
                });
            }
            return Create(zipFilePath, fileEntries.ToArray(), password, compressLevel);
        }

        public static int Create(string zipFilePath, ZipFileEntry[] files, 
            string password = null, CompressLevel compressLevel = CompressLevel.Level1)
        {
            ZipOutputStream zipOutputStream = null;
            FileStream fileStream = null;
            int count = 0;

            try {
                if (File.Exists(zipFilePath)) {
                    File.Delete(zipFilePath);
                }
                Directory.CreateDirectory(Path.GetDirectoryName(zipFilePath));
                Crc32 crc32 = new Crc32();
                zipOutputStream = new ZipOutputStream(File.Create(zipFilePath));
                zipOutputStream.SetLevel(Convert.ToInt32(compressLevel));
                if (password != null && password.Trim().Length > 0) {
                    zipOutputStream.Password = password;
                }
                foreach (ZipFileEntry file in files) {
                    if (!string.IsNullOrEmpty(file.FilePath) && File.Exists(file.FilePath)) {
                        byte[] buffer = new byte[] { };
                        using (FileStream stream = File.OpenRead(file.FilePath)) {
                            if (stream.Length > 0) {
                                buffer = new byte[stream.Length];
                                stream.Read(buffer, 0, buffer.Length);
                            }
                            stream.Close();
                        }
                        crc32.Reset();
                        crc32.Update(buffer);
                        ZipEntry zipEntry = new ZipEntry(Path.Combine(
                            file.RelativePath ?? string.Empty, Path.GetFileName(file.FilePath)));
                        zipEntry.Flags |= (int)GeneralBitFlags.UnicodeText;
                        zipEntry.DateTime = DateTime.Now;
                        zipEntry.Size = buffer.Length;
                        zipEntry.Crc = crc32.Value;
                        zipOutputStream.PutNextEntry(zipEntry);
                        zipOutputStream.Write(buffer, 0, buffer.Length);
                        count++;
                    }
                }
            } catch (Exception ex) {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            } finally {
                if (fileStream != null) {
                    fileStream.Close();
                }
                if (zipOutputStream != null) {
                    zipOutputStream.Finish();
                    zipOutputStream.Close();
                }
            }

            return count;
        }

        public static string Unzip(string zip)
        {
            string tmpdir = Path.GetTempFileName();
            File.Delete(tmpdir);
            Directory.CreateDirectory(tmpdir);
            Unzip(zip, tmpdir);
            return tmpdir;
        }

        public static void Unzip(string zipFilePath, string extraTo)
        {
            if (File.Exists(Path.GetFullPath(zipFilePath)) && !string.IsNullOrEmpty(extraTo)) {
                if (extraTo[extraTo.Length - 1] == '\\')
                    extraTo = (extraTo.Length == 1 ? string.Empty : extraTo.Substring(0, extraTo.Length - 1));
                using (ZipInputStream zip = new ZipInputStream(File.OpenRead(zipFilePath))) {
                    try {
                        ZipEntry entry = zip.GetNextEntry();
                        while (entry != null) {
                            string folder = Path.GetDirectoryName(entry.Name);
                            string name = Path.GetFileName(entry.Name);
                            string extra = extraTo;
                            if (!string.IsNullOrEmpty(folder)) {
                                extra += "\\";
                                extra += folder;
                                if (!Directory.Exists(extra)) { Directory.CreateDirectory(extra); }
                            }
                            if (!string.IsNullOrEmpty(name)) {
                                string tar = extra + "\\" + name;
                                using (FileStream stream = File.Create(tar)) {
                                    byte[] data = new byte[1024];
                                    while (true) {
                                        int size = zip.Read(data, 0, data.Length);
                                        if (size > 0) {
                                            stream.Write(data, 0, size);
                                        } else {
                                            break;
                                        }
                                    }
                                }
                                File.SetLastWriteTime(tar, entry.DateTime);
                            }
                            entry = zip.GetNextEntry();
                        }
                    } catch (Exception ex) {
                        Debug.WriteLine(ex.Message);
                        Debug.WriteLine(ex.StackTrace);
                    }
                }
            }
        }

        public static ZipData[] UnzipToMemory(string zipFilePath)
        {
            List<ZipData> zipData = new List<ZipData>();
            if (!string.IsNullOrEmpty(zipFilePath)) {
                string fullFilePath = Path.GetFullPath(zipFilePath);
                if (File.Exists(fullFilePath)) {
                    using (ZipInputStream zip = new ZipInputStream(File.OpenRead(zipFilePath))) {
                        try {
                            ZipEntry entry = zip.GetNextEntry();
                            while (entry != null) {
                                string folder = Path.GetDirectoryName(entry.Name);
                                string name = Path.GetFileName(entry.Name);
                                if (!string.IsNullOrEmpty(name)) {
                                    using (MemoryStream m = new MemoryStream()) {
                                        byte[] data = new byte[1024];
                                        while (true) {
                                            int size = zip.Read(data, 0, data.Length);
                                            if (size > 0) {
                                                m.Write(data, 0, size);
                                            } else {
                                                break;
                                            }
                                        }
                                        if (m.Length > 0) {
                                            zipData.Add(new ZipData() {
                                                Folder = folder,
                                                Name = name,
                                                Data = m.ToArray(),
                                            });
                                        }
                                    }
                                }
                                entry = zip.GetNextEntry();
                            }
                        } catch (Exception ex) {
                            Debug.WriteLine(ex.Message);
                            Debug.WriteLine(ex.StackTrace);
                        }
                    }
                }
            }
            return zipData.ToArray();
        }
    }

    public class ZipData
    {
        public string Folder { get; internal set; }
        public string Name { get; internal set; }
        public byte[] Data { get; internal set; }
    }

    public class ZipFileEntry
    {
        public string FilePath { get; set; }
        public string RelativePath { get; set; }

        public ZipFileEntry()
        {
            FilePath = string.Empty;
            RelativePath = string.Empty;
        }
    }
}