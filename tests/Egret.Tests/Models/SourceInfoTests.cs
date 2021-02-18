using Egret.Cli.Models;
using Egret.Tests.Support;
using Xunit;

namespace Egret.Tests.Models
{
    public class SourceInfoTests
    {
        public SourceInfoTests()
        {

        }

        [Fact]
        public void SourceInfosAreStructurallyEquatable()
        {
            var a = new SourceInfo(Helpers.DefaultTestConfigPath, 4, 5, 10, 1);
            var b = new SourceInfo(Helpers.DefaultTestConfigPath, 4, 5, 10, 1);

            Assert.Equal(a, b);
        }
    }
}