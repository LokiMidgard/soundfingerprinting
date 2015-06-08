namespace SoundFingerprinting.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Ninject;
#if WINDOWS_UAP
    using Windows.ApplicationModel;
#endif

    internal class DependencyResolver
    {
        private static readonly IDependencyResolver Resolver;

        static DependencyResolver()
        {
            Resolver = new DefaultDependencyResolver();
        }

        public static IDependencyResolver Current
        {
            get
            {
                return Resolver;
            }
        }

        private sealed class DefaultDependencyResolver : IDependencyResolver, IDisposable
        {
            private const string MainAssemblyName = "SoundFingerprinting";

            private readonly IKernel kernel;

            public DefaultDependencyResolver()
            {
                kernel = new StandardKernel();
                SubscribeToAssemblyLoadingEvent();
                LoadAllAssemblyBindings();
            }

            ~DefaultDependencyResolver()
            {
                Dispose(false);
            }

            public T Get<T>()
            {
                return kernel.Get<T>();
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool isDisposing)
            {
                if (isDisposing)
                {
                    kernel.Dispose();
                }
            }

            private void SubscribeToAssemblyLoadingEvent()
            {
#if WINDOWS_UAP
            }

#else
                AppDomain.CurrentDomain.AssemblyLoad += CurrentDomainOnAssemblyLoad;
            }

            private void CurrentDomainOnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
            {
                if (args.LoadedAssembly.FullName.Contains(MainAssemblyName))
                {
                    LoadAssemblyBindings(args.LoadedAssembly);
                }
            }
#endif

            private void LoadAllAssemblyBindings()
            {
#if WINDOWS_UAP
                Task.Run(async () =>
                {
                    var folder = Package.Current.InstalledLocation;
                    foreach (var file in await folder.GetFilesAsync())
                    {
                        if (file.FileType == ".dll")
                        {
                            var assemblyName = new AssemblyName(file.DisplayName);
                            if (!assemblyName.FullName.Contains(MainAssemblyName))
                                continue;
                            var assembly = Assembly.Load(assemblyName);
                            LoadAssemblyBindings(assembly);

                        }
                    }
                }).Wait();
#else

                var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies() 
                                                .Where(assembly => assembly.FullName.Contains(MainAssemblyName));

                foreach (var loadedAssembly in loadedAssemblies)
                {
                    LoadAssemblyBindings(loadedAssembly);
                }
#endif
            }

            private void LoadAssemblyBindings(Assembly loadedAssembly)
            {
                var moduleLoaders = GetModuleLoaders(loadedAssembly);
                foreach (var moduleLoader in moduleLoaders)
                {
                    moduleLoader.LoadAssemblyBindings(kernel);
                }
            }

            private IEnumerable<IModuleLoader> GetModuleLoaders(Assembly loadedAssembly)
            {
#if WINDOWS_UAP
                var moduleLoaders = from type in loadedAssembly.DefinedTypes
                                    where type.ImplementedInterfaces.Contains(typeof(IModuleLoader)) && (type.DeclaredConstructors.Where(c => c.GetParameters().Length == 0).Any() || !type.DeclaredConstructors.Any())
                                    select Activator.CreateInstance(type.AsType()) as IModuleLoader;
                return moduleLoaders;
#else
                var moduleLoaders = from type in loadedAssembly.GetTypes()
                                    where type.GetInterfaces().Contains(typeof(IModuleLoader)) && type.GetConstructor(Type.EmptyTypes) != null
                                    select Activator.CreateInstance(type) as IModuleLoader;
                return moduleLoaders;
#endif
            }
        }
    }
}
