using System.Windows.Forms;
using pluginfun.common;

namespace pluginfun
{
    class InternalPluginTwo : IPluginTwo
    {
        public string Name => "Internal Plugin Two";

        public void Execute()
        {
            MessageBox.Show("Execute in Internal Plugin Two!");
        }
    }
}
