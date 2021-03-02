using Egret.Cli.Commands;
using Egret.Cli.Extensions;
using Egret.Cli.Hosting;
using System.IO;

namespace Egret.Cli.Processing
{
    public class OutputFile
    {
        private readonly TestCommandOptions options;
        private readonly string filestem;
        private readonly string dateStamp;

        public OutputFile(TestCommandOptions options, RunInfo runInfo)
        {
            this.options = options;
            filestem = options.Configuration.Filestem();
            dateStamp = runInfo.StartedAt.ToString("yyyyMMdd-HHmmss");
        }

        public string GetOutputPath(string extension)
        {
            return Path.Combine(options.Output.FullName, $"{dateStamp}_{filestem}_results.{extension}");
        }

        public FileInfo GetOutputFile(string extension)
        {
            return options.Output.Combine($"{dateStamp}_{filestem}_results.{extension}");
        }
    }
}