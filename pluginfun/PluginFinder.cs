using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace pluginfun
{
    public class PluginFinder : MarshalByRefObject
    {
        internal List<Type> SearchPath<T>(string path, string searchPattern, SearchOption searchOption)
        {
            var applicableTypes = new List<Type>();

            foreach (var file in Directory.GetFiles(path, searchPattern, searchOption))
            {
                var assembly = Assembly.LoadFrom(file);
                
                applicableTypes.AddRange(assembly.GetTypes().Where(t => typeof(T).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract).ToList());
            }

            return applicableTypes;
        }
    }
}
