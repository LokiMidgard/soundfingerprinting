namespace SongAnalizer
{
    internal class PcmInfo
    {
        public PcmInfo()
        {
        }

        public int Chanels { get; set; }
        public float[] Data { get; set; }
        public int SampleRate { get; set; }
        public double Seconds { get; set; }
    }
}