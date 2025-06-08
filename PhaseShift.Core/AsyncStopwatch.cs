using System.Diagnostics;

namespace PhaseShift.Core;

/// <summary>
/// Represents an asynchronous stopwatch that executes a specified asynchronous callback at regular intervals.
/// </summary>
/// <param name="tickHandler">The asynchronous callback to be executed at each interval.</param>
/// <param name="intervalMilliseconds">The interval in milliseconds at which the callback is executed. Default is 10 milliseconds.</param>
public class AsyncStopwatch(Action<TimeSpan> tickHandler, int intervalMilliseconds = 10)
{
    private readonly int _intervalMilliseconds = intervalMilliseconds;
    private readonly object _stateLock = new();
    private readonly Stopwatch _stopwatch = new();
    private readonly Action<TimeSpan> _tickHandler = tickHandler;
    private CancellationTokenSource _cancellationTokenSource = new();
    private Task? _tickLoopTask;

    public TimeSpan ElapsedTime => _stopwatch.Elapsed;

    public bool IsRunning { get; private set; }

    /// <summary>
    /// Starts the stopwatch and begins executing the callback at the specified intervals.
    /// </summary>
    public void Start(CancellationToken externalToken = default)
    {
        lock (_stateLock)
        {
            if (IsRunning)
            {
                return;
            }

            IsRunning = true;

            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token, externalToken);

            _stopwatch.Start();
            _tickLoopTask = ExecuteTickLoopAsync(linkedCts.Token);
        }
    }

    /// <summary>
    /// Stops the stopwatch and cancels the execution of the callback.
    /// </summary>
    public void Stop(CancellationToken externalToken = default)
    {
        Task? oldTickLoopTask;
        CancellationTokenSource? oldCts;

        lock (_stateLock)
        {
            if (!IsRunning)
            {
                return;
            }

            _stopwatch.Stop();

            oldTickLoopTask = _tickLoopTask;
            oldCts = _cancellationTokenSource;

            _cancellationTokenSource = new CancellationTokenSource();
            _tickLoopTask = null;
            IsRunning = false;
        }

        CancelAndCleanupOldTickLoop(oldTickLoopTask, oldCts, externalToken);
    }

    /// <summary>
    /// Resets the stopwatch and invokes the callback with the reset elapsed time.
    /// </summary>
    public void Reset(CancellationToken externalToken = default)
    {
        Task? oldTickLoopTask;
        CancellationTokenSource? oldCts;

        lock (_stateLock)
        {
            _stopwatch.Reset();
            _tickHandler(_stopwatch.Elapsed);

            if (!IsRunning)
            {
                // Nothing to cancel
                _tickLoopTask = null;
                return;
            }

            oldCts = _cancellationTokenSource;
            oldTickLoopTask = _tickLoopTask;

            _tickLoopTask = null;
            _cancellationTokenSource = new CancellationTokenSource();
            IsRunning = false;
        }

        CancelAndCleanupOldTickLoop(oldTickLoopTask, oldCts, externalToken);
    }

    private static void CancelAndCleanupOldTickLoop(Task? oldTickLoopTask, CancellationTokenSource? oldCts, CancellationToken externalToken)
    {
        oldCts?.Cancel();

        try
        {
            if (oldTickLoopTask != null &&
                oldTickLoopTask.Status != TaskStatus.Created &&
                oldTickLoopTask.Status != TaskStatus.WaitingForActivation)
            {
                oldTickLoopTask.Wait(externalToken);
            }
        }
        catch (OperationCanceledException)
        {
            // cancelled externally
        }
        finally
        {
            oldCts?.Dispose();
        }
    }

    private async Task ExecuteTickLoopAsync(CancellationToken cancelToken)
    {
        while (true)
        {
            try
            {
                await Task.Run(() => _tickHandler(_stopwatch.Elapsed), CancellationToken.None).ConfigureAwait(false);
                await Task.Delay(_intervalMilliseconds, cancelToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}