using System.Windows.Forms;
using pluginfun.shared;

namespace pluginfun
{
    class InternalPluginTwo : IPluginOne
    {
        public string Name => "Internal Plugin Two";

        public void DoTheThing()
        {
            MessageBox.Show("Doing the thing in Internal Plugin Two!");
        }
    }
}
