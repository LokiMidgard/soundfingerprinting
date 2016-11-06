using Apollon.Common;
using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Audio;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Die Vorlage "Leere Seite" ist unter http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409 dokumentiert.

namespace SampleApp
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Button.IsEnabled = false;
                this.ProgressRing.IsActive = true;
                var loop = new MusicAnalizer.LongestLoop();

                var dialog = new Windows.Storage.Pickers.FileOpenPicker();

                dialog.FileTypeFilter.Add(".mp3");

                var file = await dialog.PickSingleFileAsync();

                var fileSong = await FileSong.Create(file);
                var song = new Apollon.Common.Song(fileSong);
                var result = await loop.Analyze(song, null);
                List.Items.Clear();
                foreach (var item in result)
                    List.Items.Add(item);

            }
            finally
            {
                this.Button.IsEnabled = true;
                this.ProgressRing.IsActive = false;
            }
        }
    }


    class FileSong : ISongData
    {
        private IStorageFile file;
        [DataMember]
        public string Album { get; }
        [DataMember]
        public string Artist { get; }
        [DataMember]
        public TimeSpan Duration { get; }
        [DataMember]
        public string Title { get; }
        [DataMember]
        public uint TrackNumber { get; }



        private FileSong(StorageFile file, string album, string artist, TimeSpan duration, string title, uint trackNumber)
        {
            this.file = file;
            this.Album = album;
            this.Artist = artist;
            this.Duration = duration;
            this.Title = title;
            this.TrackNumber = trackNumber;
        }

        public async Task<AudioFileInputNode> CreateNode(AudioGraph graph)
        {
            var result = await graph.CreateFileInputNodeAsync(file);
            if (result.Status != AudioFileNodeCreationStatus.Success)
                throw new Exception("Faild To Create InputNode");
            return result.FileInputNode;
        }

        public static async Task<FileSong> Create(StorageFile file)
        {
            var musicPropertys = await file.Properties.GetMusicPropertiesAsync();
            var album = musicPropertys.Album;
            var artist = musicPropertys.Artist;
            var duration = musicPropertys.Duration;
            var title = musicPropertys.Title;
            var trackNumber = musicPropertys.TrackNumber;
            return new FileSong(file, album, artist, duration, title, trackNumber);
        }

        [Export(typeof(ISongLookUp))]
        private class Poplator : ISongLookUp
        {
            private Dictionary<ISongData, FileSong> lookup;
            TaskCompletionSource<object> waiter;
            private async Task Init()
            {
                if (waiter != null)
                {
                    await waiter.Task;
                    return;
                }
                waiter = new TaskCompletionSource<object>();

                var musicLib = await Windows.Storage.StorageLibrary.GetLibraryAsync(Windows.Storage.KnownLibraryId.Music);

                var folderQueue = new Queue<Windows.Storage.StorageFolder>(musicLib.Folders);
                var mp3List = new List<Windows.Storage.StorageFile>();
                while (folderQueue.Any())
                {
                    var f = folderQueue.Dequeue();
                    foreach (var subFolder in await f.GetFoldersAsync())
                        folderQueue.Enqueue(subFolder);

                    foreach (var file in await f.GetFilesAsync())
                    {
                        if (file.FileType != ".mp3")
                            continue;
                        mp3List.Add(file);
                    }
                }

                var songs = await Task.WhenAll(mp3List
                                            .AsParallel()
                                            .Select(FileSong.Create));
                this.lookup = songs.ToDictionary(x => x, x => x, new SongMetadataEqualaty());

                this.waiter.SetResult(lookup);
            }




            public async Task<bool> UpdateSong(Song song)
            {
                if (song.IsReady)
                    return true;
                await Init();
                if (lookup.ContainsKey(song))
                {
                    song.Update(lookup[song]);
                    return true;
                }

                return false;
            }

        }
    }
}
