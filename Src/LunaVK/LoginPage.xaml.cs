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

        private void PerformLoginAttempt()
        {
            this._error.Text = "";
            this._error.Opacity = 0;
            this._progressRing.IsActive = true;
            this.passwordBox.IsEnabled = this.textBoxUsername.IsEnabled = this.LoginBtn.IsEnabled = false;
            
            VKRequestsDispatcher.DispatchLoginRequest(this.textBoxUsername.Text, this.passwordBox.Password, this.Callback);
        }

        private void Callback(VKErrors error, string description)
        {
            if (error == VKErrors.None)
            {
                Regex QueryStringRegex = new Regex("access_token=(?<access_token>.+)&.+user_id=(?<user_id>\\d+)");
                Match m = QueryStringRegex.Match(description);
                string access_token = m.Groups["access_token"].Value;
                uint user_id = uint.Parse(m.Groups["user_id"].Value);
                
                Settings.AccessToken = access_token;
                Settings.UserId = user_id;
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
