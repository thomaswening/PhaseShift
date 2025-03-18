using System.Collections.Concurrent;

namespace PhaseShift.Core.AccuracyTestTool.Tests;

/// <summary>
/// Represents a statistical test that performs multiple runs of a test candidate function,
/// collects samples, and calculates statistical metrics such as the mean and standard deviation of the means.
/// </summary>
public class StatisticalTest
{
    private readonly ConcurrentBag<double> _samples = [];

    private readonly int _sampleCount;
    private readonly Func<double> _testCandidate;

    private int _totalSamples;

    /// <param name="testCandidate">The function to be tested.</param>
    /// <param name="numberOfRuns">The number of runs to execute the test.</param>
    /// <param name="sampleCount">The number of samples to collect per run.</param>
    public StatisticalTest(Func<double> testCandidate, int sampleCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(sampleCount, 1, nameof(sampleCount));

        _sampleCount = sampleCount;
        _testCandidate = testCandidate;
    }

    public event EventHandler<int>? SampleGenerated;

    public double SampleMean => _samples.Average();
    public double SampleStandardDeviation => Math.Sqrt(_samples.Average(x => Math.Pow(x - SampleMean, 2)));
    public double RelativeSampleStandardDeviation => SampleMean != 0 ? SampleStandardDeviation / SampleMean : double.NaN;

    public void Execute()
    {
        Reset();

        for (int i = 0; i < _sampleCount; i++)
        {
            _samples.Add(_testCandidate());
            SampleGenerated?.Invoke(this, ++_totalSamples);
        }
    }

    /// <summary>
    /// Resets the collected data and measurement count.
    /// </summary>
    private void Reset()
    {
        _samples.Clear();
        _totalSamples = 0;
    }
}
