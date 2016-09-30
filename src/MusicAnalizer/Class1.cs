using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apollon.Common;
using Apollon.Common.SongAnalizer;
using System.Composition;
using Windows.Media.Audio;
using System.Runtime.InteropServices;
using SoundFingerprinting.Builder;

namespace MusicAnalizer
{
    [Export(typeof(Apollon.Common.SongAnalizer.IMusicAnalizer))]
    public class LongestLoop : Apollon.Common.SongAnalizer.IMusicAnalizer
    {

        public string Name => "Longest Loop";

        public MusicAnalizerConfiguration PrototypeConfiguration
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public async Task<IEnumerable<Jump>> Analyze(Song song, MusicAnalizerConfiguration configuration)
        {
            var graphResult = await AudioGraph.CreateAsync(new AudioGraphSettings(Windows.Media.Render.AudioRenderCategory.Media) { });
            if (graphResult.Status != AudioGraphCreationStatus.Success)
                throw new Exception("Something somewhere went somehow wrong");

            uint sampleRate;
            float[] samples = null;
            double seconds;
            int chanels = 0;

            List<Tuple<int, IEnumerable<SoundFingerprinting.Query.ResultEntry>>> results = new List<Tuple<int, IEnumerable<SoundFingerprinting.Query.ResultEntry>>>();

            using (var graph = graphResult.Graph)
            {
                using (var inputNode = await song.CreateNode(graph))
                {
                    sampleRate = inputNode.EncodingProperties.SampleRate;
                    var duration = inputNode.Duration.TotalSeconds;
                    graph.EncodingProperties.SampleRate = sampleRate;
                    using (var output = graph.CreateFrameOutputNode())
                    {
                        inputNode.AddOutgoingConnection(output);
                        var waiter = new TaskCompletionSource<object>();
                        inputNode.FileCompleted += (sender, e) =>
                        {
                            output.Stop();
                            graph.Stop();
                            waiter.SetResult(null);
                        };
                        graph.UnrecoverableErrorOccurred += (sender, e) =>
                          {
                              waiter.SetException(new Exception(e.Error.ToString()));
                          };
                        graph.Start();
                        await waiter.Task; // Wait for finnish decoding

                        var frame = output.GetFrame();
                        using (var lockedBuffer = frame.LockBuffer(Windows.Media.AudioBufferAccessMode.ReadWrite))
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
                                        int length = Math.Min((int)(sampleRate * duration) * chanels, (int)(capacity / sizeof(float)));
                                        float* b = (float*)(p);
                                        samples = new float[length];
                                        for (int i = 0; i < samples.Length; i++)
                                            samples[i] = b[i];
                                        seconds = length / (double)output.EncodingProperties.SampleRate / chanels;
                                    }
                                });
                            }
                        }


                    }
                }
            }

            var memoryModel = new SoundFingerprinting.InMemory.InMemoryModelService();
            var fingerprintCommandBuilder = new FingerprintCommandBuilder();
            var secondsTotest = 3;
            int samplesToTest = (int)(secondsTotest * sampleRate * chanels);
            var stepSize = samplesToTest / 2;
            var currentSamples = new float[samplesToTest];

            // build up database

            for (int start = 0; start + samplesToTest < samples.Length; start += stepSize)
            {
                var track = memoryModel.InsertTrack(new SoundFingerprinting.DAO.Data.TrackData() { ReleaseYear = start });

                Array.Copy(samples, start, currentSamples, 0, samplesToTest);

                var hashes = await fingerprintCommandBuilder.BuildFingerprintCommand()
                     .From(new SoundFingerprinting.Audio.AudioSamples()
                     {
                         SampleRate = (int)sampleRate,
                         Duration = stepSize / chanels / (double)sampleRate,
                         Origin = start.ToString(),
                         Samples = currentSamples
                     })
                    .WithFingerprintConfig(config => config.SampleRate = (int)sampleRate)
                    .UsingServices(null)
                    .Hash();

                memoryModel.InsertHashDataForTrack(hashes, track);

            }

            // search

            var querryCommandBuilder = new QueryCommandBuilder();

            for (int start = 0; start + samplesToTest < samples.Length; start += stepSize)
            {

                Array.Copy(samples, start, currentSamples, 0, samplesToTest);

                var query = await querryCommandBuilder.BuildQueryCommand()
                     .From(new SoundFingerprinting.Audio.AudioSamples()
                     {
                         SampleRate = (int)sampleRate,
                         Duration = stepSize / chanels / (double)sampleRate,
                         Origin = start.ToString(),
                         Samples = currentSamples
                     })
                     .UsingServices(memoryModel, null)
                     .Query();

                if (!query.IsSuccessful)
                    continue;

                results.Add(Tuple.Create(start, query.ResultEntries.Where(x => x.Track.ReleaseYear != start)));





            }



            return results
                .SelectMany(x => x.Item2.Select(y => new { StartSample = Math.Max(x.Item1, y.Track.ReleaseYear), TargetSample = Math.Min(x.Item1, y.Track.ReleaseYear), Similarity = y.Similarity }))

                .OrderBy(x => x.Similarity)
                    .ThenBy(x => Math.Abs(x.StartSample - x.TargetSample))

                    .Select(x => new Jump()
                    {
                        TargetSong = song,
                        CrossFade = TimeSpan.FromSeconds(secondsTotest),
                        Origin = TimeSpan.FromSeconds(x.StartSample / chanels / (double)sampleRate),
                        TargetTime = TimeSpan.FromSeconds(x.TargetSample / chanels / (double)sampleRate),
                    }).ToArray();

        }
    }

    [ComImport]
    [Guid("5b0d3235-4dba-4d44-865e-8f1d0e4fd04d")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }
}
