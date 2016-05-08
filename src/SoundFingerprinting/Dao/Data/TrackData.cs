namespace SoundFingerprinting.DAO.Data
{
    using System;

#if WINDOWS_UWP
    using System.Runtime.Serialization;
#endif

    using SoundFingerprinting.DAO;
    using SoundFingerprinting.Data;

#if WINDOWS_UWP
    [DataContract]
#else
    [Serializable]
#endif
    public class TrackData
    {
        public TrackData()
        {
            // no op
        }

        public TrackData(string isrc, string artist, string title, string album, int releaseYear, double trackLength)
        {
            ISRC = isrc;
            Artist = artist;
            Title = title;
            Album = album;
            ReleaseYear = releaseYear;
            TrackLengthSec = trackLength;
        }

        public TrackData(string isrc, string artist, string title, string album, int releaseYear, double trackLength, IModelReference trackReference)
            : this(isrc, artist, title, album, releaseYear, trackLength)
        {
            TrackReference = trackReference;
        }

#if WINDOWS_UWP
        [DataMember]
#endif
        public string Artist { get; set; }

#if WINDOWS_UWP
        [DataMember]
#endif
        public string Title { get; set; }

#if WINDOWS_UWP
        [DataMember]
#endif
        public string ISRC { get; set; }

#if WINDOWS_UWP
        [DataMember]
#endif
        public string Album { get; set; }

#if WINDOWS_UWP
        [DataMember]
#endif
        public int ReleaseYear { get; set; }

#if WINDOWS_UWP
        [DataMember]
#endif
        public double TrackLengthSec { get; set; }

#if WINDOWS_UWP
        [DataMember]
#endif
        public string GroupId { get; set; }

#if WINDOWS_UWP
        [DataMember]
#endif
        [IgnoreBinding]
        public IModelReference TrackReference { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (!(obj is TrackData))
            {
                return false;
            }

            return ((TrackData)obj).TrackReference.Equals(TrackReference);
        }

        public override int GetHashCode()
        {
            return TrackReference.GetHashCode();
        }
    }
}
