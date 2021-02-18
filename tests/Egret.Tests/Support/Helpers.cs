using Divergic.Logging.Xunit;
using Egret.Cli;
using FluentAssertions;
using FluentAssertions.Equivalency;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections;
using System.IO.Abstractions.TestingHelpers;
using System.Runtime;
using Xunit.Abstractions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Egret.Tests.Support
{
    public static class Helpers
    {
        static Helpers()
        {
            AssertionOptions.AssertEquivalencyUsing(x => x.WithTracing());
        }

        public readonly static IOptions<AppSettings> DefaultAppSettings = Options.Create<AppSettings>(new());
        public readonly static INamingConvention DefaultNamingConvention = UnderscoredNamingConvention.Instance;

        public readonly static string DefaultTestPath = "/";
        public readonly static string DefaultTestConfigPath = "/config.egret.yaml";

        public static void AddFile(this MockFileSystem fileSystem, TestFile file)
        {
            fileSystem.AddFile(file.Path, file.Contents);
        }

        public static void BeEquivalentTo_Record<TExpectation>(
            this FluentAssertions.Primitives.ObjectAssertions host,
            TExpectation target,
            Func<EquivalencyAssertionOptions<TExpectation>, EquivalencyAssertionOptions<TExpectation>> config = default,
            string because = "",
            params object[] becauseArgs)
        {
            host.BeEquivalentTo(target, config.Compose(x => x.Using(new RecordStructuralEqualityEquivalencyStep())), because, becauseArgs);
        }

        // HACK: https://stackoverflow.com/a/64307613/224512
        public static bool IsRecord(this Type type) => type.GetMethod("<Clone>$") != null;
    }

    public class RecordStructuralEqualityEquivalencyStep : StructuralEqualityEquivalencyStep
    {
        public new bool CanHandle(IEquivalencyValidationContext context, IEquivalencyAssertionOptions config)
        {
            return context.CompileTimeType.IsRecord();
        }
    }
}