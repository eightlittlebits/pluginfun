using System.Windows.Forms;

namespace pluginfun
{
    class DisplayControl : Control
    {
        public DisplayControl()
        {
            SetStyle(ControlStyles.ResizeRedraw, true);
        }
    }
}
