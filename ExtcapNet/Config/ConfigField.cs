using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

namespace ExtcapNet.Config
{
    /// <summary>
    /// Represents a configuration field for the extcap plugin
    /// </summary>
    public class ConfigField
    {
        /// <summary>
        /// Available types of extcap configuration fields
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "Offical names from Wireshark's code")]
        [SuppressMessage("ReSharper", "CommentTypo")]
        [SuppressMessage("ReSharper", "IdentifierTypo")]
        public enum FieldType
        {
            Integer,
            Unsigned,
            /*may Include Scientific / Special Notation*/
            Long,
            Float,
            /*display Checkbox*/
            Boolean,
            /*display Textbox*/
            String,
            /*display A Textbox With Masked Text*/
            Password,
            /*display Selector Table, All Values As Strings*/
            Selector,
            /*display Group Of Radio Buttons With Provided Values, All Values As Strings*/
            Radio,
            /*display A Textbox For Selecting Multiple Options, Values As Strings*/
            Multicheck,
            /*display A Dialog To Select A File From The Filesystem, Value As String*/
            Fileselect,
            /*display A Calendar*/
            Timestamp
        }

        /// <summary>
        /// Display name to show in Wireshark's GUI
        /// </summary>
        public string DisplayName { get; }
        /// <summary>
        /// Type of the configuration field
        /// </summary>
        public FieldType Type { get; }
        /// <summary>
        /// Whether the field must be set to run the plugin
        /// </summary>
        public bool Required { get; }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="displayName">Display name to show in Wireshark's GUI</param>
        /// <param name="type">Type of the configuration field</param>
        /// <param name="required">Whether the field must be set to run the plugin</param>
        /// <exception cref="ArgumentNullException">If <see cref="DisplayName"/> is null</exception>
        /// <exception cref="ArgumentException">If <see cref="type"/> is not defined</exception>
        public ConfigField(string displayName, FieldType type, bool required = true)
        {
            if(!Enum.IsDefined(typeof(FieldType), type)) throw new ArgumentException($"{nameof(type)} must have a value defined in the Enum");

            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            Type = type;
            Required = required;
        }


        /// <summary>
        /// Create a command line flag to represent the configuration field
        /// </summary>
        /// <param name="fieldId">A unique identifier within all configuration fields for the plugin</param>
        /// <returns>CMD flag which represents the configuration field</returns>
        [SuppressMessage("Globalization", "CA1304:Specify CultureInfo", Justification = "Wirehsark requires lowercase, Non-English no expected")]
        public string DeriveCmdFlag(int fieldId)
        {
            // Deriving a CMD flag for config field based on the display name
            // Some sanitization is required since i cant allow special chars like spaces, commas, dots, etc...
            // also - it must be all in lowercase.
            string sanitizedFlag = new String(DisplayName.ToLower().Where(char.IsLetter).ToArray());
            // Adding a unique prefix to prevent collisions
            sanitizedFlag = $"extcapnet_{fieldId}_{sanitizedFlag}";
            return sanitizedFlag;
        }

        /// <summary>
        /// Generate a string representation of the configuration field in extcap's format
        /// </summary>
        /// <param name="fieldId">A unique identifier within all configuration fields for the plugin</param>
        /// <returns>A string representation of the configuration field in extcap's format</returns>
        [SuppressMessage("Globalization", "CA1304:Specify CultureInfo", Justification = "Wirehsark requires lowercase, Non-English no expected")]
        public virtual string FormatSelf(int fieldId)
        {
            string typeStr = Type.ToString().ToLower();

            return $"arg {{number={fieldId}}}" +
                   $"{{call=--{DeriveCmdFlag(fieldId)}}}" +
                   $"{{display={DisplayName}}}" +
                   $"{{type={typeStr}}}" +
                   $"{{required={Required}}}";
        }
    }
}