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
    public class SpectralImageData
    {
        public SpectralImageData(float[] image, int orderNumber, IModelReference trackReference)
        {
            Image = image;
            TrackReference = trackReference;
            OrderNumber = orderNumber;
        }

#if WINDOWS_UAP
        [DataMember]
#endif
        public float[] Image { get; set; }

#if WINDOWS_UAP
        [DataMember]
#endif
        public int OrderNumber { get; set; }

#if WINDOWS_UAP
        [DataMember]
#endif
        [IgnoreBinding]
        public IModelReference TrackReference { get; set; }
    }
}
