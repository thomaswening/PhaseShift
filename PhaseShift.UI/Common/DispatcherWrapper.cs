﻿using System.Windows;
using System.Windows.Threading;

namespace PhaseShift.UI.Common;

/// <summary>
/// Provides an abstraction for dispatching actions on the UI thread.
/// Meant to be used in view models to allow easier unit testing.
/// </summary>
public interface IDispatcher
{
    Task InvokeAsync(Action action);
    void Invoke(Action action);
}

/// <summary>
/// Implementation of <see cref="IDispatcher"/> as a wrapper around <see cref="Dispatcher"/> for WPF.
/// </summary>
public class DispatcherWrapper(Dispatcher dispatcher) : IDispatcher
{
    private readonly Dispatcher _dispatcher = dispatcher;

    public DispatcherWrapper() : this(Application.Current.Dispatcher) { }

    /// <inheritdoc cref="Dispatcher.InvokeAsync(Action)"/>
    public Task InvokeAsync(Action action)
    {
        return _dispatcher.InvokeAsync(action).Task;
    }

    /// <inheritdoc cref="Dispatcher.Invoke(Action)"/>
    public void Invoke(Action action)
    {
        _dispatcher.Invoke(action);
    }
}
