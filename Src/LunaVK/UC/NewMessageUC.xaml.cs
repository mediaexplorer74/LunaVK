using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using LunaVK.Core.DataObjects;
using LunaVK.Core.Library;
using LunaVK.Core.Utils;
using LunaVK.UC;
using Windows.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Core;
using System.Collections;
using System.Collections.ObjectModel;

namespace LunaVK.UC
{
    public sealed partial class NewMessageUC : UserControl
    {
        // Track whether send is enabled
        private bool _sendEnabled = true;

        // New callback for voice message send events
        public Action<AudioRecorderUC.VoiceMessageSentEvent> OnVoiceMessageSent { get; set; }

        public NewMessageUC()
        {
            this.InitializeComponent();

            // Hook audio recorder if present
            try
            {
                if (this.AudioRecorder != null)
                    this.AudioRecorder.RecordDone += AudioRecorder_RecordDone;
            }
            catch { }

            // Try to set initial send button state
            try { ActivateSendButton(false); } catch { }

            // Hook send button if exists
            try { EnsureSendButtonHooked(); } catch { }
        }

        private void AudioRecorder_RecordDone(object sender, AudioRecorderUC.VoiceMessageSentEvent e)
        {
            try
            {
                // Try to add new outbound voice attachment to bound attachments collection (if any)
                try
                {
                    var itemsSource = this.ItemsControlAttachments?.ItemsSource;
                    if (itemsSource is ObservableCollection<IOutboundAttachment> obs)
                    {
                        var voice = new OutboundVoiceMessageAttachment(e.File, e.Duration, e.Waveform);
                        // mark as uploading and start upload in background
                        try { voice.UploadState = OutboundAttachmentUploadState.Uploading; } catch { }
                        obs.Add(voice);
                        try
                        {
                            voice.Upload(() => { /* completion callback: nothing special here */ }, (p) => { /* progress ignored */ });
                        }
                        catch { }

                        // enable send
                        ActivateSendButton(true);
                    }
                    else if (itemsSource is IList list)
                    {
                        try
                        {
                            var voice = new OutboundVoiceMessageAttachment(e.File, e.Duration, e.Waveform);
                            try { voice.UploadState = OutboundAttachmentUploadState.Uploading; } catch { }
                            list.Add(voice);
                            try { voice.Upload(() => { }, (p) => { }); } catch { }
                            ActivateSendButton(true);
                        }
                        catch { }
                    }
                }
                catch { }

                // Raise external callback so page/viewmodel can also handle upload/send if needed
                this.OnVoiceMessageSent?.Invoke(e);

                // Close recorder UI if any
                try { this.AudioRecorder.IsOpened = false; } catch { }
            }
            catch { }
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
            try
            {
                _sendEnabled = enable;
                // If there is a named send button in XAML try to toggle it
                try
                {
                    var btn = this.FindName("btnSend") as Button ?? this.FindName("sendButton") as Button;
                    if (btn != null)
                    {
                        btn.IsEnabled = enable;
                    }
                }
                catch { }
            }
            catch { }
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

        // Event handlers referenced from XAML - minimal implementations
        private void SpriteListControl_ItemClick(object sender, RoutedEventArgs e)
        {
            VKSticker sticker = null;
            try
            {
                var fe = e.OriginalSource as FrameworkElement;
                if (fe?.DataContext is VKSticker s)
                    sticker = s;
                else if ((sender as FrameworkElement)?.DataContext is VKSticker s2)
                    sticker = s2;
            }
            catch { }

            if (sticker != null)
            {
                try { this.StickerTapped?.Invoke(this, sticker); }
                catch { }
            }
        }

        private void SpriteListControl_EmojiClick(object sender, RoutedEventArgs e)
        {
            VKSticker sticker = null;
            try
            {
                var fe = e.OriginalSource as FrameworkElement;
                if (fe?.DataContext is VKSticker s)
                    sticker = s;
                else if ((sender as FrameworkElement)?.DataContext is VKSticker s2)
                    sticker = s2;
            }
            catch { }

            if (sticker != null)
            {
                try { this.StickerTapped?.Invoke(this, sticker); }
                catch { }
            }
        }

        private void ListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // No-op for layout changes used in original app
        }

