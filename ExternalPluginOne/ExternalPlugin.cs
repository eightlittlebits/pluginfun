﻿using System.Windows.Forms;
using pluginfun.shared;

namespace ExternalPluginOne
{
    public class ExternalPlugin : IPluginOne
    {
        public string Name => "External Plugin One";

        public void DoTheThing()
        {
            MessageBox.Show("Really, from a dynamically loaded external plugin?");
        }
    }
}
