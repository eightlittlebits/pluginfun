﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace pluginfun
{
    static class Program
    {
        public const string PluginsDirectory = @".\plugins";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            // set timer resolution to 1ms so the sleep gets the required accurcacy in the wait loop
            elb_utilities.NativeMethods.WinMM.TimeBeginPeriod(1);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        // https://weblog.west-wind.com/posts/2016/Dec/12/Loading-NET-Assemblies-out-of-Seperate-Folders
        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            Debug.WriteLine($"{nameof(CurrentDomain_AssemblyResolve)} - {args.Name} {args.RequestingAssembly}");

            // Ignore missing resources
            if (args.Name.Contains(".resources"))
                return null;

            // check for assemblies already loaded
            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == args.Name);
            if (assembly != null)
                return assembly;

            // Try to load by filename - split out the filename of the full assembly name
            string filename = args.Name.Split(',')[0] + ".dll";

            // recursively check the plugins directory
            string assemblyLocation = FindFileInPath(PluginsDirectory, filename);

            if (!string.IsNullOrEmpty(assemblyLocation))
            {
                return Assembly.LoadFrom(assemblyLocation);
            }

            return null;
        }

        private static string FindFileInPath(string path, string filename)
        {
            return Directory.EnumerateFiles(path, filename, SearchOption.AllDirectories).FirstOrDefault();

        }
    }
}
