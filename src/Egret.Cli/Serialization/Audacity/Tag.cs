namespace Egret.Cli.Serialization.Audacity
{
    using System.Xml.Serialization;

    public record Tag
    {
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; init; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; init; }

        public Tag()
        {
        }

        public Tag(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }
}