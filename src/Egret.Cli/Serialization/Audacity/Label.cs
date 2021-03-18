namespace Egret.Cli.Serialization.Audacity
{
    using System;
    using System.Xml.Serialization;

    public record Label
    {
        [XmlAttribute(AttributeName = "title")]
        public string Title { get; init; }

        [XmlAttribute(AttributeName = "t")]
        public double TimeStart { get; init; }

        [XmlAttribute(AttributeName = "t1")]
        public double TimeEnd { get; init; }

        [XmlAttribute(AttributeName = "selLow")]
        public double SelLow { get; init; }

        [XmlAttribute(AttributeName = "selHigh")]
        public double SelHigh { get; init; }

        /// <summary>
        /// Is this label for a point in time?
        /// If false, this label covers a time span greater than 0.01.
        /// </summary>
        public bool IsTimePoint => Math.Abs(this.TimeStart - this.TimeEnd) < 0.01;

        /// <summary>
        /// Is this label for a frequency point?
        /// If false, this label covers a frequency range greater than 0.01.
        /// </summary>
        public bool IsSelPoint => Math.Abs(this.SelLow - this.SelHigh) < 0.01;

        public Label()
        {
        }

        public Label(string title, double timeStart, double timeEnd)
        {
            Title = title;
            TimeStart = timeStart;
            TimeEnd = timeEnd;
        }

        public Label(string title, double timeStart, double timeEnd, double selLow, double selHigh)
        {
            Title = title;
            TimeStart = timeStart;
            TimeEnd = timeEnd;
            SelLow = selLow;
            SelHigh = selHigh;
        }
    }
}