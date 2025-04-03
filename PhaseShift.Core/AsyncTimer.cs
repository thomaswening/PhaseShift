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
    private CancellationTokenSource _cancellationTokenSource = new();
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
        _cancellationTokenSource.Cancel();
    }

    public void Start()
    {
        if (IsRunning)
        {
            return;
        }

        try
        {
            Task.Run(() => StartAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            ResetCancellationToken();
        }
    }

    public void Stop()
    {
        _stopwatch.Stop();
        _cancellationTokenSource.Cancel();
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
            ResetCancellationToken();
            return;
        }

        _timerCompletedCallback();
        _stopwatch.Reset();
    }

    private void ResetCancellationToken()
    {
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();
    }
}

