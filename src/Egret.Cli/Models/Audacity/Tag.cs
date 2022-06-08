﻿namespace Egret.Cli.Models.Audacity
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// Audacity project file tag.
    /// A tag is a key value pair.
    /// </summary>
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

        public static implicit operator KeyValuePair<string, string>(Tag tag)
        {
            return new(tag.Name, tag.Value);
        }

        public static implicit operator Tuple<string, string>(Tag tag)
        {
            return new(tag.Name, tag.Value);
        }
        public static implicit operator ValueTuple<string, string>(Tag tag)
        {
            return new(tag.Name, tag.Value);
        }

        public static explicit operator Tag(KeyValuePair<string, string> pair)
        {
            return new(pair.Key, pair.Value);
        }

        public static explicit operator Tag(Tuple<string, string> pair)
        {
            return new(pair.Item1, pair.Item2);
        }

        public static explicit operator Tag(ValueTuple<string, string> pair)
        {
            return new(pair.Item1, pair.Item2);
        }
    }
}