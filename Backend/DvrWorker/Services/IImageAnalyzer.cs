namespace DvrWorker.Services;


public interface IImageAnalyzer
{
    ImageMetrics Measure(ReadOnlyMemory<byte> jpegBytes);
    bool IsBlurry(ImageMetrics m);
    bool IsSingleColor(ImageMetrics m);
}

public sealed record ImageMetrics (double LaplacianVariance, double LumaStdDev);