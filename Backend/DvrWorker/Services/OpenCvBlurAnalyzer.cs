using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DvrWorker.Services;

public sealed class OpenCvBlurAnalyzer
{
    public static double LaplacianVariance(ReadOnlyMemory<byte> jpegBytes)
    {
        using var mat = Cv2.ImDecode(jpegBytes.ToArray(),ImreadModes.Grayscale);
        if(mat.Empty()) return 0;

        using var lap = new Mat();
        Cv2.Laplacian(mat, lap, MatType.CV_64F);
        Cv2.MeanStdDev(lap, out _, out var stddev);
        return stddev.Val0 * stddev.Val0;
    }
}
