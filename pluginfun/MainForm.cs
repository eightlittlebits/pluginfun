using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
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

        long _lastFrameTimestamp;

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

            var components = new List<Type>();

            // get currently loaded assemblies excluding any in the GAC and any dynamically generated assemblies (xmlserlializer etc)
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.GlobalAssemblyCache && !a.IsDynamic).ToList();

#if DEBUG
            // this might be loaded if we've used the debugger before we reach this point
            loadedAssemblies.RemoveAll(x => x.FullName.StartsWith("Microsoft.VisualStudio.Debugger.Runtime"));
#endif

            // load any matching types in the currently loaded assemblies
            components.AddRange(AddinLoader.GetImplementationsFromAssemblies<IDynamicallyLoadableComponent>(loadedAssemblies));

            // load components from the plugins directory
            components.AddRange(AddinLoader.Load<IDynamicallyLoadableComponent>(Program.PluginsDirectory));

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

            aboutToolStripMenuItem.Text = $"About {Application.ProductName}";

            if (!_emulationInitialised)
            {
                statusToolStripStatusLabel.Text = "Ready";
            }
            else
            {
                statusToolStripStatusLabel.Text = _emulationPaused ? "Paused" : "Running";
            }
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

            recentFilesToolStripMenuItem.Enabled = _config.RecentFiles.Count > 0;

            if (recentFilesToolStripMenuItem.Enabled)
            {
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
        }

        private void PrepareDataBindings()
        {
            // emulation menu databinding
            pauseToolStripMenuItem.DataBindings.Add(nameof(pauseToolStripMenuItem.Checked), _emulationPaused, nameof(_emulationPaused.Value), false, DataSourceUpdateMode.OnPropertyChanged);

            pauseToolStripMenuItem.DataBindings.Add(nameof(pauseToolStripMenuItem.Enabled), _emulationInitialised, nameof(_emulationInitialised.Value), false, DataSourceUpdateMode.OnPropertyChanged);
            resetToolStripMenuItem.DataBindings.Add(nameof(resetToolStripMenuItem.Enabled), _emulationInitialised, nameof(_emulationInitialised.Value), false, DataSourceUpdateMode.OnPropertyChanged);

            // options menu databinding
            limitFpsToolStripMenuItem.DataBindings.Add(nameof(limitFpsToolStripMenuItem.Checked), _config, nameof(_config.LimitFps), false, DataSourceUpdateMode.OnPropertyChanged);
            pauseWhenFocusLostToolStripMenuItem.DataBindings.Add(nameof(pauseWhenFocusLostToolStripMenuItem.Checked), _config, nameof(_config.PauseOnLostFocus), false, DataSourceUpdateMode.OnPropertyChanged);
        }

        private void LoadFile(string path)
        {
            AddFileToRecentFiles(path);
        }

        private void RunFrame()
        {
            long currentTimeStamp = Stopwatch.GetTimestamp();
            long elapsedTicks = currentTimeStamp - _lastFrameTimestamp;

            if (_config.LimitFps && elapsedTicks < _targetFrameTicks)
            {
                // get ms to sleep for, cast to int to truncate to nearest millisecond
                // take 1 ms off the sleep time as we don't always hit the sleep exactly, trade
                // burning extra cpu in the spin loop for accuracy
                int sleepMilliseconds = (int)((_targetFrameTicks - elapsedTicks) * 1000 / _stopwatchFrequency) - 1;

                if (sleepMilliseconds > 0)
                {
                    Thread.Sleep(sleepMilliseconds);
                }

                // spin for the remaining partial millisecond to hit target frame rate
                while ((Stopwatch.GetTimestamp() - _lastFrameTimestamp) < _targetFrameTicks) ;
            }

            long endFrameTimestamp = Stopwatch.GetTimestamp();

            long totalFrameTicks = endFrameTimestamp - _lastFrameTimestamp;

            _lastFrameTimestamp = endFrameTimestamp;
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

        private void displayControl_Paint(object sender, PaintEventArgs e)
        {
            // https://stackoverflow.com/a/14731922
            // https://opensourcehacker.com/2011/12/01/calculate-aspect-ratio-conserving-resize-for-images-in-javascript/
            (int width, int height) CalculateAspectRatioFit(int sourceWidth, int sourceHeight, int destWidth, int destHeight)
            {
                var ratio = Math.Min((double)destWidth / sourceWidth, (double)destHeight / sourceHeight);

                return ((int)(sourceWidth * ratio), (int)(sourceHeight * ratio));
            }

            int ScreenWidth = 160;
            int ScreenHeight = 144;

            var display = (DisplayControl)sender;

            var (width, height) = CalculateAspectRatioFit(ScreenWidth, ScreenHeight, display.ClientSize.Width, display.ClientSize.Height);

            using (var myBrush = new SolidBrush(Color.Red))
            using (var formGraphics = display.CreateGraphics())
            {
                int left = 0, top = 0;

                if (width < display.Width)
                    left = (display.Width - width) / 2;
                else
                    top = (display.Height - height) / 2;

                formGraphics.Clear(Color.Black);
                formGraphics.FillRectangle(myBrush, new Rectangle(left, top, width, height));
            }
        }
    }
}
