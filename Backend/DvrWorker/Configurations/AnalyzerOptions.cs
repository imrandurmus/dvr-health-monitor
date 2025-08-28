namespace DvrWorker.Configurations;

public sealed class AnalyzerOptions
{
    public double BlurLaplacianMin { get; set; } = 140;  // Laplacian = 2D second derivative of image intensity; highlights edges.
                                                         // Variance of Laplacian ? high = sharp (lots of edges), low = blurry (edges lost). 

    public double ColorStdMin { get; set; } = 2.0;  // Luma StdDev = pixel brightness variation; low = single-color/flat image, high = normal detail.
}