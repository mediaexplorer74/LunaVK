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

            // Build display text using group/user title + localized action + short preview of highlighted text
            string displayName = string.Empty;
            try
            {
                if (user != null)
                {
                    var p = user.GetType().GetProperty("Title");
                    if (p != null)
                        displayName = (string)(p.GetValue(user) ?? string.Empty);
                }
                else if (this.Notification.Owner != null)
                {
                    var p = this.Notification.Owner.GetType().GetProperty("Title");
                    if (p != null)
                        displayName = (string)(p.GetValue(this.Notification.Owner) ?? string.Empty);
                }
            }
            catch { }

            string localText = this.GetLocalizableText();
            string shortPreview = string.Empty;
            try
            {
                if (!string.IsNullOrEmpty(highlightedText))
                {
                    shortPreview = UIStringFormatterHelper.CutTextGently(highlightedText, 120);
                }
            }
            catch { }

            // compose title and preview text (generalized gender forms used for action phrases)
            string titleLine = string.Empty;
            if (!string.IsNullOrEmpty(displayName))
                titleLine = string.Format("{0} {1}", displayName, localText);
            else
                titleLine = localText;

            string titleText = titleLine;
            try { titleText = UIStringFormatterHelper.SubstituteMentionsWithNames(titleLine); } catch { titleText = titleLine; }

            string previewText = string.Empty;
            try { if (!string.IsNullOrEmpty(shortPreview)) previewText = UIStringFormatterHelper.SubstituteMentionsWithNames(shortPreview.Trim()); } catch { previewText = shortPreview?.Trim() ?? string.Empty; }

            // create stacked content for left column (title + optional preview)
            var leftContent = new StackPanel { Orientation = Orientation.Vertical };
            var titleBlock = new TextBlock
            {
                Text = titleText,
                TextWrapping = TextWrapping.NoWrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            };
            leftContent.Children.Add(titleBlock);
            if (!string.IsNullOrEmpty(previewText))
            {
                var previewBlock = new TextBlock
                {
                    Text = previewText,
                    TextWrapping = TextWrapping.Wrap,
                    MaxLines = 2,
                    Foreground = new SolidColorBrush(Windows.UI.Colors.Gray),
                    Margin = new Thickness(0, 4, 0, 0)
                };
                leftContent.Children.Add(previewBlock);
            }

            // Decide preview images before adding controls so we can allocate columns
            var previewUrls = this.GetPreviewUrls();
            if (previewUrls == null || previewUrls.Count == 0)
            {
                try
                {
                    string thumb = this.GetThumb();
                    if (!string.IsNullOrEmpty(thumb))
                        previewUrls = new List<string> { thumb };
                }
                catch { }
            }

            // Fallback: if still no preview found, use owner's avatar as a small thumbnail
            if ((previewUrls == null || previewUrls.Count == 0) && this.Notification?.Owner != null)
            {
                try
                {
                    var owner = this.Notification.Owner as VKBaseDataForGroupOrUser;
                    string ownerPhoto = owner?.MinPhoto ?? string.Empty;
                    if (!string.IsNullOrEmpty(ownerPhoto))
                    {
                        if (ownerPhoto.StartsWith("//")) ownerPhoto = "https:" + ownerPhoto;
                        previewUrls = new List<string> { ownerPhoto };
                      }
                }
                catch { }
            }

            if (previewUrls != null && previewUrls.Count > 0)
            {
                // two columns: text (star) + thumbnail (fixed)
                ContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                ContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(84) });

                Grid.SetColumn(leftContent, 0);
                ContentGrid.Children.Add(leftContent);

                // show only first thumbnail in notification list
                var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(8, 0, 0, 0) };
                string url = previewUrls[0];
                if (!string.IsNullOrEmpty(url))
                {
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
                    btn.Tapped += (s, e) => { this.Preview_Tapped(0); };
                    panel.Children.Add(btn);
                }

                Grid.SetColumn(panel, 1);
                ContentGrid.Children.Add(panel);
            }
            else
            {
                // single column: only text
                ContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                Grid.SetColumn(leftContent, 0);
                ContentGrid.Children.Add(leftContent);
            }

            // preview panel already added above when previewUrls present
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
                        if (low.Contains("birthday") || (low.Contains("день") && low.Contains("рожд")) )
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
                try
                {
                    if (ex is System.Runtime.InteropServices.COMException comEx)
                        Logger.Instance.Error($"Avatar_Tapped navigation COMException HResult=0x{comEx.HResult:X8} Message={comEx.Message}", comEx);
                    else
                        Logger.Instance.Error("Avatar_Tapped: navigation failed", ex);
                }
                catch { Debug.WriteLine($"Avatar_Tapped: navigation failed: {ex}"); }
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

                // Prefer explicit item types: if main/additional item explicitly is photo, treat as photo (not wall publish)
                try
                {
                    var addType = raw.SelectToken("additional_item.type")?.ToString() ?? raw.SelectToken("additional_item[0].type")?.ToString();
                    var mainType = raw.SelectToken("main_item.type")?.ToString() ?? raw.SelectToken("main_item[0].type")?.ToString();
                    if (!string.IsNullOrEmpty(addType) && addType.Equals("photo", StringComparison.OrdinalIgnoreCase))
                        return false;
                    if (!string.IsNullOrEmpty(mainType) && mainType.Equals("photo", StringComparison.OrdinalIgnoreCase))
                        return false;
                }
                catch { }

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

        private bool IsRawItemStory()
        {
            try
            {
                var raw = this.Notification?.RawItem;
                if (raw == null)
                    return false;

                // header may contain "published a new story"
                var header = raw.Value<string>("header");
                if (!string.IsNullOrEmpty(header))
                {
                    var low = header.ToLowerInvariant();
                    if (low.Contains("story") && (low.Contains("published") || low.Contains("published a") || low.Contains("published new") || low.Contains("опубликовал") || low.Contains("история") || low.Contains("историю") ))
                        return true;
                }

                // action URL usually contains /story<owner>_<id>
                var actionUrl = raw.Value<string>("action_url") ?? raw.Value<string>("additional_action_url");
                if (!string.IsNullOrEmpty(actionUrl))
                {
                    if (actionUrl.IndexOf("/story", StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                    // regex for story<owner>_<id>
                    if (System.Text.RegularExpressions.Regex.IsMatch(actionUrl, @"story\d+_\d+"))
                        return true;
                }

                // check main_item or additional_item action.url
                var mainAction = raw.SelectToken("main_item.action.url")?.ToString();
                if (!string.IsNullOrEmpty(mainAction) && mainAction.IndexOf("/story", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;

                var addAction = raw.SelectToken("additional_item.action.url")?.ToString() ?? raw.SelectToken("additional_item[0].action.url")?.ToString();
                if (!string.IsNullOrEmpty(addAction) && addAction.IndexOf("/story", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;

                // also inspect raw JSON for story pattern
                var rawStr = raw.ToString();
                if (System.Text.RegularExpressions.Regex.IsMatch(rawStr, @"story\d+_\d+"))
                    return true;
            }
            catch { }
            return false;
        }

        private bool IsRawItemLike()
        {
            try
            {
                var raw = this.Notification?.RawItem;
                if (raw == null)
                    return false;

                var header = raw.Value<string>("header");
                if (!string.IsNullOrEmpty(header))
                {
                    var low = header.ToLowerInvariant();
                    if (low.Contains("liked") || low.Contains("like") || low.Contains("оценил") || low.Contains("оценили") || low.Contains("понрав") || low.Contains("лайк"))
                        return true;
                }

                var actionUrl = raw.Value<string>("action_url") ?? raw.Value<string>("additional_action_url");
                if (!string.IsNullOrEmpty(actionUrl))
                {
                    var low = actionUrl.ToLowerInvariant();
                    if (low.Contains("like") || low.Contains("likes") )
                        return true;
                }

                var rawStr = raw.ToString();
                if (!string.IsNullOrEmpty(rawStr))
                {
                    var low = rawStr.ToLowerInvariant();
                    if (low.Contains("\"type\":\"like\"") || low.Contains("\"type\": 'like'") || low.Contains("\"liked\"") || low.Contains("оценил") || low.Contains("понрав"))
                        return true;
                }
            }
            catch { }
            return false;
        }

        private string GetLikeLocalizedText()
        {
            // prefer parsed parent types to determine like subtype
            try
            {
                var pp = this.Notification?.ParsedParent;
                if (pp is VKPhoto)
                    return LocalizedStrings.GetString("Notification_LikePhoto");
                if (pp is VKVideoBase)
                    return LocalizedStrings.GetString("Notification_LikeVideo");
                if (pp is VKComment)
                    return LocalizedStrings.GetString("Notification_LikeComment");
                if (pp is VKWallPost)
                    return LocalizedStrings.GetString("Notification_LikePost");

                // fallback: inspect raw
                var raw = this.Notification?.RawItem;
                if (raw != null)
                {
                    var s = raw.ToString().ToLowerInvariant();
                    if (s.Contains("photo")) return LocalizedStrings.GetString("Notification_LikePhoto");
                    if (s.Contains("video")) return LocalizedStrings.GetString("Notification_LikeVideo");
                    if (s.Contains("comment")) return LocalizedStrings.GetString("Notification_LikeComment");
                    return LocalizedStrings.GetString("Notification_LikePost");
                }
            }
            catch { }
            return LocalizedStrings.GetString("Notification_LikePost");
        }

        private string GetLocalizableText()
        {
            VKUserSex gender = this.GetGender();

            // Helper to try resource with gender suffix, fallback to base key
            Func<string, string> pick = (baseKey) =>
            {
                string suffix = gender == VKUserSex.Male ? "Male" : (gender == VKUserSex.Female ? "Female" : "Plural");
                string fullKey = baseKey + suffix;
                try
                {
                    string v = LocalizedStrings.GetString(fullKey);
                    if (!string.IsNullOrEmpty(v))
                        return v;
                }
                catch { }

                try
                {
                    string v = LocalizedStrings.GetString(baseKey);
                    if (!string.IsNullOrEmpty(v))
                        return v;
                }
                catch { }

                return string.Empty;
            };

            // story detection: unified label for stories (use gender-aware resource)
            try
            {
                if (IsRawItemStory())
                    return pick("Notification_StoryPublish");
            }
            catch { }

            // If raw indicates a like, prefer like localized text
            try
            {
                if (IsRawItemLike())
                    return GetLikeLocalizedText();
            }
            catch { }

            // birthday
            if (_isBirthdayDetected || (this.Notification != null && this.Notification.type == VKNotification.NotificationType.birthday))
            {
                return LocalizedStrings.GetString("Notification_Birthday");
            }

            // If raw item or explicit type indicates a wall post publication -> unified string
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
                            return LocalizedStrings.GetString("Notification_WallPublish");
                        }
                    }
                }
            }
            catch { }

            if (this.Notification != null && (this.Notification.type == VKNotification.NotificationType.wall || this.Notification.type == VKNotification.NotificationType.wall_publish))
            {
                return LocalizedStrings.GetString("Notification_WallPublish");
            }

            if (this.Notification != null && (this.Notification.type == VKNotification.NotificationType.follow && IsRawItemIndicatesWallPublish()))
            {
                return LocalizedStrings.GetString("Notification_WallPublish");
            }

            // Special-case: comment on photo/video replying to user
            if (this.Notification?.ParsedFeedback is VKComment cmt)
            {
                if ((this.Notification.type == VKNotification.NotificationType.comment_photo || this.Notification.type == VKNotification.NotificationType.comment_video) && cmt.reply_to_user == Settings.UserId)
                {
                    return LocalizedStrings.GetString("Notification_ReplyCommentOrTopic");
                }
            }

            // Map notification types to resource base keys
            switch (this.Notification?.type)
            {
                case VKNotification.NotificationType.birthday:
                    return LocalizedStrings.GetString("Notification_Birthday");
                case VKNotification.NotificationType.follow:
                    return pick("Notification_Follow");
                case VKNotification.NotificationType.friend_accepted:
                    return pick("Notification_FriendAccepted");
                case VKNotification.NotificationType.mention_comments:
                    return pick("Notification_MentionComments");
                case VKNotification.NotificationType.comment_post:
                    return LocalizedStrings.GetString("Notification_CommentPost");
                case VKNotification.NotificationType.comment_photo:
                    return pick("Notification_CommentPhoto");
                case VKNotification.NotificationType.comment_video:
                    return pick("Notification_CommentVideo");
                case VKNotification.NotificationType.reply_comment:
                case VKNotification.NotificationType.reply_topic:
                case VKNotification.NotificationType.reply_comment_photo:
                case VKNotification.NotificationType.reply_comment_video:
                case VKNotification.NotificationType.reply_comment_market:
                    return LocalizedStrings.GetString("Notification_ReplyCommentOrTopic");
                case VKNotification.NotificationType.like_post:
                    return pick("Notification_LikePost");
                case VKNotification.NotificationType.like_comment:
                case VKNotification.NotificationType.like_comment_photo:
                case VKNotification.NotificationType.like_comment_video:
                case VKNotification.NotificationType.like_comment_topic:
                    return pick("Notification_LikeComment");
                case VKNotification.NotificationType.like_photo:
                    return pick("Notification_LikePhoto");
                case VKNotification.NotificationType.like_video:
                    return pick("Notification_LikeVideo");
                case VKNotification.NotificationType.copy_post:
                    return pick("Notification_CopyPost");
                case VKNotification.NotificationType.copy_photo:
                    return pick("Notification_CopyPhoto");
                case VKNotification.NotificationType.copy_video:
                    return pick("Notification_CopyVideo");
                case VKNotification.NotificationType.mention_comment_photo:
                    return LocalizedStrings.GetString("Notification_MentionInPhotoComment");
                case VKNotification.NotificationType.mention_comment_video:
                    return LocalizedStrings.GetString("Notification_MentionInVideoComment");
                default:
                    return string.Empty;
            }
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

            // if raw item indicates like, prefer like icon
            try
            {
                if (IsRawItemLike() || (notifType == VKNotification.NotificationType.like_post || notifType == VKNotification.NotificationType.like_photo || notifType == VKNotification.NotificationType.like_comment || notifType == VKNotification.NotificationType.like_video || notifType == VKNotification.NotificationType.like_comment_photo || notifType == VKNotification.NotificationType.like_comment_video || notifType == VKNotification.NotificationType.like_comment_topic))
                {
                    this.FeedBackIconBorder.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 230, 70, 70));
                    return "\xEB52"; // like glyph
                }
            }
            catch { }

            if (notifType == VKNotification.NotificationType.follow && IsRawItemIndicatesWallPublish())
                notifType = VKNotification.NotificationType.wall_publish;

            // story should have its own icon (do early check)
            try
            {
                if (IsRawItemStory())
                {
                    // use distinct icon and background for stories
                    this.FeedBackIconBorder.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 140, 0)); // orange-ish
                    return "\xE8B8"; // story-like glyph (fallback)
                }
            }
            catch { }

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

                Action<Action> safeNav = (a) =>
                {
                    try
                    {
                        Execute.ExecuteOnUIThread(() =>
                        {
                            try { a(); }
                            catch (Exception navEx)
                            {
                                try
                                {
                                    if (navEx is System.Runtime.InteropServices.COMException comEx)
                                        Logger.Instance.Error($"Content_Tapped navigation COMException HResult=0x{comEx.HResult:X8} Message={comEx.Message}", comEx);
                                    else
                                        Logger.Instance.Error("Content_Tapped: navigation call failed", navEx);
                                }
                                catch { Debug.WriteLine(navEx.ToString()); }
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        try { Logger.Instance.Error("Content_Tapped: scheduling navigation failed", ex); } catch { Debug.WriteLine(ex.ToString()); }
                    }
                };

                // Try parsed parent first
                try
                {
                    var parsed = this.Notification.ParsedParent;
                    if (parsed is VKWallPost wp)
                    {
                        int ownerId = 0;
                        uint postId = 0;

                        try { var ownerIdProp = wp.GetType().GetProperty("owner_id"); if (ownerIdProp != null) ownerId = Convert.ToInt32(ownerIdProp.GetValue(wp)); } catch { }
                        try { if (ownerId == 0) { var fromIdProp = wp.GetType().GetProperty("from_id"); if (fromIdProp != null) ownerId = Convert.ToInt32(fromIdProp.GetValue(wp)); } } catch { }
                        try { if (ownerId == 0 && wp.Owner != null) { if (wp.Owner is VKGroup g) ownerId = (int)-g.id; else ownerId = ((VKUser)wp.Owner).Id; } } catch { }
                        try
                        {
                            if (!TryGetUIntProperty(wp, out postId, "id", "post_id", "postId"))
                            {
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
                        Debug.WriteLine($"Content_Tapped: navigating to wall post via ParsedParent owner={ownerId} post={postId}");

                        safeNav(() => Library.NavigatorImpl.Instance.NavigateToWallPostComments(ownerId, postId, 0, wp));
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error("Content_Tapped: error using ParsedParent", ex);
                }

                // If no parsed parent, inspect RawItem. Use explicit signed object ids first to preserve group sign.
                int ownerIdParsed = 0;
                uint postIdParsed = 0;
                string actionUrl = null;
                string url = null;

                try
                {
                    var raw = this.Notification.RawItem;
                    if (raw != null)
                    {
                        // Prefer photo navigation when notification contains a photo object in main_item/additional_item
                        try
                        {
                            JToken addItem = raw.SelectToken("additional_item") ?? raw.SelectToken("additional_item.0");
                            JToken mainItem = raw.SelectToken("main_item") ?? raw.SelectToken("main_item.0");
                            JToken candidate = addItem ?? mainItem;
                            if (candidate != null)
                            {
                                string candType = candidate.Value<string>("type");
                                if (!string.IsNullOrEmpty(candType) && candType.Equals("photo", StringComparison.OrdinalIgnoreCase))
                                {
                                    string objId = candidate.Value<string>("object_id") ?? candidate.Value<string>("objectId");
                                    if (!string.IsNullOrEmpty(objId))
                                    {
                                        var parts = objId.Split('_');
                                        if (parts.Length == 2)
                                        {
                                            if (int.TryParse(parts[0], out int photoOwner) && uint.TryParse(parts[1], out uint photoId))
                                            {
                                                // navigate directly to photo comments/viewer
                                                safeNav(() => Library.NavigatorImpl.Instance.NavigateToPhotoWithComments(photoOwner, photoId));
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch { }

                        try
                        {
                            var mainObjId = raw.Value<string>("main_object_id");
                            var addObjId = raw.Value<string>("additional_object_id");
                            string objId = mainObjId ?? addObjId;
                            if (!string.IsNullOrEmpty(objId))
                            {
                                var parts = objId.Split('_');
                                if (parts.Length == 2)
                                {
                                    if (int.TryParse(parts[0], out int photoOwner) && uint.TryParse(parts[1], out uint photoId))
                                    {
                                        // navigate directly to photo comments/viewer
                                        safeNav(() => Library.NavigatorImpl.Instance.NavigateToPhotoWithComments(photoOwner, photoId));
                                        return;
                                    }
                                }
                            }
                        }
                        catch { }

                        try
                        {
                            int ownerId = 0;
                            uint postId = 0;

                            try { var ownerIdProp = raw.GetType().GetProperty("owner_id"); if (ownerIdProp != null) ownerId = Convert.ToInt32(ownerIdProp.GetValue(raw)); } catch { }
                            try { if (ownerId == 0) { var fromIdProp = raw.GetType().GetProperty("from_id"); if (fromIdProp != null) ownerId = Convert.ToInt32(fromIdProp.GetValue(raw)); } } catch { }
                            try { if (ownerId == 0 && this.Notification.Owner != null) { if (this.Notification.Owner is VKGroup g) ownerId = (int)-g.id; else ownerId = ((VKUser)this.Notification.Owner).Id; } } catch { }

                            try
                            {
                                if (!TryGetUIntProperty(raw, out postId, "id", "post_id", "postId"))
                                {
                                    var idProp = raw.GetType().GetProperty("id");
                                    if (idProp != null)
                                    {
                                        var idVal = idProp.GetValue(raw);
                                        if (idVal != null)
                                            postId = Convert.ToUInt32(idVal);
                                    }
                                }
                            }
                            catch { postId = 0; }

                            Logger.Instance.Info($"Content_Tapped: navigating to wall post via RawItem owner={ownerId} post={postId}");
                            Debug.WriteLine($"Content_Tapped: navigating to wall post via RawItem owner={ownerId} post={postId}");

                            safeNav(() => Library.NavigatorImpl.Instance.NavigateToWallPostComments(ownerId, postId, 0, null));
                            return;
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance.Error("Content_Tapped: error using RawItem", ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error("Content_Tapped: error inspecting RawItem", ex);
                }
            }
            catch { }
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
            if (this.Notification == null || this.Notification.ParsedParent == null)
                return;

            List<string> urls = null;
            try
            {
                urls = this.GetPreviewUrls();
            }
            catch { }

            if (urls == null || urls.Count == 0)
                return;

            string url = null;
            try { url = urls[index]; } catch { }

            if (string.IsNullOrEmpty(url))
                return;

            try
            {
                if (url.StartsWith("//"))
                    url = "https:" + url;

                // open in web preview / viewer using existing navigator
                Library.NavigatorImpl.Instance.NavigateToWebUri(url);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Preview_Tapped: navigation to media preview failed: {ex}");
            }
        }
    }
}
