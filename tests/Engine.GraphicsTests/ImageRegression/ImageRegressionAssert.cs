using Shouldly;

namespace Engine.GraphicsTests.ImageRegression;

internal static class ImageRegressionAssert
{
    private const byte SoftwarePerChannelTolerance = 2;
    private const double SoftwareMaxDifferentRatio = 0.01;
    private const double SoftwareMaxMse = 16.0;

    private static readonly string GoldenRoot = Path.Combine(AppContext.BaseDirectory, "Golden");
    private static readonly string SourceGoldenRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Golden"));
    private static readonly string OutputRoot =
        Path.Combine(AppContext.BaseDirectory, "TestResults", "ImageRegression");

    public static void MatchesBaseline(
        string baselineName,
        byte[] actual,
        int width,
        int height,
        bool exact = false)
    {
        var goldenPath = Path.Combine(GoldenRoot, $"{baselineName}.rgba");

        if (ShouldUpdateGoldens())
        {
            File.WriteAllBytes(Path.Combine(SourceGoldenRoot, $"{baselineName}.rgba"), actual);
            return;
        }

        File.Exists(goldenPath).ShouldBeTrue(
            $"Missing golden image: {goldenPath}. Run with UPDATE_GOLDENS=1 to create it.");

        var expected = File.ReadAllBytes(goldenPath);
        expected.Length.ShouldBe(actual.Length, $"Golden size mismatch for {baselineName}");

        if (!Compare(expected, actual, width, height, exact, out var differentPixels, out var mse, out var message))
        {
            Directory.CreateDirectory(OutputRoot);
            var actualPath = Path.Combine(OutputRoot, $"{baselineName}-actual.rgba");
            var diffPath = Path.Combine(OutputRoot, $"{baselineName}-diff.rgba");
            File.WriteAllBytes(actualPath, actual);
            File.WriteAllBytes(diffPath, BuildDiff(expected, actual));
            throw new ShouldAssertException(
                $"{baselineName}: {message} (differentPixels={differentPixels}, mse={mse:F2}). " +
                $"Wrote {actualPath} and {diffPath}.");
        }
    }

    private static bool Compare(
        byte[] expected,
        byte[] actual,
        int width,
        int height,
        bool exact,
        out int differentPixels,
        out double mse,
        out string message)
    {
        differentPixels = 0;
        mse = 0;
        message = string.Empty;

        var pixelCount = width * height;
        double squaredError = 0;
        var tolerance = exact ? (byte)0 : SoftwarePerChannelTolerance;

        for (var i = 0; i < pixelCount; i++)
        {
            var offset = i * 4;
            var dr = System.Math.Abs(expected[offset] - actual[offset]);
            var dg = System.Math.Abs(expected[offset + 1] - actual[offset + 1]);
            var db = System.Math.Abs(expected[offset + 2] - actual[offset + 2]);
            var da = System.Math.Abs(expected[offset + 3] - actual[offset + 3]);

            squaredError += dr * dr + dg * dg + db * db + da * da;

            if (dr > tolerance || dg > tolerance || db > tolerance || da > tolerance)
                differentPixels++;
        }

        mse = squaredError / (pixelCount * 4);
        var ratio = (double)differentPixels / pixelCount;

        if (exact ? differentPixels > 0 : ratio > SoftwareMaxDifferentRatio || mse > SoftwareMaxMse)
        {
            message = exact
                ? "Pixels differ from golden."
                : $"Pixel diff ratio {ratio:P2} or MSE {mse:F2} exceeded tolerance.";
            return false;
        }

        return true;
    }

    private static bool ShouldUpdateGoldens()
    {
        var value = Environment.GetEnvironmentVariable("UPDATE_GOLDENS");
        return string.Equals(value, "1", StringComparison.Ordinal)
               || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static byte[] BuildDiff(byte[] expected, byte[] actual)
    {
        var diff = new byte[expected.Length];
        for (var i = 0; i < expected.Length; i += 4)
        {
            if (expected[i] == actual[i]
                && expected[i + 1] == actual[i + 1]
                && expected[i + 2] == actual[i + 2]
                && expected[i + 3] == actual[i + 3])
            {
                diff[i + 3] = 255;
                continue;
            }

            diff[i] = 255;
            diff[i + 2] = 255;
            diff[i + 3] = 255;
        }

        return diff;
    }
}
