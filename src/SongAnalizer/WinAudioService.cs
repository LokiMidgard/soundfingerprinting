using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SoundFingerprinting.Audio;
using Windows.Storage;

namespace SongAnalizer
{
    class WinAudioService : SoundFingerprinting.Audio.AudioService
    {
        public override IReadOnlyCollection<string> SupportedFormats
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override AudioSamples ReadMonoSamplesFromFile(string pathToSourceFile, int sampleRate, int seconds, int startAt)
        {
            throw new NotImplementedException();
        }

        private async Task<AudioSamples> ReadMonoSamplesFromFile(IStorageFile file, int sampleRate, int length, int startAt)
        {

            int chanels = 0;
            float[] erg = null;
            var graphResult = await Windows.Media.Audio.AudioGraph.CreateAsync(new Windows.Media.Audio.AudioGraphSettings(Windows.Media.Render.AudioRenderCategory.Media));
            using (var graph = graphResult.Graph)
            {
                var encodingProperties = Windows.Media.MediaProperties.AudioEncodingProperties.CreatePcm(22050, 1, 32);

                var fileResult = await graph.CreateFileInputNodeAsync(file);
                var fileInput = fileResult.FileInputNode;
                {
                    var output = graph.CreateFrameOutputNode(encodingProperties);
                    {

                        fileInput.AddOutgoingConnection(output);
                        var duratio = fileInput.Duration.TotalSeconds;
                        var taskSource = new TaskCompletionSource<object>();
                        fileInput.FileCompleted += (source, e) =>
                        {
                            graph.Stop();
                            output.Stop();
                            taskSource.TrySetResult(null);
                        };
                        graph.Start();
                        await taskSource.Task;

                        var audioFrame = output.GetFrame();
                        using (var lockedBuffer = audioFrame.LockBuffer(Windows.Media.AudioBufferAccessMode.ReadWrite))
                        {
                            using (var refference = lockedBuffer.CreateReference())
                            {
                                await Task.Run(() =>
                                {
                                    unsafe
                                    {
                                        var memoryByteAccess = refference as IMemoryBufferByteAccess;
                                        byte* p;
                                        uint capacity;

                                        memoryByteAccess.GetBuffer(out p, out capacity);
                                        chanels = (int)output.EncodingProperties.ChannelCount;
                                        sampleRate = (int)output.EncodingProperties.SampleRate;
                                        int length = Math.Min((int)(sampleRate * duratio) * chanels, (int)(capacity / sizeof(float)));
                                        float* b = (float*)(p);
                                        erg = new float[length];
                                        for (int i = 0; i < erg.Length; i++)
                                            erg[i] = b[i];
                                        seconds = length / (double)output.EncodingProperties.SampleRate / chanels;
                                    }

                                });
                            }
                        }
                    }
                }
                var sb = new StringBuilder();
                foreach (var item in erg.SkipWhile(y => y == 0).Take(sampleRate * 2))
                {
                    sb.Append($"{item} ");
                }
                System.Diagnostics.Debug.WriteLine($"Leading zeros {erg.TakeWhile(y => y == 0).Count()} ");
                System.Diagnostics.Debug.WriteLine(sb);
                return new AudioSamples() { Data = erg, Seconds = seconds, SampleRate = sampleRate, Chanels = chanels };

            }
        }
    }
}
