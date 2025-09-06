using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

using LunaVK.Network;
using LunaVK.Core;
using LunaVK.Core.Network;
using LunaVK.Core.Enums;
using LunaVK.ViewModels;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.ViewManagement;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using LunaVK.Library;
using LunaVK.Framework;
using LunaVK.Core.Framework;
using LunaVK.Pages;
using System.Threading.Tasks;
using Windows.Networking.PushNotifications;
using Windows.Storage;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Metadata;
using LunaVK.Common;
using VkLib.Error; // Added for VkLib exceptions

namespace LunaVK
{
    public sealed partial class LoginPage : Page
    {
        /// <summary>
        /// Если есть континум анимация
        /// </summary>
        bool needAnimation;
        //private bool Accessed;
        private bool _isCompleted;

        public LoginPage()
        {
            this.InitializeComponent();
            this.Loaded += this.LoginPage_Loaded;
            this.Unloaded += this.LoginPage_Unloaded;

            InputPane.GetForCurrentView().Showing += this.Keyboard_Showing;
            InputPane.GetForCurrentView().Hiding += this.Keyboard_Hiding;
        }

        private void LoginPage_Unloaded(object sender, RoutedEventArgs e)
        {
            InputPane.GetForCurrentView().Showing -= this.Keyboard_Showing;
            InputPane.GetForCurrentView().Hiding -= this.Keyboard_Hiding;
        }

        private void Keyboard_Showing(InputPane sender, InputPaneVisibilityEventArgs args)
        {
            //this.tr.Y = sender.OccludedRect.Height / (-2);
            this.ShowingMoveSpline.Value = (sender.OccludedRect.Height / (-2));
            this.MoveMiddleOnShowing.Begin();
        }

        private void Keyboard_Hiding(InputPane sender, InputPaneVisibilityEventArgs args)
        {
            //this.tr.Y = 0;
            this.ShowingMoveSpline.Value = 0;
            this.MoveMiddleOnShowing.Begin();
        }

        private void LoginPage_Loaded(object sender, RoutedEventArgs e)
        {
            CustomFrame.Instance.SuppressMenu = true;
            CustomFrame.Instance.Header.Visibility = Visibility.Collapsed;

            if (this.needAnimation)
                this.StoryName.Begin();
            else
            {
                this.RootBack.Opacity = 1.0;
                Transform1.Y = Transform2.Y = 0;
                gridLogin.Opacity = gridPass.Opacity = 1;
            }


            if (Settings.IsAuthorized)
            {
                PushNotifications.Instance.UpdateDeviceRegistration((ret) =>
                {
                    Settings.UserId = 0;
                    Settings.AccessToken = Settings.LoggedInUserName = Settings.LoggedInUserPhoto = Settings.LoggedInUserStatus = "";
                }, true);
            }

            EventAggregator.Instance.PublishCounters(new Core.DataObjects.CountersArgs());
            DialogsViewModel.Instance.Items.Clear();
            CacheManager.TryDelete("News");
            LongPollServerService.Instance.Stop();
            LongPollServerService.Instance.SetUnreadMessages(0);
            //PushNotificationsManager.Instance.EnsureTheChannelIsClosed();
            ContactsManager.Instance.DeleteAllContactsAsync();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            this.PerformLoginAttempt();
        }

        private async void PerformLoginAttempt()
        {
            this._error.Text = "";
            this._error.Opacity = 0;
            this._progressRing.IsActive = true;
            this.passwordBox.IsEnabled = this.textBoxUsername.IsEnabled = this.LoginBtn.IsEnabled = false;
            
            // Use VkLib direct authentication instead of the complex OAuth flow
            try
            {
                var token = await VkService.Instance.Auth.Login(this.textBoxUsername.Text, this.passwordBox.Password, 
                    VkLib.Auth.VkScopeSettings.CanAccessFriends |
                    VkLib.Auth.VkScopeSettings.CanAccessGroups |
                    VkLib.Auth.VkScopeSettings.CanAccessMessages |
                    VkLib.Auth.VkScopeSettings.CanAccessWall |
                    VkLib.Auth.VkScopeSettings.CanAccessVideos |
                    VkLib.Auth.VkScopeSettings.CanAccessPhotos |
                    VkLib.Auth.VkScopeSettings.CanAccessDocs |
                    VkLib.Auth.VkScopeSettings.CanAccessAudios |
                    VkLib.Auth.VkScopeSettings.Offline);

                if (token != null)
                {
                    // Convert VkLib token to LunaVK format
                    Settings.AccessToken = token.Token;
                    Settings.UserId = (uint)token.UserId;
                    
                    // Continue with the rest of the login process
                    this.Callback(VKErrors.None, "");
                }
                else
                {
                    this.Callback(VKErrors.AccessDenied, "Не удалось получить токен доступа");
                }
            }
            catch (VkCaptchaNeededException ex)
            {
                this.Callback(VKErrors.CaptchaNeeded, $"Требуется капча: {ex.CaptchaImg}");
            }
            catch (VkInvalidClientException ex)
            {
                this.Callback(VKErrors.AccessDenied, "Неверный логин или пароль");
            }
            catch (VkNeedValidationException ex)
            {
                this.Callback(VKErrors.NeedValidation, "Требуется подтверждение безопасности");
            }
            catch (Exception ex)
            {
                this.Callback(VKErrors.UnknownError, $"Ошибка авторизации: {ex.Message}");
            }
        }

