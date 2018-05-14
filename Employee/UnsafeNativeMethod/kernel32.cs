using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Employee.UnsafeNativeMethod
{
    public class kernel32
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll")]
        public extern static IntPtr LoadLibrary(string path);

        [DllImport("kernel32.dll")]
        public extern static IntPtr GetProcAddress(IntPtr lib, string funcName);

        [DllImport("kernel32.dll")]
        public extern static bool FreeLibrary(IntPtr lib);

        public enum ResTypeEx
        {
            DIFFERENCE = 32
        }
        public enum ResType
        {
            RT_FIRST = 1,
            RT_CURSOR = 1,
            RT_BITMAP = 2,
            RT_ICON = 3,
            RT_MENU = 4,
            RT_DIALOG = 5,
            RT_STRING = 6,
            RT_FONTDIR = 7,
            RT_FONT = 8,
            RT_ACCELERATOR = 9,
            RT_RCDATA = 10,
            RT_MESSAGETABLE = 11,
            RT_GROUP_CURSOR = (RT_CURSOR + ResTypeEx.DIFFERENCE),
            RT_GROUP_ICON = (RT_ICON + ResTypeEx.DIFFERENCE),
            RT_VERSION = 16,
            RT_DLGINCLUDE = 17,
            RT_PLUGPLAY = 19,
            RT_VXD = 20,
            RT_ANICURSOR = 21,
            RT_ANIICON = 22,
            RT_HTML = 23,
            RT_MANIFEST = 24,
            RT_LAST
        }

        public enum LoadLibraryFlags : uint
        {
            LOAD_LIBRARY_AS_DATAFILE = 0x00000002,
        }

        public delegate bool EnumResTypeProcHandle(IntPtr hModule, IntPtr lpszType, IntPtr lParam);
        public delegate bool EnumResNameProcHandle(IntPtr hModule, IntPtr lpszType, IntPtr lpszName, IntPtr lParam);
        public delegate bool EnumResLanguageProcHandle(IntPtr hModule, IntPtr lpszType, IntPtr lpszName, int wIDLanguage, IntPtr lParam);

        [DllImport("kernel32.dll", EntryPoint = "LoadLibraryExW", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Winapi)]
        public static extern IntPtr LoadLibraryEx(string lpLibFileName, IntPtr hFile, LoadLibraryFlags dwFlags);

        [DllImport("kernel32.dll", EntryPoint = "EnumResourceTypesW", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Winapi)]
        public static extern bool EnumResourceTypes(IntPtr hModule, EnumResTypeProcHandle EnumResTypeProc, IntPtr lParam);

        [DllImport("kernel32.dll", EntryPoint = "EnumResourceNamesW", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Winapi)]
        public static extern bool EnumResourceNames(IntPtr hModule, IntPtr lpType, EnumResNameProcHandle EnumResNameProc, IntPtr lParam);

        [DllImport("kernel32.dll", EntryPoint = "EnumResourceLanguagesW", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Winapi)]
        public static extern bool EnumResourceLanguages(IntPtr hModule, IntPtr lpType, IntPtr lpName, EnumResLanguageProcHandle EnumResLanguageProc, IntPtr lParam);

        [DllImport("kernel32.dll", EntryPoint = "FindResourceW", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Winapi)]
        public static extern IntPtr FindResource(IntPtr hInstance, IntPtr lpName, IntPtr lpType);

        [DllImport("kernel32.dll", EntryPoint = "FindResourceExW", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Winapi)]
        public static extern IntPtr FindResourceEx(IntPtr hModule, IntPtr lpType, IntPtr lpName, uint wLanguage);

        [DllImport("kernel32.dll", EntryPoint = "LoadResource", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Winapi)]
        public static extern IntPtr LoadResource(IntPtr hInstance, IntPtr hResInfo);

        [DllImport("kernel32.dll", EntryPoint = "LockResource", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Winapi)]
        public static extern IntPtr LockResource(IntPtr hResData);

        [DllImport("kernel32.dll", EntryPoint = "SizeofResource", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Winapi)]
        public static extern uint SizeofResource(IntPtr hModule, IntPtr hResInfo);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int LoadString(IntPtr hInstance, uint uID, StringBuilder lpBuffer, int nBufferMax);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern uint GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString, uint nSize, string lpFileName);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        public static extern long WritePrivateProfileString(string lpApplicationName, string lpKeyName, string lpString, string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetCurrentProcess();

        [DllImport("Advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccesss, out IntPtr tokenHandle);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern Boolean CloseHandle(IntPtr hObject);
    }
}
