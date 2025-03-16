using System.Diagnostics;

namespace PhaseShift.Core;

/// <summary>
/// Represents an asynchronous stopwatch that executes a specified asynchronous callback at regular intervals.
/// </summary>
/// <param name="tickHandler">The asynchronous callback to be executed at each interval.</param>
/// <param name="intervalMilliseconds">The interval in milliseconds at which the callback is executed. Default is 10 milliseconds.</param>
public class AsyncStopwatch(Action<TimeSpan> tickHandler, int intervalMilliseconds = 10)
{
    private readonly Action<TimeSpan> _tickHandler = tickHandler;
    private readonly int _intervalMilliseconds = intervalMilliseconds;
    private readonly Stopwatch _stopwatch = new();
    private CancellationTokenSource? _cancellationTokenSource;

    public TimeSpan ElapsedTime => _stopwatch.Elapsed;
    public bool IsRunning { get; private set; }

    /// <summary>
    /// Resets the stopwatch and invokes the callback with the reset elapsed time.
    /// </summary>
    public void Reset()
    {
        CancelAndDisposeToken();

        _stopwatch.Reset();
        _tickHandler(_stopwatch.Elapsed);

        IsRunning = false;
    }

    /// <summary>
    /// Starts the stopwatch and begins executing the callback at the specified intervals.
    /// </summary>
    public void Start()
    {
        if (IsRunning)
        {
            return;
        }

        _cancellationTokenSource = new CancellationTokenSource();

        IsRunning = true;
        Task.Run(() => StartAsync(_cancellationTokenSource.Token)).ConfigureAwait(false);
    }

    /// <summary>
    /// Stops the stopwatch and cancels the execution of the callback.
    /// </summary>
    public void Stop()
    {
        CancelAndDisposeToken();

        _stopwatch.Stop();
        IsRunning = false;
    }

    private void CancelAndDisposeToken()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }

    private async Task StartAsync(CancellationToken cancelToken)
    {
        _stopwatch.Start();

        while (!cancelToken.IsCancellationRequested)
        {
            // Don't await the task to prevent the stopwatch from being stopped
            // granted, the handler should execute faster than the tick interval!

            Task.Run(() => _tickHandler(_stopwatch.Elapsed), CancellationToken.None).ConfigureAwait(false);

            try
            {
                await Task.Delay(_intervalMilliseconds, cancelToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                // Stopwatch was stopped, do nothing
            }
        }
    }
}
