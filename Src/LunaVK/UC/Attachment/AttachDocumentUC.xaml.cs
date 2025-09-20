using LunaVK.Core.DataObjects;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using LunaVK.Core.Utils;

namespace LunaVK.UC.Attachment
{
    public sealed partial class AttachDocumentUC : UserControl, ThumbnailsLayoutHelper.IThumbnailSupport
    {
        public AttachDocumentUC()
        {
            this.InitializeComponent();
        }

        public AttachDocumentUC(VKDocument doc) : this()
        {
            this.Data = doc;
        }

        public AttachDocumentUC(VKDocument doc, bool param) : this(doc)
        {
            // param can be used to change UI; ignored in this stub
        }

        public VKDocument Data
        {
            get { return (VKDocument)GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        public static readonly DependencyProperty DataProperty = DependencyProperty.Register("Data", typeof(VKDocument), typeof(AttachDocumentUC), new PropertyMetadata(null));

        public bool IsCompact
        {
            get { return (bool)GetValue(IsCompactProperty); }
            set { SetValue(IsCompactProperty, value); }
        }

        public static readonly DependencyProperty IsCompactProperty = DependencyProperty.Register("IsCompact", typeof(bool), typeof(AttachDocumentUC), new PropertyMetadata(false));

        public event TappedEventHandler OnTap;

        private void Root_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.OnTap?.Invoke(this, e);
        }

        // IThumbnailSupport implementation
        public ThumbnailsLayoutHelper.ThumbnailSize ThumbnailSize { get; set; }

        double ThumbnailsLayoutHelper.IThumbnailSupport.Width
        {
            get
            {
                if (this.Data == null)
                    return 100;
                if (this.Data.preview != null && this.Data.preview.photo != null && this.Data.preview.photo.sizes != null && this.Data.preview.photo.sizes.Count > 0)
                    return this.Data.preview.photo.sizes[0].width;
                return this.Data.size > 0 ? this.Data.size : 100;
            }
        }

        double ThumbnailsLayoutHelper.IThumbnailSupport.Height
        {
            get
            {
                if (this.Data == null)
                    return 100;
                if (this.Data.preview != null && this.Data.preview.photo != null && this.Data.preview.photo.sizes != null && this.Data.preview.photo.sizes.Count > 0)
                    return this.Data.preview.photo.sizes[0].height;
                return 100;
            }
        }

        public string ThumbnailSource
        {
            get
            {
                if (this.Data != null && this.Data.preview != null && this.Data.preview.photo != null && this.Data.preview.photo.sizes != null && this.Data.preview.photo.sizes.Count > 0)
                    return this.Data.preview.photo.sizes[0].src;
                if (this.Data != null)
                    return this.Data.url ?? string.Empty;
                return string.Empty;
            }
        }

        public double GetRatio()
        {
            var w = ((ThumbnailsLayoutHelper.IThumbnailSupport)this).Width;
            var h = ((ThumbnailsLayoutHelper.IThumbnailSupport)this).Height;
            if (h == 0)
                return 1.0;
            return Math.Max(0.1, w / h);
        }
    }
}
