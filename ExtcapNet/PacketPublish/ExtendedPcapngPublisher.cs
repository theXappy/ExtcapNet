using System;
using System.Collections.Generic;
using System.IO;
using Haukcode.PcapngUtils.Common;
using Haukcode.PcapngUtils.PcapNG;
using Haukcode.PcapngUtils.PcapNG.BlockTypes;
using Haukcode.PcapngUtils.PcapNG.CommonTypes;
using Haukcode.PcapngUtils.PcapNG.OptionTypes;

namespace ExtcapNet.PacketPublish
{
    class ExtendedPcapngPublisher : IPacketsPublisher
    {
        private readonly int _defaultInterfaceId;
        private readonly Dictionary<LinkLayerType, int> _linkLayerToIfaceId;
        private readonly PcapNGWriter _writer;

        public ExtendedPcapngPublisher(Stream strm, LinkLayerType defaultLinkLayer, IEnumerable<LinkLayerType> additionalLinkLayers)
        {
            // Maintaining a list of inerfaces descriptions to be provided to PcapngUtils
            List<InterfaceDescriptionBlock> ifacesDescsList = new List<InterfaceDescriptionBlock>();

            // This dictionary is for my class to map between link layers and matching interface ids
            int ifaceId = 0;
            _linkLayerToIfaceId = new Dictionary<LinkLayerType, int>();

            //Adding default iface
            _defaultInterfaceId = ifaceId;
            _linkLayerToIfaceId[defaultLinkLayer] = ifaceId;
            ifacesDescsList.Add(new InterfaceDescriptionBlock((LinkTypes)defaultLinkLayer, 0, new InterfaceDescriptionOption()));
            ifaceId++;

            // Here's a trick to support subsequent link layers: We're declaring new 'interfaces' in the PCAPNG format
            // each interface has it's onw 'interface description block' which indicates, among other things, it's link layer time.
            // Finally when a packet is published with a link layer different from the default one, we just associate it we the
            // interface we made specifically for that link layer.
            foreach (var additionalLinkLayer in additionalLinkLayers)
            {
                _linkLayerToIfaceId[additionalLinkLayer] = ifaceId;
                ifacesDescsList.Add(new InterfaceDescriptionBlock((LinkTypes)additionalLinkLayer, 0, new InterfaceDescriptionOption()));
                ifaceId++;
            }

            // Finally create the pcapng writer
            var shb = SectionHeaderBlock.GetEmptyHeader(false);
            var hwid = new HeaderWithInterfacesDescriptions(shb, ifacesDescsList);
            _writer = new PcapNGWriter(strm, new List<HeaderWithInterfacesDescriptions>() { hwid });
        }

        private void SendInner(byte[] data, int ifaceId, TimestampHelper ts, string comment)
        {
            List<string> commentsList = new List<string>();
            if(comment != null) 
                commentsList.Add(comment);
            _writer.WritePacket(new EnhancedPacketBlock(ifaceId, ts, data.Length, data, new EnhancedPacketOption(commentsList)));
        }

        public void Send(byte[] data)
        {
            var ts = new TimeSpan();
            SendInner(data, _defaultInterfaceId, new TimestampHelper((uint)ts.Seconds, (uint)(ts.Milliseconds * 1000)), null);
        }


        public void Send(byte[] data, LinkLayerType linkLayer)
        {
            if (!_linkLayerToIfaceId.TryGetValue(linkLayer, out var ifaceId))
                throw new ArgumentOutOfRangeException(
                    $"A packet with unfamiliar link layer was passed to {nameof(ExtendedPcapngPublisher)} in Send(...). " +
                            $"Send argument Link Layer: {linkLayer}");
            var ts = new TimeSpan();
            SendInner(data, ifaceId, new TimestampHelper((uint)ts.Seconds, (uint)(ts.Milliseconds * 1000)), null);
        }

        public void Send(PacketToSend pkt)
        {
            int ifaceId = _defaultInterfaceId;
            if (pkt.LinkLayer.HasValue)
            {
                if (!_linkLayerToIfaceId.TryGetValue(pkt.LinkLayer.Value, out ifaceId))
                    throw new ArgumentOutOfRangeException(
                        $"A packet with unfamiliar link layer was passed to {nameof(ExtendedPcapngPublisher)} in Send(...). " +
                                $"Send argument Link Layer: {pkt.LinkLayer}");
            }
            TimeSpan ts = new TimeSpan();
            if (pkt.TimeFromCaptureStart.HasValue)
            {
                ts = pkt.TimeFromCaptureStart.Value;
            }

            SendInner(pkt.Data, ifaceId, new TimestampHelper((uint)ts.Seconds, (uint)ts.Milliseconds * 1000), pkt.Comment);
        }


        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _writer?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
