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
            {
                return;
            }

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

            // Show previews (one or more). Prefer parsed attachments, then RawItem.additional_item or main_item.
            var previewUrls = this.GetPreviewUrls();
            if (previewUrls != null && previewUrls.Count > 0)
            {
                ContentGrid.ColumnDefinitions.Add(new ColumnDefinition());
                ContentGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(84) });

                var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(8, 0, 0, 0) };
                int idx = 0;
                foreach (var u in previewUrls)
                {
                    string url = u;
                    if (url.StartsWith("//"))
                        url = "https:" + url;
                    var btn = new Button { Padding = new Thickness(0), Margin = new Thickness(2), Background = null, BorderThickness = new Thickness(0) };
                    var img = new Image { Width = 64, Height = 64, Stretch = Windows.UI.Xaml.Media.Stretch.UniformToFill };
                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.UriSource = new Uri(url);
                        bitmap.ImageFailed += (s, e) => { Debug.WriteLine($"ItemNotificationUC: preview ImageFailed for '{url}' Error: {e?.ErrorMessage}"); };
                        img.Source = bitmap;
                    }
                    catch { }

                    btn.Content = img;
                    int captureIdx = idx;
                    btn.Tapped += (s, e) => { this.Preview_Tapped(captureIdx); };
                    panel.Children.Add(btn);
                    idx++;
                }

                Grid.SetColumn(panel, 1);
                ContentGrid.Children.Add(panel);
            }
        }

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

            // Try to extract preview from raw notification item (additional_item or main_item)
            try
            {
                var raw = this.Notification.RawItem;
                if (raw != null)
                {
                    // Try main/additional items for any image
                    JToken add = raw.SelectToken("additional_item") ?? raw.SelectToken("additional_item.0");
                    JToken mi = raw.SelectToken("main_item") ?? raw.SelectToken("main_item.0");

                    string url = TryExtractImageFromItem(add);
                    if (!string.IsNullOrEmpty(url))
                        return url;

                    url = TryExtractImageFromItem(mi);
                    if (!string.IsNullOrEmpty(url))
                        return url;

                    // fallback: check arrays of image_object
                    var ai = raw.SelectToken("additional_item.image_object") ?? raw.SelectToken("main_item.image_object");
                    if (ai != null && ai.Type == JTokenType.Array && ai.HasValues)
                    {
                        var first = ai.First;
                        string found = TryExtractImageFromItem(first);
                        if (!string.IsNullOrEmpty(found))
                            return found;
                    }
                }
            }
            catch { }

            return string.Empty;
        }

        private string TryExtractImageFromItem(JToken item)
        {
            if (item == null)
                return string.Empty;

            // common fields where image url may be present
            string[] candidates = new[] { "photo_130", "photo_100", "photo_200", "photo", "thumb_photo", "url", "src", "image_url", "image" };
            foreach (var name in candidates)
            {
                var t = item.SelectToken(name);
                if (t != null && t.Type == JTokenType.String)
                {
                    string s = t.ToString();
                    if (!string.IsNullOrEmpty(s))
                        return s.StartsWith("//") ? "https:" + s : s;
                }
            }

            // sometimes image_object is an array of objects containing 'url'
            var imgObj = item.SelectToken("image_object");
            if (imgObj != null && imgObj.Type == JTokenType.Array && imgObj.HasValues)
            {
                foreach (var entry in imgObj)
                {
                    foreach (var name in candidates)
                    {
                        var t = entry.SelectToken(name);
                        if (t != null && t.Type == JTokenType.String)
                        {
                            string s = t.ToString();
                            if (!string.IsNullOrEmpty(s))
                                return s.StartsWith("//") ? "https:" + s : s;
                        }
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

        private bool _isBirthdayDetected = false;

        /// <summary>
        /// Задаём аватарку и запоминаем пользователя
        /// </summary>
        private void GenerateLayout()
        {
            user = null;
            _isBirthdayDetected = false;

            string str = string.Empty;

            if (this.Notification == null)
            {
                return;
            }

            try
            {
                if (this.Notification.ParsedFeedback is string rawFeedbackStr && !string.IsNullOrWhiteSpace(rawFeedbackStr))
                {
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
                            try
                            {
                                string rawText = first.ToString();
                                if (!string.IsNullOrEmpty(rawText))
                                {
                                    var low = rawText.ToLowerInvariant();
                                    if (low.Contains("birthday") || (low.Contains("день") && low.Contains("рожд")))
                                    {
                                        _isBirthdayDetected = true;
                                    }
                                }
                            }
                            catch { }

                            JToken photoToken = first.SelectToken("photo_100") ?? first.SelectToken("photo_200") ?? first.SelectToken("photo_50") ?? first.SelectToken("photo_130") ?? first.SelectToken("photo") ?? first.SelectToken("photo_604");
                            if (photoToken != null)
                            {
                                string photoUrl = photoToken.ToString();
                                if (!string.IsNullOrEmpty(photoUrl) && photoUrl.StartsWith("//"))
                                    photoUrl = "https:" + photoUrl;

                                if (Uri.TryCreate(photoUrl, UriKind.Absolute, out Uri photoUri))
                                {
                                    try
                                    {
                                        var extractedBmp = new BitmapImage();
                                        extractedBmp.UriSource = photoUri;
                                        extractedBmp.ImageFailed += (s, e) => { Debug.WriteLine($"GenerateLayout: extracted-photo ImageFailed for '{photoUri}' Error: {e?.ErrorMessage}"); };
                                        img_from.ImageSource = extractedBmp;
                                        avatarGlyph.Visibility = Visibility.Collapsed;
                                        return; // done
                                    }
                                    catch { }
                                }
                            }

                            long? actorIdFromRaw = first.Value<long?>("from_id") ?? first.Value<long?>("owner_id") ?? first.Value<long?>("user_id") ?? first.Value<long?>("id");
                            if (actorIdFromRaw.HasValue)
                            {
                                long rawId = actorIdFromRaw.Value;
                                if (rawId > 0)
                                {
                                    uint uid = (uint)rawId;
                                    var cached = UsersService.Instance.GetCachedUser(uid);
                                    if (cached != null)
                                    {
                                        user = cached;
                                    }
                                    else
                                    {
                                        UsersService.Instance.GetUsers(new List<uint> { uid }, (res) =>
                                        {
                                            if (res == null)
                                            {
                                                Debug.WriteLine($"GenerateLayout: GetUsers returned null for uid={uid}");
                                            }
                                            else if (res.Count == 0)
                                            {
                                                Debug.WriteLine($"GenerateLayout: GetUsers returned empty list for uid={uid}");
                                            }
                                            else
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
                                    if (cachedGroup != null)
                                    {
                                        user = cachedGroup;
                                    }
                                    else
                                    {
                                        GroupsService.Instance.GetGroupInfo(gid, true, (grRes) =>
                                        {
                                            if (grRes == null)
                                            {
                                                Debug.WriteLine($"GenerateLayout: GetGroupInfo returned null for gid={gid}");
                                            }
                                            else if (grRes.error.error_code != VKErrors.None)
                                            {
                                                Debug.WriteLine($"GenerateLayout: GetGroupInfo error for gid={gid} code={(int)grRes.error.error_code}");
                                            }
                                            else if (grRes.response == null)
                                            {
                                                Debug.WriteLine($"GenerateLayout: GetGroupInfo response null for gid={gid}");
                                            }
                                            else
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

            try
            {
                if (!_isBirthdayDetected && this.Notification?.RawItem != null)
                {
                    var header = this.Notification.RawItem.Value<string>("header");
                    if (!string.IsNullOrEmpty(header))
                    {
                        var low = header.ToLowerInvariant();
                        if (low.Contains("birthday") || (low.Contains("день") && low.Contains("рожд")))
                        {
                            _isBirthdayDetected = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GenerateLayout: error while checking RawItem.header: {ex}");
            }

            long? actorIdNullable = null;
            bool actorIsGroup = false;

            if (this.Notification.ParsedFeedback is VKCountedItemsObject<FeedbackUser> countedFeedback && countedFeedback.count > 0)
            {
                var first = countedFeedback.items[0];
                long actorId = first.from_id != 0 ? first.from_id : first.owner_id;
                actorIdNullable = Math.Abs(actorId);
                actorIsGroup = actorId < 0;
            }
            else if (this.Notification.ParsedFeedback is List<FeedbackUser> feedbackUsers && feedbackUsers.Count > 0)
            {
                var first = feedbackUsers[0];
                long actorId = first.from_id != 0 ? first.from_id : first.owner_id;
                actorIdNullable = Math.Abs(actorId);
                actorIsGroup = actorId < 0;
            }
            else if (this.Notification.ParsedFeedback is List<FeedbackCopyInfo> listCopy && listCopy.Count > 0)
            {
                var first = listCopy[0];
                long actorId = first.from_id != 0 ? first.from_id : first.owner_id;
                actorIdNullable = Math.Abs(actorId);
                actorIsGroup = actorId < 0;
            }
            else if (this.Notification.ParsedFeedback is VKComment comment)
            {
                actorIdNullable = (long?)Math.Abs((long)comment.from_id);
                actorIsGroup = comment.from_id < 0;
            }
            else if (this.Notification.ParsedFeedback is VKWallPost post)
            {
                long actor = post.owner_id != 0 ? post.owner_id : post.from_id;
                actorIdNullable = (long?)Math.Abs(actor);
                actorIsGroup = actor < 0;
            }
            else if (this.Notification.ParsedFeedback is VKCountedItemsObject<FeedbackCopyInfo> countedCopy && countedCopy.count > 0)
            {
                var first = countedCopy.items[0];
                long actorId = first.from_id != 0 ? first.from_id : first.owner_id;
                actorIdNullable = Math.Abs(actorId);
                actorIsGroup = actorId < 0;
            }
            else if (this.Notification.ParsedFeedback is List<FeedbackCopyInfo> listCopy2 && listCopy2.Count > 0)
            {
                var first = listCopy2[0];
                long actorId = first.from_id != 0 ? first.from_id : first.owner_id;
                actorIdNullable = Math.Abs(actorId);
                actorIsGroup = actorId < 0;
            }

            if (actorIdNullable.HasValue)
            {
                uint uid = (uint)actorIdNullable.Value;
                if (!actorIsGroup)
                {
                    user = UsersService.Instance.GetCachedUser(uid);
                    if (user == null)
                    {
                        UsersService.Instance.GetUsers(new List<uint> { uid }, (result) =>
                        {
                            if (result == null)
                            {
                                Debug.WriteLine($"GenerateLayout: GetUsers returned null for uid={uid}");
                            }
                            else if (result.Count == 0)
                            {
                                Debug.WriteLine($"GenerateLayout: GetUsers returned empty list for uid={uid}");
                            }
                            else
                            {
                                var fetched = result[0];
                                Execute.ExecuteOnUIThread(() =>
                                {
                                    user = fetched;
                                    UpdateAvatarFromUser(user);
                                });
                            }
                        });
                    }
                }
                else
                {
                    if (this.Notification.Owner is VKGroup g && g.id == (int)actorIdNullable.Value)
                    {
                        user = this.Notification.Owner;
                    }
                    else
                    {
                        uint gid = (uint)actorIdNullable.Value;
                        if (this.Notification.Owner != null)
                            user = this.Notification.Owner;
                        else
                        {
                            GroupsService.Instance.GetGroupInfo(gid, true, (res) =>
                            {
                                if (res == null)
                                {
                                    Debug.WriteLine($"GenerateLayout: GetGroupInfo returned null for gid={gid}");
                                }
                                else if (res.error.error_code != VKErrors.None)
                                {
                                    Debug.WriteLine($"GenerateLayout: GetGroupInfo error for gid={gid} code={(int)res.error.error_code}");
                                }
                                else if (res.response == null)
                                {
                                    Debug.WriteLine($"GenerateLayout: GetGroupInfo response null for gid={gid}");
                                }
                                else
                                {
                                    Execute.ExecuteOnUIThread(() =>
                                    {
                                        user = res.response;
                                        UpdateAvatarFromUser(user);
                                    });
                                }
                            });
                        }
                    }
                }
            }

            if (user == null)
            {
                user = this.Notification.Owner;
            }

            if (user != null)
            {
                str = user.MinPhoto ?? string.Empty;
            }

            if (!string.IsNullOrEmpty(str) && str.StartsWith("//"))
                str = "https:" + str;

            BitmapImage bmp = null;
            if (!string.IsNullOrWhiteSpace(str) && Uri.TryCreate(str, UriKind.Absolute, out Uri uri))
            {
                try {
                    bmp = new BitmapImage();
                    bmp.UriSource = uri;
                    bmp.ImageFailed += (s, e) => { Debug.WriteLine($"GenerateLayout: BitmapImage ImageFailed for '{uri}' Error: {e?.ErrorMessage}"); };
                } catch { bmp = null; }
            }

            if (bmp == null)
            {
                try { bmp = new BitmapImage(new Uri("ms-appx:///Assets/Icons/appbar.user.png")); } catch { bmp = null; }
            }

            try { img_from.ImageSource = bmp; avatarGlyph.Visibility = Visibility.Collapsed; } catch { }
        }

        private void UpdateAvatarFromUser(VKBaseDataForGroupOrUser fetchedUser)
        {
            if (fetchedUser == null)
                return;

            string s = fetchedUser.MinPhoto ?? string.Empty;
            if (!string.IsNullOrEmpty(s) && s.StartsWith("//"))
                s = "https:" + s;

            if (!string.IsNullOrWhiteSpace(s) && Uri.TryCreate(s, UriKind.Absolute, out Uri uri))
            {
                try
                {
                    var bmp = new BitmapImage();
                    bmp.UriSource = uri;
                    bmp.ImageFailed += (ss, ee) =>
                    {
                        Debug.WriteLine($"UpdateAvatarFromUser: Bitmap ImageFailed for '{uri}' Error: {ee?.ErrorMessage}");
                        try { img_from.ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/Icons/appbar.user.png")); avatarGlyph.Visibility = Visibility.Visible; } catch { }
                    };

                    img_from.ImageSource = bmp; avatarGlyph.Visibility = Visibility.Collapsed;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"UpdateAvatarFromUser: exception while assigning image: {ex}");
                }
            }
            else
            {
                try { img_from.ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/Icons/appbar.user.png")); avatarGlyph.Visibility = Visibility.Visible; } catch { }
            }
        }

        private void Avatar_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                if (user == null)
                    return;
                Library.NavigatorImpl.Instance.NavigateToProfilePage(user.Id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Avatar_Tapped: navigation failed: {ex}");
            }
        }

        private VKUserSex GetGender()
        {
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

        private bool IsRawItemIndicatesWallPublish()
        {
            try
            {
                var raw = this.Notification?.RawItem;
                if (raw == null)
                    return false;

                // header like "[club187062591|На максималках] posted '3' new posts"
                var header = raw.Value<string>("header");
                if (!string.IsNullOrEmpty(header))
                {
                    var low = header.ToLowerInvariant();
                    if (low.Contains("posted") || low.Contains("new post") || low.Contains("new posts") || low.Contains("опубликовал") || low.Contains("опубликовала") || low.Contains("опубликовало") )
                        return true;
                }

                // action URLs may point to wall- links
                var actionUrl = raw.Value<string>("action_url") ?? raw.Value<string>("additional_action_url");
                if (!string.IsNullOrEmpty(actionUrl))
                {
                    if (System.Text.RegularExpressions.Regex.IsMatch(actionUrl, @"wall-?-?\d+_\d+"))
                        return true;
                }

                // check main_item or additional_item action.url
                var mainAction = raw.SelectToken("main_item.action.url")?.ToString();
                if (!string.IsNullOrEmpty(mainAction) && System.Text.RegularExpressions.Regex.IsMatch(mainAction, @"wall-?-?\d+_\d+"))
                    return true;

                var addAction = raw.SelectToken("additional_item.action.url")?.ToString() ?? raw.SelectToken("additional_item[0].action.url")?.ToString();
                if (!string.IsNullOrEmpty(addAction) && System.Text.RegularExpressions.Regex.IsMatch(addAction, @"wall-?-?\d+_\d+"))
                    return true;

                // also inspect any image_object action urls
                var rawStr = raw.ToString();
                if (System.Text.RegularExpressions.Regex.IsMatch(rawStr, @"wall-?(?<owner>-?\d+)_(?<post>\d+)"))
                    return true;
            }
            catch { }
            return false;
        }

        private string GetLocalizableText()
        {
            VKUserSex gender = this.GetGender();
            string str = "";

            if (_isBirthdayDetected || (this.Notification != null && this.Notification.type == VKNotification.NotificationType.birthday))
            {
                return LocalizedStrings.GetString("Notification_Birthday");
            }

            try
            {
                if (this.Notification?.RawItem != null)
                {
                    var act = this.Notification.RawItem.Value<string>("action_url");
                    if (string.IsNullOrEmpty(act))
                        act = this.Notification.RawItem.Value<string>("additional_action_url");
                    if (!string.IsNullOrEmpty(act))
                    {
                        var low = act.ToLowerInvariant();
                        if (low.Contains("/wall") || low.Contains("vk.com/wall") || low.Contains("/wall-"))
                        {
                            if (gender == VKUserSex.Male)
                                return "опубликовал новый пост";
                            if (gender == VKUserSex.Female)
                                return "опубликовала новый пост";
                            return "опубликовало новый пост";
                        }
                    }
                }
            }
            catch { }

            if (this.Notification != null && (this.Notification.type == VKNotification.NotificationType.wall || this.Notification.type == VKNotification.NotificationType.wall_publish))
            {
                if (gender == VKUserSex.Male)
                    return "опубликовал новый пост";
                if (gender == VKUserSex.Female)
                    return "опубликовала новый пост";
                return "опубликовало новый пост";
            }

            // Some notifications (group publications) may be delivered with type 'follow' but raw item contains wall links -> treat as wall_publish
            if (this.Notification != null && (this.Notification.type == VKNotification.NotificationType.follow && IsRawItemIndicatesWallPublish()))
            {
                if (gender == VKUserSex.Male)
                    return "опубликовал новый пост";
                if (gender == VKUserSex.Female)
                    return "опубликовала новый пост";
                return "опубликовало новый пост";
            }

            switch (this.Notification.type)
            {
                case VKNotification.NotificationType.birthday:
                    str = "Notification_Birthday";
                    break;
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
                return parsedParent2.text;
            VKTopic parsedParent3 = this.Notification.ParsedParent as VKTopic;
            if (parsedParent3 != null)
                return parsedParent3.title;
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

        private string SetIcon()
        {
            // if raw item indicates wall publish, override icon selection
            var notifType = this.Notification?.type ?? VKNotification.NotificationType.follow;
            if (notifType == VKNotification.NotificationType.follow && IsRawItemIndicatesWallPublish())
                notifType = VKNotification.NotificationType.wall_publish;

            switch (notifType)
             {
                 case VKNotification.NotificationType.birthday:
                     {
                         this.FeedBackIconBorder.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 200, 100, 200));
                         return "\xE7F4";
                     }
                 case VKNotification.NotificationType.comment_post:
                 case VKNotification.NotificationType.comment_photo:
                 case VKNotification.NotificationType.comment_video:
                     {
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
             }
             return "";
         }

        private bool _detailsExpanded = false;

        private void Content_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                if (this.Notification == null)
                    return;

                // helper to call navigation safely on UI thread and log exceptions
                Action<Action> safeNav = (a) =>
                {
                    try
                    {
                        Execute.ExecuteOnUIThread(() =>
                        {
                            try
                            {
                                a();
                            }
                            catch (Exception navEx)
                            {
                                try { Logger.Instance.Error("Content_Tapped: navigation call failed", navEx); } catch { Debug.WriteLine(navEx.ToString()); }
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        try { Logger.Instance.Error("Content_Tapped: scheduling navigation failed", ex); } catch { Debug.WriteLine(ex.ToString()); }
                    }
                };

                // First try to navigate using ParsedParent if it's a VKWallPost
                try
                {
                    var parsed = this.Notification.ParsedParent;
                    if (parsed is VKWallPost wp)
                    {
                        int ownerId = 0;
                        try
                        {
                            // prefer explicit owner_id/from_id when available
                            var ownerIdProp = wp.GetType().GetProperty("owner_id");
                            if (ownerIdProp != null)
                                ownerId = Convert.ToInt32(ownerIdProp.GetValue(wp));
                        }
                        catch { }

                        try
                        {
                            if (ownerId == 0)
                            {
                                var fromIdProp = wp.GetType().GetProperty("from_id");
                                if (fromIdProp != null)
                                    ownerId = Convert.ToInt32(fromIdProp.GetValue(wp));
                            }
                        }
                        catch { }

                        try
                        {
                            if (ownerId == 0 && wp.Owner != null)
                            {
                                if (wp.Owner is VKGroup g)
                                    ownerId = (int)-g.id;
                                else
                                    ownerId = ((VKUser)wp.Owner).Id;
                            }
                        }
                        catch { }

                        uint postId = 0;
                        try
                        {
                            // Try common id property names via reflection helper
                            if (!TryGetUIntProperty(wp, out postId, "id", "post_id", "postId"))
                            {
                                // try field 'id' via property access fallback
                                var idProp = wp.GetType().GetProperty("id");
                                if (idProp != null)
                                {
                                    var idVal = idProp.GetValue(wp);
                                    if (idVal != null)
                                        postId = Convert.ToUInt32(idVal);
                                }
                            }
                        }
                        catch { postId = 0; }

                        Logger.Instance.Info($"Content_Tapped: navigating to wall post via ParsedParent owner={ownerId} post={postId}");

                        safeNav(() => Library.NavigatorImpl.Instance.NavigateToWallPostComments(ownerId, postId, 0, wp));
                         return;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error("Content_Tapped: error using ParsedParent", ex);
                }

                // If no parsed parent or failed, try to extract URL from RawItem
                string url = null;
                try
                {
                    var raw = this.Notification.RawItem;
                    if (raw != null)
                    {
                        url = raw.Value<string>("action_url") ?? raw.Value<string>("additional_action_url");
                        if (string.IsNullOrEmpty(url))
                        {
                            // try to extract URL from header markup like [https://vk.com/wall-...|text]
                            try
                            {
                                var header = raw.Value<string>("header");
                                if (!string.IsNullOrEmpty(header))
                                {
                                    var mHead = System.Text.RegularExpressions.Regex.Match(header, "\\[(https?:\\/\\/[^|\\]]+)");
                                    if (mHead.Success)
                                    {
                                        url = mHead.Groups[1].Value;
                                        Logger.Instance.Info($"Content_Tapped: extracted url from header: {url}");
                                    }
                                }
                            }
                            catch (Exception exHead)
                            {
                                Logger.Instance.Error("Content_Tapped: failed to extract url from RawItem.header", exHead);
                            }

                            if (string.IsNullOrEmpty(url))
                            {
                                var additional = raw["additional_item"];
                                if (additional != null)
                                {
                                    var aaction = additional["action"];
                                    if (aaction != null)
                                        url = aaction.Value<string>("url");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error("Content_Tapped: error extracting URL from RawItem", ex);
                }

                if (!string.IsNullOrEmpty(url))
                {
                    try
                    {
                        try { url = System.Net.WebUtility.UrlDecode(url); } catch { }

                        // If URL points to an id- page (invalid for post), try to locate wall link inside raw item
                        if (url.Contains("/id-") || url.Contains("id-"))
                        {
                            try
                            {
                                string rawStr = this.Notification.RawItem?.ToString() ?? string.Empty;
                                var found = System.Text.RegularExpressions.Regex.Match(rawStr, @"wall-?(?<owner>-?\d+)_(?<post>\d+)");
                                if (found.Success)
                                {
                                    int ownerId = int.Parse(found.Groups["owner"].Value);
                                    uint postId = uint.Parse(found.Groups["post"].Value);
                                    // prefer mobile url
                                    string mobileUrl = $"https://m.vk.com/wall{ownerId}_{postId}";
                                    Logger.Instance.Info($"Content_Tapped: url was id-..., extracted wall link from RawItem and navigating to mobile URL {mobileUrl}");
                                    safeNav(() => Library.NavigatorImpl.Instance.NavigateToWallPostComments(ownerId, postId, 0, this.Notification.ParsedParent as VKWallPost));
                                     return;
                                }
                                else
                                {
                                    Logger.Instance.Info("Content_Tapped: url contains id- but no wall- link found in RawItem");
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Instance.Error("Content_Tapped: failed to extract wall link from RawItem when url contained id-", ex);
                            }
                        }

                        var pattern = @"(?:https?:\\/\\/)?(?:w{3}\\.|m\\.)?vk\.com\/.+?wall-?(?<owner>-?\d+)_(?<post>\d+)";
                        var m = System.Text.RegularExpressions.Regex.Match(url, pattern);
                        if (!m.Success)
                            m = System.Text.RegularExpressions.Regex.Match(url, @"wall-?(?<owner>-?\d+)_(?<post>\d+)");

                        if (m.Success)
                        {
                            int ownerId = int.Parse(m.Groups["owner"].Value);
                            uint postId = uint.Parse(m.Groups["post"].Value);

                            object postData = null;
                            try { if (this.Notification.ParsedParent is VKWallPost wp2) postData = wp2; } catch { }

                            Logger.Instance.Info($"Content_Tapped: navigating to wall post owner={ownerId} post={postId}");
                            safeNav(() => Library.NavigatorImpl.Instance.NavigateToWallPostComments(ownerId, postId, 0, postData));
                             return;
                        }
                        else
                        {
                            Logger.Instance.Info($"Content_Tapped: NavigateToWebUri for url: {url}");
                            safeNav(() => Library.NavigatorImpl.Instance.NavigateToWebUri(url));
                             return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Error("Content_Tapped: url navigation failed", ex);
                    }
                }
 
                 // Fallback: open notification detail
                safeNav(() => Library.NavigatorImpl.Instance.NavigateToNotificationDetail(this.Notification));
             }
             catch (Exception ex)
             {
                 Logger.Instance.Error("Content_Tapped: navigation failed", ex);
             }
         }

        private void ToggleDetails()
        {
            if (DetailsPanel == null)
                return;

            if (_detailsExpanded)
            {
                DetailsPanel.Children.Clear();

                var fullTextBlock = new ScrollableTextBlock();
                fullTextBlock.Margin = new Thickness(0, 4, 0, 0);
                fullTextBlock.IsHitTestVisible = false;

                string fullText = BuildFullDetailsText();
                fullTextBlock.Text = fullText;

                DetailsPanel.Children.Add(fullTextBlock);
                DetailsPanel.Visibility = Visibility.Visible;

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
            try
            {
                string actor = user != null ? user.Title : "";
                string action = GetLocalizableText();
                string parentText = GetHighlightedText();

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
                Debug.WriteLine("BuildFullDetailsText error: " + ex.Message);
                return string.Empty;
            }
        }

        private List<string> GetPreviewUrls()
        {
            var list = new List<string>();

            // First from ParsedParent attachments (prefer photo_130, photo_200, photo_604)
            try
            {
                if (this.Notification?.ParsedParent is VKWallPost wp && wp.attachments != null && wp.attachments.Count > 0)
                {
                    foreach (var a in wp.attachments)
                    {
                        if (a.type == VKAttachmentType.Photo && a.photo != null)
                        {
                            if (!string.IsNullOrEmpty(a.photo.photo_130)) list.Add(a.photo.photo_130);
                            else if (!string.IsNullOrEmpty(a.photo.photo_200)) list.Add(a.photo.photo_200);
                            else if (!string.IsNullOrEmpty(a.photo.photo_604)) list.Add(a.photo.photo_604);
                        }
                        else if (a.type == VKAttachmentType.Video && a.video != null)
                        {
                            if (!string.IsNullOrEmpty(a.video.photo_130)) list.Add(a.video.photo_130);
                        }
                    }
                }
                else if (this.Notification?.ParsedParent is VKPhoto p)
                {
                    if (!string.IsNullOrEmpty(p.photo_130)) list.Add(p.photo_130);
                }
            }
            catch { }

            // Next try RawItem
            try
            {
                var raw = this.Notification.RawItem;
                if (raw != null)
                {
                    JToken add = raw.SelectToken("additional_item") ?? raw.SelectToken("additional_items") ?? raw.SelectToken("additional_item.0");
                    JToken mi = raw.SelectToken("main_item") ?? raw.SelectToken("main_item.0");

                    if (add != null)
                    {
                        if (add.Type == JTokenType.Array)
                        {
                            foreach (var it in add)
                            {
                                var ex = TryExtractImageFromItem(it);
                                if (!string.IsNullOrEmpty(ex)) list.Add(ex);
                            }
                        }
                        else
                        {
                            var ex = TryExtractImageFromItem(add);
                            if (!string.IsNullOrEmpty(ex)) list.Add(ex);
                        }
                    }

                    if (mi != null)
                    {
                        var ex = TryExtractImageFromItem(mi);
                        if (!string.IsNullOrEmpty(ex)) list.Add(ex);
                    }

                    // also try image_object arrays
                    var objs = raw.SelectTokens("..image_object");
                    foreach (var o in objs)
                    {
                        if (o != null && o.Type == JTokenType.Array)
                        {
                            foreach (var it in o)
                            {
                                var ex = TryExtractImageFromItem(it);
                                if (!string.IsNullOrEmpty(ex)) list.Add(ex);
                            }
                        }
                    }
                }
            }
            catch { }

            // Deduplicate and return up to 4 previews
            var res = new List<string>();
            foreach (var s in list)
            {
                if (string.IsNullOrEmpty(s)) continue;
                if (!res.Contains(s)) res.Add(s);
                if (res.Count >= 4) break;
            }
            return res;
        }

        private void Preview_Tapped(int index)
        {
            // Prefer navigating to wall post if we can determine owner/post, otherwise open image url
            try
            {
                var raw = this.Notification?.RawItem;
                int ownerId = 0; uint postId = 0;
                string actionUrl = null;

                if (raw != null)
                {
                    // try to extract wall-<owner>_<post> from RawItem
                    var rawStr = raw.ToString();
                    var m = System.Text.RegularExpressions.Regex.Match(rawStr, @"wall-?(?<owner>-?\d+)_(?<post>\d+)");
                    if (m.Success)
                    {
                        int.TryParse(m.Groups["owner"].Value, out ownerId);
                        uint.TryParse(m.Groups["post"].Value, out postId);
                    }

                    // try common action fields as fallback
                    actionUrl = raw.Value<string>("action_url") ?? raw.Value<string>("additional_action_url");
                    if (string.IsNullOrEmpty(actionUrl))
                        actionUrl = raw.SelectToken("additional_item.action.url")?.ToString() ?? raw.SelectToken("main_item.action.url")?.ToString();
                }

                if (ownerId != 0 && postId != 0)
                {
                    try
                    {
                        Execute.ExecuteOnUIThread(() => Library.NavigatorImpl.Instance.NavigateToWallPostComments(ownerId, postId, 0, this.Notification.ParsedParent as VKWallPost));
                        return;
                    }
                    catch (Exception navEx)
                    {
                        Debug.WriteLine($"Preview_Tapped: NavigateToWallPostComments failed owner={ownerId} post={postId}: {navEx}");
                        // fallback to opening action url
                        if (!string.IsNullOrEmpty(actionUrl))
                        {
                            try { Execute.ExecuteOnUIThread(() => Library.NavigatorImpl.Instance.NavigateToWebUri(actionUrl)); return; } catch { }
                        }
                    }
                }

                var previews = this.GetPreviewUrls();
                if (index >= 0 && index < previews.Count)
                {
                    var url = previews[index];
                    if (!string.IsNullOrEmpty(url))
                    {
                        if (url.StartsWith("//")) url = "https:" + url;
                        try
                        {
                            Execute.ExecuteOnUIThread(() => Library.NavigatorImpl.Instance.NavigateToWebUri(url));
                        }
                        catch (Exception webEx)
                        {
                            Debug.WriteLine($"Preview_Tapped: NavigateToWebUri failed for url={url}: {webEx}");
                            // try actionUrl if available
                            if (!string.IsNullOrEmpty(actionUrl))
                            {
                                try { Execute.ExecuteOnUIThread(() => Library.NavigatorImpl.Instance.NavigateToWebUri(actionUrl)); } catch (Exception ex) { Debug.WriteLine("Preview_Tapped fallback web navigation failed: " + ex); }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Preview_Tapped navigation failed: " + ex);
            }
        }
    }
}
