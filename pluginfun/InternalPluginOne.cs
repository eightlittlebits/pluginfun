﻿using System.Windows.Forms;
using pluginfun.shared;

namespace pluginfun
{
    class InternalPluginOne : IPlugin
    {
        public string Name => "Internal Plugin One";

        public void DoTheThing()
        {
            MessageBox.Show("Doing the thing in Internal Plugin One!");
        }
    }
}
