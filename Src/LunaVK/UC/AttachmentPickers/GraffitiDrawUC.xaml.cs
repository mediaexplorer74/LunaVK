using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace LunaVK.UC.AttachmentPickers
{
    public sealed partial class GraffitiDrawUC : UserControl
    {
        public Action<RenderTargetBitmap, string> SendAction;

        public GraffitiDrawUC()
        {
            this.InitializeComponent();
        }

        private void drawCanvas_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e) { }
        private void drawCanvas_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e) { }
        private void drawCanvas_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e) { }
        private void drawCanvas_Tapped(object sender, TappedRoutedEventArgs e) { }

        private void Clear_Tapped(object sender, TappedRoutedEventArgs e) { }
        private void borderThickness_Tapped(object sender, TappedRoutedEventArgs e) { }
        private void Thickness_Tapped(object sender, TappedRoutedEventArgs e) { }
        private void Color_PointerReleased(object sender, PointerRoutedEventArgs e) { }
        private void Action_Tapped(object sender, TappedRoutedEventArgs e) { }
        private void ApplyBack_Tapped(object sender, TappedRoutedEventArgs e) { }
        private void Undo_Tapped(object sender, TappedRoutedEventArgs e) { }
    }
}
