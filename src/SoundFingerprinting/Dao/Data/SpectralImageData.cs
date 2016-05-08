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
    public class SpectralImageData
    {
        public SpectralImageData(float[] image, int orderNumber, IModelReference trackReference)
        {
            Image = image;
            TrackReference = trackReference;
            OrderNumber = orderNumber;
        }

#if WINDOWS_UWP
        [DataMember]
#endif
        public float[] Image { get; set; }

#if WINDOWS_UWP
        [DataMember]
#endif
        public int OrderNumber { get; set; }

#if WINDOWS_UWP
        [DataMember]
#endif
        [IgnoreBinding]
        public IModelReference TrackReference { get; set; }
    }
}
