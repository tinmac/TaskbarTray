#nullable enable

using Serilog;
using SkiaSharp;
using Svg.Skia;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;


namespace PowerSwitch.stuff;


public static class ImageHelper
{
    public static void ConvertSvgToIco(string svgPath, string icoOutputPath, int width = 32, int height = 32, SKColor? foregroundColor = null)
    {
        if (!File.Exists(svgPath))
        {
            Log.Error($"SVG file not found:  {svgPath}");
            throw new FileNotFoundException("SVG file not found.", svgPath);
        }

        Log.Information($"\nConverting svg to ico... {svgPath}");

        var svg = new SKSvg();
        using var svgStream = File.OpenRead(svgPath);
        svg.Load(svgStream);

        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);

        float scaleX = width / svg.Picture.CullRect.Width;
        float scaleY = height / svg.Picture.CullRect.Height;
        float scale = Math.Min(scaleX, scaleY);
        var matrix = SKMatrix.CreateScale(scale, scale);

        // Create color filter paint if foregroundColor is provided
        SKPaint? paint = null;
        if (foregroundColor.HasValue)
        {
            paint = new SKPaint
            {
                ColorFilter = SKColorFilter.CreateBlendMode(foregroundColor.Value, SKBlendMode.SrcIn)
            };
        }

        // Draw SVG with or without color override
        canvas.DrawPicture(svg.Picture, ref matrix, paint);
        canvas.Flush();

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100); // PNG format for .ico
        using var ms = new MemoryStream(data.ToArray());
        using var bmp = new Bitmap(ms);

        Log.Information($"saving ico... {icoOutputPath}");
        SaveBitmapAsIco(bmp, icoOutputPath);
    }

    private static void SaveBitmapAsIco(Bitmap image, string outputPath)
    {
        using var fs = new FileStream(outputPath, FileMode.Create);
        using var bw = new BinaryWriter(fs);

        bw.Write((ushort)0);   // Reserved
        bw.Write((ushort)1);   // ICO type
        bw.Write((ushort)1);   // Image count

        bw.Write((byte)image.Width);
        bw.Write((byte)image.Height);
        bw.Write((byte)0);     // Color palette
        bw.Write((byte)0);     // Reserved
        bw.Write((ushort)1);   // Color planes
        bw.Write((ushort)32);  // Bits per pixel

        using var imgStream = new MemoryStream();
        image.Save(imgStream, ImageFormat.Png);
        byte[] pngData = imgStream.ToArray();

        bw.Write(pngData.Length);
        bw.Write(22); // Offset
        bw.Write(pngData);
    }
}
