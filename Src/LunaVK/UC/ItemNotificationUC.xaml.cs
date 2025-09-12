using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using LunaVK.Core.DataObjects;
using LunaVK.Core;
using LunaVK.Core.Enums;
using LunaVK.Library;
using LunaVK.Core.Network;
using LunaVK.Core.Utils;
using LunaVK.Core.Library;
using Windows.UI.Xaml.Media.Imaging;
using System.Diagnostics;

namespace LunaVK.UC
{
    //NewsFeedbackItem
    public sealed partial class ItemNotificationUC : UserControl
    {
        public ItemNotificationUC()
        {
            this.InitializeComponent();
            //
            //if(Windows.ApplicationModel.DesignMode.DesignModeEnabled==true)
            //{
            //    img_from.ImageSource = new BitmapImage(new Uri("https://pp.userapi.com/c845520/v845520850/d6911/aTBAhpzF3eo.jpg?ava=1"));
            //}
            //
        }

        private VKBaseDataForGroupOrUser user = null;
#region Data
        public static readonly DependencyProperty DataProperty = DependencyProperty.Register("Data", typeof(object), typeof(ItemNotificationUC), new PropertyMetadata(default(object), OnDataChanged));

        /// <summary>
        /// Данные
        /// </summary>
        public object Data
        {
            get { return (object)GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        private static void OnDataChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ((ItemNotificationUC)obj).ProcessData();
        }
#endregion
        private VKNotification Notification
        {
            get { return this.Data as VKNotification;}
        }

        private void ProcessData()
        {
            this.ContentGrid.Children.Clear();
            this.ContentGrid.ColumnDefinitions.Clear();

            if (this.Data == null || this.Notification == null)
                return;

            this.icon.Glyph = this.SetIcon();

            this.GenerateLayout();

            string highlightedText = this.GetHighlightedText();

            try
            {
                this.date.Text = UIStringFormatterHelper.FormatDateTimeForUI(this.Notification.date);
            }
            catch
            {
                this.date.Text = string.Empty;
            }

            ScrollableTextBlock tb = new ScrollableTextBlock();
            string text = "";
            
            // Ensure user is not null before formatting text
            if (user != null)
            {
                if(this.Notification.type == VKNotification.NotificationType.reply_comment)
                {
                    VKComment feedbackComment = this.Notification.ParsedFeedback as VKComment;
                    if (feedbackComment != null && !string.IsNullOrEmpty(feedbackComment.text))
                    {
                        text = string.Format("[id{0}|{1}]\n{2} {3} {4}", user.Id, user.Title, feedbackComment.text, this.GetLocalizableText(), highlightedText);
                    }
                    else
                    {
                        text = string.Format("[id{0}|{1}] {2} {3}", user.Id, user.Title, this.GetLocalizableText(), highlightedText);
                    }
                }
                else if (this.Notification.type == VKNotification.NotificationType.comment_post)
                {
                    VKComment feedbackComment = this.Notification.ParsedFeedback as VKComment;
                    if (feedbackComment != null && !string.IsNullOrEmpty(feedbackComment.text))
                    {
                        string dateStr = feedbackComment.date > DateTime.MinValue ? feedbackComment.date.ToString("d MMM yyyy") : "";
                        text = string.Format("[id{0}|{1}] оставил комментарий:\n{2} {3} от {4}", user.Id, user.Title, feedbackComment.text, this.GetLocalizableText(), dateStr);
                    }
                    else
                    {
                        text = string.Format("[id{0}|{1}] {2} {3}", user.Id, user.Title, this.GetLocalizableText(), highlightedText);
                    }
                }
                else
                {
                    text = string.Format("[id{0}|{1}] {2} {3}", user.Id, user.Title, this.GetLocalizableText(), highlightedText);
                }
            }
            else
            {
                // Fallback text if user is null
                text = string.Format("{0} {1}", this.GetLocalizableText(), highlightedText);
            }
            
            tb.Text = text;

            ContentGrid.Children.Add(tb);

            string thumb = this.GetThumb();
            if(!string.IsNullOrEmpty(thumb))
            {
                // normalize protocol-relative URL
                if (thumb.StartsWith("//"))
                    thumb = "https:" + thumb;

                if (Uri.TryCreate(thumb, UriKind.Absolute, out Uri thumbUri))
                {
                    ContentGrid.ColumnDefinitions.Add(new ColumnDefinition());
                    ContentGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(64) });

                    Image img = new Image();
                    try
                    {
                        img.Source = new BitmapImage(thumbUri);
                    }
                    catch
                    {
                        // ignore invalid image
                    }

                   // img.Margin = new Thickness(0,0,15,0);
                    Grid.SetColumn(img, 1);

                    ContentGrid.Children.Add(img);
                }
            }
        }

