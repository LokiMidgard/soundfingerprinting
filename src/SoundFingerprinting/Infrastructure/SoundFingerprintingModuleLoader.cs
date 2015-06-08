namespace SoundFingerprinting.Infrastructure
{
    using System;

    using Ninject;

    using SoundFingerprinting.Audio;
    using SoundFingerprinting.Builder;
    using SoundFingerprinting.Configuration;
    using SoundFingerprinting.FFT;
    using SoundFingerprinting.FFT.FFTW;
    using SoundFingerprinting.InMemory;
    using SoundFingerprinting.LCS;
    using SoundFingerprinting.LSH;
    using SoundFingerprinting.Math;
    using SoundFingerprinting.MinHash;
    using SoundFingerprinting.Utils;
    using SoundFingerprinting.Wavelets;

    internal class SoundFingerprintingModuleLoader : IModuleLoader
    {
        public void LoadAssemblyBindings(IKernel kernel)
        {
            kernel.Bind<IFingerprintService>().To<FingerprintService>();
            kernel.Bind<ISpectrumService>().To<SpectrumService>();
            kernel.Bind<ILogUtility>().To<LogUtility>().InSingletonScope();
            kernel.Bind<IAudioSamplesNormalizer>().To<AudioSamplesNormalizer>().InSingletonScope();
            kernel.Bind<IWaveletDecomposition>().To<StandardHaarWaveletDecomposition>().InSingletonScope();
#if WINDOWS_UAP
            bool noARM = false;
            bool noX64 = false;
            bool noX86 = false;
            try
            {
                using (var x = new FFTWService64())
                    x.FFTForward(new float[] { 0, 0, 1, 0, 0 }, 0, 5);
            }
            catch
            {
                noX64 = true;
            }

            try
            {
                using (var x = new FFTWService86())
                    x.FFTForward(new float[] { 0, 0, 1, 0, 0 }, 0, 5);
            }
            catch
            {
                noX86 = true;
            }

            try
            {
                using (var x = new FFTWServiceARM())
                    x.FFTForward(new float[] { 0, 0, 1, 0, 0 }, 0, 5);
            }
            catch
            {
                noARM = true;
            }
            
            if (noX86 && noX64 && noARM)
                kernel.Bind<IFFTService>().To<FFTWServiceFallBack>().InSingletonScope();
            else
                kernel.Bind<IFFTService>().To<CachedFFTWService>().InSingletonScope();

            if (!noX64)
                kernel.Bind<FFTWService>().To<FFTWService64>().WhenInjectedInto<CachedFFTWService>().InSingletonScope();
            else if (!noX86)
                kernel.Bind<FFTWService>().To<FFTWService86>().WhenInjectedInto<CachedFFTWService>().InSingletonScope();
            else if (!noARM)
                kernel.Bind<FFTWService>().To<FFTWServiceARM>().WhenInjectedInto<CachedFFTWService>().InSingletonScope();
#else
            kernel.Bind<IFFTService>().To<CachedFFTWService>().InSingletonScope();
            if (Environment.Is64BitProcess)
            {
                kernel.Bind<FFTWService>().To<FFTWService64>().WhenInjectedInto<CachedFFTWService>().InSingletonScope();
            }
            else
            {
                kernel.Bind<FFTWService>().To<FFTWService86>().WhenInjectedInto<CachedFFTWService>().InSingletonScope();
            }
#endif

            kernel.Bind<IFingerprintDescriptor>().To<FingerprintDescriptor>().InSingletonScope();

            kernel.Bind<IMinHashService>().To<MinHashService>().InSingletonScope();
            kernel.Bind<IPermutations>().To<DefaultPermutations>().InSingletonScope();
            kernel.Bind<ILocalitySensitiveHashingAlgorithm>().To<LocalitySensitiveHashingAlgorithm>().InSingletonScope();
            kernel.Bind<IAudioSequencesAnalyzer>().To<AudioSequencesAnalyzer>().InSingletonScope();
            kernel.Bind<ISimilarityUtility>().To<SimilarityUtility>().InSingletonScope();

            kernel.Bind<IFingerprintCommandBuilder>().To<FingerprintCommandBuilder>();
            kernel.Bind<IQueryFingerprintService>().To<QueryFingerprintService>();
            kernel.Bind<IQueryCommandBuilder>().To<QueryCommandBuilder>();

            kernel.Bind<IRAMStorage>().To<RAMStorage>()
                                      .InSingletonScope()
                                      .WithConstructorArgument("numberOfHashTables", FingerprintConfiguration.Default.HashingConfig.NumberOfLSHTables);
        }
    }
}
