using CommunityToolkit.Mvvm.ComponentModel;

namespace PhaseShift.UI.Common;

/// <summary>
/// Represents a view model for a page in the main window.
/// </summary>
internal abstract class PageViewModel : ObservableObject
{
    public abstract string Title { get; }
}
