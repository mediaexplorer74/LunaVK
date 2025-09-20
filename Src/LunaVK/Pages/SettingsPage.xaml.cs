using System;
using System.Linq;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;
using LunaVK.Core;

namespace LunaVK.Pages
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();
            InitializeLanguageSelection();
        }

        private void InitializeLanguageSelection()
        {
            string saved = Settings.SelectedLanguageCode;
            string primary = Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride;
            string current = saved ?? primary ?? Windows.Globalization.ApplicationLanguages.Languages.FirstOrDefault();

            foreach (ComboBoxItem item in LanguageComboBox.Items)
            {
                var tag = item.Tag as string;
                if (string.IsNullOrEmpty(tag))
                    continue;
                if (string.Equals(tag, current, StringComparison.OrdinalIgnoreCase) ||
                    (current != null && current.StartsWith(tag.Split('-')[0], StringComparison.OrdinalIgnoreCase)))
                {
                    LanguageComboBox.SelectedItem = item;
                    return;
                }
            }

            LanguageComboBox.SelectedIndex = 0;
        }

        private async void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(LanguageComboBox.SelectedItem is ComboBoxItem item))
                return;

            var tag = item.Tag as string;
            if (string.IsNullOrEmpty(tag))
                return;

            // Persist via app Settings (SettingsHelper)
            Settings.SelectedLanguageCode = tag;

            // Set PrimaryLanguageOverride for resource lookup (requires app restart to fully apply)
            Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = tag;

            var dialog = new MessageDialog($"Language changed to {item.Content}. The app must be restarted to apply the change. Restart now?");
            dialog.Commands.Add(new UICommand("Restart", async (cmd) =>
            {
                await Windows.ApplicationModel.Core.CoreApplication.RequestRestartAsync(string.Empty);
            }));
            dialog.Commands.Add(new UICommand("Later"));
            await dialog.ShowAsync();
        }
    }
}