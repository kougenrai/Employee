using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace AssemblySimplify
{
    class Program
    {
        private static int GZipBufferSize => 8192;

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(Environment.GetCommandLineArgs())
                .WithParsed(opts => RunOptionsAndReturnExitCode(opts))
                .WithNotParsed((errs) => HandleParseError(errs));
        }

        private static void RunOptionsAndReturnExitCode(Options opts)
        {
            if (Directory.Exists(opts.WorkDirectory)) {
                Console.WriteLine(string.Format("Starting AssemblySimplify: {0}", opts.WorkDirectory));
                foreach (FileInfo file in new DirectoryInfo(opts.WorkDirectory).GetFiles("*.dll")) {
                    Console.Write(string.Format("Compressing: {0}...", Path.GetFileName(file.FullName)));
                    byte[] compressedData = Compress(file.FullName);
                    if (compressedData != null && compressedData.Length > 0) {
                        string compressedFilePath = string.Format(@"{0}\{1}.gzip",
                            Path.GetDirectoryName(file.FullName), Path.GetFileNameWithoutExtension(file.FullName));
                        if (File.Exists(compressedFilePath)) {
                            File.SetAttributes(compressedFilePath, FileAttributes.Normal);
                            File.Delete(compressedFilePath);
                        }
                        File.WriteAllBytes(compressedFilePath, compressedData);
                        Console.WriteLine(string.Format(@" {0:N0} bytes->{1:N0} bytes({2}%)",
                            file.Length, compressedData.Length, (int)((float)compressedData.Length / file.Length * 100)));
                    } else {
                        Console.WriteLine("Failed.");
                    }
                }
                Console.WriteLine("Done.");
            }
        }

        private static void HandleParseError(IEnumerable<Error> errs)
        {
            Console.WriteLine("Useage: [-d AssembliesDirectoryPath]");
        }

        public static byte[] Compress(string file)
        {
            byte[] compressedData = null;
            if (!string.IsNullOrEmpty(file) && File.Exists(file)) {
                using (FileStream fs = new FileStream(file, FileMode.Open)) {
                    byte[] data = new byte[GZipBufferSize];
                    using (MemoryStream stream = new MemoryStream()) {
                        GZipStream gZip = new GZipStream(stream, CompressionMode.Compress, true);
                        int bytesOfRead = fs.Read(data, 0, data.Length);
                        while (bytesOfRead > 0) {
                            gZip.Write(data, 0, bytesOfRead);
                            bytesOfRead = fs.Read(data, 0, data.Length);
                        }
                        gZip.Close();
                        compressedData = stream.ToArray();
                    }
                }
            }
            return compressedData;
        }
    }
}
