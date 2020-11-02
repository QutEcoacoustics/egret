
using System;
using System.Collections.Generic;
using System.Numerics;


namespace Egret.Cli.Models
{
    public class Config
    {
        public Dictionary<string, Tool> Tools { get; set; }

        public Dictionary<string, Suite> TestSuites { get; set; }

        public Dictionary<string, Case[]> CommonCases { get; set; }
    }

    public class Tool
    {
        public string Command { get; set; }

        public string ResultPattern { get; set; }
    }

    public class Suite
    {

        public string Name { get; set; }

        public string DefaultLabel { get; set; }

        public Case[] Cases { get; set; }

        public string[] IncludeCases { get; set; } = Array.Empty<string>();

        public Dictionary<string, Case[]> SharedCases { get; set; } = new();

        public Dictionary<string, object> ToolConfigs { get; set; }

    }

    public class Case
    {
        public Expectation[] ExpectEvents { get; set; }
        public AggregateExpectation[] Expect { get; set; }

        public string File { get; set; }

        public Uri Uri { get; set; }
    }



    public class Expectation
    {
        public string Label { get; set; }

        public string[] Labels { get; set; }

        public Bounds Bounds { get; set; }

        public Centroid Centroid { get; set; }

        public bool Match { get; set; } = true;


        public Interval Duration { get; set; }
        public Interval Bandwidth { get; set; }

        public string Condition { get; set; }

    }

    public abstract class AggregateExpectation
    {

    }

    public class NoEvents : AggregateExpectation
    {

    }

    public class EventCount : AggregateExpectation
    {
        public int Count { get; set; }
    }


    public struct Interval
    {
        public double Minimum { get; set; }

        public double Maximum { get; set; }
    }

    /// <summary>
    /// Represents a geometric center of an event.
    // Coordinates are encoded as a pair of intervals to allow fuzzy matching.
    /// <summary>
    public struct Centroid
    {
        public Interval Seconds { get; set; }

        public Interval Hertz { get; set; }
    }
    /// <summary>
    /// Represents a rectangular event.
    // Coordinates are encoded as four intervals to allow fuzzy matching.
    /// <summary>
    public struct Bounds
    {
        public Interval StartSeconds { get; set; }

        public Interval EndSeconds { get; set; }

        public Interval LowHertz { get; set; }

        public Interval HighHertz { get; set; }
    }




}