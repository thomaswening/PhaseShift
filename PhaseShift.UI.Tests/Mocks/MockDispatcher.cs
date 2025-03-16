using PhaseShift.UI.Common;

namespace PhaseShift.UI.Tests.Mocks;
internal class MockDispatcher : IDispatcher
{
    public void Invoke(Action action)
    {
        action();
    }

    public Task InvokeAsync(Action action)
    {
        action();
        return Task.CompletedTask;
    }
}
