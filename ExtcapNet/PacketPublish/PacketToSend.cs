using System;

namespace ExtcapNet.PacketPublish
{
    public struct PacketToSend
    {
        public byte[] Data { get; set; }
        public LinkLayerType? LinkLayer { get; set; }
        public TimeSpan? TimeFromCaptureStart { get; set; }
        public string Comment { get; set; }
    }
}
