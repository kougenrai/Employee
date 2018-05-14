using System;
using System.Collections;
using System.IO;
using System.Reflection;

namespace Employee
{
    public static class AssemblyResolve
    {
        public static event ResolveEventHandler Resolve;
        private static bool m_enable = false;
        private static Hashtable m_assemblies = new Hashtable();

        public static void Enable(Assembly gzippedManifestResourcesAssembly, string gzipFileExtension, string rootAssemblyName)
        {
            if (!m_enable && gzippedManifestResourcesAssembly != null && !string.IsNullOrEmpty(gzipFileExtension)) {
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
                foreach (string resourceName in gzippedManifestResourcesAssembly.GetManifestResourceNames()) {
                    string simplyResourceName = resourceName.Replace(rootAssemblyName, string.Empty);
                    if (Path.GetExtension(resourceName).Equals(gzipFileExtension, StringComparison.CurrentCultureIgnoreCase)) {
                        using (Stream stream = gzippedManifestResourcesAssembly.GetManifestResourceStream(resourceName)) {
                            try {
                                byte[] compressedData = new byte[stream.Length];
                                stream.Read(compressedData, 0, compressedData.Length);
                                byte[] decompressData = GZip.Decompress(compressedData);
                                m_assemblies[Path.GetFileNameWithoutExtension(simplyResourceName).ToLower()] = Assembly.Load(decompressData);
                            } catch {

                            }
                        }
                    }
                }
                m_enable = true;
            }
        }

        public static void Disable()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
            m_enable = false;
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            Assembly missedAssembly = null;
            if (m_enable) {
                string missedAssemblyName = new AssemblyName(args.Name).Name.ToLower();
                if (m_assemblies.Contains(missedAssemblyName)) {
                    missedAssembly = m_assemblies[missedAssemblyName] as Assembly;
                } else {
                    missedAssembly = Resolve?.Invoke(sender, args);
                }
            }
            return missedAssembly;
        }
    }
}
