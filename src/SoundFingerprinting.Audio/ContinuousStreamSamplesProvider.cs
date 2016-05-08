namespace SoundFingerprinting.Audio
{
    using System.Threading;
    using System.Threading.Tasks;

    public class ContinuousStreamSamplesProvider : ISamplesProvider
    {
        private const int MillisecondsTimeout = 500;

        private readonly ISamplesProvider provider;

        public ContinuousStreamSamplesProvider(ISamplesProvider provider)
        {
            this.provider = provider;
        }

        public int GetNextSamples(float[] buffer)
        {
            int bytesRead = provider.GetNextSamples(buffer);

            while (bytesRead == 0)
            {
#if WINDOWS_UWP
                Task.Delay(MillisecondsTimeout).Wait(); // lame but required to fill the buffer from continuous stream, either microphone or url
#else
                Thread.Sleep(MillisecondsTimeout); // lame but required to fill the buffer from continuous stream, either microphone or url
#endif
                bytesRead = provider.GetNextSamples(buffer);
            }

            return bytesRead;
        }
    }
}