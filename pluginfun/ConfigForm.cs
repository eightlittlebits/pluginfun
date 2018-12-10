using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace pluginfun
{
    public partial class ConfigForm : Form
    {
        public ConfigForm(ISystemConfiguration configuration)
        {
            InitializeComponent();

            var properties = configuration.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).ToList();

            configTableLayoutPanel.RowCount = properties.Count + 1;

            for (int i = 0; i < properties.Count; i++)
            {
                PropertyInfo property = properties[i];

                // get the description attribute if present
                string propertyDescription = property.GetCustomAttribute<DescriptionAttribute>()?.Description ?? property.Name;

                if (property.PropertyType == typeof(bool))
                {
                    // add a checkbox for bool properties
                    var checkBox = new CheckBox() { Text = propertyDescription, Dock = DockStyle.Fill, AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(6, 3, 3, 3) };
                    checkBox.DataBindings.Add(nameof(checkBox.Checked), configuration, property.Name, false, DataSourceUpdateMode.OnPropertyChanged);
                    configTableLayoutPanel.Controls.Add(checkBox, 0, i);
                    configTableLayoutPanel.SetColumnSpan(checkBox, 4);
                }
                else
                {
                    configTableLayoutPanel.Controls.Add(new Label() { Text = propertyDescription, Dock = DockStyle.Fill, AutoSize = true, TextAlign = ContentAlignment.MiddleLeft }, 0, i);

                    if (property.PropertyType == typeof(string))
                    {
                        var textbox = new TextBox() { Dock = DockStyle.Fill, TextAlign = HorizontalAlignment.Left };
                        textbox.DataBindings.Add(nameof(textbox.Text), configuration, property.Name, false, DataSourceUpdateMode.OnPropertyChanged);
                        configTableLayoutPanel.Controls.Add(textbox, 1, i);
                        configTableLayoutPanel.SetColumnSpan(textbox, 3);
                    }

                }
            }
        }
    }
}
