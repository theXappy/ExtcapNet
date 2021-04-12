namespace ExtcapNet.Config
{
    /// <summary>
    /// Represents an option in a multi-option configuration field
    /// </summary>
    public class ConfigOption
    {
        public string DisplayName { get; }
        public string Value { get; }

        public ConfigOption(string DisplayName, string Value)
        {
            this.DisplayName = DisplayName;
            this.Value = Value;
        }

        protected bool Equals(ConfigOption other)
        {
            if (other == null)
                return false; // Current is not null
            return DisplayName == other.DisplayName && Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == this.GetType() && Equals((ConfigOption) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((DisplayName != null ? DisplayName.GetHashCode() : 0) * 397) ^ (Value != null ? Value.GetHashCode() : 0);
            }
        }
    }
}