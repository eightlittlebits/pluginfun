using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;
using pluginfun.common;

namespace pluginfun
{
    partial class ConfigForm : Form
    { 
        public IConfiguration Configuration { get; }

        public ConfigForm(IEmulatedSystem emulatedSystem)
        {
            InitializeComponent();

            Text = $"{emulatedSystem.Name} Configuration";

            Configuration = emulatedSystem.Configuration.Copy();

            PrepareUserInterface();
        }

        private void PrepareUserInterface()
        {
            // list all public, non-inherited instance properties
            var properties = Configuration.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).ToList();

            configTableLayoutPanel.RowCount = properties.Count;

            for (int i = 0; i < properties.Count; i++)
            {
                PropertyInfo property = properties[i];

                // get the description attribute if present
                string propertyDescription = property.GetCustomAttribute<DescriptionAttribute>()?.Description ?? property.Name;

                if (property.PropertyType == typeof(bool))
                {
                    // add a checkbox for bool properties
                    var checkBox = new CheckBox() { Text = propertyDescription, Dock = DockStyle.Fill, AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(6, 3, 3, 3) };
                    checkBox.DataBindings.Add(nameof(checkBox.Checked), Configuration, property.Name, false, DataSourceUpdateMode.OnPropertyChanged);
                    configTableLayoutPanel.Controls.Add(checkBox, 0, i);
                    configTableLayoutPanel.SetColumnSpan(checkBox, 4);
                }
                else
                {
                    configTableLayoutPanel.Controls.Add(new Label() { Text = propertyDescription, Dock = DockStyle.Fill, AutoSize = true, TextAlign = ContentAlignment.MiddleLeft }, 0, i);

                    if (property.PropertyType == typeof(string))
                    {
                        var textBox = new TextBox() { Dock = DockStyle.Fill, TextAlign = HorizontalAlignment.Left };
                        textBox.DataBindings.Add(nameof(textBox.Text), Configuration, property.Name, false, DataSourceUpdateMode.OnPropertyChanged);
                        configTableLayoutPanel.Controls.Add(textBox, 1, i);

                        if (property.Name.EndsWith("Path"))
                        {
                            textBox.ReadOnly = true;

                            var browseButton = new Button() { Text = "...", ClientSize = new Size(25, textBox.Height), Tag = textBox };
                            configTableLayoutPanel.Controls.Add(browseButton, 2, i);

                            switch (property.GetCustomAttribute<PathTypeAttribute>())
                            {
                                case FilePathAttribute file:
                                    browseButton.Click += (s, ev) =>
                                    {
                                        var pathTextBox = (TextBox)((Button)s).Tag;
                                        string filepath = !string.IsNullOrWhiteSpace(pathTextBox.Text) ? Path.GetDirectoryName(pathTextBox.Text) : string.Empty;

                                        pathTextBox.Text = DisplayOpenFileDialog(filepath, false);
                                    };
                                    break;

                                case FolderPathAttribute folder:
                                    browseButton.Click += (s, ev) =>
                                    {
                                        var pathTextBox = (TextBox)((Button)s).Tag;

                                        pathTextBox.Text = DisplayOpenFileDialog(pathTextBox.Text, true);
                                    };
                                    break;

                                case null:
                                    throw new Exception($"Path setting {property.Name} missing the path type attribute");
                            }

                            var clearButton = new Button() { Text = "Clear", ClientSize = new Size(40, textBox.Height), Tag = textBox };
                            clearButton.Click += (s, ev) => ((TextBox)((Button)s).Tag).Text = string.Empty;

                            configTableLayoutPanel.Controls.Add(clearButton, 3, i);
                        }
                        else
                            configTableLayoutPanel.SetColumnSpan(textBox, 3);
                    }
                    else if (property.PropertyType.BaseType == typeof(Enum))
                    {
                        var comboBox = new ComboBox()
                        {
                            Dock = DockStyle.Fill,
                            DropDownStyle = ComboBoxStyle.DropDownList,
                            DisplayMember = "Text",
                            ValueMember = "Value",
                        };

                        comboBox.DataSource = Enum.GetValues(property.PropertyType)
                            .Cast<Enum>()
                            .Select(value => new
                            {
                                Text = property.PropertyType.GetField(value.ToString()).GetCustomAttribute<DescriptionAttribute>()?.Description ?? Enum.GetName(property.PropertyType, value),
                                Value = value
                            }).ToList();

                        comboBox.DataBindings.Add(nameof(comboBox.SelectedValue), Configuration, property.Name, false, DataSourceUpdateMode.OnPropertyChanged);
                        configTableLayoutPanel.Controls.Add(comboBox, 1, i);
                        configTableLayoutPanel.SetColumnSpan(comboBox, 3);
                    }
                }
            }
        }

        private string DisplayOpenFileDialog(string path, bool isFolderPicker)
        {
            using (var openFileDialog = new CommonOpenFileDialog())
            {
                openFileDialog.InitialDirectory = path;
                openFileDialog.IsFolderPicker = isFolderPicker;

                if (openFileDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    path = openFileDialog.FileName;
                }
            }

            return path;
        }
    }
}
