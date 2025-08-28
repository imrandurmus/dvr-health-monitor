using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DvrWorker.Services;

public sealed class ImageSharpColorAnalyzer
{
    public static double LumaStdDev(ReadOnlyMemory<byte> jpegBytes)
    {
        using var image = Image.Load<Rgba32>(jpegBytes.ToArray());
        var lumas = new double[image.Width * image.Height];

        int i = 0;
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                var p = image[x, y]; //Rgba32 pixel
                var luma = 0.299 * p.R + 0.587 * p.G + 0.114 * p.B;
                lumas[i++] = luma;
            }
        }

        var mean = lumas.Average();
        var variance = lumas.Select(l => (l - mean) * (l - mean)).Average();
        return Math.Sqrt(variance);
    }
}
