﻿namespace PhaseShift.Core;

/// <summary>
/// Represents an asynchronous timer that executes a specified callback when the countdown finishes
/// and another tick callback at regular intervals.
/// </summary>
public class AsyncTimer
{
    private readonly Action _timerCompletedCallback;
    private readonly int _intervalMilliseconds;
    private readonly AsyncStopwatch _stopwatch;
    private readonly object _stateLock = new();

    private CancellationTokenSource _cancellationTokenSource = new();
    private TimeSpan _duration;
    private Task? _timerTask;

    public AsyncTimer(Action timerCompletedCallback, Action<TimeSpan> tickCallback, TimeSpan duration, int intervalMilliseconds = 10)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _stopwatch = new AsyncStopwatch(tickCallback, intervalMilliseconds);
        _timerCompletedCallback = timerCompletedCallback;
        _intervalMilliseconds = intervalMilliseconds;
        Duration = duration;
    }

    public TimeSpan Duration
    {
        get
        {
            lock (_stateLock) return _duration;
        }
        set
        {
            lock (_stateLock) SetDuration(value);
        }
    }

    public TimeSpan ElapsedTime => _stopwatch.ElapsedTime;
    public bool IsRunning => _stopwatch.IsRunning;
    public double Progress => ElapsedTime.TotalSeconds / Duration.TotalSeconds;

    public TimeSpan RemainingTime => GetRemainingTime();

    public void Start(CancellationToken externalToken = default)
    {
        lock (_stateLock)
        {
            if (IsRunning)
            {
                return;
            }

            // Link the external cancellation token with the internal CTS to ensure the timer
            // respects both internal and external cancellation requests.
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token, externalToken);
            _stopwatch.Start(linkedCts.Token);
            _timerTask = ExecuteTimerLoopAsync(linkedCts.Token);
        }
    }

    public void Stop(CancellationToken externalToken = default)
    {
        CancellationTokenSource? oldCts = null;
        Task? oldTimerTask = null;

        lock (_stateLock)
        {
            oldCts = _cancellationTokenSource;
            oldTimerTask = _timerTask;

            _cancellationTokenSource = new CancellationTokenSource();
        }

        CancelAndCleanupOldTimer(oldCts, oldTimerTask, externalToken);

        lock (_stateLock)
        {
            _stopwatch.Stop(externalToken);
        }
    }

    public void Reset(CancellationToken externalToken = default)
    {
        CancellationTokenSource? oldCts = null;
        Task? oldTimerTask = null;

        lock (_stateLock)
        {            

            oldCts = _cancellationTokenSource;
            oldTimerTask = _timerTask;

            _timerTask = null;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        CancelAndCleanupOldTimer(oldCts, oldTimerTask, externalToken);

        lock (_stateLock)
        {
            _stopwatch.Reset(externalToken);
        }
    }

    private static void CancelAndCleanupOldTimer(CancellationTokenSource? oldCts, Task? oldTimerTask, CancellationToken externalToken)
    {
        oldCts?.Cancel();

        try
        {
            if (oldTimerTask != null &&
                oldTimerTask.Status != TaskStatus.Created &&
                oldTimerTask.Status != TaskStatus.WaitingForActivation)
            {
                oldTimerTask.Wait(externalToken);
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

        oldCts?.Dispose();
    }

    private TimeSpan GetRemainingTime()
    {
        var remainingTime = Duration - _stopwatch.ElapsedTime;
        return remainingTime < TimeSpan.Zero ? TimeSpan.Zero : remainingTime;
    }

    private void SetDuration(TimeSpan value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, TimeSpan.Zero, nameof(Duration));

        if (IsRunning && value < _stopwatch.ElapsedTime)
        {
            Reset();
            _timerCompletedCallback();
        }

        _duration = value;
    }

    private async Task ExecuteTimerLoopAsync(CancellationToken cancelToken)
    {
        while (!cancelToken.IsCancellationRequested && _stopwatch.IsRunning && _stopwatch.ElapsedTime <= Duration)
        {
            try
            {
                await Task.Delay(_intervalMilliseconds, cancelToken);
            }
            catch (TaskCanceledException)
            {
                // Timer was stopped
                break;
            }
        }

        if (cancelToken.IsCancellationRequested)
        {
            return;
        }

        _timerCompletedCallback();
        _stopwatch.Reset(cancelToken);
    }
}

