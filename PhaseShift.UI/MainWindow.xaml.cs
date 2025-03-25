using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;

using Brush = System.Windows.Media.Brush;
using MaterialDesignThemes.Wpf;

using PhaseShift.UI.Common;
using PhaseShift.UI.Notifications;

namespace PhaseShift.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly NotificationService _notificationService;

    public MainWindow()
    {
        InitializeComponent();

        Title = AppConstants.AppName;

        var dispatcher = new DispatcherWrapper(System.Windows.Application.Current.Dispatcher);

        var packIcon = new PackIcon
        {
            Kind = PackIconKind.Timer,
        };

        var notifyIcon = new NotifyIcon
        {
            Icon = new Icon(IconFromPackIcon(packIcon), 40, 40),
            Visible = true,
            Text = AppConstants.AppName,
        };

        var notificationService = new NotificationService(dispatcher, AppConstants.AppName, notifyIcon);
        _notificationService = notificationService;
    }

    private static Icon IconFromPackIcon(PackIcon packIcon, Brush? foreground = null, Brush? background = null)
    {
        // Set default colors if not provided
        foreground ??= new SolidColorBrush(Colors.White);
        background ??= new SolidColorBrush(Colors.Transparent);

        // Apply colors to the PackIcon
        packIcon.Foreground = foreground;

        const int iconSize = 16;

        // Create a temporary container to properly render the PackIcon
        var grid = new Grid
        {
            Width = iconSize,
            Height = iconSize,
            Background = background
        };
        grid.Children.Add(packIcon);

        // Measure and arrange the container
        grid.Measure(new System.Windows.Size(iconSize, iconSize));
        grid.Arrange(new Rect(0, 0, iconSize, iconSize));

        // Render the grid into a bitmap
        var renderTarget = new RenderTargetBitmap(iconSize, iconSize, 96, 96, PixelFormats.Pbgra32);
        renderTarget.Render(grid);

        // Encode the bitmap as PNG
        var bitmapEncoder = new PngBitmapEncoder();
        bitmapEncoder.Frames.Add(BitmapFrame.Create(renderTarget));

        using var memoryStream = new MemoryStream();
        bitmapEncoder.Save(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);

        // Convert the bitmap to an Icon
        using var tempBitmap = new Bitmap(memoryStream);
        return System.Drawing.Icon.FromHandle(tempBitmap.GetHicon());
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowVm vm)
        {
            return;
        }

        vm.TimerCompleted += _notificationService.OnTimerCompleted;
    }
}
