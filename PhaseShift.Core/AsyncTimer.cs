namespace PhaseShift.Core;

/// <summary>
/// Represents an asynchronous timer that executes a specified callback when the countdown finishes
/// and another tick callback at regular intervals.
/// </summary>
public class AsyncTimer
{
    private readonly Action _timerCompletedCallback;
    private readonly int _intervalMilliseconds;
    private readonly AsyncStopwatch _stopwatch;
    private CancellationTokenSource? _cancellationTokenSource;
    private TimeSpan _duration;

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
        get => _duration; 
        set => SetDuration(value); 
    }

    public TimeSpan ElapsedTime => _stopwatch.ElapsedTime;
    public bool IsRunning => _stopwatch.IsRunning;
    public double Progress => ElapsedTime.TotalSeconds / Duration.TotalSeconds;

    public TimeSpan RemainingTime => GetRemainingTime();

    public void Reset()
    {
        _stopwatch.Reset();
        CancelAndDisposeToken();
    }

    public void Start()
    {
        if (IsRunning)
        {
            return;
        }

        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        _cancellationTokenSource = new CancellationTokenSource();

        Task.Run(() => StartAsync(_cancellationTokenSource.Token));
    }

    public void Stop()
    {
        _stopwatch.Stop();
        CancelAndDisposeToken();
    }

    private void CancelAndDisposeToken()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
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
            Stop();
            _timerCompletedCallback();
        }

        _duration = value;
    }

    private async Task StartAsync(CancellationToken cancelToken)
    {
        _stopwatch.Start();

        while (_stopwatch.IsRunning && _stopwatch.ElapsedTime <= Duration)
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
        _stopwatch.Reset();
    }
}

