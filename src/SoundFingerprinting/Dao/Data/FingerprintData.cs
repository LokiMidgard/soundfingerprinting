namespace SoundFingerprinting.DAO.Data
{
    using System;

#if WINDOWS_UAP
    using System.Runtime.Serialization;
#endif

    using SoundFingerprinting.DAO;
    using SoundFingerprinting.Data;

#if WINDOWS_UAP
    [DataContract]
#else
    [Serializable]
#endif
    public class FingerprintData
    {
        public FingerprintData()
        {
            // no op
        }

        public FingerprintData(bool[] signature, IModelReference trackReference)
        {
            Signature = signature;
            TrackReference = trackReference;
        }

#if WINDOWS_UAP
        [DataMember]
#endif
        [IgnoreBinding]
        public bool[] Signature { get; set; }

#if WINDOWS_UAP
        [DataMember]
#endif
        [IgnoreBinding]
        public IModelReference FingerprintReference { get; set; }

#if WINDOWS_UAP
        [DataMember]
#endif
        [IgnoreBinding]
        public IModelReference TrackReference { get; set; }
    }
}
