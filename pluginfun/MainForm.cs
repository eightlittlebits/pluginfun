using System;
using System.Linq;
using System.Windows.Forms;

namespace pluginfun
{
    public partial class MainForm : Form
    {
        const int RecentFileCount = 10;

        PluginfunConfig _config;

        public MainForm()
        {
            InitializeComponent();

            _config = PluginfunConfig.Load();

            PrepareUserInterface();
            //PrepareDataBindings();
        }

        private void PrepareUserInterface()
        {
            UpdateRecentFilesMenu();
        }

        #region Recent Files

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
                    string filePath = (string)(s as ToolStripMenuItem).Tag;

                    MessageBox.Show(filePath);
                };

                recentFilesToolStripMenuItem.DropDownItems.Add(menuItem);
            }
        }

        #endregion

        private void PrepareDataBindings()
        {
            throw new NotImplementedException();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _config.Save();

            base.OnFormClosing(e);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
