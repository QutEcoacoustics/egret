
using Egret.Cli.Serialization;
using Egret.Cli.Serialization.Yaml;
using LanguageExt;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization;
using static LanguageExt.Prelude;

namespace Egret.Cli.Models
{
    /// <summary>
    /// A set of test cases and tests imported from other sources.
    /// Not evaluateable.
    /// </summary>
    /// <remarks>
    /// A test suite is derived from this class. A suite needs: a set of tests,
    /// tool configuration, and other test related functionality. A suite is actually evaluateable.
    /// 
    /// A set of tests by itself is merely a manifest of files and expectations. By breaking a suite and a test set up
    /// we can form composable and reusable sets of tests, that are shared among many test suites.
    /// As such TestCaseSets are used in the CommonTests section in the config.
    /// </remarks>
    public class TestCaseSet : IKeyedObject, ISourceInfo
    {
        private readonly string name;

        public string Name
        {
            get
            {
                return name ?? ((IKeyedObject)this).Key;
            }
            init
            {
                name = value;
            }
        }

        public Arr<TestCase> Tests { get; set; } = Empty;

        public Arr<TestCaseInclude> IncludeTests { get; set; } = Empty;

        string IKeyedObject.Key { get; set; }

        public SourceInfo SourceInfo { get; set; }

        public IEnumerable<TestCase> GetAllTests()
        {
            // functional collections result in no null reference errors
            return Tests.Concat(IncludeTests.SelectMany(i => i.Tests));
        }
    }

    /// <summary>
    /// A suite of test cases (possibly imported from other sources)
    /// Along with tool configs.
    /// A suite is the basic grouping component in Egret and is represents an
    /// evaluateable object.
    /// </summary>
    /// <remarks>
    /// A test suite is the base of this class. A suite needs: a set of tests,
    /// tool configuration, and other test related functionality. A suite is actually evaluateable.
    /// 
    /// A set of tests by itself is merely a manifest of files and expectations. By breaking a suite and a test set up
    /// we can form composable and reusable sets of tests, that are shared among many test suites.
    /// As such TestCaseSets are used in the CommonTests section in the config.
    /// </remarks>
    public class Suite : TestCaseSet
    {
        public AliasedString LabelAliases { get; init; } = new AliasedString();

        public Dictionary<string, Dictionary<string, object>> ToolConfigs { get; init; }
    }
}