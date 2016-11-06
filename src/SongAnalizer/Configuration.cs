using System;
using System.Collections.Generic;
using Apollon.Common.SongAnalizer;
using System.Linq;

namespace SongAnalizer
{
    internal class Configuration : MusicAnalizerConfiguration
    {
        public Configuration() : base(CreateConfiguration())
        {

        }

        public TimeSpanConfiguration Corssfade => this[0] as TimeSpanConfiguration;
        public TimeSpanConfiguration Grain => this[1] as TimeSpanConfiguration;

        private static IEnumerable<ConfigurationElement> CreateConfiguration()
        {
            return new ConfigurationElement[] {
                new TimeSpanConfiguration("Corossfad", TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(30)) { Value = TimeSpan.FromSeconds(5) },
                new TimeSpanConfiguration("Grain", TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(5)){ Value = TimeSpan.FromMilliseconds(500) },

            };
        }
    }
}