using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Reflection;

using LunaVK.Core.DataObjects;
using LunaVK.Core;
using LunaVK.Core.Enums;
using LunaVK.Library;
using LunaVK.Core.Network;
using LunaVK.Core.Utils;
using LunaVK.Core.Library;
using LunaVK.Core.Framework; // added for Execute
using Windows.UI.Xaml.Media.Imaging;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

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
            Debug.WriteLine($"ItemNotificationUC.ProcessData start. Data is null? {this.Data==null}");

            this.ContentGrid.Children.Clear();
            this.ContentGrid.ColumnDefinitions.Clear();

            if (this.Data == null || this.Notification == null)
            {
                Debug.WriteLine("ItemNotificationUC.ProcessData: no notification data, returning.");
                return;
            }

            Debug.WriteLine($"ItemNotificationUC.ProcessData: Notification type={this.Notification.type} date={this.Notification.date} parsedParent={this.Notification.ParsedParent?.GetType().Name} parsedFeedback={this.Notification.ParsedFeedback?.GetType().Name}");

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
            
            try
            {
                tb.Text = UIStringFormatterHelper.SubstituteMentionsWithNames(text);
            }
            catch
            {
                tb.Text = text;
            }

            ContentGrid.Children.Add(tb);

            string thumb = this.GetThumb();
            Debug.WriteLine($"ItemNotificationUC.ProcessData: GetThumb -> '{thumb}'");
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
                        var bitmap = new BitmapImage();
                        bitmap.UriSource = thumbUri;
                        // If loading fails, set fallback image
                        bitmap.ImageFailed += (s, e) =>
                        {
                            Debug.WriteLine($"ItemNotificationUC: thumbnail ImageFailed for '{thumbUri}'");
                            try { img.Source = new BitmapImage(new Uri("ms-appx:///Assets/Icons/appbar.user.png")); } catch { }
                        };

                        img.Source = bitmap;
                        Debug.WriteLine($"ItemNotificationUC: thumbnail BitmapImage assigned for '{thumbUri}'");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"ItemNotificationUC: exception creating thumbnail BitmapImage: {ex}");
                        // ignore invalid image - set fallback
                        try { img.Source = new BitmapImage(new Uri("ms-appx:///Assets/Icons/appbar.user.png")); } catch { img.Source = null; }
                    }

                   // img.Margin = new Thickness(0,0,15,0);
                    Grid.SetColumn(img, 1);

                    ContentGrid.Children.Add(img);
                }
                else
                {
                    Debug.WriteLine($"ItemNotificationUC: GetThumb returned non-absolute URL: '{thumb}'");
                }
            }
            else
            {
                Debug.WriteLine("ItemNotificationUC: no thumb available for this notification");
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

        // Reflection helpers to read common id properties from parsed objects
        private static bool TryGetIntProperty(object obj, out int value, params string[] names)
        {
            value = 0;
            if (obj == null)
                return false;
            Type t = obj.GetType();
            foreach (var name in names)
            {
                var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p != null)
                {
                    try
                    {
                        var v = p.GetValue(obj);
                        if (v == null) continue;
                        value = Convert.ToInt32(v);
                        return true;
                    }
                    catch { }
                }
            }
            return false;
        }

        private static bool TryGetUIntProperty(object obj, out uint value, params string[] names)
        {
            value = 0;
            if (obj == null)
                return false;
            Type t = obj.GetType();
            foreach (var name in names)
            {
                var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p != null)
                {
                    try
                    {
                        var v = p.GetValue(obj);
                        if (v == null) continue;
                        value = Convert.ToUInt32(v);
                        return true;
                    }
                    catch { }
                }
            }
            return false;
        }

        /// <summary>
        /// Задаём аватарку и запоминаем пользователя
        /// </summary>
        private void GenerateLayout()
        {
            // Reset user to null at the beginning of each layout generation
            user = null;
            Debug.WriteLine("GenerateLayout: start");
            
            string str = string.Empty;

            if (this.Notification == null)
            {
                Debug.WriteLine("GenerateLayout: Notification is null, returning.");
                return;
            }

            // If ParsedFeedback is a raw JSON string (some notifications are raw), try to parse a photo directly
            try
            {
                if (this.Notification.ParsedFeedback is string rawFeedbackStr && !string.IsNullOrWhiteSpace(rawFeedbackStr))
                {
                    Debug.WriteLine("GenerateLayout: ParsedFeedback is raw string, attempting JSON parse to find photo fields");
                    try
                    {
                        var token = JToken.Parse(rawFeedbackStr);
                        JToken first = null;
                        if (token.Type == JTokenType.Array)
                            first = token.First;
                        else if (token["items"] != null && token["items"].Type == JTokenType.Array)
                            first = token["items"].First;
                        else
                            first = token;

                        if (first != null)
                        {
                            JToken photoToken = first.SelectToken("photo_100") ?? first.SelectToken("photo_200") ?? first.SelectToken("photo_50") ?? first.SelectToken("photo_130") ?? first.SelectToken("photo") ?? first.SelectToken("photo_604");
                            if (photoToken != null)
                            {
                                string photoUrl = photoToken.ToString();
                                Debug.WriteLine($"GenerateLayout: extracted photo from raw feedback = '{photoUrl}'");
                                if (!string.IsNullOrEmpty(photoUrl) && photoUrl.StartsWith("//"))
                                    photoUrl = "https:" + photoUrl;

                                if (Uri.TryCreate(photoUrl, UriKind.Absolute, out Uri photoUri))
                                {
                                    try
                                    {
                                        var extractedBmp = new BitmapImage();
                                        extractedBmp.UriSource = photoUri;
                                        extractedBmp.ImageFailed += (s, e) => { Debug.WriteLine($"GenerateLayout: extracted-photo ImageFailed for '{photoUri}'"); };
                                        img_from.ImageSource = extractedBmp;
                                        avatarGlyph.Visibility = Visibility.Collapsed;
                                        Debug.WriteLine("GenerateLayout: avatar set from raw ParsedFeedback photo and returning");
                                        return; // done
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"GenerateLayout: exception creating BitmapImage from extracted photo: {ex}");
                                    }
                                }
                            }

                            // If no photo found, try extract actor id from raw JSON so we can fetch user/group
                            long? actorIdFromRaw = first.Value<long?>("from_id") ?? first.Value<long?>("owner_id") ?? first.Value<long?>("user_id") ?? first.Value<long?>("id");
                            if (actorIdFromRaw.HasValue)
                            {
                                long rawId = actorIdFromRaw.Value;
                                Debug.WriteLine($"GenerateLayout: extracted actor id from raw ParsedFeedback: {rawId}");
                                if (rawId > 0)
                                {
                                    uint uid = (uint)rawId;
                                    var cached = UsersService.Instance.GetCachedUser(uid);
                                    Debug.WriteLine($"GenerateLayout: cached user for uid={uid} is null? {cached==null}");
                                    if (cached != null)
                                    {
                                        user = cached;
                                    }
                                    else
                                    {
                                        // fetch and update avatar when ready
                                        Debug.WriteLine($"GenerateLayout: fetching user info for uid={uid} (from raw ParsedFeedback)");
                                        UsersService.Instance.GetUsers(new List<uint> { uid }, (res) =>
                                        {
                                            Debug.WriteLine($"UsersService.GetUsers callback (rawParsed): result null? {res==null} count={(res!=null?res.Count:0)} for uid={uid}");
                                            if (res != null && res.Count > 0)
                                            {
                                                var fetched = res[0];
                                                Execute.ExecuteOnUIThread(() =>
                                                {
                                                    user = fetched;
                                                    UpdateAvatarFromUser(user);
                                                });
                                            }
                                        });
                                    }
                                }
                                else if (rawId < 0)
                                {
                                    uint gid = (uint)(-rawId);
                                    var cachedGroup = GroupsService.Instance.GetCachedGroup(gid);
                                    Debug.WriteLine($"GenerateLayout: cached group for gid={gid} is null? {cachedGroup==null}");
                                    if (cachedGroup != null)
                                    {
                                        user = cachedGroup;
                                    }
                                    else
                                    {
                                        Debug.WriteLine($"GenerateLayout: fetching group info for gid={gid} (from raw ParsedFeedback)");
                                        GroupsService.Instance.GetGroupInfo(gid, true, (grRes) =>
                                        {
                                            Debug.WriteLine($"GroupsService.GetGroupInfo callback (rawParsed): res null? {grRes==null}");
                                            if (grRes != null && grRes.error.error_code == VKErrors.None && grRes.response != null)
                                            {
                                                Execute.ExecuteOnUIThread(() =>
                                                {
                                                    user = grRes.response;
                                                    UpdateAvatarFromUser(user);
                                                });
                                            }
                                        });
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"GenerateLayout: failed to parse ParsedFeedback JSON: {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GenerateLayout: unexpected error while handling raw ParsedFeedback: {ex}");
            }

            long? actorIdNullable = null;
            bool actorIsGroup = false;

            // Extract actor id from various ParsedFeedback shapes
            if (this.Notification.ParsedFeedback is VKCountedItemsObject<FeedbackUser> countedFeedback && countedFeedback.count > 0)
            {
                var first = countedFeedback.items[0];
                long actorId = first.from_id != 0 ? first.from_id : first.owner_id;
                actorIdNullable = Math.Abs(actorId);
                actorIsGroup = actorId < 0;
                Debug.WriteLine($"GenerateLayout: countedFeedback actorId={actorId} abs={actorIdNullable} isGroup={actorIsGroup}");
            }
            else if (this.Notification.ParsedFeedback is List<FeedbackCopyInfo> listCopy && listCopy.Count > 0)
            {
                var first = listCopy[0];
                long actorId = first.from_id != 0 ? first.from_id : first.owner_id;
                actorIdNullable = Math.Abs(actorId);
                actorIsGroup = actorId < 0;
                Debug.WriteLine($"GenerateLayout: listCopy actorId={actorId} abs={actorIdNullable} isGroup={actorIsGroup}");
            }
            else if (this.Notification.ParsedFeedback is VKComment comment)
            {
                actorIdNullable = (long?)Math.Abs((long)comment.from_id);
                actorIsGroup = comment.from_id < 0;
                Debug.WriteLine($"GenerateLayout: VKComment from_id={comment.from_id}");
            }
            else if (this.Notification.ParsedFeedback is VKWallPost post)
            {
                // For publications prefer owner_id (the community/user whose page contains the post)
                long actor = post.owner_id != 0 ? post.owner_id : post.from_id;
                actorIdNullable = (long?)Math.Abs(actor);
                actorIsGroup = actor < 0;
                Debug.WriteLine($"GenerateLayout: VKWallPost owner_id={post.owner_id} from_id={post.from_id} chosenActor={actor} isGroup={actorIsGroup}");
            }
            else if (this.Notification.ParsedFeedback is VKCountedItemsObject<FeedbackCopyInfo> countedCopy && countedCopy.count > 0)
            {
                var first = countedCopy.items[0];
                long actorId = first.from_id != 0 ? first.from_id : first.owner_id;
                actorIdNullable = Math.Abs(actorId);
                actorIsGroup = actorId < 0;
                Debug.WriteLine($"GenerateLayout: countedCopy actorId={actorId} abs={actorIdNullable} isGroup={actorIsGroup}");
            }
            else if (this.Notification.ParsedFeedback is List<FeedbackCopyInfo> listCopy2 && listCopy2.Count > 0)
            {
                var first = listCopy2[0];
                long actorId = first.from_id != 0 ? first.from_id : first.owner_id;
                actorIdNullable = Math.Abs(actorId);
                actorIsGroup = actorId < 0;
                Debug.WriteLine($"GenerateLayout: listCopy2 actorId={actorId} abs={actorIdNullable} isGroup={actorIsGroup}");
            }

            // Try retrieve from cache if we found actor id
            if (actorIdNullable.HasValue)
            {
                uint uid = (uint)actorIdNullable.Value;
                Debug.WriteLine($"GenerateLayout: resolved actor uid={uid} isGroup={actorIsGroup}");
                if (!actorIsGroup)
                {
                    user = UsersService.Instance.GetCachedUser(uid);
                    Debug.WriteLine($"GenerateLayout: cached user for uid={uid} is null? {user==null}");
                    if (user == null)
                    {
                        Debug.WriteLine($"GenerateLayout: fetching user info for uid={uid}");
                        // fetch user async and update image when ready
                        UsersService.Instance.GetUsers(new List<uint> { uid }, (result) =>
                        {
                            Debug.WriteLine($"UsersService.GetUsers callback: result null? {result==null} count={(result!=null?result.Count:0)} for uid={uid}");
                            if (result != null && result.Count > 0)
                            {
                                var fetched = result[0];
                                // update UI
                                Execute.ExecuteOnUIThread(() =>
                                {
                                    Debug.WriteLine($"UsersService.GetUsers: fetched id={fetched.Id} MinPhoto='{fetched.MinPhoto}'");
                                    user = fetched;
                                    UpdateAvatarFromUser(user);
                                });
                            }
                        });
                    }
                }
                else
                {
                    Debug.WriteLine($"GenerateLayout: actor is group, trying to use Notification.Owner or fetch group info");
                    // actor is group — try fallback to Notification.Owner or fetch group info if needed
                    if (this.Notification.Owner is VKGroup g && g.id == (int)actorIdNullable.Value)
                    {
                        user = this.Notification.Owner;
                        Debug.WriteLine($"GenerateLayout: Notification.Owner is group id={g.id}");
                    }
                    else
                    {
                        uint gid = (uint)actorIdNullable.Value;
                        // Try to use Notification.Owner first
                        if (this.Notification.Owner != null)
                            user = this.Notification.Owner;
                        else
                        {
                            Debug.WriteLine($"GenerateLayout: fetching group info for gid={gid}");
                            // fetch group info
                            GroupsService.Instance.GetGroupInfo(gid, true, (res) =>
                            {
                                Debug.WriteLine($"GroupsService.GetGroupInfo callback: res null? {res==null}");
                                if (res != null && res.error.error_code == VKErrors.None && res.response != null)
                                {
                                    Execute.ExecuteOnUIThread(() =>
                                    {
                                        Debug.WriteLine($"GroupsService.GetGroupInfo: fetched group id={res.response.id} MinPhoto='{res.response.MinPhoto}'");
                                        user = res.response;
                                        UpdateAvatarFromUser(user);
                                    });
                                }
                            });
                        }
                    }
                }
            }

            // If we still don't have user, fallback to Notification.Owner
            if (user == null)
            {
                user = this.Notification.Owner;
                Debug.WriteLine($"GenerateLayout: fallback to Notification.Owner is null? {user==null}");
            }
            
            if (user != null)
            {
                str = user.MinPhoto ?? string.Empty;
            }

            Debug.WriteLine($"GenerateLayout: chosen MinPhoto='{str}'");

            // Normalize protocol-relative URLs
            if (!string.IsNullOrEmpty(str) && str.StartsWith("//"))
                str = "https:" + str;

            Debug.WriteLine($"GenerateLayout: normalized MinPhoto='{str}'");

            BitmapImage bmp = null;
            if (!string.IsNullOrWhiteSpace(str) && Uri.TryCreate(str, UriKind.Absolute, out Uri uri))
            {
                try {
                    Debug.WriteLine($"GenerateLayout: creating BitmapImage for uri={uri}");
                    bmp = new BitmapImage();
                    bmp.UriSource = uri;
                    // fallback to default avatar if loading fails
                    bmp.ImageFailed += (s, e) =>
                    {
                        Debug.WriteLine($"GenerateLayout: BitmapImage ImageFailed for '{uri}'");
                        try { img_from.ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/Icons/appbar.user.png")); } catch { }
                    };
                } catch (Exception ex) { Debug.WriteLine($"GenerateLayout: exception creating BitmapImage: {ex}"); bmp = null; }
            }

            if (bmp == null)
            {
                try { bmp = new BitmapImage(new Uri("ms-appx:///Assets/Icons/appbar.user.png")); } catch { bmp = null; }
            }

            try { img_from.ImageSource = bmp; avatarGlyph.Visibility = Visibility.Collapsed; Debug.WriteLine("GenerateLayout: avatar image assigned and glyph hidden"); } catch { Debug.WriteLine("GenerateLayout: failed to assign avatar image"); }
        }

        private void UpdateAvatarFromUser(VKBaseDataForGroupOrUser fetchedUser)
        {
            Debug.WriteLine($"UpdateAvatarFromUser: called fetchedUser null? {fetchedUser==null}");
            if (fetchedUser == null)
                return;

            string s = fetchedUser.MinPhoto ?? string.Empty;
            Debug.WriteLine($"UpdateAvatarFromUser: MinPhoto before normalization='{s}'");
            if (!string.IsNullOrEmpty(s) && s.StartsWith("//"))
                s = "https:" + s;

            Debug.WriteLine($"UpdateAvatarFromUser: MinPhoto normalized='{s}'");

            if (!string.IsNullOrWhiteSpace(s) && Uri.TryCreate(s, UriKind.Absolute, out Uri uri))
            {
                try
                {
                    Debug.WriteLine($"UpdateAvatarFromUser: creating BitmapImage for '{uri}'");
                    var bmp = new BitmapImage();
                    bmp.UriSource = uri;
                    bmp.ImageFailed += (ss, ee) =>
                    {
                        Debug.WriteLine($"UpdateAvatarFromUser: Bitmap ImageFailed for '{uri}'");
                        try { img_from.ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/Icons/appbar.user.png")); avatarGlyph.Visibility = Visibility.Visible; } catch { }
                    };

                    img_from.ImageSource = bmp; avatarGlyph.Visibility = Visibility.Collapsed;
                    Debug.WriteLine($"UpdateAvatarFromUser: image source assigned for '{uri}'");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"UpdateAvatarFromUser: exception while assigning image: {ex}");
                }
            }
            else
            {
                Debug.WriteLine("UpdateAvatarFromUser: no valid MinPhoto Uri, setting fallback and showing glyph");
                try { img_from.ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/Icons/appbar.user.png")); avatarGlyph.Visibility = Visibility.Visible; } catch { }
            }
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
            // Handle wall publications explicitly
            if (this.Notification != null && (this.Notification.type == VKNotification.NotificationType.wall || this.Notification.type == VKNotification.NotificationType.wall_publish))
            {
                // For groups or unknown gender use neuter form
                if (gender == VKUserSex.Male)
                    return "опубликовал новый пост";
                if (gender == VKUserSex.Female)
                    return "опубликовала новый пост";
                return "опубликовало новый пост";
            }

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
                        // fallback handled above
                        break;
                    }
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
            if (user == null)
                return;
            Library.NavigatorImpl.Instance.NavigateToProfilePage(user.Id);
        }

        private bool _detailsExpanded = false;

        private void Content_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Navigate to detailed page for this notification
            try
            {
                if (this.Notification != null)
                    Library.NavigatorImpl.Instance.NavigateToNotificationDetail(this.Notification);
            }
            catch { }
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
        }
    }
}
