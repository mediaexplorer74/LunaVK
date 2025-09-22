using LunaVK.Core;
using LunaVK.Core.Framework;
using LunaVK.Core.Network;
using LunaVK.Core.Utils;
using LunaVK.Framework;
using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Diagnostics;

namespace LunaVK.UC
{
    public sealed partial class GenericInfoUC : UserControl
    {
        private readonly DelayedExecutor _deHide;
        private const int DEFAULT_DELAY = 1000;

        public GenericInfoUC(int delayToHide = GenericInfoUC.DEFAULT_DELAY)
        {
            this.InitializeComponent();
            this._deHide = new DelayedExecutor(delayToHide);
        }

        public void ShowAndHideLater(string text, Grid elementToFadeout = null)
        {
            PopUpService ds = new PopUpService();
            this.textBlockInfo.Text = text;
            ds.BackgroundBrush = null;
            ds.Child = this;
            //ds.KeepAppBar = true;
            if (elementToFadeout != null)
                ds.OverlayGrid = elementToFadeout;
            ds.Show();

            this._deHide.AddToDelayedExecution(() => Execute.ExecuteOnUIThread(() => ds.Hide()));

        }

        public static void ShowBasedOnResult(string successString = "", VKError error = null)
        {
            if (error != null)
            {
                if (error.error_code == Core.Enums.VKErrors.None)
                {
                    if (string.IsNullOrWhiteSpace(successString))
                        return;
                    new GenericInfoUC().ShowAndHideLater(successString, null);
                }

                else
                {
                    int delayToHide = 3000;

                    if (error.error_code == Core.Enums.VKErrors.NoNetwork)
                        error.error_msg = LocalizedStrings.GetString("FailedToConnectError").Replace("\\r\\n", Environment.NewLine);

                    // If access denied, write last attempted URI to debug for diagnostics
                    try
                    {
                        if (error.error_code == Core.Enums.VKErrors.AccessDenied)
                        {
                            string lastUri = "<unknown>";
                            try { lastUri = LunaVK.Library.NavigatorImpl.Instance.LastAttemptedUri ?? "<null>"; } catch { }
                            Debug.WriteLine($"Access Denied while navigating. LastAttemptedUri={lastUri} ErrorMessage={error.error_msg}");
                        }
                    }
                    catch { }

                    new GenericInfoUC(delayToHide).ShowAndHideLater(error.error_msg, null);
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(successString))
                    return;
                new GenericInfoUC().ShowAndHideLater(successString, null);
            }
        }
    }
}
