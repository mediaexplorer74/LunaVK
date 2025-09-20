using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace LunaVK
{
    public sealed partial class AboutPage : PageBase
    {
        public AboutPage()
        {
            this.InitializeComponent();
        }

        private void TextBlock_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Minimal handler: mark event handled. Real implementation could open Store rating.
            e.Handled = true;
        }
    }
}