        /// <summary>
        /// Возвращаем ссылку на изображение
        /// </summary>
        /// <returns></returns>
        private string GetThumb()
        {
            if (this.Notification == null)
                return string.Empty;

            if (this.Notification.ParsedParent is VKWallPost parsedParent)
            {
                if (parsedParent.attachments != null && parsedParent.attachments.Count > 0)
                {
                    if(parsedParent.attachments[0].type == VKAttachmentType.Photo)
                    {
                        return parsedParent.attachments[0].photo?.photo_130 ?? string.Empty;
                    }
                    else if (parsedParent.attachments[0].type == VKAttachmentType.Video)
                    {
                        return parsedParent.attachments[0].video?.photo_130 ?? string.Empty;
                    }
                }
            }

            if (this.Notification.ParsedParent is VKPhoto parsedParent2)
            {
                return parsedParent2.photo_130;
            }

            if (this.Notification.ParsedParent is VKVideoBase parsedParent3)
            {
                return parsedParent3.photo_130;
            }

            if (this.Notification.ParsedParent is VKComment parsedParent4)
            {
                if (parsedParent4.attachments != null && parsedParent4.attachments.Count > 0)
                {
                    if (parsedParent4.attachments[0].type == VKAttachmentType.Photo)
                    {
                        return parsedParent4.attachments[0].photo?.photo_130 ?? string.Empty;
                    }
                    else if (parsedParent4.attachments[0].type == VKAttachmentType.Video)
                    {
                        return parsedParent4.attachments[0].video?.photo_130 ?? string.Empty;
                    }
                    else if (parsedParent4.attachments[0].type == VKAttachmentType.Market)
                    {
                        return parsedParent4.attachments[0].market?.thumb_photo ?? string.Empty;
                    }
                }

            }

            return string.Empty;
        }

        /// <summary>
        /// Задаём аватарку и запоминаем пользователя
        /// </summary>
        private void GenerateLayout()
        {
            string str = string.Empty;

            if (this.Notification == null)
                return;

            // Properly retrieve user information based on notification type
            if (this.Notification.ParsedFeedback is List<FeedbackUser> list)
            {
                // For follow, like, and copy notifications which can be grouped
                if (list.Count > 0)
                {
                    int actorId = list[0].from_id != 0 ? list[0].from_id : list[0].owner_id;
                    user = UsersService.Instance.GetCachedUser((uint)Math.Abs(actorId));
                }
            }
            else if(this.Notification.ParsedFeedback is VKComment comment)
            {
                user = UsersService.Instance.GetCachedUser((uint)Math.Abs(comment.from_id));
            }
            else if(this.Notification.ParsedFeedback is VKWallPost post)
            {
                user = UsersService.Instance.GetCachedUser((uint)Math.Abs(post.from_id));
            }
            else if (this.Notification.ParsedFeedback is List<FeedbackCopyInfo> info)
            {
                if (info.Count > 0)
                {
                    long rawId = info[0].from_id != 0 ? info[0].from_id : info[0].owner_id;
                    user = UsersService.Instance.GetCachedUser((uint)Math.Abs((int)rawId));
                }
            }
            
            // Fallback to Notification.Owner if user is still null
            if (user == null)
            {
                user = this.Notification.Owner;
            }
            
            if (user != null)
            {
                str = user.MinPhoto ?? string.Empty;
            }

            // Normalize protocol-relative URLs
            if (!string.IsNullOrEmpty(str) && str.StartsWith("//"))
                str = "https:" + str;

            BitmapImage bmp = null;
            if (!string.IsNullOrWhiteSpace(str) && Uri.TryCreate(str, UriKind.Absolute, out Uri uri))
            {
                try { bmp = new BitmapImage(uri); } catch { bmp = null; }
            }

            if (bmp == null)
            {
                try { bmp = new BitmapImage(new Uri("ms-appx:///Assets/Icons/appbar.user.png")); } catch { bmp = null; }
            }

            img_from.ImageSource = bmp;
        }

        private VKUserSex GetGender()
        {
            //VKBaseDataForGroupOrUser user = null;
            if (this.Notification?.ParsedFeedback is VKCountedItemsObject<FeedbackUser> list)
            {
                if (list.count > 1)
                    return VKUserSex.Unknown;
            }
            else if (this.Notification?.ParsedFeedback is List<FeedbackCopyInfo> info)
            {
                if (info.Count > 1)
                    return VKUserSex.Unknown;
            }

            if (user == null || user is VKGroup)
                return VKUserSex.Unknown;
            return ((VKUser)user).sex;
        }
        

