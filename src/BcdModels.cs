using System.Collections.Generic;

namespace DualBootSwitcher
{
    internal sealed class BootConfiguration
    {
        public BootConfiguration(List<BootEntry> entries, int timeoutSeconds)
        {
            Entries = entries;
            TimeoutSeconds = timeoutSeconds;
        }

        public List<BootEntry> Entries { get; private set; }

        public int TimeoutSeconds { get; private set; }
    }

    internal sealed class BootEntry
    {
        public BootEntry(string identifier, string description, string device)
        {
            Identifier = identifier;
            Description = description;
            Device = device;
        }

        public string Identifier { get; private set; }

        public string Description { get; private set; }

        public string Device { get; private set; }

        public bool IsDefault { get; set; }

        public string DisplayName
        {
            get
            {
                return string.IsNullOrWhiteSpace(Device)
                    ? Description
                    : Description + " (" + Device + ")";
            }
        }
    }
}
