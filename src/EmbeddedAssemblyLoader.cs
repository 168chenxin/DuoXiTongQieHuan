using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace SysSwitch
{
    internal static class EmbeddedAssemblyLoader
    {
        private const string ResourcePrefix = "SysSwitch.Dependencies.";
        private static readonly object SyncRoot = new object();
        private static readonly Dictionary<string, Assembly> LoadedAssemblies =
            new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
        private static bool registered;

        public static void Register()
        {
            if (registered)
            {
                return;
            }

            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
            registered = true;
        }

        private static Assembly ResolveAssembly(object sender, ResolveEventArgs eventArgs)
        {
            string assemblyName = new AssemblyName(eventArgs.Name).Name;
            lock (SyncRoot)
            {
                Assembly loadedAssembly;
                if (LoadedAssemblies.TryGetValue(assemblyName, out loadedAssembly))
                {
                    return loadedAssembly;
                }

                string resourceName = ResourcePrefix + assemblyName + ".dll";
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        return null;
                    }

                    var assemblyBytes = new byte[stream.Length];
                    int offset = 0;
                    while (offset < assemblyBytes.Length)
                    {
                        int bytesRead = stream.Read(assemblyBytes, offset, assemblyBytes.Length - offset);
                        if (bytesRead <= 0)
                        {
                            break;
                        }

                        offset += bytesRead;
                    }

                    if (offset != assemblyBytes.Length)
                    {
                        throw new InvalidDataException("Embedded assembly resource is incomplete: " + resourceName);
                    }

                    loadedAssembly = Assembly.Load(assemblyBytes);
                    LoadedAssemblies[assemblyName] = loadedAssembly;
                    return loadedAssembly;
                }
            }
        }
    }
}
