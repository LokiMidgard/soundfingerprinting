using Apollon.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Audio;
using Windows.Storage;
using Windows.UI.Xaml;

namespace SampleApp
{
    public class PlayerViewmodel : DependencyObject
    {



        public Windows.Storage.StorageFile File
        {
            get { return (Windows.Storage.StorageFile)GetValue(FileProperty); }
            set { SetValue(FileProperty, value); }
        }

        // Using a DependencyProperty as the backing store for File.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FileProperty =
            DependencyProperty.Register("File", typeof(Windows.Storage.StorageFile), typeof(PlayerViewmodel), new PropertyMetadata(null, FileChanged));

        private static async void FileChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var me = d as PlayerViewmodel;
            var file = e.NewValue as StorageFile;
            if (file != null)
            {
                var m = await file.Properties.GetMusicPropertiesAsync();
                me.Duration = m.Duration.TotalSeconds;

                me.MusicPlayer = new Player(file, me);
                await me.MusicPlayer.InitAudio();
                me.MusicPlayer.Start = TimeSpan.FromSeconds(me.Start);
                me.MusicPlayer.End = TimeSpan.FromSeconds(me.End);

                me.MusicPlayer.PropertyChanged += async (sender, ep) =>
                {
                    if (ep.PropertyName == nameof(Player.Position))
                        await me.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            me.Position = me.MusicPlayer.Position.TotalSeconds;
                        });
                };

            }
        }

        public double Duration
        {
            get { return (double)GetValue(DurutationProperty); }
            set { SetValue(DurutationProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Durutation.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DurutationProperty =
            DependencyProperty.Register(nameof(Duration), typeof(double), typeof(PlayerViewmodel), new PropertyMetadata(0.2));





        public double Start
        {
            get { return (double)GetValue(StartProperty); }
            set { SetValue(StartProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Start.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StartProperty =
            DependencyProperty.Register("Start", typeof(double), typeof(PlayerViewmodel), new PropertyMetadata(0.0, StartChanged));

        private static void StartChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var me = d as PlayerViewmodel;
            var time = TimeSpan.FromSeconds((double)e.NewValue);

            if (me.MusicPlayer != null)
                me.MusicPlayer.Start = time;
        }

        public double End
        {
            get { return (double)GetValue(EndProperty); }
            set { SetValue(EndProperty, value); }
        }

        // Using a DependencyProperty as the backing store for End.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EndProperty =
            DependencyProperty.Register("End", typeof(double), typeof(PlayerViewmodel), new PropertyMetadata(0.0, EndChanged));

        private static void EndChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var me = d as PlayerViewmodel;
            var time = TimeSpan.FromSeconds((double)e.NewValue);

            if (me.MusicPlayer != null)
                me.MusicPlayer.End = time;
        }

        public double Position
        {
            get { return (double)GetValue(PositionProperty); }
            set { SetValue(PositionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Position.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PositionProperty =
            DependencyProperty.Register("Position", typeof(double), typeof(PlayerViewmodel), new PropertyMetadata(0.0));




        public bool IsPlaying
        {
            get { return (bool)GetValue(IsPlayingProperty); }
            set { SetValue(IsPlayingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsPlaying.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsPlayingProperty =
            DependencyProperty.Register("IsPlaying", typeof(bool), typeof(PlayerViewmodel), new PropertyMetadata(false));




        public Apollon.Common.Jump Jump
        {
            get { return (Apollon.Common.Jump)GetValue(JumpProperty); }
            set { SetValue(JumpProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Jump.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty JumpProperty =
            DependencyProperty.Register("Jump", typeof(Apollon.Common.Jump), typeof(PlayerViewmodel), new PropertyMetadata(null, JumpChanged));




        public Player MusicPlayer
        {
            get { return (Player)GetValue(MusicPlayerProperty); }
            set { SetValue(MusicPlayerProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MusicPlayer.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MusicPlayerProperty =
            DependencyProperty.Register("MusicPlayer", typeof(Player), typeof(PlayerViewmodel), new PropertyMetadata(null));



        private static void JumpChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var me = d as PlayerViewmodel;
            var newJump = e.NewValue as Jump;
            if (newJump != null)
            {
                me.Start = newJump.Origin.TotalSeconds;
                me.End = newJump.TargetTime.TotalSeconds;
            }
        }

        public void Play()
        {
            MusicPlayer.IsPlaying = !MusicPlayer.IsPlaying;
        }

        [PropertyChanged.ImplementPropertyChanged]
        public class Player : INotifyPropertyChanged
        {
            private readonly StorageFile file;

            public event PropertyChangedEventHandler PropertyChanged;

            public AudioGraph Graph { get; private set; }
            public AudioFileInputNode InputPrimary { get; private set; }
            public AudioFileInputNode InputSecondary { get; private set; }
            public AudioDeviceOutputNode Output { get; private set; }

            public Player(StorageFile file, DependencyObject parent)
            {
                this.file = file;
                this.parent = parent;
            }


            public TimeSpan Start { get; set; }
            public TimeSpan End { get; set; }
            public TimeSpan CrossFade { get; set; } = TimeSpan.FromSeconds(5);






            public double Gain1 { get; private set; }
            public double Gain2 { get; private set; }
            public double Pos1 { get; private set; }
            public double Pos2 { get; private set; }





            public TimeSpan Position
            {
                get
                {
                    return InputPrimary.Position;
                }
                set
                {
                    InputPrimary.Seek(value);
                }
            }


            private bool isPlaying;
            public bool IsPlaying
            {
                get { return isPlaying; }
                set
                {
                    if (isPlaying == value)
                        return;
                    isPlaying = value;
                    if (isPlaying)
                    {
                        Graph.Start();
                        IsSeccondaryPlaying = false;
                    }
                    else
                        Graph.Stop();
                }
            }
            private bool isSecondaryPlaying;
            private readonly DependencyObject parent;

            public bool IsSeccondaryPlaying
            {
                get { return isSecondaryPlaying; }
                set
                {
                    if (isSecondaryPlaying == value)
                        return;
                    isSecondaryPlaying = value;
                    if (isSecondaryPlaying)
                        InputSecondary.Start();
                    else
                        InputSecondary.Stop();
                }
            }

            public async Task InitAudio()
            {
                if (Graph == null)
                    await InitAudioGraph();
                if (InputPrimary == null)
                    InputPrimary = await CreateDeviceInputNode(Graph, file);
                if (InputSecondary == null)
                    InputSecondary = await CreateDeviceInputNode(Graph, file);
                if (Output == null)
                    await CreateDeviceOutputNode(Graph);

                InputPrimary.AddOutgoingConnection(Output);
                InputSecondary.AddOutgoingConnection(Output);
                InputSecondary.Stop();
                Graph.QuantumProcessed += (sender, e) =>
                {
                    if (Position > Start)
                    {
                        Position -= Start - End; // Move Back
                        IsSeccondaryPlaying = false; // Stop Seccond
                        InputPrimary.OutgoingGain = 1; // restet Sound
                        Gain1 = 1;
                    }

                    if (Position > Start - CrossFade) // Modify Volume & Start second Stream
                    {
                        if (!IsSeccondaryPlaying)
                        {
                            InputSecondary.Seek(Position - (Start - End));
                            IsSeccondaryPlaying = true;
                        }
                        InputPrimary.OutgoingGain = 1 - ((Position - (Start - CrossFade)).TotalMilliseconds / CrossFade.TotalMilliseconds);
                        InputSecondary.OutgoingGain = ((Position - (Start - CrossFade)).TotalMilliseconds / CrossFade.TotalMilliseconds);
                    }

                    Pos2 = InputSecondary.Position.TotalSeconds;
                    Pos1 = InputPrimary.Position.TotalSeconds;
                    Gain2 = InputSecondary.OutgoingGain;
                    Gain1 = InputPrimary.OutgoingGain;

                    NotifyPropertyChanged(nameof(Position));
                };
            }

            protected async void NotifyPropertyChanged([CallerMemberName] string propertyName = null) =>
                await parent.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                 PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));



            private async Task InitAudioGraph()
            {

                AudioGraphSettings settings = new AudioGraphSettings(Windows.Media.Render.AudioRenderCategory.Media);

                CreateAudioGraphResult result = await AudioGraph.CreateAsync(settings);
                if (result.Status != AudioGraphCreationStatus.Success)
                {
                    throw new Exception();
                }

                Graph = result.Graph;

            }

            private async Task<AudioFileInputNode> CreateDeviceInputNode(AudioGraph audioGraph, IStorageFile file)
            {
                // Create a device output node
                var result = await audioGraph.CreateFileInputNodeAsync(file);

                if (result.Status != AudioFileNodeCreationStatus.Success)
                {
                    throw new Exception();
                }

                return result.FileInputNode;
            }

            private async Task CreateDeviceOutputNode(AudioGraph audioGraph)
            {
                // Create a device output node
                CreateAudioDeviceOutputNodeResult result = await audioGraph.CreateDeviceOutputNodeAsync();

                if (result.Status != AudioDeviceNodeCreationStatus.Success)
                {
                    // Cannot create device output node
                    throw new Exception();
                }

                Output = result.DeviceOutputNode;
            }
        }
    }
}
