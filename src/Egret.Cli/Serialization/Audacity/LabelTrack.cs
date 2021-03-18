namespace Egret.Cli.Serialization.Audacity
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    public record LabelTrack
    {
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; init; }

        [XmlAttribute(AttributeName = "isSelected")]
        public int IsSelected { get; init; }

        [XmlAttribute(AttributeName = "height")]
        public int Height { get; init; } = 100;

        [XmlAttribute(AttributeName = "minimized")]
        public int Minimized { get; init; }

        [XmlElement(ElementName = "label")]
        public List<Label> Labels { get; init; }

        [XmlAttribute(AttributeName = "numlabels")]
        public int NumLabels { get => Labels.Count; init => _ = value; }

        public LabelTrack()
        {
        }

        public LabelTrack(string name, int isSelected, int height, int minimized, List<Label> labels)
        {
            Name = name;
            IsSelected = isSelected;
            Height = height;
            Minimized = minimized;
            Labels = labels;
        }
    }
}