namespace Egret.Cli.Processing
{
    using Commands;
    using Extensions;
    using Hosting;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class OutputFile
    {
        private readonly string dateStamp;
        private readonly string filestem;
        private readonly TestCommandOptions options;

        public OutputFile(TestCommandOptions options, RunInfo runInfo)
        {
            this.options = options;
            filestem = options.Configuration.Filestem();
            dateStamp = runInfo.StartedAt.ToString("yyyyMMdd-HHmmss");
        }

        public string GetOutputPath(string extension, params string[] nameParts)
        {
            string name = BuildOutputFileName(extension, nameParts);
            return Path.Combine(options.Output.FullName, name);
        }

        public FileInfo GetOutputFile(string extension, params string[] nameParts)
        {
            string name = BuildOutputFileName(extension, nameParts);
            return options.Output.Combine(name);
        }

        private string BuildOutputFileName(string extension, IReadOnlyCollection<string> nameParts)
        {
            var parts = new List<string> {dateStamp, filestem};

            if (nameParts != null && nameParts.Count > 0)
            {
                parts.AddRange(nameParts);
            }

            parts.Add("results");

            var validParts = parts.Where(i => !string.IsNullOrWhiteSpace(i));
            string name = string.Join('_', validParts);
            return $"{name}.{extension.Trim('.')}";
        }
    }
}