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
    public class SubFingerprintData
    {
        public SubFingerprintData()
        {
            // no op
        }

        public SubFingerprintData(byte[] signature, int sequenceNumber, double sequenceAt, IModelReference subFingerprintReference, IModelReference trackReference)
        {
            Signature = signature;
            SubFingerprintReference = subFingerprintReference;
            TrackReference = trackReference;
            SequenceNumber = sequenceNumber;
            SequenceAt = sequenceAt;
        }

#if WINDOWS_UAP
        [DataMember]
#endif
        public byte[] Signature { get; set; }

#if WINDOWS_UAP
        [DataMember]
#endif
        public int SequenceNumber { get; set; }

#if WINDOWS_UAP
        [DataMember]
#endif
        public double SequenceAt { get; set; }

#if WINDOWS_UAP
        [DataMember]
#endif
        [IgnoreBinding]
        public IModelReference SubFingerprintReference { get; set; }

#if WINDOWS_UAP
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

            if (!(obj is SubFingerprintData))
            {
                return false;
            }

            return ((SubFingerprintData)obj).SubFingerprintReference.Equals(SubFingerprintReference);
        }

        public override int GetHashCode()
        {
            return SubFingerprintReference.GetHashCode();
        }
    }
}
