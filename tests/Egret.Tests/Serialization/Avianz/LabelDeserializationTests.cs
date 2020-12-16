using Egret.Cli.Models.Avianz;
using System.Text.Json;
using Xunit;

namespace Egret.Tests.Serialization.Avianz
{



    public class LabelDeserializationTests
    {
        public LabelDeserializationTests()
        {

        }

        [Fact]
        public void CanDeserializeLabel()
        {
            var json = @"{""filter"": ""M"", ""species"": ""Ghff"", ""certainty"": 100, ""calltype"": ""GHFF""}";
            var actual = JsonSerializer.Deserialize<Label>(json);

            Assert.Equal(new Label("M", "Ghff", 100, "GHFF"), actual);
        }

        [Fact]
        public void CanDeserializeLabelArray()
        {
            var json = @"[{""filter"": ""M"", ""species"": ""Ghff"", ""certainty"": 100, ""calltype"": ""GHFF""}]";
            var actual = JsonSerializer.Deserialize<Label[]>(json);

            Assert.Equal(new[] { new Label("M", "Ghff", 100, "GHFF") }, actual);
        }
    }

}