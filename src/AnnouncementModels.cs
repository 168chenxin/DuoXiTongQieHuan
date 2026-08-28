namespace SysSwitch
{
    internal enum AnnouncementBlockKind
    {
        Title,
        Subtitle,
        Paragraph,
        Bullet,
        Link,
        Divider,
        Image
    }

    internal sealed class AnnouncementBlock
    {
        public AnnouncementBlock(AnnouncementBlockKind kind, string text, string imageUrl)
        {
            Kind = kind;
            Text = text;
            ImageUrl = imageUrl;
        }

        public AnnouncementBlockKind Kind { get; private set; }

        public string Text { get; private set; }

        public string ImageUrl { get; private set; }
    }
}
