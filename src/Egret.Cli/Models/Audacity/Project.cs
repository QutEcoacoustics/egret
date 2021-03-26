namespace Egret.Cli.Models.Audacity
{
    using Models;
    using Serialization.Yaml;
    using System.Xml.Serialization;

    [XmlRoot(Namespace = "http://audacity.sourceforge.net/xml/", ElementName = "project")]
    public record Project : ISourceInfo
    {
        [XmlAttribute(AttributeName = "snapto")]
        public string SnapTo { get; init; }

        [XmlAttribute(AttributeName = "projname")]
        public string ProjectName { get; init; }

        [XmlArray(ElementName = "tags")]
        [XmlArrayItem(ElementName = "tag")]
        public Tag[] Tags { get; init; }

        [XmlElement(ElementName = "labeltrack")]
        public LabelTrack[] Tracks { get; init; }

        [XmlAttribute(AttributeName = "sel0")]
        public double Sel0 { get; init; }

        [XmlAttribute(AttributeName = "sel1")]
        public double Sel1 { get; init; }

        [XmlAttribute(AttributeName = "selLow")]
        public double SelLow { get; init; }

        [XmlAttribute(AttributeName = "selHigh")]
        public double SelHigh { get; init; }

        [XmlAttribute(AttributeName = "vpos")]
        public double VPos { get; init; }

        [XmlAttribute(AttributeName = "h")]
        public double HVal { get; init; }

        [XmlAttribute(AttributeName = "zoom")]
        public double Zoom { get; init; }

        [XmlAttribute(AttributeName = "rate")]
        public double Rate { get; init; }

        [XmlAttribute(AttributeName = "version")]
        public string Version { get; init; }

        [XmlAttribute(AttributeName = "audacityversion")]
        public string AudacityVersion { get; init; }

        [XmlAttribute(AttributeName = "selectionformat")]
        public string SelectionFormat { get; init; }

        [XmlAttribute(AttributeName = "frequencyformat")]
        public string FrequencyFormat { get; init; }

        [XmlAttribute(AttributeName = "bandwidthformat")]
        public string BandwidthFormat { get; init; }

        [XmlIgnore]
        public SourceInfo SourceInfo { get; set; }
    }
}