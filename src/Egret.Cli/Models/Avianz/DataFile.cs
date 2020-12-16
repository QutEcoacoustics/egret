using System;
using System.Text.Json.Serialization;
using System.Collections;
using System.Collections.Generic;
using Egret.Cli.Serialization.Json.Avianz;
using Egret.Cli.Serialization.Yaml;

namespace Egret.Cli.Models.Avianz
{
    /// <summary>
    /// The avianz data file uses an array where the first element is optional metadata.
    /// https://github.com/smarsland/AviaNZ/blob/57e6a2b43ceaaf871afa524a02c1035f0a50dd7e/Docs/file_format_specification.md#L6
    /// </summary>
    /// <remarks>
    /// See also <see cref="DataFileConverter"/>
    /// </remarks>
    [JsonConverter(typeof(DataFileConverterFactory))]
    public class DataFile : IReadOnlyCollection<MetadataOrAnnotation>, ISourceInfo
    {

        public Metadata Metadata { get; init; } = null;

        public IReadOnlyCollection<Annotation> Annotations { get; init; } = Array.Empty<Annotation>();

        public SourceInfo SourceInfo { get; set; }

        int IReadOnlyCollection<MetadataOrAnnotation>.Count => (Metadata is null ? 0 : 1) + Annotations.Count;

        IEnumerator<MetadataOrAnnotation> IEnumerable<MetadataOrAnnotation>.GetEnumerator()
        {
            if (Metadata is not null)
            {
                yield return Metadata;
            }

            foreach (var annotation in Annotations)
            {
                yield return annotation;

            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<MetadataOrAnnotation>)this).GetEnumerator();
        }
    }


}