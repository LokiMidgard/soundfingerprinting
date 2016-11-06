using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apollon.Common;
using Apollon.Common.SongAnalizer;
using SoundFingerprinting;
using SoundFingerprinting.Audio;
using SoundFingerprinting.Builder;
using SoundFingerprinting.DAO.Data;
using System.Composition;
using System.Threading;

namespace SongAnalizer
{
    [Export(typeof(IMusicAnalize))]
    class Analizer : Apollon.Common.SongAnalizer.IMusicAnalize
    {
        public string Name => "Sound Fingerprinting Analizer";

        public MusicAnalizerConfiguration PrototypeConfiguration => new Configuration();


        private readonly IFingerprintCommandBuilder fingerprintCommandBuilder = new FingerprintCommandBuilder();

        public Task<IEnumerable<Jump>> Analyze(Windows.Storage.StorageFile song, MusicAnalizerConfiguration configuration, IProgress<AnalizingProgess> p = null, CancellationToken canel = default(CancellationToken))
        {
            configuration = configuration ?? PrototypeConfiguration;
            if (!(configuration is Configuration))
                throw new ArgumentException("Configuration must be created using Prototype Configuration", nameof(configuration));
            return Analyze(song, configuration as Configuration, p, canel);

        }
        //private async Task<IEnumerable<Jump>> Analyze(Windows.Storage.StorageFile song, Configuration configuration, IProgress<AnalizingProgess> p = null, CancellationToken canel = default(CancellationToken))
        //{
        //    int timeFrameInMs = (int)configuration.Corssfade.Value.TotalMilliseconds;
        //    int grain = (int)configuration.Grain.Value.TotalMilliseconds;
        //    WinAudioService audioService = await WinAudioService.Create(song);

        //    var mp = await song.Properties.GetMusicPropertiesAsync();

        //    IModelService modelService = new SoundFingerprinting.InMemory.InMemoryModelService(); // store fingerprints in memory
        //    IQueryCommandBuilder queryCommandBuilder = new QueryCommandBuilder();

        //    List<Results> resultEntries = new List<Results>();

        //    p?.Report(new AnalizingProgess(1, 0, 0));

        //    for (int i = 0; i < mp.Duration.TotalMilliseconds; i += grain)
        //    {
        //        var track = new TrackData("GBBKS1200164", "Adele", "Skyfall", "Skyfall", i, timeFrameInMs);

        //        // store track metadata in the database
        //        var trackReference = modelService.InsertTrack(track);

        //        var data = audioService.ReadMonoSamplesFromFileAsync(TimeSpan.FromMilliseconds(timeFrameInMs), TimeSpan.FromMilliseconds(i));

        //        if (canel.IsCancellationRequested)
        //            break;

        //        // query the underlying database for similar audio sub-fingerprints
        //        var queryResult = await queryCommandBuilder.BuildQueryCommand()
        //                                             .From(data)
        //                                             .UsingServices(modelService, null)
        //                                             .Query();
        //        resultEntries.AddRange(queryResult.ResultEntries.Select(x => new Results() { Start = x.Track.ReleaseYear, End = track.ReleaseYear, Result = x }));

        //        // create sub-fingerprints and its hash representation
        //        var hashedFingerprints = await fingerprintCommandBuilder
        //                                    .BuildFingerprintCommand()
        //                                    .From(data)
        //                                    .UsingServices(null)
        //                                    .Hash();

        //        // store sub-fingerprints and its hash representation in the database 
        //        modelService.InsertHashDataForTrack(hashedFingerprints, trackReference);

        //        var percentage = i / mp.Duration.TotalMilliseconds;

        //        p?.Report(new AnalizingProgess(1, percentage * percentage, 0));
        //    }
        //    p?.Report(new AnalizingProgess(1, 1, 0));


        //    var orderd = resultEntries
        //        .OrderBy(x => x.Result.Similarity)
        //        .ThenBy(x => Math.Abs(x.Start - x.End))
        //        .Select(x => new Jump()
        //        {
        //            CrossFade = TimeSpan.FromMilliseconds(timeFrameInMs),
        //            Origin = TimeSpan.FromMilliseconds(x.End) + TimeSpan.FromMilliseconds(timeFrameInMs),
        //            TargetTime = TimeSpan.FromMilliseconds(x.Start),
        //            TargetSong = song
        //        }).ToArray();

        //    p?.Report(new AnalizingProgess(1, 1, 1));

        //    return orderd;
        //}


