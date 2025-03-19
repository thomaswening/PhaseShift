using System.Collections.Concurrent;

namespace PhaseShift.AccuracyTestTool.Tests;

/// <summary>
/// Represents a statistical test that performs multiple runs of a test candidate function,
/// collects samples, and calculates statistical metrics such as the mean and standard deviation of the means.
/// </summary>
public class StatisticalTest
{
    private readonly ParallelOptions _parallelOptions = new() { MaxDegreeOfParallelism = Environment.ProcessorCount };
    private readonly int _sampleCount;
    private readonly ConcurrentBag<double> _samples = [];
    private readonly Func<double> _testCandidate;
    private int _generatedSamples;

    public StatisticalTest(Func<double> testCandidate, int sampleCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(sampleCount, 1, nameof(sampleCount));

        _sampleCount = sampleCount;
        _testCandidate = testCandidate;
    }

    public event EventHandler? SampleGenerated;

    public int GeneratedSamples => _generatedSamples;
    public double RelativeSampleStandardDeviation => SampleMean != 0 ? SampleStandardDeviation / SampleMean : double.NaN;
    public double SampleMean => _samples.Average();
    public double SampleStandardDeviation => Math.Sqrt(_samples.Average(x => Math.Pow(x - SampleMean, 2)));

    /// <summary>
    /// Executes the test candidate function for the specified number of runs.
    /// </summary>
    /// <param name="parallelize">
    /// Whether to execute the test candidate function in parallel.
    /// Defaults to <see langword="false"/>.
    /// </param>
    /// <remarks>
    /// Cautiously use parallelization with this method 
    /// as it may affect the test candidate's accuracy under heavy load!
    /// </remarks>
    public void Execute(bool parallelize = false)
    {
        Reset();

        if (parallelize)
        {
            ExecuteParallel();
        }
        else
        {
            ExecuteSynchronous();
        }
    }

    private void ExecuteParallel()
    {
        Parallel.For(0, _sampleCount, _parallelOptions, i =>
        {
            _samples.Add(_testCandidate());
            Interlocked.Increment(ref _generatedSamples);
            SampleGenerated?.Invoke(this, EventArgs.Empty);
        });
    }

    private void ExecuteSynchronous()
    {
        for (int i = 0; i < _sampleCount; i++)
        {
            _samples.Add(_testCandidate());
            _generatedSamples++;
            SampleGenerated?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Resets the collected data and measurement count.
    /// </summary>
    private void Reset()
    {
        _samples.Clear();
        _generatedSamples = 0;
    }
}
