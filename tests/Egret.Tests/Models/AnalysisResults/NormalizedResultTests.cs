using Egret.Cli.Extensions;
using Egret.Cli.Models;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace Egret.Tests.Models.AnalysisResults
{
    public class NormalizedResultTests
    {
        private readonly JsonElement Example = JsonDocument.Parse(@"
            {
                ""Label"": ""ABC"",
                ""Low"": 123,
                ""High"": 456,
                ""Start"": 7.89,
                ""End"": 10.123
            }
        ").RootElement;

        private readonly SourceInfo ExampleSource = new SourceInfo("/a/directory/a_path_to_a_json_file.json");

        [Fact]
        public void HasCustomToString()
        {
            //Given
            var result = new JsonResult(Example, ExampleSource);

            //When
            var actual = result.ToString();

            //Then
            Assert.Equal("a_path_to_a_json_file.json: Label[s]={Label: {ABC}} Start={Start: 7.89} End={End: 10.123} Low={Low: 123} High={High: 456}", actual);
        }

        [Theory]
        [InlineData("a", "b,c,d", "a")]
        [InlineData("a", null, "a")]
        [InlineData(null, "b,c,d", "b,c,d")]
        [InlineData(null, null, null)]
        public void LabelValidationsCombine(string label, string labels, string expect)
        {
            var storage = new Dictionary<string, object>();
            if (label is not null)
            {
                storage.Add("label", label);
            }

            if (labels is not null)
            {
                storage.Add("labels", labels.Split(","));
            }

            var result = new DictionaryResult(storage);

            var actual = result.Labels;
            if (expect is null)
            {
                actual.IsFail.Should().BeTrue();
                actual.FailToSeq().Should().ContainMatch("*`label`*").And.ContainMatch("*`labels`*");
            }
            else
            {
                actual.IsSuccess.Should().BeTrue();
                ((KeyedValue<IEnumerable<string>>)actual).Value.Should().Equal(expect.Split(","));
            }
        }

        public class DictionaryResult : NormalizedResult
        {
            private readonly Dictionary<string, object> labels;
            private readonly Dictionary<string, object>.KeyCollection keys;

            public DictionaryResult(Dictionary<string, object> labels)
            {
                this.labels = labels;
                keys = labels.Keys;
            }

            public override SourceInfo SourceInfo { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public override bool TryGetValue<T>(string key, out T value, StringComparison comparison = StringComparison.InvariantCulture)
            {
                var foundKey = keys.Find(k => k.Equals(key, comparison));
                if (foundKey)
                {
                    var resultValue = labels[(string)foundKey];
                    value = (T)resultValue;
                    return true;
                }
                else
                {
                    value = default;
                    return false;
                }
            }
        }
    }
}