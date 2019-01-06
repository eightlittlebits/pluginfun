using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using elb_utilities.Configuration;
using elb_utilities.NativeMethods;
using elb_utilities.WinForms;
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

        readonly double _stopwatchFrequency;
        double _targetFrameTicks = 0;

        long _lastFrameTimestamp;

        static bool ApplicationStillIdle => !User32.PeekMessage(out _, IntPtr.Zero, 0, 0, 0);

        public MainForm()
        {
            InitializeComponent();

            _programNameVersion = $"{Application.ProductName} {Application.ProductVersion}";

            _stopwatchFrequency = Stopwatch.Frequency;

            _config = PluginfunConfig.Load();

            _emulatedSystem = new EmulatedSystem();

            _emulationInitialised = new NotifyValue<bool>(false);
            _emulationPaused = new NotifyValue<bool>(false);

            LoadDynamicComponents();

            PrepareUserInterface();
            PrepareDataBindings();

            InitBitmaps();

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
            void AddBinding(IBindableComponent component, string propertyName, object dataSource, string dataMember, bool formattingEnabled = false, DataSourceUpdateMode updateMode = DataSourceUpdateMode.OnPropertyChanged)
            {
                component.DataBindings.Add(propertyName, dataSource, dataMember, formattingEnabled, updateMode);
            }

            // emulation menu databinding
            AddBinding(pauseToolStripMenuItem, nameof(pauseToolStripMenuItem.Enabled), _emulationInitialised, nameof(_emulationInitialised.Value));
            AddBinding(resetToolStripMenuItem, nameof(resetToolStripMenuItem.Enabled), _emulationInitialised, nameof(_emulationInitialised.Value));

            AddBinding(pauseToolStripMenuItem, nameof(pauseToolStripMenuItem.Checked), _emulationPaused, nameof(_emulationPaused.Value));

            // options menu databinding
            AddBinding(limitFpsToolStripMenuItem, nameof(limitFpsToolStripMenuItem.Checked), _config, nameof(_config.LimitFps));
            AddBinding(pauseWhenFocusLostToolStripMenuItem, nameof(pauseWhenFocusLostToolStripMenuItem.Checked), _config, nameof(_config.PauseOnLostFocus));
            AddBinding(forceSquarePixelsToolStripMenuItem, nameof(forceSquarePixelsToolStripMenuItem.Checked), _config, nameof(_config.ForceSquarePixels));
        }

        private void LoadFile(string path)
        {
            AddFileToRecentFiles(path);
        }

        private void RunFrame()
        {
            // run frame

            // render

            //sleep
            long elapsedTicks = Stopwatch.GetTimestamp() - _lastFrameTimestamp;

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

            _lastFrameTimestamp = Stopwatch.GetTimestamp();
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

        //private void displayControl_Paint(object sender, PaintEventArgs e)
        //{
        //    // https://stackoverflow.com/a/14731922
        //    // https://opensourcehacker.com/2011/12/01/calculate-aspect-ratio-conserving-resize-for-images-in-javascript/
        //    (int width, int height) CalculateAspectRatioFit(int sourceWidth, int sourceHeight, int destWidth, int destHeight)
        //    {
        //        var ratio = Math.Min((double)destWidth / sourceWidth, (double)destHeight / sourceHeight);

        //        return ((int)(sourceWidth * ratio), (int)(sourceHeight * ratio));
        //    }

        //    int screenWidth = 160;
        //    int screenHeight = 144;

        //    double pixelAspectRatio = _config.ForceSquarePixels ? 1 : 47.0 / 43; // GB screen 47mm x 43mm, pixels not square

        //    var display = (DisplayControl)sender;

        //    var (width, height) = CalculateAspectRatioFit((int)(screenWidth * pixelAspectRatio), screenHeight, display.ClientSize.Width, display.ClientSize.Height);

        //    using (var myBrush = new SolidBrush(Color.CornflowerBlue))
        //    {
        //        int left = 0, top = 0;

        //        if (width < display.Width)
        //            left = (display.Width - width) / 2;
        //        else
        //            top = (display.Height - height) / 2;

        //        e.Graphics.Clear(Color.Black);
        //        e.Graphics.FillRectangle(myBrush, left, top, width, height);
        //    }
        //}

        int _activeScreenBuffer = 0;
        int _lastScreenBuffer = 1;
        Bitmap[] _screenBuffer = new Bitmap[2];

        private void InitBitmaps()
        {
            for (int i = 0; i < _screenBuffer.Length; i++)
            {
                _screenBuffer[i] = new Bitmap(160, 144, PixelFormat.Format32bppRgb);

                using (var graphics = Graphics.FromImage(_screenBuffer[i]))
                {
                    graphics.Clear(Color.FromArgb(0xFF, 0xEF, 0xEF, 0xEF));
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

            int screenWidth = 160;
            int screenHeight = 144;

            double pixelAspectRatio = _config.ForceSquarePixels ? 1 : 47.0 / 43; // GB screen 47mm x 43mm, pixels not square

            var display = (DisplayControl)sender;

            var (width, height) = CalculateAspectRatioFit((int)(screenWidth * pixelAspectRatio), screenHeight, display.ClientSize.Width, display.ClientSize.Height);

            int left = 0, top = 0;

            if (width < display.Width)
                left = (display.Width - width) / 2;
            else
                top = (display.Height - height) / 2;

            FadeImages();

            var screenBuffer = _screenBuffer[_activeScreenBuffer];

            //var sourceRectangle = new RectangleF(-0.5f, -0.5f, screenBuffer.Width, screenBuffer.Height);
            //var destinationRectangle = new RectangleF(left, top, width, height);

            //e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
            //e.Graphics.DrawImage(screenBuffer, destinationRectangle, sourceRectangle, GraphicsUnit.Pixel);

            using (var grSrc = Graphics.FromImage(screenBuffer))
            {
                IntPtr hdcDest = IntPtr.Zero;
                IntPtr hdcSrc = IntPtr.Zero;
                IntPtr hBitmap = IntPtr.Zero;
                IntPtr hOldObject = IntPtr.Zero;

                try
                {
                    hdcDest = e.Graphics.GetHdc();
                    hdcSrc = grSrc.GetHdc();
                    hBitmap = screenBuffer.GetHbitmap();

                    hOldObject = Gdi32.SelectObject(hdcSrc, hBitmap);
                    if (hOldObject == IntPtr.Zero)
                        throw new Win32Exception();

                    if (!Gdi32.StretchBlt(hdcDest, left, top, width, height,
                                            hdcSrc, 0, 0, screenBuffer.Width, screenBuffer.Height,
                                            Gdi32.TernaryRasterOperations.SRCCOPY))

                        throw new Win32Exception();
                }
                finally
                {
                    if (hOldObject != IntPtr.Zero) Gdi32.SelectObject(hdcSrc, hOldObject);
                    if (hBitmap != IntPtr.Zero) Gdi32.DeleteObject(hBitmap);
                    if (hdcSrc != IntPtr.Zero) grSrc.ReleaseHdc(hdcSrc);
                }
            }

            _activeScreenBuffer = 1 - _activeScreenBuffer;
            _lastScreenBuffer = 1 - _lastScreenBuffer;
        }

        private void FadeImages()
        {
            using (var sourceGraphics = Graphics.FromImage(_screenBuffer[_lastScreenBuffer]))
            using (var destinationGraphics = Graphics.FromImage(_screenBuffer[_activeScreenBuffer]))
            {
                IntPtr sourceDC = IntPtr.Zero;
                IntPtr sourceBitmapHandle = IntPtr.Zero;
                IntPtr oldSourceObjectHandle = IntPtr.Zero;

                IntPtr destinationDC = IntPtr.Zero;
                IntPtr destinationBitmapHandle = IntPtr.Zero;
                IntPtr oldDestinationObjectHandle = IntPtr.Zero;

                try
                {
                    sourceDC = sourceGraphics.GetHdc();
                    sourceBitmapHandle = _screenBuffer[_lastScreenBuffer].GetHbitmap();

                    oldSourceObjectHandle = Gdi32.SelectObject(sourceDC, sourceBitmapHandle);
                    if (oldSourceObjectHandle == IntPtr.Zero)
                        throw new Win32Exception();

                    destinationDC = destinationGraphics.GetHdc();
                    destinationBitmapHandle = _screenBuffer[_activeScreenBuffer].GetHbitmap();

                    oldDestinationObjectHandle = Gdi32.SelectObject(destinationDC, destinationBitmapHandle);
                    if (oldDestinationObjectHandle == IntPtr.Zero)
                        throw new Win32Exception();

                    var constantAlphaBlend = new Gdi32.BlendFunction
                    {
                        BlendOp = Gdi32.AC_SRC_OVER,
                        SourceConstantAlpha = 0x99,
                    };

                    if (!Gdi32.AlphaBlend(destinationDC, 0, 0, _screenBuffer[_activeScreenBuffer].Width, _screenBuffer[_activeScreenBuffer].Height,
                                            sourceDC, 0, 0, _screenBuffer[_lastScreenBuffer].Width, _screenBuffer[_lastScreenBuffer].Height,
                                            constantAlphaBlend))
                        throw new Win32Exception();
                }
                finally
                {
                    if (oldDestinationObjectHandle != IntPtr.Zero) Gdi32.SelectObject(destinationDC, oldDestinationObjectHandle);
                    if (destinationBitmapHandle != IntPtr.Zero) Gdi32.DeleteObject(destinationBitmapHandle);
                    if (destinationDC != IntPtr.Zero) destinationGraphics.ReleaseHdc(destinationDC);

                    if (oldSourceObjectHandle != IntPtr.Zero) Gdi32.SelectObject(sourceDC, oldSourceObjectHandle);
                    if (sourceBitmapHandle != IntPtr.Zero) Gdi32.DeleteObject(sourceBitmapHandle);
                    if (sourceDC != IntPtr.Zero) sourceGraphics.ReleaseHdc(sourceDC);
                }
            }
        }
    }
}
