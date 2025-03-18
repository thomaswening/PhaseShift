
using PhaseShift.Core.AccuracyTestTool.Tests;
using PhaseShift.UI.StopwatchFeature;

namespace PhaseShift.Core.AccuracyTestTool;

internal class Program
{
    private const int AcceptableDeviationMilliseconds = 10;
    private const int ExpectedElapsedTimeMilliseconds = 10_000;
    private const int SampleCount = 10;

    static void Main(string[] args)
    {
        var options = new AccuracyTest.Options()
        {
            SampleCount = SampleCount,
            AcceptableDeviationMilliseconds = AcceptableDeviationMilliseconds,
            ExpectedElapsedTimeMilliseconds = ExpectedElapsedTimeMilliseconds
        };

        var tests = new List<AccuracyTest>
        {
            new AsyncStopwatchTest(options),
            new StopwatchVmTest(options),
        };

        foreach (var test in tests)
        {
            Console.WriteLine($"Starting {test.Title} accuracy test...\n");
            test.Execute();
            Console.WriteLine();
        }

        PrintSummary(tests);
    }

    private static void PrintSummary(List<AccuracyTest> tests)
    {
        Console.WriteLine("Test Summary:\n");
        foreach (var test in tests)
        {
            if (test.IsSuccess)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"{test.Title} Test: Succeeded");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{test.Title} Test: Failed");
            }
            Console.ResetColor();
        }
    }
}
