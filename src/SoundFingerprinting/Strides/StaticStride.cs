﻿namespace SoundFingerprinting.Strides
{
    using System;

    #if WINDOWS_UAP
    using System.Runtime.Serialization;
#endif

    /// <summary>
    ///   Static Stride class
    /// </summary>
#if WINDOWS_UAP
    [DataContract]
#else
    [Serializable]
#endif
    public class StaticStride : IStride
    {
        private readonly int nextStride;

        /// <summary>
        /// Initializes a new instance of the <see cref="StaticStride"/> class. 
        /// </summary>
        /// <param name="strideSize">
        /// Stride size, used each time StrideSize method is invoked
        /// </param>
        public StaticStride(int strideSize)
        {
            nextStride = strideSize;
            FirstStride = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StaticStride"/> class. 
        /// </summary>
        /// <param name="strideSize">
        /// Stride size
        /// </param>
        /// <param name="firstStride">
        /// First stride
        /// </param>
        public StaticStride(int strideSize, int firstStride) : this(strideSize)
        {
            FirstStride = firstStride;
        }

#if WINDOWS_UAP
        [DataMember]
#endif
        public int FirstStride { get; protected set; }

        public int GetNextStride()
        {
            return nextStride;
        }
    }
}