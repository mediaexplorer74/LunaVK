using LunaVK.Core.DataObjects;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls;
using LunaVK.Framework;
using LunaVK.UC;

namespace LunaVK.Pages
{
    public sealed partial class NotificationsDetailPage : PageBase
    {
        public NotificationsDetailPage()
        {
            this.InitializeComponent();
        }

        private VKNotification Notification { get; set; }

        protected override void LoadState(object navigationParameter, Dictionary<string, object> pageState)
        {
            if (navigationParameter is Dictionary<string, object> dict && dict.ContainsKey("Data"))
            {
                this.Notification = dict["Data"] as VKNotification;
                this.DataContext = this.Notification;
                this.BuildUI();
            }
        }

        private void BuildUI()
        {
            var header = new ItemNotificationUC();
            header.Data = this.Notification;
            this.TopPanel.Children.Add(header);
        }
    }
}