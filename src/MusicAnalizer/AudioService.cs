using System;
using System.Collections.Generic;
using SoundFingerprinting.Audio;

namespace MusicAnalizer
{
    internal class AudioService : IAudioService
    {
        public IReadOnlyCollection<string> SupportedFormats
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public AudioSamples ReadMonoSamplesFromFile(string pathToSourceFile, int sampleRate)
        {
            throw new NotImplementedException();
        }

        public AudioSamples ReadMonoSamplesFromFile(string pathToSourceFile, int sampleRate, int seconds, int startAt)
        {
            throw new NotImplementedException();
        }
    }
}