        private void BotKeyboardButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                var vm = (sender as FrameworkElement)?.DataContext as dynamic;
                string label = null, payload = null;
                try { label = vm?.Label; } catch { }
                try { payload = vm?.Payload; } catch { }
                this.OnSendPayloadTap?.Invoke(label ?? string.Empty, payload ?? string.Empty);
            }
            catch { }
        }

        private void AddAttachTapped(object sender, TappedRoutedEventArgs e)
        {
            try { this.OnAddAttachTap?.Invoke(sender as FrameworkElement); }
            catch { }
        }

        private void textBoxPost_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            try
            {
                // Send on Enter (without Shift). Allow Shift+Enter for newline.
                if (e.Key == VirtualKey.Enter)
                {
                    var shift = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift);
                    bool shiftDown = (shift & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
                    if (!shiftDown)
                    {
                        e.Handled = true;
                        try { this.OnSendTap?.Invoke(); }
                        catch { }
                        // Optionally clear text after send
                        try { this.textBoxPost.Text = string.Empty; } catch { }
                        // Update send button state
                        try { ActivateSendButton(false); } catch { }
                    }
                }
            }
            catch { }
        }

        private void TextBoxPost_TextChanging(object sender, TextBoxTextChangingEventArgs e)
        {
            try
            {
                string txt = this.textBoxPost?.Text ?? string.Empty;
                bool shouldEnable = !string.IsNullOrWhiteSpace(txt) || (this.ItemsControlAttachments?.Items?.Count > 0);
                ActivateSendButton(shouldEnable);

                // Show mention picker on '@'
                try
                {
                    if (!string.IsNullOrEmpty(txt) && txt.EndsWith("@"))
                    {
                        if (this.MentionPicker != null)
                            this.MentionPicker.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        if (this.MentionPicker != null)
                            this.MentionPicker.Visibility = Visibility.Collapsed;
                    }
                }
                catch { }
            }
            catch { }
        }

        private void EnsureSendButtonHooked()
        {
            try
            {
                var btn = this.FindName("btnSend") as Button ?? this.FindName("sendButton") as Button;
                if (btn != null)
                {
                    btn.Click -= Btn_Click_Internal;
                    btn.Click += Btn_Click_Internal;
                }
            }
            catch { }
        }

        private void Btn_Click_Internal(object sender, RoutedEventArgs e)
        {
            try { TriggerSend(); }
            catch { }
        }

        public void TriggerSend()
        {
            try
            {
                this.OnSendTap?.Invoke();
            }
            catch { }
            try { this.textBoxPost.Text = string.Empty; } catch { }
            try { ActivateSendButton(false); } catch { }
        }

        public void OpenRecorder()
        {
            try
            {
                if (this.AudioRecorder != null)
                    this.AudioRecorder.IsOpened = true;
            }
            catch { }
        }

        public void CloseRecorder()
        {
            try
            {
                if (this.AudioRecorder != null)
                    this.AudioRecorder.IsOpened = false;
            }
            catch { }
        }

        private void _borderVoice_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            try
            {
                // Toggle recorder
                if (this.AudioRecorder != null)
                {
                    this.AudioRecorder.IsOpened = !this.AudioRecorder.IsOpened;
                }
            }
            catch { }
            e.Handled = true;
        }

        private void _borderVoice_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void _borderVoice_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            // No-op stub
        }

        private void _borderVoice_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            // No-op stub
        }

        private void BotKeyboard_Tapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void Smiles_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Toggle sticker/emoji panel in real app. Minimal: do nothing.
        }

        private void Attachment_Loaded(object sender, RoutedEventArgs e)
        {
            // Placeholder for animations when attachment items load
        }

        private void Image_Tap(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
            try
            {
                dynamic vm = (sender as FrameworkElement)?.DataContext;
                if (vm != null)
                {
                    try { vm.Upload(null); } catch { }
                }
            }
            catch { }
        }

        private void Delete_Tapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
            try
            {
                var attachment = (sender as FrameworkElement)?.DataContext as IOutboundAttachment;
                if (attachment != null)
                    this.OnImageDeleteTap?.Invoke(attachment);
            }
            catch { }
        }

        private void checkBoxAsCommunity_Checked(object sender, RoutedEventArgs e)
        {
            // Minimal stub: no-op
        }

        private void ucReplyUser_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try { this.ReplyNameTapped?.Invoke(); }
            catch { }
        }

        private void Mention_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Minimal: treat as selection of mention. No-op.
            e.Handled = true;
        }
    }
}