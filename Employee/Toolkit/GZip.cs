using System.IO;
using System.IO.Compression;

namespace Employee
{
    public static class GZip
    {
        private static int GZipBufferSize => 8192;

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

        public static byte[] Compress(byte[] data)
        {
            byte[] compressedData = null;
            using (MemoryStream stream = new MemoryStream()) {
                GZipStream gZip = new GZipStream(stream, CompressionMode.Compress, true);
                gZip.Write(data, 0, data.Length);
                gZip.Close();
                compressedData = stream.ToArray();
            }
            return compressedData;
        }

        public static byte[] Decompress(byte[] data)
        {
            byte[] decompressedData = null;
            using (MemoryStream stream = new MemoryStream(data)) {
                GZipStream zip = new GZipStream(stream, CompressionMode.Decompress);
                MemoryStream decompressed = new MemoryStream();
                byte[] _buffer = new byte[GZipBufferSize];
                while (true) {
                    int bytesRead = zip.Read(_buffer, 0, _buffer.Length);
                    if (bytesRead <= 0) {
                        break;
                    } else {
                        decompressed.Write(_buffer, 0, bytesRead);
                    }
                }
                zip.Close();
                decompressedData = decompressed.ToArray();
            }
            return decompressedData;
        }
    }
}
