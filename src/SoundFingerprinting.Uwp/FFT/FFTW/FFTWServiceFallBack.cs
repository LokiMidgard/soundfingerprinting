namespace SoundFingerprinting.FFT.FFTW
{
    using System;
    using System.Linq;
    using System.Runtime.InteropServices;

    using SoundFingerprinting.FFT.FFTW.X64;

    public class FFTWServiceFallBack : IFFTService
    {
        public float[] FFTForward(float[] signal, int startIndex, int length)
        {
            var input = signal.Skip(startIndex).Take(length).Select(x => new System.Numerics.Complex(x, 0)).ToArray();
            MathNet.Numerics.IntegralTransforms.Fourier.Forward(input, MathNet.Numerics.IntegralTransforms.FourierOptions.Default);
            var result = input.Select(x => (float) x.Magnitude).ToArray();
            return result;
        }

    }
}
