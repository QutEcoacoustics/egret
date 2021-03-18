namespace Egret.Tests.Support
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using Xunit.Sdk;

    /// <summary>
    /// A xUnit Data Attribute for loading data from a file.
    /// </summary>
    public class FileDataAttribute : DataAttribute
    {
        private readonly string filePath;

        /// <summary>
        /// Create a new File Data Attribute.
        /// Loads data from a file.
        /// </summary>
        /// <param name="filePath">The path to the file.</param>
        public FileDataAttribute(string filePath)
        {
            this.filePath = filePath;
        }

        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            if (testMethod == null)
            {
                throw new ArgumentNullException(nameof(testMethod));
            }

            // Get the absolute path to the file
            var isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            var isAbsolutePath = isWindows
                ? Path.IsPathFullyQualified(this.filePath)
                : Path.IsPathRooted(this.filePath);
            var path = isAbsolutePath
                ? this.filePath
                : Path.Combine(Directory.GetCurrentDirectory(), this.filePath.Trim('\\'));

            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Could not find file at path: {path}");
            }

            // The return data is an enumerable of object array.
            // The array is the test method parameters. Each item in the enumerable is one call of the test method.
            // return one item in the enumerable, one item in the object array, which is the path to the file
            return new[] {new[] {path}};
        }
    }
}