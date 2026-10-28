using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ComOfficeKit;
using Microsoft.Win32;

namespace SampleWpfPreview;

// Shows a document page by page. The page is rendered at the monitor's own scale, so it is sharp at
// 100 %, 150 % or 200 %, and handed to WPF as Pbgra32: the format PageImage already holds.
// The licence key comes from the COMOFFICEKIT_LICENSE_KEY environment variable.
public partial class MainWindow : Window
{
    private Document? document;
    private int page;

    public MainWindow()
    {
        InitializeComponent();
        Closed += (_, _) => document?.Dispose();
        var key = Environment.GetEnvironmentVariable("COMOFFICEKIT_LICENSE_KEY");
        try
        {
            if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException("Set COMOFFICEKIT_LICENSE_KEY to your licence key.");
            License.Activate(key);
        }
        catch (Exception e) when (e is InvalidOperationException or ComOfficeKitException)
        {
            OpenButton.IsEnabled = false;
            StatusLabel.Text = e.Message;
        }
    }

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Documents|*.docx;*.xlsx;*.xlsm;*.pptx;*.csv;*.tsv",
        };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            var opened = Document.Open(dialog.FileName);
            document?.Dispose();
            document = opened;
            page = 0;
            StatusLabel.Text = $"{document.Kind}: {System.IO.Path.GetFileName(dialog.FileName)}";
            ShowPage();
        }
        catch (ComOfficeKitException error)
        {
            StatusLabel.Text = $"{error.Kind}: {error.Message}";
        }
    }

    private void Previous_Click(object sender, RoutedEventArgs e) { page--; ShowPage(); }

    private void Next_Click(object sender, RoutedEventArgs e) { page++; ShowPage(); }

    private void ShowPage()
    {
        if (document is null) return;
        int count = document.PageCount;
        if (count == 0)
        {
            PageView.Source = null;
            PageLabel.Text = "no pages";
            PreviousButton.IsEnabled = NextButton.IsEnabled = false;
            return;
        }
        page = Math.Clamp(page, 0, count - 1);

        // 96 dpi is one device-independent pixel per pixel; the monitor's scale multiplies it.
        var scale = VisualTreeHelper.GetDpi(this).DpiScaleX;
        var image = document.RenderPage(page, dpi: Math.Clamp(96 * scale, 24, 600));
        var bitmap = BitmapSource.Create(image.Width, image.Height, image.Dpi, image.Dpi,
                                         PixelFormats.Pbgra32, null, image.Pixels.ToArray(), image.Stride);
        bitmap.Freeze();
        PageView.Source = bitmap;

        PageLabel.Text = $"{page + 1} / {count}";
        PreviousButton.IsEnabled = page > 0;
        NextButton.IsEnabled = page < count - 1;
    }
}
