using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using LunaVK.Network;
using LunaVK.Core;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.Globalization;
using Windows.UI.Core;

namespace LunaVK
{
    public sealed partial class SettingsGeneralPage : PageBase
    {
        public SettingsGeneralPage()
        {
            this.InitializeComponent();
            base.Title = LocalizedStrings.GetString("SettingsGeneral");
            this.Loaded += SettingsGeneralPage_Loaded;

            this._switchProxy.IsChecked = Settings.UseProxy;
        }

        private void SettingsGeneralPage_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= SettingsGeneralPage_Loaded;
            try
            {
                var vm = new ViewModels.SettingsViewModel();

                // Ensure VM languages are initialized before binding to avoid timing issues
                var dummy = vm.LanguagesFiltered;

                base.DataContext = vm;

                // Fallback: ensure ComboBox has items in case binding didn't update yet
                try
                {
                    var combo = this.FindName("LanguageComboBox") as ComboBox;
                    if (combo != null)
                    {
                        var items = vm.LanguagesFiltered;
                        if (items != null && items.Count > 0)
                        {
                            combo.ItemsSource = items;
                            combo.SelectedValuePath = "Code";

                            // Safely select item asynchronously on UI dispatcher to avoid runtime crash
                            try
                            {
                                string code = vm.SelectedLanguageCode ?? string.Empty;
                                int selIdx = -1;
                                for (int i = 0; i < items.Count; i++)
                                {
                                    if (string.Equals(items[i].Code, code, StringComparison.OrdinalIgnoreCase))
                                    {
                                        selIdx = i;
                                        break;
                                    }
                                }

                                var ignored = combo.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                                {
                                    try
                                    {
                                        if (selIdx >= 0 && selIdx < combo.Items.Count)
                                            combo.SelectedIndex = selIdx;
                                        else if (combo.Items.Count > 0)
                                            combo.SelectedIndex = 0;
                                    }
                                    catch (Exception ex)
                                    {
                                        try { LunaVK.Core.Utils.Logger.Instance.Error("Combo selection async failed: " + ex.ToString()); } catch { }
                                    }
                                });
                            }
                            catch (Exception ex)
                            {
                                try { LunaVK.Core.Utils.Logger.Instance.Error("Combo selection setup failed: " + ex.ToString()); } catch { }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    try { LunaVK.Core.Utils.Logger.Instance.Error("SettingsGeneralPage_Loaded fallback: " + ex.ToString()); } catch { }
                }
            }
            catch (Exception ex)
            {
                try { LunaVK.Core.Utils.Logger.Instance.Error("SettingsGeneralPage_Loaded: " + ex.ToString()); } catch { }
            }
        }

        private ViewModels.SettingsViewModel VM
        {
            get { return base.DataContext as ViewModels.SettingsViewModel; }
        }

        private async void BorderDoc_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var diagFolder = new FolderPicker() { SuggestedStartLocation = PickerLocationId.Downloads };
            diagFolder.FileTypeFilter.Add("*");
            var outputFolder = await diagFolder.PickSingleFolderAsync();
            if (outputFolder == KnownFolders.DocumentsLibrary)
                outputFolder = KnownFolders.AppCaptures;
            if (outputFolder != null)
            {
                this.VM.SaveFolderDoc = outputFolder.Path;
            }
        }

        private async void BorderPhoto_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var diagFolder = new FolderPicker() { SuggestedStartLocation = PickerLocationId.PicturesLibrary };
            diagFolder.FileTypeFilter.Add("*");
            var outputFolder = await diagFolder.PickSingleFolderAsync();
            if (outputFolder != null)
            {
                this.VM.SaveFolderPhoto = outputFolder.Path;
            }
        }

        private async void BorderVoice_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var diagFolder = new FolderPicker() { SuggestedStartLocation = PickerLocationId.MusicLibrary };
            diagFolder.FileTypeFilter.Add("*");
            var outputFolder = await diagFolder.PickSingleFolderAsync();
            if (outputFolder != null)
            {
                this.VM.SaveFolderVoice = outputFolder.Path;
            }
        }

        private async void BorderVideo_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var diagFolder = new FolderPicker() { SuggestedStartLocation = PickerLocationId.VideosLibrary };
            diagFolder.FileTypeFilter.Add("*");
            var outputFolder = await diagFolder.PickSingleFolderAsync();
            if (outputFolder != null)
            {
                this.VM.SaveFolderVideo = outputFolder.Path;
            }
        }
    }
}
