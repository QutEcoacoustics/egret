using Egret.Cli.Models;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.ColorSpaces.Conversion;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.CommandLine;
using System.CommandLine.IO;
using System.CommandLine.Rendering;

namespace Egret.Cli.Extensions
{
    public static class AnsiStringExtensions
    {


        public static ContainerSpan StyleRgb(this string @string, byte r, byte g, byte b)
        {

            return new ContainerSpan(
                ForegroundColorSpan.Rgb(r, g, b),
                new ContentSpan(@string),
                ForegroundColorSpan.Reset());
        }

        public static ContainerSpan StyleColor(this string @string, ForegroundColorSpan color)
        {
            return new ContainerSpan(
                color,
                new ContentSpan(@string),
                ForegroundColorSpan.Reset());
        }
        public static ContainerSpan StyleHighlight(this string @string, BackgroundColorSpan color)
        {
            return new ContainerSpan(
                color,
                new ContentSpan(@string),
                BackgroundColorSpan.Reset());
        }

        public static ContainerSpan StyleValue(this string @string)
        {
            return new ContainerSpan(
                ForegroundColorSpan.Rgb(255, 175, 135),
                new ContentSpan(@string),
                ForegroundColorSpan.Reset());
        }
        public static ContainerSpan StyleNumber(this string @string)
        {
            return new ContainerSpan(
                ForegroundColorSpan.Rgb(175, 215, 175),
                new ContentSpan(@string),
                ForegroundColorSpan.Reset());
        }

        public static ContainerSpan StyleUnimportant(this string @string)
        {
            return new ContainerSpan(
                ForegroundColorSpan.DarkGray(),
                new ContentSpan(@string),
                ForegroundColorSpan.Reset());
        }

        public static ContainerSpan StyleSuccess(this string @string)
        {
            return new ContainerSpan(
                ForegroundColorSpan.LightGreen(),
                new ContentSpan(@string),
                ForegroundColorSpan.Reset());
        }

        private static readonly ColorSpaceConverter colorConverter = new ColorSpaceConverter();
        public static ContainerSpan StyleGrade(this string @string, double performance, Interval? range)
        {
            var y = (range ?? Interval.Unit).UnitNormalize(performance);
            // poor man's color scale, red - yellow - green
            var hsv = new Hsv(h: (float)(y * 120), s: 1, v: 1);
            var rgb = (Rgb24)colorConverter.ToRgb(hsv);

            return new ContainerSpan(
                ForegroundColorSpan.Rgb(rgb.R, rgb.G, rgb.B),
                new ContentSpan(@string),
                ForegroundColorSpan.Reset());
        }

        public static ContainerSpan StyleFailure(this string @string)
        {
            return new ContainerSpan(
                ForegroundColorSpan.LightRed(),
                new ContentSpan(@string),
                ForegroundColorSpan.Reset());
        }
        public static ContainerSpan StyleBold(this string @string)
        {
            return new ContainerSpan(
                StyleSpan.BoldOn(),
                new ContentSpan(@string),
                StyleSpan.BoldOff());
        }
        public static ContainerSpan StyleUnderline(this string @string)
        {
            return new ContainerSpan(
                StyleSpan.UnderlinedOn(),
                new ContentSpan(@string),
                StyleSpan.UnderlinedOff());
        }

        public static ContentSpan AsTextSpan(this string @string)
        {
            return new ContentSpan(@string);
        }
    }
}