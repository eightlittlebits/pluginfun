using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace pluginfun
{
    [Serializable]
    class AddinLoader : MarshalByRefObject
    {
        internal static List<Type> Load<T>(string pluginPath)
        {
            if (!typeof(T).IsInterface) throw new Exception($"{nameof(Load)} called with non-interface type {typeof(T).Name}");

            var addins = new List<Type>();

            // load any matching types in the currently loaded assemblies, excluding any in the GAC and any dynamically generated assemblies (xmlserlializer etc)
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.GlobalAssemblyCache && !a.IsDynamic).ToList();

#if DEBUG
            // this might be loaded if we've used the debugger before we reach this point
            loadedAssemblies.RemoveAll(x => x.FullName.StartsWith("Microsoft.VisualStudio.Debugger.Runtime"));
#endif

            addins.AddRange(GetImplementationsFromAssemblies<T>(loadedAssemblies));

            // scan for plugins in other dlls in a new appdomain so any assemblies scanned and not containing plugins are unloaded
            using (var appDomain = new AppDomainWithType<AddinLoader>())
            {
                var addinLoader = appDomain.TypeObject;

                addins.AddRange(addinLoader.GetImplementationsFromPath<T>(pluginPath, "*.dll", true));
            }

            return addins;
        }

        private List<Type> GetImplementationsFromPath<T>(string path, string searchPattern, bool recurseSubdirectories)
        {
            SearchOption searchOption = recurseSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            return GetImplementationsFromAssemblies<T>(Directory.GetFiles(path, searchPattern, searchOption).Select(file => Assembly.LoadFrom(file))).ToList();
        }

        private static IEnumerable<Type> GetImplementationsFromAssemblies<T>(IEnumerable<Assembly> assemblies)
        {
            return assemblies.SelectMany(a => GetImplementationsFromAssembly<T>(a));
        }

        private static List<Type> GetImplementationsFromAssembly<T>(Assembly assembly)
        {
            return assembly.GetTypes().Where(t => typeof(T).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract).ToList();
        }
    }
}
