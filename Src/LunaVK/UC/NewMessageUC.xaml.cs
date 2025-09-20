using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using LunaVK.Core.DataObjects;
using LunaVK.Core.Library;
using LunaVK.UC;

namespace LunaVK.UC
{
    public sealed partial class NewMessageUC : UserControl
    {
        public NewMessageUC()
        {
            this.InitializeComponent();
        }

        // Public API expected by callers
        public bool IsVoiceMessageButtonEnabled { get; set; }

        public Action OnSendTap { get; set; }
        public Action<string, string> OnSendPayloadTap { get; set; }
        public Action<FrameworkElement> OnAddAttachTap { get; set; }
        public Action<IOutboundAttachment> OnImageDeleteTap { get; set; }

        // Reply name tapped callback
        public Action ReplyNameTapped { get; set; }

        // Triggered when a sticker is tapped. Handlers expect signature (object, VKSticker)
        public event Action<object, VKSticker> StickerTapped;

        // Event when opened/closed panel state changes; sender, height
        public event Action<object, double> IsOpenedChanged;

        // Expose named controls as properties for code that sets bindings/reads text
        public ListView ItemsControlAttachments => this.itemsControlAttachments;
        public TextBox TextBoxNewComment => this.textBoxPost;
        public AudioRecorderUC AudioRecorder => this.ucAudioRecorder;
        public ListView MentionPicker => this.mentionPicker;

        public string ReplyToUserName
        {
            get => this.textBlockTitle?.Text ?? string.Empty;
            set
            {
                if (this.textBlockTitle != null)
                    this.textBlockTitle.Text = value;
            }
        }

        public bool IsFromGroupChecked
        {
            get => this.checkBoxAsCommunity != null && (this.checkBoxAsCommunity.IsChecked ?? false);
            set
            {
                if (this.checkBoxAsCommunity != null)
                    this.checkBoxAsCommunity.IsChecked = value;
            }
        }

        // Mode enum and property
        public enum Mode
        {
            NewMessageEmpty,
            EditMessage
        }

        public Mode ControlMode { get; set; }

        // Some other methods used by callers - provide safe no-op implementations
        public void ActivateSendButton(bool enable)
        {
            // Update visual state if needed. Keep no-op to be safe.
        }

        public void SetAdminLevel(int level)
        {
            // No-op fallback
        }

        public void UpdateVisibilityState()
        {
            // No-op fallback
        }

        public void UpdateVoiceMessageAvailability()
        {
            // No-op fallback
        }

        // HidePanel expected to return bool in some callers
        public bool HidePanel()
        {
            try
            {
                if (this.HidingMoveSpline != null)
                {
                    this.HidingMoveSpline.Value = 250;
                    this.MoveMiddleOnHiding?.Begin();
                }
            }
            catch { }
            return true;
        }

        // ClosePanel used in other places
        public void ClosePanel()
        {
            HidePanel();
        }

        // Internal helper to raise sticker event from XAML handlers
        private void RaiseStickerTapped(VKSticker sticker)
        {
            try
            {
                StickerTapped?.Invoke(this, sticker);
            }
            catch { }
        }

        // Internal helper to raise IsOpenedChanged
        private void RaiseIsOpenedChanged(double height)
        {
            try
            {
                IsOpenedChanged?.Invoke(this, height);
            }
            catch { }
        }
    }
}