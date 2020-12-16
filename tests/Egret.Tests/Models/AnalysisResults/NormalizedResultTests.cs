using Egret.Cli.Models;
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
            Assert.Equal("a_path_to_a_json_file.json: Label={Label: ABC} Start={Start: 7.89} End={End: 10.123} Low={Low: 123} High={High: 456}", actual);
        }
    }
}