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
    //public class BasicPcapngPublisher : IPacketsPublisher
    //{
    //    private readonly PcapNGWriter _writer;
    //    private readonly LinkLayerType _defaultLinkLayer;

    //    public BasicPcapngPublisher(Stream strm, LinkLayerType defaultLinkLayer)
    //    {
    //        var shb = SectionHeaderBlock.GetEmptyHeader(false);
    //        var hwid = new HeaderWithInterfacesDescriptions(shb,
    //            new List<InterfaceDescriptionBlock>()
    //            {
    //                new InterfaceDescriptionBlock((LinkTypes)defaultLinkLayer, 8192, new InterfaceDescriptionOption())
    //            });
    //        _writer = new PcapNGWriter(strm, new List<HeaderWithInterfacesDescriptions>() { hwid });
    //        _defaultLinkLayer = defaultLinkLayer;
    //    }

    //    public void Send(byte[] data)
    //    {
    //        _writer.WritePacket(new SimplePacketBlock(data));
    //    }

    //    public void Send(byte[] data, TimeSpan timeFromCaptureStart)
    //    {
    //        if (data == null) return;

    //        _writer.WritePacket(
    //            new EnhancedPacketBlock(
    //                0,
    //                new TimestampHelper((uint)timeFromCaptureStart.Seconds,(uint)timeFromCaptureStart.Milliseconds*1000),
    //                data.Length,
    //                data,
    //                new EnhancedPacketOption()));
    //    }

    //    public void Send(byte[] data, LinkLayerType linkLayer)
    //    {
    //        if(linkLayer != _defaultLinkLayer)
    //            throw new ArgumentOutOfRangeException(
    //                $"{nameof(BasicPcapngPublisher)} only supports a single, default, link layer. " +
    //                $"Using Send(...) with a different link layer is forbidden. Default Link Layer: {_defaultLinkLayer}, Send argument Link Layer: {linkLayer}");
    //        Send(data);
    //    }

    //    public void Send(byte[] data, LinkLayerType linkLayer, TimeSpan timeFromCaptureStart)
    //    {
    //        if (linkLayer != _defaultLinkLayer)
    //            throw new ArgumentOutOfRangeException(
    //                $"{nameof(BasicPcapngPublisher)} only supports a single, default, link layer. " +
    //                $"Using Send(...) with a different link layer is forbidden. Default Link Layer: {_defaultLinkLayer}, Send argument Link Layer: {linkLayer}");
    //        Send(data, timeFromCaptureStart);
    //    }

    //    protected virtual void Dispose(bool disposing)
    //    {
    //        if (disposing)
    //        {
    //            _writer?.Dispose();
    //        }
    //    }

    //    public void Dispose()
    //    {
    //        Dispose(true);
    //        GC.SuppressFinalize(this);
    //    }
    //}
}