            /// <summary>
            /// Возвращаем текст действия на основе типа уведомления
            /// </summary>
            /// <returns></returns>
        private string GetLocalizableText()
        {
            VKUserSex gender = this.GetGender();
            string str = "";
            switch (this.Notification.type)
            {
                case  VKNotification.NotificationType.follow:
                    switch (gender)
                    {
                        case VKUserSex.Male:
                            str = "Notification_FollowMale";
                            break;
                        case VKUserSex.Female:
                            str = "Notification_FollowFemale";
                            break;
                        case VKUserSex.Unknown:
                            str = "Notification_FollowPlural";
                            break;
                    }
                    break;
                case  VKNotification.NotificationType.friend_accepted:
                    switch (gender)
                    {
                        case VKUserSex.Male:
                            str = "Notification_FriendAcceptedMale";
                            break;
                        case VKUserSex.Female:
                            str = "Notification_FriendAcceptedFemale";
                            break;
                        case VKUserSex.Unknown:
                            str = "Notification_FriendAcceptedPlural";
                            break;
                    }
                    break;
                case  VKNotification.NotificationType.mention_comments:
                    if (gender != VKUserSex.Male)
                    {
                        if (gender == VKUserSex.Female)
                        {
                            str = "Notification_MentionCommentsFemale";
                            break;
                        }
                        break;
                    }
                    str = "Notification_MentionCommentsMale";
                    break;
                case  VKNotification.NotificationType.comment_post:
                    str = "Notification_CommentPost";
                    break;
                case  VKNotification.NotificationType.comment_photo:
                    if (this.Notification.ParsedFeedback is VKComment comment)
                    {
                        if (comment.reply_to_user == Settings.UserId)
                        {
                            str = "Notification_ReplyCommentOrTopic";
                            break;
                        }
                    }

                    if (gender != VKUserSex.Male)
                    {
                        
                        if (gender == VKUserSex.Female)
                        {
                            str = "Notification_CommentPhotoFemale";
                            break;
                        }
                        break;
                    }
                    str = "Notification_CommentPhotoMale";
                    break;
                case  VKNotification.NotificationType.comment_video:
                    if (gender != VKUserSex.Male)
                    {
                        if (gender == VKUserSex.Female)
                        {
                            str = "Notification_CommentVideoFemale";
                            break;
                        }
                        break;
                    }
                    str = "Notification_CommentVideoMale";
                    break;
                case  VKNotification.NotificationType.reply_comment:
                case  VKNotification.NotificationType.reply_topic:
                case  VKNotification.NotificationType.reply_comment_photo:
                case  VKNotification.NotificationType.reply_comment_video:
                case  VKNotification.NotificationType.reply_comment_market:
                    str = "Notification_ReplyCommentOrTopic";
                    break;
                case  VKNotification.NotificationType.like_post:
                    switch (gender)
                    {
                        case VKUserSex.Male:
                            str = "Notification_LikePostMale";
                            break;
                        case VKUserSex.Female:
                            str = "Notification_LikePostFemale";
                            break;
                        case VKUserSex.Unknown:
                            str = "Notification_LikePostPlural";
                            break;
                    }
                    break;
                case  VKNotification.NotificationType.like_comment:
                case  VKNotification.NotificationType.like_comment_photo:
                case  VKNotification.NotificationType.like_comment_video:
                case  VKNotification.NotificationType.like_comment_topic:
                    switch (gender)
                    {
                        case VKUserSex.Male:
                            str = "Notification_LikeCommentMale";
                            break;
                        case VKUserSex.Female:
                            str = "Notification_LikeCommentFemale";
                            break;
                        case VKUserSex.Unknown:
                            str = "Notification_LikeCommentPlural";
                            break;
                    }
                    break;
                case  VKNotification.NotificationType.like_photo:
                    switch (gender)
                    {
                        case VKUserSex.Male:
                            str = "Notification_LikePhotoMale";
                            break;
                        case VKUserSex.Female:
                            str = "Notification_LikePhotoFemale";
                            break;
                        case VKUserSex.Unknown:
                            str = "Notification_LikePhotoPlural";
                            break;
                    }
                    break;
                case VKNotification.NotificationType.like_video:
                    switch (gender)
                    {
                        case VKUserSex.Male:
                            str = "Notification_LikeVideoMale";
                            break;
                        case VKUserSex.Female:
                            str = "Notification_LikeVideoFemale";
                            break;
                        case VKUserSex.Unknown:
                            str = "Notification_LikeVideoPlural";
                            break;
                    }
                    break;//
                case VKNotification.NotificationType.copy_post:
                    switch (gender)
                    {
                        case VKUserSex.Male:
                            str = "Notification_CopyPostMale";
                            break;
                        case VKUserSex.Female:
                            str = "Notification_CopyPostFemale";
                            break;
                        case VKUserSex.Unknown:
                            str = "Notification_CopyPostPlural";
                            break;
                    }
                    break;
                case  VKNotification.NotificationType.copy_photo:
                    switch (gender)
                    {
                        case VKUserSex.Male:
                            str = "Notification_CopyPhotoMale";
                            break;
                        case VKUserSex.Female:
                            str = "Notification_CopyPhotoFemale";
                            break;
                        case VKUserSex.Unknown:
                            str = "Notification_CopyPhotoPlural";
                            break;
                    }
                    break;
                case  VKNotification.NotificationType.copy_video:
                    switch (gender)
                    {
                        case VKUserSex.Male:
                            str = "Notification_CopyVideoMale";
                            break;
                        case VKUserSex.Female:
                            str = "Notification_CopyVideoFemale";
                            break;
                        case VKUserSex.Unknown:
                            str = "Notification_CopyVideoPlural";
                            break;
                    }
                    break;
                case  VKNotification.NotificationType.mention_comment_photo:
                    str = "Notification_MentionInPhotoComment";
                    break;
                case  VKNotification.NotificationType.mention_comment_video:
                    str = "Notification_MentionInVideoComment";
                    break;



                case VKNotification.NotificationType.wall_publish:
                    {
                        return "опубликована ваша новость";
                    }
                    /*
                case  VKNotification.NotificationType.money_transfer_received:
                    MoneyTransfer parsedFeedback1 = (MoneyTransfer)this.Notyfication.ParsedFeedback;
                    str = string.Format(gender == VKUserSex.Male ? "MoneyTransferSentMale : "MoneyTransferSentFemale, ((string)parsedFeedback1.amount.text).Replace(' ', ' '));
                    break;
                case  VKNotification.NotificationType.money_transfer_accepted:
                    MoneyTransfer parsedFeedback2 = (MoneyTransfer)this.Notyfication.ParsedFeedback;
                    str = string.Format(gender == VKUserSex.Male ? "MoneyTransferAcceptedMale : "MoneyTransferAcceptedFemale, ((string)parsedFeedback2.amount.text).Replace(' ', ' '));
                    break;
                case  VKNotification.NotificationType.money_transfer_declined:
                    MoneyTransfer parsedFeedback3 = (MoneyTransfer)this.Notyfication.ParsedFeedback;
                    str = string.Format(gender == VKUserSex.Male ? "MoneyTransferDeclinedMale : "MoneyTransferDeclinedFemale, ((string)parsedFeedback3.amount.text).Replace(' ', ' '));
                    break;*/
            }
            if (string.IsNullOrEmpty(str))
                return "";

            return LocalizedStrings.GetString(str);
        }

