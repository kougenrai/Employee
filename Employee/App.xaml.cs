using System;
using System.Collections.Generic;
#if !DEBUG
using System.Reflection;
#endif
using System.Windows;
using System.Windows.Media;

namespace Employee
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, ISingleInstanceApp
    {
        private static string Unique => "Employee.App";

        public static FontFamily DefaultFontFamily => new FontFamily(Employee.Properties.Resources.IDS_FONT_FAMILY);

        [STAThread]
        public static void Main()
        {
            if (SingleInstance<App>.InitializeAsFirstInstance(Unique)) {
#if !DEBUG
                AssemblyResolve.Enable(Assembly.GetExecutingAssembly(), ".gzip", "Employee.Assemblies.");
#endif
                App application = new App();
                application.InitializeComponent();
                application.Run();
                SingleInstance<App>.Cleanup();
            }
        }

        public bool SignalExternalCommandLineArgs(IList<string> args)
        {
            if (MainWindow != null) {
                if (MainWindow.WindowState == WindowState.Minimized) {
                    MainWindow.WindowState = WindowState.Normal;
                }
                MainWindow.Activate();
            }
            return true;
        }
    }
}
