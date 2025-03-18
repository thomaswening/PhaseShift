namespace PhaseShift.Core.AccuracyTestTool.Tests;

internal abstract class AccuracyTest
{
    protected readonly int _expectedElapsedTimeMilliseconds;
    private const int SecondsInAnHour = 3_600;

    private readonly int _acceptableDeviationMilliseconds;
    private readonly int _sampleCount;
    private readonly StatisticalTest _test;

    protected AccuracyTest(Options options)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(options.SampleCount, 1, nameof(options.SampleCount));
        ArgumentOutOfRangeException.ThrowIfLessThan(options.AcceptableDeviationMilliseconds, 1, nameof(options.AcceptableDeviationMilliseconds));
        ArgumentOutOfRangeException.ThrowIfLessThan(options.ExpectedElapsedTimeMilliseconds, 1, nameof(options.ExpectedElapsedTimeMilliseconds));

        _sampleCount = options.SampleCount;
        _acceptableDeviationMilliseconds = options.AcceptableDeviationMilliseconds;
        _expectedElapsedTimeMilliseconds = options.ExpectedElapsedTimeMilliseconds;

        _test = new StatisticalTest(MeasureElapsedTime, _sampleCount);
        _test.SampleGenerated += (_, totalSamples) =>
        {
            Console.WriteLine($"Generated sample {totalSamples} of {_sampleCount} ({totalSamples / (double)_sampleCount:P2})");
        };
    }

    public double DeviationMilliseconds => _test.SampleMean - _expectedElapsedTimeMilliseconds;
    public bool IsSuccess => Math.Abs(DeviationMilliseconds) < _acceptableDeviationMilliseconds;
    public abstract string Title { get; }
    public void Execute()
    {
        _test.Execute();
        PrintTestSummary();
    }

    protected abstract double MeasureElapsedTime();

    private void PrintTestSummary()
    {
        Console.WriteLine($"\nMean: {_test.SampleMean:F2} ms");
        Console.WriteLine($"Standard deviation: {_test.SampleStandardDeviation:F2} ms ({_test.RelativeSampleStandardDeviation:P2})");
        Console.WriteLine();

        var acceptableDeviationSecondsPerHour = _acceptableDeviationMilliseconds / _expectedElapsedTimeMilliseconds * SecondsInAnHour;
        Console.WriteLine($"Acceptable deviation: {_acceptableDeviationMilliseconds:F2} ms ({acceptableDeviationSecondsPerHour:F2} s per hour)");
        Console.WriteLine($"Expected elapsed time: {_expectedElapsedTimeMilliseconds:F2} ms");
        Console.WriteLine();

        var actualDeviationSecondsPerHour = DeviationMilliseconds / _expectedElapsedTimeMilliseconds * SecondsInAnHour;
        var relativeDeviation = DeviationMilliseconds / _expectedElapsedTimeMilliseconds;

        Console.WriteLine($"Actual deviation: {DeviationMilliseconds:F2} ms ({relativeDeviation:P2}, {actualDeviationSecondsPerHour:F2} s per hour)");

        if (IsSuccess)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Test passed!");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Test failed!");
            Console.ResetColor();
        }
    }

    public struct Options()
    {
        public int AcceptableDeviationMilliseconds { get; set; } = 1;
        public int ExpectedElapsedTimeMilliseconds { get; set; } = 1000;
        public int SampleCount { get; set; } = 10;
    }
}
