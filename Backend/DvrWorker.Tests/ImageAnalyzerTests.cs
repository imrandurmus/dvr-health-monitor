using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using DvrWorker.Services;
using System.IO;

namespace DvrWorker.Tests;
public class ImageAnalyzerTests
{
    private readonly IImageAnalyzer _analyzer = new ImageAnalyzer();

    [Fact]
    public void SharpImage_ShouldHaveHighLaplacianVariance()
    {
        var bytes = File.ReadAllBytes("TestImages/sharp.jpg");
        var metrics = _analyzer.Measure(bytes);


        Assert.True(metrics.LaplacianVariance > 140,
            $"Expected sharp image to have high variance (>140), got {metrics.LaplacianVariance}");

    }

    [Fact]
    public void BlurryImage_ShouldHaveLowLaplacianVariance()
    {
        var bytes = File.ReadAllBytes("TestImages/blur.jpg");
        var metrics = _analyzer.Measure(bytes);

        Assert.True(metrics.LaplacianVariance < 50,
            $"Expected blurry image to have low variance (<50), got {metrics.LaplacianVariance}");

    }

    [Fact]
    public void SolidImage_ShouldHaveLowLumaStdDev()
    {
        var bytes = File.ReadAllBytes("TestImages/solid.jpg");
        var metrics = _analyzer.Measure(bytes);

        Assert.True(metrics.LumaStdDev < 2,
            $"Expected solid image to have low stddev (<2), got {metrics.LumaStdDev}");
    }
}
