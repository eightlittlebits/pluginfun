using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace pluginfun
{
    [Serializable]
    class PluginFinder : MarshalByRefObject
    {
        internal List<Type> SearchDirectory<T>(string path, string searchPattern, bool recurseSubdirectories)
        {
            SearchOption searchOption = recurseSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            return SearchAssemblies<T>(Directory.GetFiles(path, searchPattern, searchOption).Select(file => Assembly.LoadFrom(file)));
        }

        internal List<Type> SearchAssemblies<T>(IEnumerable<Assembly> assemblies)
        {
            var applicableTypes = new List<Type>();

            foreach (var assembly in assemblies)
            {
                applicableTypes.AddRange(GetImplementationsFromAssembly<T>(assembly));
            }

            return applicableTypes;
        }

        private List<Type> GetImplementationsFromAssembly<T>(Assembly assembly)
        {
            if (!typeof(T).IsInterface) throw new Exception($"{nameof(GetImplementationsFromAssembly)} called with non-interface type {typeof(T).Name}");

            return assembly.GetTypes().Where(t => typeof(T).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract).ToList();
        }
    }
}
