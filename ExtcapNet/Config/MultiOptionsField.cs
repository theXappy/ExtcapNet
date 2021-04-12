using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExtcapNet.Config
{
    /// <summary>
    /// Represents a configuration field that contains multiple options (values) to choose from
    /// </summary>
    public class MultiOptionsField : ConfigField
    {
        /// <summary>
        /// All registered options and their uniquely representing identifiers.
        /// </summary>
        private Dictionary<ConfigOption, int> OptionsAndIds { get; }
        /// <summary>
        /// All registered options for the configuraiton field.
        /// </summary>
        public List<ConfigOption> Options => OptionsAndIds.Keys.ToList();

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="DisplayName">Display name to show in Wireshark's GUI</param>
        /// <param name="type">Type of the configuration field. Only multi-option types are accepted</param>
        /// <param name="options"></param>
        /// <param name="Required">Whether the field must be set to run the plugin</param>
        /// <exception cref="ArgumentException">If <see cref="type"/> is not a multi-option type</exception>
        /// <exception cref="ArgumentNullException">If <see cref="type"/> is null</exception>
        public MultiOptionsField(string DisplayName, FieldType type, List<ConfigOption> options, bool Required = true) : base(DisplayName, type, Required)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (type != FieldType.Multicheck &&
                type != FieldType.Selector &&
                type != FieldType.Radio)
                throw new ArgumentException($"Only types allowed for {nameof(MultiOptionsField)} are " +
                                            $"{nameof(FieldType.Multicheck)}, {nameof(FieldType.Selector)}, {nameof(FieldType.Radio)}. " +
                                            $"Received: {type}");
            OptionsAndIds = new Dictionary<ConfigOption, int>();

            // Assign an ID for every option
            for (var i = 0; i < options.Count; i++)
            {
                OptionsAndIds[options[i]] = i;
            }
        }

        /// <summary>
        /// Generates a mapping of every option and it's command line value representation
        /// </summary>
        /// <param name="fieldId">A unique identifier within all configuration fields for the plugin</param>
        /// <returns>Dictionary mapping options and the strings that should be used in the command line to choose them</returns>
        public Dictionary<ConfigOption, string> GetOptionsCmdValues(int fieldId)
        {
            var output = new Dictionary<ConfigOption, string>();
            // Deriving a CMD flag for every config field based on the display name
            // Some sanitization is required since we cant allow special chars like spaces, commas, dots, etc...
            foreach (var optionAndId in OptionsAndIds)
            {
                ConfigOption option = optionAndId.Key;
                int optId = optionAndId.Value;
                string oneLineDisplayName = option.DisplayName.Trim();
                string sanitizedValue = new String(oneLineDisplayName.Where(char.IsLetter).Take(5).ToArray());
                // Adding a unique prefix to prevent collisions
                sanitizedValue = $"extcapnet_{fieldId}_{optId}_{sanitizedValue}";
                output[option] = sanitizedValue;
            }
            return output;
        }

        /// <summary>
        /// Generate a string representation of the configuration field in extcap's format. The string includes representations of the different options.
        /// </summary>
        /// <param name="fieldId">A unique identifier within all configuration fields for the plugin</param>
        /// <returns>A string representation of the configuration field in extcap's format</returns>
        public override string FormatSelf(int fieldId)
        {
            // Calling base to format the 'arg' line
            StringBuilder sb = new StringBuilder(base.FormatSelf(fieldId));
            sb.AppendLine();

            // Now writing the 'value' lines - one per option
            // Note: value is a tricky word. it's used both  to mean the entire option for this arg and as the 'cmd value to pass to represent this option'
            foreach (var optionAndCmdVal in GetOptionsCmdValues(fieldId))
            {
                // Deriving a CMD flag for config field based on the display name
                // Some sanitization is required since i cant allow special chars like spaces, commas, dots, etc...
                ConfigOption option = optionAndCmdVal.Key;
                string optVal = optionAndCmdVal.Value;
                sb.AppendLine($"value {{arg={fieldId}}}{{value={optVal}}}{{display={option.DisplayName.Trim()}}}{{enabled=true}}");
            }

            return sb.ToString().TrimEnd();
        }
    }
}