        private void Callback(VKErrors error, string description)
        {
            if (error == VKErrors.None)
            {
                // No need to parse access_token from description since we already have it
                LongPollServerService.Instance.Restart();
                PushNotifications.Instance.UpdateDeviceRegistration();

                CustomFrame.Instance._shouldResetStack = true;

                MenuViewModel.Instance.GetBaseData((res) =>
                {
                    Execute.ExecuteOnUIThread(() =>
                    { 
                        if (res == true)
                            NavigatorImpl.Instance.NavigateToNewsFeed();
                        else
                        {
                            this._error.Text = "Авторизация выполнена, но не удалось получить данные пользователя.";
                            this._error.Opacity = 1;
                            this._progressRing.IsActive = false;
                            this.passwordBox.IsEnabled = this.textBoxUsername.IsEnabled = this.LoginBtn.IsEnabled = true;
                        }
                    });
                });
                //this.Accessed = true;
            }
            else
            {
                Execute.ExecuteOnUIThread(() =>
                {
                    if(!string.IsNullOrEmpty(description))
                    {
                        this._error.Text = description;
                        //this._error.Animate(1, 0, "Opacity", 400, 3000);
                        this._error.Opacity = 1;
                        this._errorStoryBoard.Begin();
                    }
                    this._progressRing.IsActive = false;
                    this.passwordBox.IsEnabled = this.textBoxUsername.IsEnabled = this.LoginBtn.IsEnabled = true;
                });
            }
        }


        










        

        private void TextBlock_Tapped(object sender, TappedRoutedEventArgs e)
        {
            NavigatorImpl.Instance.NavigateToWebUri("https://m.vk.com/terms", true);
        }
        
        private void Settings_Tapped(object sender, TappedRoutedEventArgs e)
        {
            CustomFrame.Instance.Navigate(typeof(SettingsGeneralPage), true);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);

            
            CustomFrame.Instance.SuppressMenu = false;
            CustomFrame.Instance.Header.Visibility = Visibility.Visible;
            //CustomFrame.Instance.MySplitView.ActivateSwipe(true);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 3))
            {
                ConnectedAnimation imageAnimation = ConnectedAnimationService.GetForCurrentView().GetAnimation("image");
                if (imageAnimation != null)
                {
                    this.needAnimation = true;
                    imageAnimation.TryStart(this.HeaderImage);
                }
            }
            else
            {
                this.needAnimation = true;
            }

        }

        private void UpdateLoginButtonState()
        {
            this.LoginBtn.IsEnabled = !string.IsNullOrWhiteSpace(this.textBoxUsername.Text) && !string.IsNullOrEmpty(this.passwordBox.Password);
        }

        private void textBoxUsername_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.UpdateLoginButtonState();
        }

        private void passwordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            this.UpdateLoginButtonState();
        }

        private void textBoxUsername_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != Windows.System.VirtualKey.Enter || string.IsNullOrEmpty(this.textBoxUsername.Text))
                return;
            this.passwordBox.Focus(FocusState.Keyboard);
        }

        private void passwordBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != Windows.System.VirtualKey.Enter || string.IsNullOrEmpty(this.textBoxUsername.Text) || string.IsNullOrEmpty(this.passwordBox.Password))
                return;
            this.PerformLoginAttempt();
        }









        private void TextBlock_Tapped_1(object sender, TappedRoutedEventArgs e)
        {
            NavigatorImpl.Instance.NavigateToRegistrationPage();
        }
    }
}