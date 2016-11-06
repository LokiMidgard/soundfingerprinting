using Apollon.Common;
using Apollon.Common.SongAnalizer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Composition.Hosting;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using System.Threading;

namespace SampleApp
{
    class Viewmodel : DependencyObject
    {
        public ObservableCollection<Jump> Data { get; } = new ObservableCollection<Jump>();

        public PlayerViewmodel Player { get; } = new PlayerViewmodel();

        public void LoadFile(StorageFile file)
        {
            this.Player.File = file;
        }



        public AnalizingProgess Progress
        {
            get { return (AnalizingProgess)GetValue(ProgressProperty); }
            set { SetValue(ProgressProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Progress.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ProgressProperty =
            DependencyProperty.Register("Progress", typeof(AnalizingProgess), typeof(Viewmodel), new PropertyMetadata(default(AnalizingProgess)));



        public bool IsLoading
        {
            get { return (bool)GetValue(IsLoadingProperty); }
            set { SetValue(IsLoadingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsLoading.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register("IsLoading", typeof(bool), typeof(Viewmodel), new PropertyMetadata(false));
        private CancellationTokenSource cancel;

        public async Task Analyze()
        {
            if (cancel != null)
            {
                cancel.Cancel();
                return;
            }
            IsLoading = true;
            try
            {
                IEnumerable<Assembly> assemblys = await GetAssemblyList();

                assemblys = assemblys.Where(x => x.FullName.Contains("SongAnalizer"));

                var configuration = new ContainerConfiguration()
                        .WithAssemblies(assemblys);
                var compositionHost = configuration.CreateContainer();

                var a = compositionHost.GetExport<IMusicAnalize>();

                cancel = new System.Threading.CancellationTokenSource();
                Progress = default(AnalizingProgess);
                var ergs = await a.Analyze(this.Player.File, null, new Progress<AnalizingProgess>(async p =>
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        if (p > Progress)
                            Progress = p;
                    });
                }),cancel.Token);
                cancel = null;

                Data.Clear();
                foreach (var item in ergs)
                    Data.Add(item);

            }
            finally
            {
                IsLoading = false;
            }
        }


        private static async Task<List<Assembly>> GetAssemblyList()
        {
            List<Assembly> assemblies = new List<Assembly>();

            var files = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFilesAsync();
            if (files == null)
                return assemblies;

            foreach (var file in files.Where(file => file.FileType == ".dll" || file.FileType == ".exe"))
            {
                try
                {
                    assemblies.Add(Assembly.Load(new AssemblyName(file.DisplayName)));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }

            }

            return assemblies;
        }


    }
}
