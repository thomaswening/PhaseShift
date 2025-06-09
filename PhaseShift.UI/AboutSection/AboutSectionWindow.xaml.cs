using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace PhaseShift.UI.AboutSection;
/// <summary>
/// Interaction logic for AboutSectionWindow.xaml
/// </summary>
public partial class AboutSectionWindow : Window
{
    public AboutSectionWindow()
    {
        InitializeComponent();
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        if (e.Uri.Scheme == "mailto")
        {
            // Open the default email client with the provided email address
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        }
        else
        {
            // Open the default web browser for other links
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        }
        e.Handled = true;
    }
}
