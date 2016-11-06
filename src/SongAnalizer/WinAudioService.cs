using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SoundFingerprinting.Audio;
using Windows.Storage;
using System.Runtime.InteropServices;
using Windows.Media.Audio;

namespace SongAnalizer
{



    class WinAudioService
    {
        private AudioSamples totalSamples;
        private readonly StorageFile file;

        private WinAudioService(StorageFile file)
        {
            this.file = file;
        }

        public static async Task<WinAudioService> Create(StorageFile file, System.Threading.CancellationToken cancel)
        {
            var audio = new WinAudioService(file);
            audio.totalSamples = await audio.GetMonoData(cancel);
            if (cancel.IsCancellationRequested)
                return null;
            return audio;
        }

        public AudioSamples ReadMonoSamplesFromFileAsync(TimeSpan length, TimeSpan startAt)
        {
            System.Diagnostics.Debug.Assert(totalSamples != null);

            var sourceIndex = (int)(startAt.TotalSeconds * totalSamples.SampleRate);
            var sampleLength = Math.Min((int)(length.TotalSeconds * totalSamples.SampleRate), totalSamples.Samples.Length - sourceIndex);

            float[] samples = new float[sampleLength];

            Array.Copy(totalSamples.Samples, sourceIndex, samples, 0, samples.Length);
            return new AudioSamples() { Duration = length.TotalSeconds, SampleRate = totalSamples.SampleRate, Samples = samples };
        }

        private async Task<AudioSamples> GetMonoData(System.Threading.CancellationToken cancel)
        {
            var graphResult = await Windows.Media.Audio.AudioGraph.CreateAsync(new Windows.Media.Audio.AudioGraphSettings(Windows.Media.Render.AudioRenderCategory.Media));
            using (var graph = graphResult.Graph)
            {
                var fileResult = await graph.CreateFileInputNodeAsync(file);
                var fileInput = fileResult.FileInputNode;
                {
                    var encodingProperties = Windows.Media.MediaProperties.AudioEncodingProperties.CreatePcm(fileInput.EncodingProperties.SampleRate, 1, 32);
                    //var encodingProperties = Windows.Media.MediaProperties.AudioEncodingProperties.CreatePcm(22050, 1, 32);

                    //var erout = await graph.CreateDeviceOutputNodeAsync();
                    //var rOut = erout.DeviceOutputNode;
                    var output = graph.CreateFrameOutputNode(encodingProperties);
                    {

                        // fileInput.AddOutgoingConnection(rOut);
                        fileInput.AddOutgoingConnection(output);
                        //var duratio = fileInput.Duration.TotalSeconds;
                        var taskSource = new TaskCompletionSource<object>();
                        var task = taskSource.Task;
                        fileInput.FileCompleted += (source, e) =>
                        {
                            if (task.IsCompleted)
                                return;
                            taskSource.TrySetResult(null);
                        };
                        cancel.Register(() => taskSource.TrySetResult(null));
                        if (cancel.IsCancellationRequested)
                            return null;
                        graph.Start();
                        await task;

                        if (cancel.IsCancellationRequested)
                            return null;


                        graph.Stop();
                        output.Stop();


                        var audioFrame = output.GetFrame();
                        using (var lockedBuffer = audioFrame.LockBuffer(Windows.Media.AudioBufferAccessMode.ReadWrite))
                        {
                            using (var refference = lockedBuffer.CreateReference())
                            {
                                return await Task.Run(() =>
                                {
                                    unsafe
                                    {
                                        var memoryByteAccess = refference as IMemoryBufferByteAccess;
                                        byte* p;
                                        uint capacity;

                                        memoryByteAccess.GetBuffer(out p, out capacity);
                                        var chanels = (int)output.EncodingProperties.ChannelCount;
                                        var sampleRate = (int)output.EncodingProperties.SampleRate;

                                        int dataLength;
                                        if (audioFrame.Duration == null)
                                        {
                                            dataLength = (int)(capacity / sizeof(float));
                                        }
                                        else
                                        {
                                            dataLength = Math.Min(
                                                (int)(sampleRate * audioFrame.Duration?.TotalSeconds) * chanels,
                                                (int)(capacity / sizeof(float)));
                                        }

                                        float* b = (float*)(p);
                                        float[] floatDataMultipleChanels = new float[dataLength];
                                        for (int i = 0; i < floatDataMultipleChanels.Length; i++)
                                            floatDataMultipleChanels[i] = b[i];

                                        var floatDataMono = new float[dataLength / chanels];

                                        for (int sample = 0; sample < floatDataMultipleChanels.Length; sample += chanels)
                                            for (int channel = 0; channel < chanels; channel++)
                                                floatDataMono[sample / chanels] += floatDataMultipleChanels[sample + channel] / chanels;

                                        var seconds = (double)floatDataMono.Length / output.EncodingProperties.SampleRate;

#if DEBUG
                                        var sb = new StringBuilder();
                                        foreach (var item in floatDataMono.SkipWhile(y => y == 0).Take(sampleRate * 2))
                                        {
                                            sb.Append($"{item} ");
                                        }
                                        System.Diagnostics.Debug.WriteLine($"Leading zeros {floatDataMono.TakeWhile(y => y == 0).Count()} ");
                                        System.Diagnostics.Debug.WriteLine(sb);
#endif


                                        return new AudioSamples() { Samples = floatDataMono, Duration = seconds, SampleRate = sampleRate };
                                    }

                                });
                            }
                        }
                    }
                }


            }
        }

        private void Graph_QuantumProcessed(Windows.Media.Audio.AudioGraph sender, object args)
        {
        }

        [ComImport]
        [Guid("5b0d3235-4dba-4d44-865e-8f1d0e4fd04d")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        unsafe interface IMemoryBufferByteAccess
        {
            void GetBuffer(out byte* buffer, out uint capacity);
        }
    }
}
