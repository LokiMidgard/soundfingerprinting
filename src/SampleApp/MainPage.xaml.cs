using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Apollon.Common;
using System.Composition.Hosting;
using System.Reflection;
using System.Threading.Tasks;
using Apollon.Common.SongAnalizer;
using System.Collections.ObjectModel;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SampleApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        internal Viewmodel Model => DataContext as Viewmodel;


        private async void Button_Click(object sender, RoutedEventArgs e)
        {


            var dialog = new Windows.Storage.Pickers.FileOpenPicker();

            dialog.FileTypeFilter.Add(".mp3");

            var file = await dialog.PickSingleFileAsync();
            if (file == null)
                return;
            Model.LoadFile(file);

        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            await Model.Analyze();
        }
    }
}
