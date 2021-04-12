using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using ExtcapNet.CaptureInterface;
using ExtcapNet.PacketPublish;

namespace ExtcapNet
{
    /// <summary>
    /// Manages the Extcap API for the plugin.
    /// </summary>
    /// <example>
    /// This is the common usage of the manager:
    /// <code>
    /// var extcapMan = new ExtcapManager();
    /// var producer = new PacketsProducer(...);
    /// var iface = extcapMan.RegisterInterface("My New Interface", producer, LinkLayerType.Ethernet);
    /// </code>
    /// </example>
    // ReSharper disable once UnusedMember.Global
    public class ExtcapManager
    {
        List<AbstractCaptureInterface> _interfaces = new List<AbstractCaptureInterface>();

        /// <summary>
        /// Register a new network interface that the plugin will support
        /// </summary>
        /// <param name="displayName">Name of the interface. Will be shown in Wireshark's GUI.</param>
        /// <param name="producer">A function which, when ran, continuously produces packets coming from the interface.</param>
        /// <param name="defaultLinkLayer">The default link layer of the produced packets</param>
        /// <returns>The registered interface instance</returns>
        public AbstractCaptureInterface RegisterInterface(string displayName, PacketsProducer producer,
            LinkLayerType defaultLinkLayer)
        {
            var newInterface = new BasicCaptureInterface(displayName, producer, defaultLinkLayer);
            _interfaces.Add(newInterface);
            return newInterface;
        }

        /// <summary>
        /// Register a new network interface that the plugin will support
        /// </summary>
        /// <param name="newInterface">The interface to register</param>
        /// <returns>The registered interface instance. Same as the parameter given.</returns>
        public AbstractCaptureInterface RegisterInterface(AbstractCaptureInterface newInterface)
        {
            _interfaces.Add(newInterface);
            return newInterface;
        }

        /// <summary>
        /// Answering wireshark's queries and possibly running the relevant packets Producer (for a specific interface)
        /// </summary>
        /// <param name="args">Arguments to the program was captured in the Main function</param>
        public void Run(string[] args)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));
            if(!_interfaces.Any()) throw new Exception("Can not run ExtcapManager without any registered interfaces. " +
                                                       $"Call {nameof(RegisterInterface)} before calling {nameof(Run)}");

            // Handle interfaces query
            if (args.Any(arg => arg == "--extcap-interfaces"))
            {
                var res = GetInterfacesQueryResponse();
                // Writing towards wireshark (he reads from my std out)
                Console.WriteLine(res);
                Environment.Exit(0);
            }

            // Interface specific commands below - need to resolve which interface they are querying/running.
            // Parsing interface identifier
            var ifaceArgIndex = Array.IndexOf(args, "--extcap-interface");
            if (ifaceArgIndex == -1 || ifaceArgIndex == args.Length - 1)
            {
                Console.WriteLine("ERROR: Command is missing --extcap-interface parameter.");
                Environment.Exit(2);
            }
            string ifaceIdentifier = args[ifaceArgIndex + 1];
            AbstractCaptureInterface selectedInterface = _interfaces
                .SingleOrDefault(iface => iface.InterfaceIdentifier == ifaceIdentifier);
            if (selectedInterface == null)
            {
                Console.WriteLine($"ERROR: No interface found with the identifier '{ifaceIdentifier}'");
                Console.WriteLine("Usage: See https://www.wireshark.org/docs/wsdg_html_chunked/ChCaptureExtcap.html");
                Environment.Exit(3);
            }

            // Handle config query
            if (args.Any(arg => arg == "--extcap-config"))
            {
                var res = selectedInterface.GetConfigQueryResponse(args);
                Console.WriteLine(res);
                Environment.Exit(0);
            }

            // Handle DLTs query
            if (args.Any(arg => arg == "--extcap-dlts"))
            {
                var res = selectedInterface.GetDltsQueryResponse(args);
                Console.WriteLine(res);
                Environment.Exit(0);
            }

            // Handle Capture requests
            if (args.Any(arg => arg == "--capture"))
            {
                // Otherwise - It's go time. Calling packets producer
                var fifoArgIndex = Array.IndexOf(args, "--fifo");
                if (fifoArgIndex == -1 || fifoArgIndex == args.Length - 1)
                {
                    Console.WriteLine("ERROR: Can not capture packets if --fifo flag or it's value are not specified.");
                    Environment.Exit(4);
                }
                // Create the named pipe towards the invoker
                int pipeNameIndex = fifoArgIndex + 1;
                string wsPipeName = args[pipeNameIndex];
                wsPipeName = wsPipeName.Substring(@"\\.\pipe\".Length);
                NamedPipeClientStream wsPipeClient = new NamedPipeClientStream(wsPipeName);
                wsPipeClient.Connect();
                // Initiate packets publisher
                IPacketsPublisher publisher = selectedInterface.GetPacketsPublisher(wsPipeClient);
                // Parse configuration from command line args
                var config = selectedInterface.GetCaptureConfiguration(args);

                // Run the producer (might be indefinitely)
                selectedInterface.Producer(config, publisher);

                // Producer returned, time to clean up.
                wsPipeClient.Close();
                Environment.Exit(0);
            }

            Console.WriteLine("Usage: See https://www.wireshark.org/docs/wsdg_html_chunked/ChCaptureExtcap.html");
            Environment.Exit(1);
        }

        /// <summary>
        /// Format the interfaces list for wireshark's '--extcap-interfaces' query
        /// </summary>
        /// <returns>String representing all the interfaces registered in the plugin</returns>
        private string GetInterfacesQueryResponse()
        {
            StringBuilder res = new StringBuilder();
            res.AppendLine(@"extcap {version=1.0.0.0}{help=http://127.0.0.1/}");
            foreach (AbstractCaptureInterface iface in _interfaces)
            {
                // Creating a "interface { ... }" line for every available interface
                res.AppendLine(@"interface {value=" + iface.InterfaceIdentifier + "}{display=" + iface.DisplayName + "}");
            }
            return res.ToString();
        }
    }
}
