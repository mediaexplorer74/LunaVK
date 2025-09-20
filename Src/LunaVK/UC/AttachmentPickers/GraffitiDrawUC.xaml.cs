using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml;

namespace LunaVK.UC.AttachmentPickers
{
    public sealed partial class GraffitiDrawUC : UserControl
    {
        public Action<RenderTargetBitmap, string> SendAction;

        public GraffitiDrawUC()
        {
            this.InitializeComponent();
        }
    }
}
