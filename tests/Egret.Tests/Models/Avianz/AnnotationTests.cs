using Egret.Cli.Models.Avianz;
using FluentAssertions.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Egret.Tests.Models.Avianz
{
    public class AnnotationTests
    {
        [Fact]
        public void CanDeserializeAnnotation()
        {
            var label = new Label("M", "Ghff", 100, "GHFF");
            var annotation = new Annotation(1.6312842304060433, 2.467507086891466, 1615, 3711, new[] {
                label
            });

            Assert.Equal(1, annotation.Labels.Count);
            Assert.Equal(label, annotation.Labels.First());
        }
    }
}