        private async Task<IEnumerable<Jump>> Analyze(Windows.Storage.StorageFile song, Configuration configuration, IProgress<AnalizingProgess> p = null, CancellationToken cancel = default(CancellationToken))
        {
            int timeFrameInMs = (int)configuration.Corssfade.Value.TotalMilliseconds;
            int grain = (int)configuration.Grain.Value.TotalMilliseconds;
            WinAudioService audioService = await WinAudioService.Create(song, cancel);
            if (cancel.IsCancellationRequested)
                return Enumerable.Empty<Jump>();
            var mp = await song.Properties.GetMusicPropertiesAsync();

            IModelService modelService = new SoundFingerprinting.InMemory.InMemoryModelService(); // store fingerprints in memory
            IQueryCommandBuilder queryCommandBuilder = new QueryCommandBuilder();

            p?.Report(new AnalizingProgess(0.5, 0, 0));

            var generatedData = GenerateIndexes(grain, mp).Select(i =>
            {
                var data = audioService.ReadMonoSamplesFromFileAsync(TimeSpan.FromMilliseconds(timeFrameInMs), TimeSpan.FromMilliseconds(i));
                return new { Data = data, Start = i };
            }).ToArray();

            p?.Report(new AnalizingProgess(0.5, 0, 0));
            await Task.WhenAll(generatedData.Select(async i =>
            {
                var track = new TrackData("GBBKS1200164", "Adele", "Skyfall", "Skyfall", i.Start, timeFrameInMs);

                // store track metadata in the database
                var trackReference = modelService.InsertTrack(track);


                try
                {
                    // create sub-fingerprints and its hash representation
                    var hashedFingerprints = await Task.Run(() => fingerprintCommandBuilder
                                              .BuildFingerprintCommand()
                                              .From(i.Data)
                                              .UsingServices(null)
                                              .Hash(), cancel);

                    if (cancel.IsCancellationRequested)
                        return;
                    // store sub-fingerprints and its hash representation in the database 
                    modelService.InsertHashDataForTrack(hashedFingerprints, trackReference);

                    var percentage = i.Start / mp.Duration.TotalMilliseconds;

                    p?.Report(new AnalizingProgess(0.5 + percentage * 0.5, 0, 0));
                }
                catch (TaskCanceledException)
                {

                }

            }));

            p?.Report(new AnalizingProgess(1, 0, 0));
            var resultEntries = (await Task.WhenAll(generatedData.Select(async i =>
              {
                  try
                  {
                      // query the underlying database for similar audio sub-fingerprints
                      var queryResult = await Task.Run(() => queryCommandBuilder.BuildQueryCommand()
                                                             .From(i.Data)
                                                             .UsingServices(modelService, null)
                                                             .Query(), cancel);

                      if (cancel.IsCancellationRequested)
                          return null;

                      var percentage = i.Start / mp.Duration.TotalMilliseconds;
                      p?.Report(new AnalizingProgess(1, percentage, 0));

                      return queryResult.ResultEntries
                          .Where(x => Math.Abs(x.Track.ReleaseYear - i.Start) > timeFrameInMs)
                          .Select(x => new Results()
                          {
                              Start = Math.Min(x.Track.ReleaseYear, i.Start),
                              End = Math.Max(x.Track.ReleaseYear, i.Start),
                              Result = x
                          });

                  }
                  catch (TaskCanceledException)
                  {
                      return null;
                  }
              }))).Where(x => x != null).SelectMany(x => x);


            p?.Report(new AnalizingProgess(1, 1, 0));


            var orderd = resultEntries
                .OrderBy(x => x.Result.Similarity)
                .ThenBy(x => Math.Abs(x.Start - x.End))
                .Select(x => new Jump()
                {
                    CrossFade = TimeSpan.FromMilliseconds(timeFrameInMs),
                    Origin = TimeSpan.FromMilliseconds(x.End) + TimeSpan.FromMilliseconds(timeFrameInMs),
                    TargetTime = TimeSpan.FromMilliseconds(x.Start),
                    TargetSong = song
                }).ToArray();

            p?.Report(new AnalizingProgess(1, 1, 1));

            return orderd;
        }

        private static IEnumerable<int> GenerateIndexes(int grain, Windows.Storage.FileProperties.MusicProperties mp)
        {
            for (int i = 0; i < mp.Duration.TotalMilliseconds; i += grain)
            {
                yield return i;
            }
        }

        private class Results
        {
            public int Start { get; set; }
            public int End { get; set; }

            public SoundFingerprinting.Query.ResultEntry Result { get; set; }
        }
    }
}
