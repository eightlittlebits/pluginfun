using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using elb_utilities;
using elb_utilities.Components;
using elb_utilities.Configuration;
using pluginfun.shared;

namespace pluginfun
{
    public partial class MainForm : Form
    {
        const int RecentFileCount = 10;

        string _programNameVersion;
        PluginfunConfig _config;

        MachineConfiguration _masterSystemConfig;

        NotifyValue<bool> _emulationInitialised;
        NotifyValue<bool> _emulationPaused;

        List<Type> _plugins;

        public MainForm()
        {
            InitializeComponent();

            _programNameVersion = $"{Application.ProductName} {Application.ProductVersion}";

            _config = PluginfunConfig.Load<PluginfunConfig>();

            _masterSystemConfig = XmlConfiguration.Load<MasterSystemConfiguration>();

            _emulationInitialised = new NotifyValue<bool>(false);
            _emulationPaused = new NotifyValue<bool>(false);

            _plugins = ScanForPlugins<IPlugin>();

            PrepareUserInterface();
            PrepareDataBindings();
        }

        private List<Type> ScanForPlugins<T>()
        {
            var plugins = new List<Type>();

            if (!typeof(T).IsInterface) throw new Exception($"{nameof(ScanForPlugins)} called with non-interface type {typeof(T).Name}");

            // load any plugins in the currently loaded assemblies, excluding any in the GAC
            plugins.AddRange(AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.GlobalAssemblyCache).SelectMany(a => GetImplementationsFromAssembly<T>(a)));

            // scan plugins directory for plugins in a new appdomain so any assemblies scanned and not containing plugins are unloaded
            using (var appDomain = new AppDomainWithType<PluginFinder>())
            {
                var pluginFinder = appDomain.TypeObject;
                plugins.AddRange(pluginFinder.SearchPath<IPlugin>(Program.PluginsDirectory, "*.dll", SearchOption.AllDirectories));
            }

            return plugins;
        }

        private IEnumerable<Type> GetImplementationsFromAssembly<T>(Assembly assembly)
        {
            if (!typeof(T).IsInterface) throw new Exception($"{nameof(GetImplementationsFromAssembly)} called with non-interface type {typeof(T).Name}");

            return assembly.GetTypes().Where(t => typeof(T).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
        }

        private void PrepareUserInterface()
        {
            SetUIText();
            UpdateRecentFilesMenu();
            PopulatePluginsMenu();
        }

        private void PopulatePluginsMenu()
        {
            foreach (var pluginType in _plugins)
            {
                var plugin = (IPlugin)Activator.CreateInstance(pluginType);

                var pluginMenuItem = new BindableToolStripMenuItem() { Text = plugin.Name };
                pluginMenuItem.Click += (s, ev) => plugin.DoTheThing();

                pluginsToolStripMenuItem.DropDownItems.Add(pluginMenuItem);
            }
        }

        private void SetUIText()
        {
            Text = _programNameVersion;
        }

        public void AddFileToRecentFiles(string filename)
        {
            var files = _config.RecentFiles;

            // remove and insert to place at top
            files.Remove(filename);
            files.Insert(0, filename);

            // only store the top RecentFileCount (10) entries
            _config.RecentFiles = files.Take(RecentFileCount).ToList();
        }

        private void RemoveFileFromRecentFiles(string filename)
        {
            _config.RecentFiles.Remove(filename);
        }

        private void UpdateRecentFilesMenu()
        {
            // remove existing entries
            recentFilesToolStripMenuItem.DropDownItems.Clear();

            foreach (var recent in _config.RecentFiles.Select((filename, index) => new { Index = index, Filename = filename }))
            {
                var menuItem = new ToolStripMenuItem($"&{recent.Index} {recent.Filename}")
                {
                    Tag = recent.Filename
                };

                menuItem.Click += (s, ev) =>
                {
                    string filePath = (string)((ToolStripMenuItem)s).Tag;

                    if (!File.Exists(filePath))
                    {
                        if (MessageBox.Show($"{filePath} not found, remove from recent files?", "File not found", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes)
                        {
                            RemoveFileFromRecentFiles(filePath);
                            UpdateRecentFilesMenu();
                        }
                    }
                    else
                        LoadFile(filePath);
                };

                recentFilesToolStripMenuItem.DropDownItems.Add(menuItem);
            }
        }

        private void PrepareDataBindings()
        {
            // emulation menu databinding
            pauseToolStripMenuItem.DataBindings.Add(nameof(pauseToolStripMenuItem.Checked), _emulationPaused, nameof(_emulationPaused.Value), false, DataSourceUpdateMode.OnPropertyChanged);

            pauseToolStripMenuItem.DataBindings.Add(nameof(pauseToolStripMenuItem.Enabled), _emulationInitialised, nameof(_emulationInitialised.Value), false, DataSourceUpdateMode.OnPropertyChanged);
            resetToolStripMenuItem.DataBindings.Add(nameof(resetToolStripMenuItem.Enabled), _emulationInitialised, nameof(_emulationInitialised.Value), false, DataSourceUpdateMode.OnPropertyChanged);

            // options menu databinding
            limitFpsToolStripMenuItem.DataBindings.Add(nameof(limitFpsToolStripMenuItem.Checked), _config, nameof(_config.LimitFps), false, DataSourceUpdateMode.OnPropertyChanged);
            pauseOnLostFocusToolStripMenuItem.DataBindings.Add(nameof(pauseOnLostFocusToolStripMenuItem.Checked), _config, nameof(_config.PauseOnLostFocus), false, DataSourceUpdateMode.OnPropertyChanged);
        }

        public void LoadFile(string path)
        {
            AddFileToRecentFiles(path);
            UpdateRecentFilesMenu();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _masterSystemConfig.Save();

            _config.Save();

            base.OnFormClosing(e);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void configurationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var configForm = new ConfigForm(_masterSystemConfig))
            {
                if (configForm.ShowDialog(this) == DialogResult.OK)
                {
                    _masterSystemConfig = configForm.Configuration;
                }
            }
        }
    }
}
