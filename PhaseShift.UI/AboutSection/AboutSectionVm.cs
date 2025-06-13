using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PhaseShift.UI.AboutSection;
internal partial class AboutSectionVm : ObservableObject
{
    public static string AppName => AppConstants.AppName;
    public static string AppVersion => AppConstants.AppVersion;
    public static string AppDescription 
        => "A stylish productivity timer app. Designed to keep your focus in phase."; // must also be changed in csproj
    public static string Author => "Thomas Wening"; // must also be changed in csproj
    public static string AuthorEmail => "thomaswening94@gmail.com";
    public static string AuthorEmailUri => $"mailto:{AuthorEmail}";
    public static string AuthorWebsite => "https://github.com/thomaswening";
    public static string AuthorLinkedIn => "https://www.linkedin.com/in/thomas-wening-a96aa51b8/";
    public static string License => "GNU General Public License v3.0"; // must also be changed in csproj
    public static string Acknowledgements => string.Join("\n", _dependencies);

    private static readonly List<string> _dependencies =
    [
        "Microsoft.NET.Test.Sdk v17.10.0",
        "NSubstitute v5.1.0",
        "NUnit v4.1.0",
        "NUnit3TestAdapter v4.5.0",
        "CommunityToolkit.Mvvm v8.4.0",
        "Material.Icons.WPF v2.1.10",
        "MaterialDesignThemes v5.1.0"
    ];

    [RelayCommand]
    private static void OpenAuthorEmail()
    {
        LaunchLink(AuthorEmailUri);
    }

    [RelayCommand]
    private static void OpenAuthorGitHub()
    {
        LaunchLink(AuthorWebsite);
    }

    [RelayCommand]
    private static void OpenAuthorLinkedInProfile()
    {
        LaunchLink(AuthorLinkedIn);
    }

    private static void LaunchLink(string uri)
    {
        Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
    }
}
