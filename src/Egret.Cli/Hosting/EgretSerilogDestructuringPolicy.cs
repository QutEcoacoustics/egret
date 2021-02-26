using Egret.Cli.Models;
using Serilog.Core;
using Serilog.Data;
using Serilog.Events;
using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Egret.Cli.Hosting
{
    public class EgretSerilogDestructuringPolicy : IDestructuringPolicy
    {
        public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue result)
        {
            bool success;
            (success, result) = value switch
            {
                FileSystemInfo i => (true, new ScalarValue(i.ToString())),
                System.IO.Abstractions.IFileSystemInfo i => (true, new ScalarValue(i.ToString())),
                Interval i => (true, new ScalarValue(i.ToString())),
                _ => (false, null),
            };

            return success;
        }
    }

}