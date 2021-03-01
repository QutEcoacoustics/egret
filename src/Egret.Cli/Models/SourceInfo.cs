using System.IO;

namespace Egret.Cli.Models
{
    public partial record SourceInfo(string Source, int? LineStart = null, int? ColumnStart = null, int? LineEnd = null, int? ColumnEnd = null);

    public partial record SourceInfo
    {
        public static readonly SourceInfo Unknown = new("<unknown>");

        public override string ToString() => ToString(shortSource: false);

        public string ToString(bool shortSource)
        {
            var source = Source ?? "<unknown>";
            source = shortSource ? Path.GetFileName(source) : source;

            // should conform to the format supported by vscode
            // https://github.com/microsoft/vscode/blob/226503ba0a2105c86f5cffe949c3afa9eb996185/src/vs/workbench/contrib/terminal/browser/links/terminalValidatedLocalLinkProvider.ts#L34-L43
            // and output a format similar to roslyn:
            // https://github.com/dotnet/roslyn/blob/master/src/Compilers/Core/Portable/Diagnostic/FileLinePositionSpan.cs#L142-L146
            return source + (LineStart, ColumnStart, LineEnd, ColumnEnd) switch
            {
                (not null, null, null, null) => $"({LineStart})",
                (not null, not null, null, null) => $"({LineStart},{ColumnStart})",
                (not null, not null, not null, not null) => $"({LineStart},{ColumnStart})-({LineEnd},{ColumnEnd})",
                (null, null, null, null) => string.Empty,
                _ => $"<invalid_line_data>",
            };
        }
    }

}