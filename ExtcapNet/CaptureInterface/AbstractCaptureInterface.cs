using System;
using System.Collections.Generic;
using System.IO.Pipes;
using ExtcapNet.Config;
using ExtcapNet.PacketPublish;

namespace ExtcapNet.CaptureInterface
{
    public abstract class AbstractCaptureInterface
    {
        // A string used in the extcap command line interface. (It's not shown to the user)
        public string InterfaceIdentifier { get; }

        public string DisplayName { get; }
        public PacketsProducer Producer { get; }
        public LinkLayerType DefaultLinkLayer { get; }

        /// <param name="displayName">Name of the interface to show in wireshark</param>
        /// <param name="producer">Function to call which will produce packets.
        /// This function should continue producing packets until it is done or indefinitely.</param>
        /// <param name="defaultLinkLayer">The default link layer to use</param>
        protected AbstractCaptureInterface(string displayName, PacketsProducer producer, LinkLayerType defaultLinkLayer)
        {
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            Producer = producer ?? throw new ArgumentNullException(nameof(producer));
            DefaultLinkLayer = defaultLinkLayer;
            InterfaceIdentifier = "ExtcapNet_" + DisplayName.Replace(" ", "");
        }

        public virtual Dictionary<ConfigField, string> GetCaptureConfiguration(string[] args)
        {
            // Empty configuration by default because we don't want producers to have to deal with nulls.
            return new Dictionary<ConfigField, string>();
        }

        public virtual string GetDltsQueryResponse(string[] args)
        {
            return $@"dlt {{number={(int)this.DefaultLinkLayer}}}{{name={this.DefaultLinkLayer}}}{{display={this.DefaultLinkLayer}}}";
        }

        public abstract IPacketsPublisher GetPacketsPublisher(NamedPipeClientStream wsPipeClient);
        public abstract string GetConfigQueryResponse(string[] Args);
    }
}
