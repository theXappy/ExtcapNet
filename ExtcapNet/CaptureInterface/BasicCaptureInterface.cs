using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using ExtcapNet.Config;
using ExtcapNet.PacketPublish;

namespace ExtcapNet.CaptureInterface
{
    public class BasicCaptureInterface : AbstractCaptureInterface
    {
        /// <summary>
        /// Keeping registered fields and their allocated field IDs
        /// </summary>
        private readonly Dictionary<Lazy<ConfigField>, int> _fieldsAndIds;
        private int _nextFieldId;

        // Custom link layers
        private readonly List<LinkLayerType> _additionalLinkLayers;

        // Custom DLT
        private bool _isCustomDltDefined = false;
        private string _customDltNumber = null;
        private string _customDltName = null;
        private string _customDltDisplayName = null;


        public BasicCaptureInterface(string displayName, PacketsProducer producer, LinkLayerType defaultLinkLayer) : base(displayName, producer, defaultLinkLayer)
        {
            _fieldsAndIds = new Dictionary<Lazy<ConfigField>, int>();
            _nextFieldId = 0;
            _additionalLinkLayers = new List<LinkLayerType>();
        }

        /// <summary>
        /// Add a new configuration field for the plugin
        /// </summary>
        /// <param name="lazyField">An lazy object wrapping the configuration field. Will only be invoked if using the configuration.</param>
        public void AddConfigField(Lazy<ConfigField> lazyField)
        {
            _fieldsAndIds[lazyField] = _nextFieldId;
            _nextFieldId++;
        }

        /// <summary>
        /// Add a new configuration field for the plugin
        /// </summary>
        /// <param name="field">The configuration field to add</param>
        public void AddConfigField(ConfigField field) => AddConfigField(new Lazy<ConfigField>(() => field));

        public override string GetConfigQueryResponse(string[] Args)
        {
            StringBuilder response = new StringBuilder();
            foreach (var fieldAndId in _fieldsAndIds)
            {
                ConfigField field = fieldAndId.Key.Value; // de-referencing the lazy
                int id = fieldAndId.Value;
                response.AppendLine(field.FormatSelf(id));

            }
            return response.ToString();
        }

        /// <summary>
        /// Add a new possible link layer of produced packets.
        /// The producer can only write packets with the default link layer or any link layer registered using this method.
        /// </summary>
        /// <param name="linkLayer">The new link layer to support</param>
        public void AddLinkLayer(LinkLayerType linkLayer)
        {
            _additionalLinkLayers.Add(linkLayer);
        }

        /// <summary>
        /// Allows specifying the link layer of the extcap plugin.
        /// The displayName will be seen in Wireshark's 'Capture Options' window in the 'Link-Layer Header' column.
        /// </summary>
        /// <param name="number">Number of the link layer. Should match a value from <see cref="LinkLayerType"/></param>
        /// <param name="Name">Name of the link layer</param>
        /// <param name="displayName">The name of the link layer to display in Wireshark's GUI</param>
        public void SetCustomDltInfo(string number, string Name, string displayName)
        {
            this._customDltNumber = number;
            this._customDltName = Name;
            this._customDltDisplayName = displayName;
            this._isCustomDltDefined = true;
        }

        public override string GetDltsQueryResponse(string[] args)
        {
            if (!_isCustomDltDefined)
            {
                return base.GetDltsQueryResponse(args);
            }

            return $@"dlt {{number={_customDltNumber}}}{{name={_customDltName}}}{{display={_customDltDisplayName}}}";
        }

        public override IPacketsPublisher GetPacketsPublisher(NamedPipeClientStream wsPipeClient)
        {
            return new ExtendedPcapngPublisher(wsPipeClient, DefaultLinkLayer, _additionalLinkLayers);
        }

        public override Dictionary<ConfigField, string> GetCaptureConfiguration(string[] args)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));

            var configuration = new Dictionary<ConfigField, string>();

            // Go over all registered fields and look for their matching command line arguments
            foreach (var fieldAndId in _fieldsAndIds)
            {
                var field = fieldAndId.Key.Value;
                var id = fieldAndId.Value;
                string flag = "--" + field.DeriveCmdFlag(id);
                int indx = Array.IndexOf(args, flag);
                if (indx == -1)
                {
                    // Current field was not found.
                    // If it was marked as 'required' we need to abort. Otherwise just ignore
                    if(!field.Required)
                        continue;
                    throw new ArgumentException(
                        $"Missing command line flag {flag} for config field \"{field.DisplayName}\"");
                }

                if (indx == args.Length - 1)
                {
                    // Flag was found but it was the last argument so it's value is missing.
                    throw new ArgumentException(
                        $"Missing value for command line flag {flag} for config field \"{field.DisplayName}\"");
                }
                string value = args[indx + 1];

                if(field is MultiOptionsField moField)
                {
                    // Multiple values field, need to resolve the cmd representation to user's string
                    ConfigOption config = moField.GetOptionsCmdValues(id)
                                                    .Where(optAndCmdVal => optAndCmdVal.Value == value)
                                                    .Select(optAndCmdVal => optAndCmdVal.Key)
                                                    .SingleOrDefault();
                    if(config == null)
                    {
                        throw new ArgumentException($"Invalid value for flag '{flag}' : '{value}'");
                    }
                    configuration[field] = config.Value;
                }
                else
                {
                    // Simple field
                    configuration[field] = value;
                }
            }
            return configuration;
        }
    }
}