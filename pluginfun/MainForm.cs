using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using elb_utilities;
using elb_utilities.Configuration;
using elb_utilities.NativeMethods;
using pluginfun.common;

namespace pluginfun
{
    partial class MainForm : Form
    {
        const int RecentFileCount = 10;

        string _programNameVersion;
        PluginfunConfig _config;

        IEmulatedSystem _emulatedSystem;

        NotifyValue<bool> _emulationInitialised;
        NotifyValue<bool> _emulationPaused;

        List<Type> _pluginOnePlugins;
        List<Type> _pluginTwoPlugins;

        static bool ApplicationStillIdle => !User32.PeekMessage(out _, IntPtr.Zero, 0, 0, 0);

        public MainForm()
        {
            InitializeComponent();

            _programNameVersion = $"{Application.ProductName} {Application.ProductVersion}";

            _config = XmlConfiguration.Load<PluginfunConfig>();

            _emulatedSystem = new EmulatedSystem();

            _emulationInitialised = new NotifyValue<bool>(false);
            _emulationPaused = new NotifyValue<bool>(false);

            LoadDynamicComponents();

            PrepareUserInterface();
            PrepareDataBindings();

            Application.Idle += (s, ev) => { while (_emulationInitialised && !_emulationPaused && ApplicationStillIdle) { RunFrame(); } };
        }

        private void LoadDynamicComponents()
        {
            IEnumerable<Type> GetComponentsOfType<T>(List<Type> componentList)
            {
                return componentList.Where(t => typeof(T).IsAssignableFrom(t));
            }

            var components = AddinLoader.Load<IDynamicallyLoadableComponent>(Program.PluginsDirectory);

            _pluginOnePlugins = new List<Type>(GetComponentsOfType<IPluginOne>(components));
            _pluginTwoPlugins = new List<Type>(GetComponentsOfType<IPluginTwo>(components));
        }

        private void PrepareUserInterface()
        {
            SetUIText();
            PopulatePluginsMenus();
            UpdateRecentFilesMenu();
        }

        private void PopulatePluginsMenus()
        {
            foreach (var pluginType in _pluginOnePlugins)
            {
                var plugin = (IPluginOne)Activator.CreateInstance(pluginType);

                var pluginMenuItem = new ToolStripMenuItem() { Text = plugin.Name };
                pluginMenuItem.Click += (s, ev) => plugin.DoTheThing();

                pluginOneToolStripMenuItem.DropDownItems.Add(pluginMenuItem);
            }

            foreach (var pluginType in _pluginTwoPlugins)
            {
                var plugin = (IPluginTwo)Activator.CreateInstance(pluginType);

                var pluginMenuItem = new ToolStripMenuItem() { Text = plugin.Name };
                pluginMenuItem.Click += (s, ev) => plugin.Execute();

                pluginTwoToolStripMenuItem.DropDownItems.Add(pluginMenuItem);
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
            UpdateRecentFilesMenu();
        }

        private void RemoveFileFromRecentFiles(string filename)
        {
            _config.RecentFiles.Remove(filename);
            UpdateRecentFilesMenu();
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

        private void LoadFile(string path)
        {
            AddFileToRecentFiles(path);
        }

        private void RunFrame()
        {

        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _emulatedSystem.Configuration.Save();

            _config.Save();

            base.OnFormClosing(e);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void configurationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var configForm = new ConfigForm(_emulatedSystem))
            {
                if (configForm.ShowDialog(this) == DialogResult.OK)
                {
                    _emulatedSystem.Configuration = configForm.Configuration;
                }
            }
        }
    }
}
