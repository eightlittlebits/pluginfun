using System.Windows.Forms;
using pluginfun.common;

namespace ExternalPluginTwo
{
    public class ExternalPlugin : IPluginTwo
    {
        public string Name => "External Plugin Two";

        public void Execute()
        {
            MessageBox.Show("Execute in External Plugin Two!");
        }
    }
}
