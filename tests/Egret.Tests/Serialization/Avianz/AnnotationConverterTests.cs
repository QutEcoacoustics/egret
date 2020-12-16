using Egret.Cli.Models.Avianz;
using System.Text.Json;
using Xunit;
using FluentAssertions;

namespace Egret.Tests.Serialization.Avianz
{
    public class AnnotationConverterTests
    {
        [Fact]
        public void CanDeserializeAnnotation()
        {
            var json = @"[1.6312842304060433, 2.467507086891466, 1615, 3711, [{""filter"": ""M"", ""species"": ""Ghff"", ""certainty"": 100, ""calltype"": ""GHFF""}]]";
            var actual = JsonSerializer.Deserialize<Annotation>(json);

            var expected = new Annotation(1.6312842304060433, 2.467507086891466, 1615, 3711, new[] {
                 new Label("M", "Ghff", 100, "GHFF")
            });

            Assert.Equal(expected, actual);
        }
    }
}