using System.Collections.Generic;
using System.Text.Json.Serialization;
using Egret.Cli.Serialization.Json.Avianz;
using LanguageExt;

namespace Egret.Cli.Models.Avianz
{
    /// <summary>
    /// The avianz data file uses an array where the first element is optional metadata.
    /// https://github.com/smarsland/AviaNZ/blob/57e6a2b43ceaaf871afa524a02c1035f0a50dd7e/Docs/file_format_specification.md#L6
    /// </summary>
    /// <remarks>
    /// See also <see cref="AnnotationConverter"/>
    /// </remarks>
    // Using Arr for structural equivalence so record equality behaves in a useful manner
    [JsonConverter(typeof(AnnotationConverter))]
    public record Annotation(double Start, double End, double Low, double High, Arr<Label> Labels) : MetadataOrAnnotation;


}