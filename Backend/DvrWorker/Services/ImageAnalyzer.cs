using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DvrWorker.Services;

public sealed class ImageAnalyzer : IImageAnalyzer
{
    public ImageMetrics Measure(ReadOnlyMemory<byte> jpegBytes)
    {
        double lapVar = OpenCvBlurAnalyzer.LaplacianVariance(jpegBytes);
        double lumaStd = ImageSharpColorAnalyzer.LumaStdDev(jpegBytes);
        return new ImageMetrics(lapVar, lumaStd);
    }
    public bool IsBlurry(ImageMetrics m) => m.LaplacianVariance < 140;
    public bool IsSingleColor(ImageMetrics m) => m.LumaStdDev < 2;
}
