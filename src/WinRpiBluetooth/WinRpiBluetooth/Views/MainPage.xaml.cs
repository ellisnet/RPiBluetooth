using Windows.UI.Xaml.Controls;
using SkiaSharp;
using SkiaSharp.Views.UWP;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace WinRpiBluetooth.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void Indicator_OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            // the the canvas and properties
            var canvas = e.Surface.Canvas;

            // get the screen density for scaling
            var scale = (float)(e.Info.Width / Indicator.Width);

            // handle the device screen density
            canvas.Scale(scale);

            // make sure the canvas is blank
            canvas.Clear(SKColors.BlueViolet);
        }
    }
}
