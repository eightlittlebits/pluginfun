﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginfun.common
{
    public interface IPluginTwo : IDynamicallyLoadableComponent
    {
        void Execute();
    }
}
