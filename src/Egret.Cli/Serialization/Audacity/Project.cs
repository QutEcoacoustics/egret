// unset

namespace Egret.Cli.Serialization.Audacity
{
    using Models;
    using System.Collections.Generic;
    using System.Xml.Serialization;
    using Yaml;

    [XmlRoot(Namespace = "http://audacity.sourceforge.net/xml/", ElementName = "project")]
    public record Project : ISourceInfo
    {
        [XmlAttribute(AttributeName = "snapto")]
        public string SnapTo = "off";

        [XmlAttribute(AttributeName = "projname")]
        public string ProjectName { get; init; }

        [XmlArray(ElementName = "tags")]
        [XmlArrayItem(ElementName = "tag")]
        public List<Tag> Tags { get; init; }

        [XmlElement(ElementName = "labeltrack")]
        public List<LabelTrack> Tracks { get; init; }

        [XmlAttribute(AttributeName = "sel0")]
        public double Sel0 { get; init; }

        [XmlAttribute(AttributeName = "sel1")]
        public double Sel1 { get; init; }

        [XmlAttribute(AttributeName = "selLow")]
        public double SelLow { get; init; } = 10.0;

        [XmlAttribute(AttributeName = "selHigh")]
        public double SelHigh { get; init; } = 10000.0;

        [XmlAttribute(AttributeName = "vpos")]
        public double VPos { get; init; }

        [XmlAttribute(AttributeName = "h")]
        public double HVal { get; init; }

        [XmlAttribute(AttributeName = "zoom")]
        public double Zoom { get; init; }

        [XmlAttribute(AttributeName = "rate")]
        public double Rate { get; init; } = 44100.0;

        [XmlAttribute(AttributeName = "version")]
        public string Version { get; init; } = "1.3.0";

        [XmlAttribute(AttributeName = "audacityversion")]
        public string AudacityVersion { get; init; } = "2.4.2";

        [XmlAttribute(AttributeName = "selectionformat")]
        public string SelectionFormat { get; init; } = "hh:mm:ss + milliseconds";

        [XmlAttribute(AttributeName = "frequencyformat")]
        public string FrequencyFormat { get; init; } = "Hz";

        [XmlAttribute(AttributeName = "bandwidthformat")]
        public string BandwidthFormat { get; init; } = "octaves";

        [XmlIgnore]
        public SourceInfo SourceInfo { get; set; }
    }
}