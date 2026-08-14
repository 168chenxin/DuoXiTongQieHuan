namespace DualBootSwitcher
{
    internal sealed class FirmwareBootEntry
    {
        public FirmwareBootEntry(string identifier, string description, string device, string path)
        {
            Identifier = identifier;
            Description = description;
            Device = device;
            Path = path;
        }

        public string Identifier { get; private set; }

        public string Description { get; private set; }

        public string Device { get; private set; }

        public string Path { get; private set; }

        public bool IsNetworkBoot
        {
            get { return FirmwareBootParser.IsNetworkBoot(this); }
        }

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Description))
                {
                    return Description;
                }

                return string.IsNullOrWhiteSpace(Path) ? Identifier : Path;
            }
        }

        public string NetworkType
        {
            get { return FirmwareBootParser.GetNetworkType(this); }
        }
    }
}
