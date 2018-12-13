using System;
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

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        //https://weblog.west-wind.com/posts/2016/Dec/12/Loading-NET-Assemblies-out-of-Seperate-Folders
        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            // Ignore missing resources
            if (args.Name.Contains(".resources"))
                return null;

            // check for assemblies already loaded
            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == args.Name);
            if (assembly != null)
                return assembly;

            // Try to load by filename - split out the filename of the full assembly name
            // and append the base path of the original assembly (ie. look in the same dir)
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
            filename = filename.ToLower();

            foreach (var fullFile in Directory.GetFiles(path))
            {
                var file = Path.GetFileName(fullFile).ToLower();
                if (file == filename)
                    return fullFile;

            }
            foreach (var dir in Directory.GetDirectories(path))
            {
                var file = FindFileInPath(dir, filename);
                if (!string.IsNullOrEmpty(file))
                    return file;
            }

            return null;
        }
    }
}
