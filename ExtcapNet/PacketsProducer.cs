using System.Collections.Generic;
using ExtcapNet.Config;
using ExtcapNet.PacketPublish;

namespace ExtcapNet
{
    /// <summary>
    /// A delegate for a function that produces packets.
    /// It is expected that calling this function ONCE will cause it to constantly publish packets using the <paramref name="publisher"/>
    /// object until no more packets are available.
    /// For capturing live traffic, this mean the function might need to run indefinitely.
    /// </summary>
    /// <param name="configuration">Configuration provided to the producer based on <see cref="ConfigField"/>s defined in the plugin</param>
    /// <param name="publisher">An object used to pass packets from the producer to the consumer (mostly this means Wireshark)</param>
    public delegate void PacketsProducer(Dictionary<ConfigField, string> configuration, IPacketsPublisher publisher);
}