        private string GetHighlightedText()
        {
            VKWallPost parsedParent1 = this.Notification.ParsedParent as VKWallPost;
            if (parsedParent1 != null)
            {
                if (!string.IsNullOrEmpty(parsedParent1.text))
                    return parsedParent1.text;
                if (IsRepost(parsedParent1))
                    return parsedParent1.copy_history[0].text;
                return "";
            }
            VKComment parsedParent2 = this.Notification.ParsedParent as VKComment;
            if (parsedParent2 != null)
                return parsedParent2.text;//todo null
            VKTopic parsedParent3 = this.Notification.ParsedParent as VKTopic;
            if (parsedParent3 != null)
                return parsedParent3.title;//todo null
            return "";
        }

        private string CutText(string text)
        {
            text = ((string)text).Replace(Environment.NewLine, " ");
            text = UIStringFormatterHelper.SubstituteMentionsWithNames(text);
            if (((string)text).Length > 50)
                text = ((string)text).Substring(0, 50);
            return text;
        }

        private bool IsRepost(VKWallPost wallPost)
        {
            if (wallPost.copy_history != null)
                return wallPost.copy_history.Count > 0;
            return false;
        }

        /// <summary>
        /// Задаём иконку и цвет иконки
        /// </summary>
        /// <returns></returns>
        private string SetIcon()
        {
            switch (this.Notification.type)
            {
                case VKNotification.NotificationType.comment_post:
                case VKNotification.NotificationType.comment_photo:
                case VKNotification.NotificationType.comment_video:
                    {
                        //https://vk.com/images/svg_icons/feedback/comment.svg
                        
                        return "\xED63";
                    }
                case VKNotification.NotificationType.friend_accepted:
                    {
                        this.FeedBackIconBorder.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 75, 179, 75));
                        return "\xE73E";
                    }
                case VKNotification.NotificationType.like_comment:
                case VKNotification.NotificationType.like_photo:
                case VKNotification.NotificationType.like_comment_photo:
                case VKNotification.NotificationType.like_comment_topic:
                case VKNotification.NotificationType.like_comment_video:
                case VKNotification.NotificationType.like_post:
                case VKNotification.NotificationType.like_video:
                    {
                        this.FeedBackIconBorder.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 230, 70, 70));
                        return "\xEB52";
                    }
                case VKNotification.NotificationType.reply_comment:
                case VKNotification.NotificationType.reply_topic:
                case VKNotification.NotificationType.reply_comment_photo:
                case VKNotification.NotificationType.reply_comment_video:
                case VKNotification.NotificationType.reply_comment_market:
                    {
                        this.FeedBackIconBorder.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255,75,179,75));
                        return "\xEA21";
                    }
                case VKNotification.NotificationType.follow:
                    {
                        this.FeedBackIconBorder.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255,92,156,230));
                        return "\xE9AF";
                    }
                case VKNotification.NotificationType.wall:
                case VKNotification.NotificationType.wall_publish:
                    {
                        this.FeedBackIconBorder.Background = new SolidColorBrush(Windows.UI.Colors.Orange);
                        return "\xE874";
                    }
                case VKNotification.NotificationType.mention_comments:
                    {
                        this.FeedBackIconBorder.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 92, 156, 230));
                        return "\xE910";
                    }
                case VKNotification.NotificationType.copy_post:
                    {
                        this.FeedBackIconBorder.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 75, 179, 75));
                        return "\xE97A";
                    }
            }
            return "";
        }

        private void Avatar_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Library.NavigatorImpl.Instance.NavigateToProfilePage(user.Id);
        }

        private bool _detailsExpanded = false;

        private void Content_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Переключаем разворачивание деталей
            _detailsExpanded = !_detailsExpanded;
            ToggleDetails();
        }

        private void ToggleDetails()
        {
            if (DetailsPanel == null)
                return;

            if (_detailsExpanded)
            {
                // Построим подробный контент
                DetailsPanel.Children.Clear();

                var fullTextBlock = new ScrollableTextBlock();
                //fullTextBlock.TextWrapping = TextWrapping.Wrap;
                fullTextBlock.Margin = new Thickness(0, 4, 0, 0);
                fullTextBlock.IsHitTestVisible = false;

                string fullText = BuildFullDetailsText();
                fullTextBlock.Text = fullText;

                DetailsPanel.Children.Add(fullTextBlock);
                DetailsPanel.Visibility = Visibility.Visible;

                // Простая анимация появления
                var sb = new Windows.UI.Xaml.Media.Animation.Storyboard();
                var fade = new Windows.UI.Xaml.Media.Animation.DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = new Duration(TimeSpan.FromMilliseconds(120))
                };
                Windows.UI.Xaml.Media.Animation.Storyboard.SetTarget(fade, DetailsPanel);
                Windows.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(fade, "Opacity");
                sb.Children.Add(fade);
                sb.Begin();
            }
            else
            {
                DetailsPanel.Visibility = Visibility.Collapsed;
                DetailsPanel.Children.Clear();
            }
        }

        private string BuildFullDetailsText()
        {
            // Полный текст: стараемся показать исходный текст родителя/коммента без обрезки + базовое форматирование
            try
            {
                string actor = user != null ? user.Title : "";
                string action = GetLocalizableText();
                string parentText = GetHighlightedText();

                // Пытаемся вытянуть текст из ParsedFeedback, если там комментарий
                if (this.Notification?.ParsedFeedback is VKComment c && !string.IsNullOrEmpty(c.text))
                {
                    parentText = string.IsNullOrEmpty(parentText) ? c.text : parentText + "\n\n" + c.text;
                }

                string result = string.Empty;
                if (!string.IsNullOrEmpty(actor))
                    result += actor + ": ";

                result += action;

                if (!string.IsNullOrWhiteSpace(parentText))
                    result += "\n\n" + parentText;

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("BuildFullDetailsText error: " + ex);
                return string.Empty;
            }
       
                //if (!(this.Notification.ParsedFeedback is MoneyTransfer))
                //    return;
                //MoneyTransfer parsedFeedback = (MoneyTransfer)this.Notification.ParsedFeedback;
                //TransferCardView.Show(parsedFeedback.id, parsedFeedback.from_id, parsedFeedback.to_id);
           
        }
    }
}
