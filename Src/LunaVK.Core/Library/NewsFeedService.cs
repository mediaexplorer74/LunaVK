using LunaVK.Core.DataObjects;
using LunaVK.Core.Network;
using LunaVK.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using LunaVK.Core.Framework;
using LunaVK.Core.Enums;


namespace LunaVK.Core.Library
{
    public class NewsFeedService
    {
        private static NewsFeedService _instance;
        public static NewsFeedService Instance
        {
            get { return NewsFeedService._instance ?? (NewsFeedService._instance = new NewsFeedService()); }
        }

        /// <summary>
        /// Позволяет скрыть объект из ленты новостей. 
        /// </summary>
        /// <param name="ignore"></param>
        /// <param name="type"></param>
        /// <param name="ownerId"></param>
        /// <param name="itemId">wall,tag,profilephoto ,video,photo,audio</param>
        /// <param name="callback"></param>
        public void IgnoreUnignoreItem(bool ignore, string type, int ownerId, uint itemId, Action<bool> callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters["type"] = type;
            parameters["owner_id"] = ownerId.ToString();
            parameters["item_id"] = itemId.ToString();

            VKRequestsDispatcher.DispatchRequestToVK<int>(ignore ? "newsfeed.ignoreItem" : "newsfeed.unignoreItem", parameters, (result) => {
                if (result.error.error_code != Enums.VKErrors.None)
                    callback(false);
                else
                    callback(result.response == 1);
            });
        }

        /// <summary>
        /// Запрещает/разрешает показывать новости от заданных пользователей и групп в ленте новостей текущего пользователя.
        /// </summary>
        /// <param name="addBan"></param>
        /// <param name="uids"></param>
        /// <param name="gids"></param>
        /// <param name="callback"></param>
        public void AddDeleteBan(bool addBan, List<uint> uids, List<uint> gids, Action<bool> callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            if (uids != null && uids.Count > 0)
                parameters["user_ids"] = uids.GetCommaSeparated();
            if (gids != null && gids.Count > 0)
                parameters["group_ids"] = gids.GetCommaSeparated();

            VKRequestsDispatcher.DispatchRequestToVK<int>(addBan ? "newsfeed.addBan" : "newsfeed.deleteBan", parameters, (result) => {
                if (result.error.error_code != Enums.VKErrors.None)
                    callback(false);
                else
                    callback(result.response == 1);
            });
        }

        // Backwards-compatible helpers restored to keep callers compiling
        public void DeleteBan(List<uint> uids, List<uint> gids, Action<bool> callback)
        {
            this.AddDeleteBan(false, uids, gids, callback);
        }

        public void GetBanned(Action<VKResponse<NotificationData>> callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            VKRequestsDispatcher.DispatchRequestToVK<NotificationData>("newsfeed.getBanned", parameters, callback);
        }

        public void MarkAsViewed()
        {
            MarkAsViewed(null);
        }

        public void MarkAsViewed(Action<VKResponse<int>> callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            VKRequestsDispatcher.DispatchRequestToVK<int>("notifications.markAsViewed", parameters, (res) => { try { callback?.Invoke(res); } catch { } });
        }

        public void GetNewsComments(int startTime, int endTime, int count, string startFrom, Action<VKResponse<VKCountedItemsObject<VKNewsfeedPost>>> callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            if (count > 0)
                parameters["count"] = count.ToString();
            if (!string.IsNullOrWhiteSpace(startFrom))
                parameters["start_from"] = startFrom;

            VKRequestsDispatcher.DispatchRequestToVK<VKCountedItemsObject<VKNewsfeedPost>>("newsfeed.getComments", parameters, callback);
        }

        public void Search(string q, int count, int param3, int param4, string startFrom, Action<VKResponse<VKCountedItemsObject<VKNewsfeedPost>>> callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(q))
                parameters["q"] = q;
            if (count > 0)
                parameters["count"] = count.ToString();
            if (!string.IsNullOrWhiteSpace(startFrom))
                parameters["start_from"] = startFrom;

            VKRequestsDispatcher.DispatchRequestToVK<VKCountedItemsObject<VKNewsfeedPost>>("newsfeed.search", parameters, callback);
        }

        public void GetSuggestedSources(int offset, int count, bool shuffle, Action<VKResponse<VKCountedItemsObject<VKUserOrGroupSource>>> callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            if (offset > 0)
                parameters["offset"] = offset.ToString();
            if (count > 0)
                parameters["count"] = count.ToString();
            parameters["shuffle"] = shuffle ? "1" : "0";

            VKRequestsDispatcher.DispatchRequestToVK<VKCountedItemsObject<VKUserOrGroupSource>>("newsfeed.getSuggestedSources", parameters, callback);
        }

        private void LogDebug(string msg)
        {
            try { Logger.Instance?.Info(msg); } catch { }
            try { Debug.WriteLine(msg); } catch { }
        }

        public void GetNotifications(/*int startTime, int endTime,*/ int offset, string fromStr, int count, Action<VKResponse<NotificationData>> callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            if (count > 0)
                parameters["count"] = count.ToString();
            if (!string.IsNullOrWhiteSpace(fromStr))
                parameters["start_from"] = fromStr;
            parameters["fields"] = "sex,photo_50,photo_100,online,screen_name,first_name_dat,last_name_gen";
            parameters["fields"] += ",is_closed,type,is_admin,is_member,photo_200";

            VKRequestsDispatcher.DispatchRequestToVK<NotificationData>("notifications.get", parameters, (result) =>
            {
                if (result.error.error_code != Enums.VKErrors.None)
                {
                    callback(result);
                    return;
                }

                List<VKGroup> groups = result.response.groups;
                List<VKUser> profiles = result.response.profiles;

                // First pass: try to resolve owner from ParsedFeedback and immediate data
                foreach (VKNotification item in result.response.items)
                {
                    VKBaseDataForGroupOrUser owner = null;
                    long? parsedActorId = null;

                    try
                    {
                        // If ParsedFeedback is a raw JSON string, try to parse primary ids
                        if (item.ParsedFeedback is string raw && !string.IsNullOrWhiteSpace(raw))
                        {
                            try
                            {
                                var token = JToken.Parse(raw);
                                JToken first = null;
                                if (token.Type == JTokenType.Array)
                                    first = token.First;
                                else if (token["items"] != null && token["items"].Type == JTokenType.Array)
                                    first = token["items"].First;
                                else
                                    first = token;

                                if (first != null)
                                {
                                    long? actorId = first.Value<long?>("from_id") ?? first.Value<long?>("owner_id") ?? first.Value<long?>("user_id") ?? first.Value<long?>("id");
                                    if (actorId.HasValue)
                                    {
                                        parsedActorId = actorId.Value;
                                        long rawId = actorId.Value;
                                        if (rawId > 0 && profiles != null)
                                            owner = profiles.Find(u => u.id == rawId);
                                        else if (rawId < 0 && groups != null)
                                            owner = groups.Find(g => g.id == -rawId);
                                    }
                                }
                            }
                            catch { }
                        }

                        // Typed shapes
                        if (owner == null)
                        {
                            if (item.ParsedFeedback is List<FeedbackUser> list && list.Count > 0)
                            {
                                int actorId = list[0].from_id != 0 ? list[0].from_id : list[0].owner_id;
                                parsedActorId = actorId;
                                if (actorId > 0)
                                    owner = profiles?.Find(u => u.id == actorId);
                                else
                                    owner = groups?.Find(g => g.id == -actorId);
                            }
                            else if (item.ParsedFeedback is VKComment comment)
                            {
                                parsedActorId = comment.from_id;
                                if (comment.from_id > 0)
                                    owner = profiles?.Find(u => u.id == comment.from_id);
                                else
                                    owner = groups?.Find(g => g.id == -comment.from_id);
                            }
                            else if (item.ParsedFeedback is VKWallPost post)
                            {
                                parsedActorId = post.from_id != 0 ? post.from_id : post.owner_id;
                                if (post.from_id > 0)
                                    owner = profiles?.Find(u => u.id == post.from_id);
                                else
                                    owner = groups?.Find(g => g.id == -post.from_id);
                            }
                            else if (item.ParsedFeedback is List<FeedbackCopyInfo> info && info.Count > 0)
                            {
                                long rawId = info[0].from_id != 0 ? info[0].from_id : info[0].owner_id;
                                parsedActorId = rawId;
                                if (rawId > 0)
                                    owner = profiles?.Find(u => u.id == (int)rawId);
                                else
                                    owner = groups?.Find(g => g.id == (int)(-rawId));
                            }
                        }
                    }
                    catch { }

                    item.Owner = owner;

                    // Populate TargetUrl from injected raw info (ActionUrl) and convert follow->reply when necessary
                    try
                    {
                        var actionUrl = item.ActionUrl;
                        if (!string.IsNullOrEmpty(actionUrl))
                        {
                            item.TargetUrl = actionUrl;
                            // If action URL indicates a reply to a comment, convert notification type to reply_comment
                            if (item.IsReplyByActionOrHeader())
                            {
                                // only change for ambiguous types like follow/mention which might actually be replies
                                if (item.type == VKNotification.NotificationType.follow || item.type == VKNotification.NotificationType.mention || item.type == VKNotification.NotificationType.wall)
                                {
                                    var old = item.type;
                                    item.type = VKNotification.NotificationType.reply_comment;
                                    LogDebug($"NewsFeedService: auto-changed notification type from {old} to {item.type} based on reply= in ActionUrl/header; actionUrl={actionUrl}");
                                }
                            }

                            // Heuristics: if ActionUrl contains a photo link, convert to like_photo; if contains wall link, convert to like_post
                            try
                            {
                                string low = actionUrl.ToLowerInvariant();
                                var photoMatch = System.Text.RegularExpressions.Regex.Match(low, @"photo-?\d+_\d+");
                                var wallMatch = System.Text.RegularExpressions.Regex.Match(low, @"wall-?\d+_\d+");
                                if ((item.type == VKNotification.NotificationType.follow || item.type == VKNotification.NotificationType.mention || item.type == VKNotification.NotificationType.wall))
                                {
                                    var oldType = item.type;
                                    if (photoMatch.Success)
                                    {
                                        item.type = VKNotification.NotificationType.like_photo;
                                        try { RequestPreviewForNotification(item); } catch { }
                                    }
                                    else if (wallMatch.Success)
                                    {
                                        item.type = VKNotification.NotificationType.like_post;
                                    }
                                    if (oldType != item.type)
                                    {
                                        LogDebug($"NewsFeedService: auto-changed notification type from {oldType} to {item.type} based on ActionUrl='{actionUrl}'");
                                    }
                                }
                            }
                            catch { }

                            // Also inspect parsed action params injected earlier
                            try
                            {
                                var raw = item.RawItem;
                                if (raw != null)
                                {
                                    var replyParam = raw.Value<string>("action_param_reply");
                                    if (!string.IsNullOrEmpty(replyParam) && (item.type == VKNotification.NotificationType.follow || item.type == VKNotification.NotificationType.mention || item.type == VKNotification.NotificationType.wall))
                                    {
                                        var old = item.type;
                                        item.type = VKNotification.NotificationType.reply_comment;
                                        LogDebug($"NewsFeedService: auto-changed type from {old} to {item.type} due to action_param_reply in RawItem");
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                    catch { }

                    // Additional heuristics using RawItem when ActionUrl absent
                    try
                    {
                        if (string.IsNullOrEmpty(item.TargetUrl) && item.RawItem != null)
                        {
                            var mt = item.RawItem.Value<string>("main_item_type");
                            var at = item.RawItem.Value<string>("additional_item_type");
                            var header = item.RawItem.Value<string>("header");
                            var mainAct = item.RawItem.Value<string>("main_action_url");
                            var addAct = item.RawItem.Value<string>("additional_action_url");
                            var topAct = item.RawItem.Value<string>("action_url");

                            if ((item.type == VKNotification.NotificationType.follow || item.type == VKNotification.NotificationType.mention || item.type == VKNotification.NotificationType.wall))
                            {
                                if ((!string.IsNullOrEmpty(mt) && mt.Equals("photo", StringComparison.OrdinalIgnoreCase)) || (!string.IsNullOrEmpty(at) && at.Equals("photo", StringComparison.OrdinalIgnoreCase)))
                                {
                                    var old = item.type;
                                    item.type = VKNotification.NotificationType.like_photo;
                                    LogDebug($"NewsFeedService: auto-changed notification type from {old} to {item.type} based on RawItem main/additional type");
                                    try { RequestPreviewForNotification(item); } catch { }
                                }
                                else if ((!string.IsNullOrEmpty(mt) && (mt.Equals("post", StringComparison.OrdinalIgnoreCase) || mt.Equals("wall", StringComparison.OrdinalIgnoreCase)))
                                      || (!string.IsNullOrEmpty(at) && (at.Equals("post", StringComparison.OrdinalIgnoreCase) || at.Equals("wall", StringComparison.OrdinalIgnoreCase))))
                                {
                                    var old = item.type;
                                    item.type = VKNotification.NotificationType.like_post;
                                    LogDebug($"NewsFeedService: auto-changed notification type from {old} to {item.type} based on RawItem main/additional type");
                                }
                                else
                                {
                                    // Inspect any action URL strings found in RawItem
                                    try
                                    {
                                        string any = (mainAct ?? addAct ?? topAct ?? header ?? "").ToLowerInvariant();
                                        if (!string.IsNullOrEmpty(any))
                                        {
                                            var photoMatch2 = System.Text.RegularExpressions.Regex.Match(any, @"photo-?\d+_\d+");
                                            var wallMatch2 = System.Text.RegularExpressions.Regex.Match(any, @"wall-?\d+_\d+");
                                            if (photoMatch2.Success)
                                            {
                                                var old = item.type;
                                                item.type = VKNotification.NotificationType.like_photo;
                                                LogDebug($"NewsFeedService: auto-changed notification type from {old} to {item.type} based on RawItem action/header containing photo link");
                                                try { RequestPreviewForNotification(item); } catch { }
                                            }
                                            else if (wallMatch2.Success)
                                            {
                                                var old = item.type;
                                                item.type = VKNotification.NotificationType.like_post;
                                                LogDebug($"NewsFeedService: auto-changed notification type from {old} to {item.type} based on RawItem action/header containing wall link");
                                            }
                                            else
                                            {
                                                // header-based textual hint for likes
                                                try
                                                {
                                                    string lowh = (header ?? "").ToLowerInvariant();
                                                    if (!string.IsNullOrEmpty(lowh) && (lowh.Contains("liked") || lowh.Contains("понравил") || lowh.Contains("понравилось") || lowh.Contains("отметил") || lowh.Contains("пометил") || lowh.Contains("лайк") || lowh.Contains("лайкнул")))
                                                    {
                                                        var old = item.type;
                                                        item.type = VKNotification.NotificationType.like_post;
                                                        LogDebug($"NewsFeedService: auto-changed notification type from {old} to {item.type} based on header textual hint");
                                                    }
                                                }
                                                catch { }
                                            }
                                        }
                                    }
                                    catch { }
                                }
                            }
                        }
                    }
                    catch { }

                    // If missing owner but have parsedActorId, fetch user/group (but prefer RawItem hints)
                    if (item.Owner == null && parsedActorId.HasValue)
                    {
                        long aid = parsedActorId.Value;

                        bool treatAsGroup = false;
                        long headerId = 0;
                        try
                        {
                            var ri = item.RawItem;
                            if (ri != null)
                            {
                                var mainType = ri.Value<string>("main_item_type");
                                var addType = ri.Value<string>("additional_item_type");
                                var header = ri.Value<string>("header");
                                if (!string.IsNullOrEmpty(mainType) && string.Equals(mainType, "group", StringComparison.OrdinalIgnoreCase))
                                    treatAsGroup = true;
                                if (!string.IsNullOrEmpty(addType) && string.Equals(addType, "group", StringComparison.OrdinalIgnoreCase))
                                    treatAsGroup = true;
                                if (!string.IsNullOrEmpty(header))
                                {
                                    var m = System.Text.RegularExpressions.Regex.Match(header, "\\[(club|id)(-?\\d+)\\|");
                                    if (m.Success)
                                    {
                                        var prefix = m.Groups[1].Value;
                                        if (string.Equals(prefix, "club", StringComparison.OrdinalIgnoreCase))
                                            treatAsGroup = true;
                                        long.TryParse(m.Groups[2].Value, out headerId);
                                    }
                                }
                            }
                        }
                        catch { }

                        if (treatAsGroup)
                        {
                            uint gid = headerId != 0 ? (uint)Math.Abs(headerId) : (uint)(aid > 0 ? aid : -aid);
                            GroupsService.Instance.GetGroupInfo(gid, true, (res) =>
                            {
                                if (res != null && res.error.error_code == VKErrors.None && res.response != null)
                                {
                                    Execute.ExecuteOnUIThread(() => item.Owner = res.response);
                                }
                            });
                        }
                        else
                        {
                            if (aid > 0)
                            {
                                uint uid = (uint)aid;
                                UsersService.Instance.GetUsers(new List<uint> { uid }, (usersRes) =>
                                {
                                    if (usersRes != null && usersRes.Count > 0)
                                    {
                                        Execute.ExecuteOnUIThread(() => item.Owner = usersRes[0]);
                                    }
                                });
                            }
                            else if (aid < 0)
                            {
                                uint gid = (uint)(-aid);
                                GroupsService.Instance.GetGroupInfo(gid, true, (res) =>
                                {
                                    if (res != null && res.error.error_code == VKErrors.None && res.response != null)
                                    {
                                        Execute.ExecuteOnUIThread(() => item.Owner = res.response);
                                    }
                                });
                            }
                        }
                    }
                }

                // Second fallback: use injected RawItem header/main/additional to set owner for remaining items
                // Enqueue preview requests for already-classified like_photo items, but prefer extracting preview from context first
                try
                {
                    foreach (var it in result.response.items)
                    {
                        try
                        {
                            if (it.type == VKNotification.NotificationType.like_photo)
                            {
                                // Try to obtain preview from raw_item, parsed parent, owner or provided profiles/groups
                                if (TryExtractPreviewFromContext(it, result.response.profiles, result.response.groups, out string found))
                                {
                                    try { it.PreviewImageUrl = found; } catch { }
                                    try
                                    {
                                        var key = GeneratePreviewCacheKey(it);
                                        if (!string.IsNullOrEmpty(key)) lock (_previewLock) { _previewCache[key] = found; }
                                    }
                                    catch { }
                                }
                                else
                                {
                                    RequestPreviewForNotification(it);
                                }
                            }
                        }
                        catch { }
                    }
                }
                catch { }

                // Third pass: batch fetch any remaining missing users/groups using classification preferring RawItem hints
                try
                {
                    var missingUserIds = new HashSet<uint>();
                    var missingGroupIds = new HashSet<uint>();

                    foreach (VKNotification item in result.response.items)
                    {
                        if (item.Owner != null)
                            continue;

                        long? parsedActor = ExtractActorId(item);
                        if (!parsedActor.HasValue)
                            continue;

                        long aid = parsedActor.Value;

                        bool preferGroup = false;
                        try
                        {
                            var r = item.RawItem;
                            if (r != null)
                            {
                                var mt = r.Value<string>("main_item_type");
                                var at = r.Value<string>("additional_item_type");
                                var header = r.Value<string>("header");
                                if (!string.IsNullOrEmpty(mt) && string.Equals(mt, "group", StringComparison.OrdinalIgnoreCase))
                                    preferGroup = true;
                                if (!string.IsNullOrEmpty(at) && string.Equals(at, "group", StringComparison.OrdinalIgnoreCase))
                                    preferGroup = true;
                                if (!string.IsNullOrEmpty(header))
                                {
                                    var m = System.Text.RegularExpressions.Regex.Match(header, "\\[(club|id)(-?\\d+)\\|");
                                    if (m.Success && string.Equals(m.Groups[1].Value, "club", StringComparison.OrdinalIgnoreCase))
                                        preferGroup = true;
                                }
                            }
                        }
                        catch { }

                        if (aid > 0)
                        {
                            uint uid = (uint)aid;
                            if (preferGroup)
                            {
                                uint gid = uid;
                                if (!(result.response.groups?.Any(g => g.id == gid) ?? false) && GroupsService.Instance.GetCachedGroup(gid) == null)
                                    missingGroupIds.Add(gid);
                            }
                            else
                            {
                                if (!(result.response.profiles?.Any(p => p.id == uid) ?? false) && UsersService.Instance.GetCachedUser(uid) == null)
                                    missingUserIds.Add(uid);
                            }
                        }
                        else if (aid < 0)
                        {
                            uint gid = (uint)(-aid);
                            if (!(result.response.groups?.Any(g => g.id == gid) ?? false) && GroupsService.Instance.GetCachedGroup(gid) == null)
                                missingGroupIds.Add(gid);
                        }
                    }

                    if (missingUserIds.Count == 0 && missingGroupIds.Count == 0)
                    {
                        callback(result);
                        return;
                    }

                    int pending = 0;
                    int invoked = 0;
                    System.Threading.Timer timer = null;

                    Action invokeOnce = () =>
                    {
                        if (System.Threading.Interlocked.Exchange(ref invoked, 1) == 0)
                        {
                            try { timer?.Dispose(); } catch { }

                            // ensure lists
                            result.response.profiles = result.response.profiles ?? new List<VKUser>();
                            result.response.groups = result.response.groups ?? new List<VKGroup>();

                            // merge cached
                            foreach (var uid in missingUserIds.ToList())
                            {
                                var cu = UsersService.Instance.GetCachedUser(uid);
                                if (cu != null && !result.response.profiles.Any(p => p.id == cu.id))
                                    result.response.profiles.Add(cu);
                            }
                            foreach (var gid in missingGroupIds.ToList())
                            {
                                var cg = GroupsService.Instance.GetCachedGroup(gid);
                                if (cg != null && !result.response.groups.Any(g => g.id == cg.id))
                                    result.response.groups.Add(cg);
                            }

                            // final owner assignment on UI thread
                            Execute.ExecuteOnUIThread(() =>
                            {
                                foreach (VKNotification item in result.response.items)
                                {
                                    if (item.Owner != null)
                                        continue;
                                    long? pa = ExtractActorId(item);
                                    if (!pa.HasValue) continue;
                                    long a = pa.Value;
                                    if (a > 0)
                                    {
                                        uint u = (uint)a;
                                        var up = result.response.profiles?.Find(p => p.id == u) ?? UsersService.Instance.GetCachedUser(u);
                                        if (up != null) item.Owner = up;
                                    }
                                    else if (a < 0)
                                    {
                                        uint g = (uint)(-a);
                                        var gp = result.response.groups?.Find(gr => gr.id == g) ?? GroupsService.Instance.GetCachedGroup(g);
                                        if (gp != null) item.Owner = gp;
                                    }
                                }

                                callback(result);
                            });
                        }
                    };

                    Action tryFinish = () => { if (System.Threading.Interlocked.Decrement(ref pending) == 0) invokeOnce(); };

                    if (missingUserIds.Count > 0) pending++;
                    if (missingGroupIds.Count > 0) pending++;

                    int timeoutMs = 3000;
                    timer = new System.Threading.Timer((s) => { Debug.WriteLine("NewsFeedService: batch fetch timeout reached"); invokeOnce(); }, null, timeoutMs, System.Threading.Timeout.Infinite);

                    if (missingUserIds.Count > 0)
                    {
                        UsersService.Instance.GetUsers(missingUserIds.ToList(), (usersRes) =>
                        {
                            try
                            {
                                if (usersRes != null && usersRes.Count > 0)
                                {
                                    UsersService.Instance.SetCachedUsers(usersRes);
                                    result.response.profiles = result.response.profiles ?? new List<VKUser>();
                                    foreach (var u in usersRes) if (!result.response.profiles.Any(p => p.id == u.id)) result.response.profiles.Add(u);
                                }
                            }
                            catch (Exception ex) { Debug.WriteLine($"NewsFeedService: users batch merge failed: {ex}"); }
                            finally { tryFinish(); }
                        });
                    }

                    if (missingGroupIds.Count > 0)
                    {
                        var gpParams = new Dictionary<string, string> { ["group_ids"] = string.Join(",", missingGroupIds), ["fields"] = "photo_50,photo_100" };
                        VKRequestsDispatcher.DispatchRequestToVK<List<VKGroup>>("groups.getById", gpParams, (groupsRes) =>
                        {
                            try
                            {
                                if (groupsRes != null && groupsRes.error.error_code == Enums.VKErrors.None && groupsRes.response != null)
                                {
                                    GroupsService.Instance.SetCachedGroups(groupsRes.response);
                                    result.response.groups = result.response.groups ?? new List<VKGroup>();
                                    foreach (var g in groupsRes.response) if (!result.response.groups.Any(gr => gr.id == g.id)) result.response.groups.Add(g);
                                }
                            }
                            catch (Exception ex) { Debug.WriteLine($"NewsFeedService: groups batch merge failed: {ex}"); }
                            finally { tryFinish(); }
                        });
                    }

                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"NewsFeedService: batch fetch failed: {ex}");
                    callback(result);
                }

            }, (jsonStr) =>
            {
                try { Debug.WriteLine($"NewsFeedService: raw notifications.get json (truncated 20000) -> {(jsonStr == null ? "" : (jsonStr.Length > 20000 ? jsonStr.Substring(0, 20000) + "..." : jsonStr))}"); } catch { }
                try { Debug.WriteLine($"NewsFeedService: raw notifications.get response length={(jsonStr == null ? 0 : jsonStr.Length)}"); } catch { }

                // inject lightweight raw info for each item: main_item.object_id, main_item.type, additional_item.object_id, additional_item.type, header
                try
                {
                    var root = JObject.Parse(jsonStr);
                    var resp = root["response"] as JObject;
                    if (resp != null && resp["items"] is JArray itemsArr)
                    {
                        foreach (var it in itemsArr.OfType<JObject>())
                        {
                            var info = new JObject();
                            try
                            {
                                var main = it["main_item"] as JObject;
                                if (main != null)
                                {
                                    var objId = main.Value<string>("object_id");
                                    if (!string.IsNullOrEmpty(objId)) info["main_object_id"] = objId;
                                    try { var mainType = main.Value<string>("type"); if (!string.IsNullOrEmpty(mainType)) info["main_item_type"] = mainType; } catch { }
                                    // capture main_item action.url if present
                                    try { var mainAct = main["action"] as JObject; var mainActUrl = mainAct?.Value<string>("url"); if (!string.IsNullOrEmpty(mainActUrl)) info["main_action_url"] = mainActUrl; } catch { }
                                }
                            }
                            catch { }
                            try
                            {
                                var add = it["additional_item"] as JObject;
                                if (add != null)
                                {
                                    var addId = add.Value<string>("object_id");
                                    if (!string.IsNullOrEmpty(addId)) info["additional_object_id"] = addId;
                                    try { var addType = add.Value<string>("type"); if (!string.IsNullOrEmpty(addType)) info["additional_item_type"] = addType; } catch { }

                                    // also capture additional_item action.url if present
                                    try { var addAct = add["action"] as JObject; var addActUrl = addAct?.Value<string>("url"); if (!string.IsNullOrEmpty(addActUrl)) info["additional_action_url"] = addActUrl; } catch { }
                                }
                            }
                            catch { }
                            try
                            {
                                var header = it.Value<string>("header");
                                if (!string.IsNullOrEmpty(header))
                                {
                                    info["header"] = header;

                                    // Try to extract explicit wall link like vk.ru/wall-<owner>_<post> from header and inject as main_object_id
                                    try
                                    {
                                        var wallMatch = System.Text.RegularExpressions.Regex.Match(header, @"wall-?(-?\d+)_([0-9]+)");
                                        if (wallMatch.Success)
                                        {
                                            string ownerStr = wallMatch.Groups[1].Value;
                                            string postStr = wallMatch.Groups[2].Value;
                                            // if header mentions club, prefer negative owner to indicate group
                                            string signedOwner = ownerStr;
                                            if (header.IndexOf("[club", StringComparison.OrdinalIgnoreCase) >= 0 || header.IndexOf("club", StringComparison.OrdinalIgnoreCase) >= 0)
                                                signedOwner = (ownerStr.StartsWith("-") ? ownerStr : ("-" + ownerStr));

                                            info["main_object_id"] = signedOwner + "_" + postStr;
                                        }
                                    }
                                    catch { }
                                }
                            }
                            catch { }

                            // capture action url if present on top-level action
                            try { var act = it["action"] as JObject; var actUrl = act?.Value<string>("url"); if (!string.IsNullOrEmpty(actUrl)) info["action_url"] = actUrl; } catch { }

                            // Parse query parameters from any discovered action URL and inject some common params
                            try
                            {
                                string anyUrl = info.Value<string>("action_url") ?? info.Value<string>("additional_action_url") ?? info.Value<string>("main_action_url");
                                if (!string.IsNullOrEmpty(anyUrl))
                                {
                                    try
                                    {
                                        anyUrl = anyUrl.Replace("\\/","/").Replace("&amp;","&").Trim();
                                        try { anyUrl = System.Net.WebUtility.UrlDecode(anyUrl); } catch { }
                                        var q = anyUrl.ParseQueryString();
                                        foreach (var key in new[] { "reply", "aid", "list", "from", "query" })
                                        {
                                            if (q.ContainsKey(key))
                                                info[$"action_param_{key}"] = q[key];
                                        }
                                    }
                                    catch { }
                                }
                            }
                            catch { }

                            if (info.HasValues) it["_raw_item"] = info;
                        }

                        jsonStr = root.ToString(Newtonsoft.Json.Formatting.None);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"NewsFeedService: failed to inject raw item info: {ex}");
                }

                jsonStr = VKRequestsDispatcher.GetArrayCountAndRemove(jsonStr, "items", out _);
                jsonStr = VKRequestsDispatcher.FixFalseArray(jsonStr, "profiles", false);
                jsonStr = VKRequestsDispatcher.FixFalseArray(jsonStr, "groups", false);
                return jsonStr;
            });
        }

        private long? ExtractActorId(VKNotification item)
        {
            long? parsedActor = null;
            try
            {
                if (item.ParsedFeedback is VKComment c1)
                    parsedActor = c1.from_id;
                else if (item.ParsedFeedback is VKWallPost p1)
                    parsedActor = p1.from_id != 0 ? p1.from_id : p1.owner_id;
                else if (item.ParsedFeedback is List<FeedbackUser> fu && fu.Count > 0)
                    parsedActor = fu[0].from_id != 0 ? fu[0].from_id : fu[0].owner_id;
                else if (item.ParsedFeedback is List<FeedbackCopyInfo> fc && fc.Count > 0)
                    parsedActor = fc[0].from_id != 0 ? fc[0].from_id : fc[0].owner_id;
                else if (item.ParsedFeedback is string rawStr && !string.IsNullOrWhiteSpace(rawStr))
                {
                    try { var tok = JToken.Parse(rawStr); JToken f = tok.Type == JTokenType.Array ? tok.First : (tok["items"] != null && tok["items"].Type == JTokenType.Array ? tok["items"].First : tok); parsedActor = f?.Value<long?>("from_id") ?? f?.Value<long?>("owner_id") ?? f?.Value<long?>("user_id") ?? f?.Value<long?>("id"); } catch { }

                    // regex fallback: try find any id-like property if JSON parsing didn't yield actor
                    if (!parsedActor.HasValue)
                    {
                        try
                        {
                            var m = System.Text.RegularExpressions.Regex.Match(rawStr, "\\\"(?:from_id|owner_id|user_id|id)\\\"\\s*:\\s*(-?\\d+)");
                            if (m.Success)
                                parsedActor = long.Parse(m.Groups[1].Value);
                        }
                        catch { }
                    }
                }
            }
            catch { }

            return parsedActor;
        }

        // Simple DTO to match the shape returned by notifications.get after preprocessing
        public class NotificationData
        {
            public List<VKNotification> items { get; set; }
            public List<VKUser> profiles { get; set; }
            public List<VKGroup> groups { get; set; }
            public string next_from { get; set; }
        }

        private readonly Dictionary<string, string> _previewCache = new Dictionary<string, string>();
        private readonly Queue<VKNotification> _previewQueue = new Queue<VKNotification>();
        private readonly object _previewLock = new object();
        private bool _previewWorkerRunning = false;

        private void EnqueuePreview(VKNotification item)
        {
            if (item == null) return;
            if (item.type != VKNotification.NotificationType.like_photo)
                return;

            string cacheKey = GeneratePreviewCacheKey(item);
            if (!string.IsNullOrEmpty(cacheKey))
            {
                lock (_previewLock)
                {
                    if (_previewCache.TryGetValue(cacheKey, out string cachedUrl))
                    {
                        try { item.PreviewImageUrl = cachedUrl; } catch { }
                        return;
                    }
                }
            }

            lock (_previewLock)
            {
                _previewQueue.Enqueue(item);
                if (!_previewWorkerRunning)
                {
                    _previewWorkerRunning = true;
                    System.Threading.Tasks.Task.Run(async () => await PreviewWorker());
                }
            }
        }

        private string GeneratePreviewCacheKey(VKNotification item)
        {
            try
            {
                var ri = item.RawItem;
                if (ri != null)
                {
                    var mainObj = ri.Value<string>("main_object_id");
                    if (!string.IsNullOrEmpty(mainObj)) return "preview:" + mainObj;
                    var addObj = ri.Value<string>("additional_object_id");
                    if (!string.IsNullOrEmpty(addObj)) return "preview:" + addObj;
                    var act = ri.Value<string>("action_url");
                    if (!string.IsNullOrEmpty(act)) return "preview_act:" + act;
                }
                if (!string.IsNullOrEmpty(item.TargetUrl)) return "preview_act:" + item.TargetUrl;
            }
            catch { }
            return null;
        }

        private async System.Threading.Tasks.Task PreviewWorker()
        {
            while (true)
            {
                VKNotification n = null;
                lock (_previewLock)
                {
                    if (_previewQueue.Count == 0)
                    {
                        _previewWorkerRunning = false;
                        return;
                    }
                    n = _previewQueue.Dequeue();
                }

                try
                {
                    string mainObj = n.RawItem?.Value<string>("main_object_id");
                    string addObj = n.RawItem?.Value<string>("additional_object_id");
                    string obj = mainObj ?? addObj;
                    string cacheKey = GeneratePreviewCacheKey(n);
                    string previewUrl = null;

                    if (!string.IsNullOrEmpty(obj))
                    {
                        // First try to extract direct image link from RawItem
                        if (TryExtractPreviewFromRaw(n, out string direct))
                        {
                            previewUrl = direct;
                        }
                        else
                        {
                            var parts = obj.Split('_');
                            if (parts.Length >= 2 && long.TryParse(parts[0], out long ownerPart) && long.TryParse(parts[1], out long idPart))
                            {
                                int ownerId = (int)ownerPart;
                                uint pid = (uint)idPart;

                                var tcs = new System.Threading.Tasks.TaskCompletionSource<string>();
                                var cts = new CancellationTokenSource(2500);
                                cts.Token.Register(() => tcs.TrySetResult(null));

                                PhotosService.Instance.GetPhotoWithFullInfo(ownerId, pid, null, 0, (res) =>
                                {
                                    try
                                    {
                                        if (res != null && res.error.error_code == Enums.VKErrors.None && res.response != null && res.response.Photo != null)
                                        {
                                            var p = res.response.Photo;
                                            string url = !string.IsNullOrEmpty(p.photo_130) ? p.photo_130 : (!string.IsNullOrEmpty(p.photo_200) ? p.photo_200 : p.photo_604);
                                            tcs.TrySetResult(url);
                                        }
                                        else
                                            tcs.TrySetResult(null);
                                    }
                                    catch { tcs.TrySetResult(null); }
                                });

                                try { previewUrl = await tcs.Task; } catch { previewUrl = null; }
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(previewUrl) && !string.IsNullOrEmpty(cacheKey))
                    {
                        lock (_previewLock) { try { _previewCache[cacheKey] = previewUrl; } catch { } }
                        Execute.ExecuteOnUIThread(() => { try { n.PreviewImageUrl = previewUrl; } catch { } });
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("NewsFeedService.PreviewWorker error: " + ex);
                }

                await System.Threading.Tasks.Task.Delay(50);
            }
        }

        private bool TryExtractPreviewFromRaw(VKNotification n, out string previewUrl)
        {
            previewUrl = null;
            try
            {
                var ri = n.RawItem;
                if (ri == null)
                    return false;

                // check likely fields for direct image URL
                var candidates = new[] {
                    ri.Value<string>("action_url"),
                    ri.Value<string>("main_action_url"),
                    ri.Value<string>("additional_action_url"),
                    ri.Value<string>("header")
                };

                foreach (var c in candidates)
                {
                    if (string.IsNullOrEmpty(c))
                        continue;
                    string s = c.Replace("\\/","/").Replace("&amp;","&").Trim();
                    try { s = System.Net.WebUtility.UrlDecode(s); } catch { }

                    // Find explicit image URL
                    var m = System.Text.RegularExpressions.Regex.Match(s, @"https?://[^\s\]\)']+?(?:\.(?:jpg|jpeg|png|gif))(?:\?[^\s\]\)]*)?", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        previewUrl = m.Value;
                        return true;
                    }

                    // If header contains [https://...] pattern we already decode ActionUrl elsewhere; try to extract any http link
                    var m2 = System.Text.RegularExpressions.Regex.Match(s, @"https?://[^\|\]\s]+", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (m2.Success && (m2.Value.IndexOf(".jpg", StringComparison.OrdinalIgnoreCase) >= 0 || m2.Value.IndexOf(".png", StringComparison.OrdinalIgnoreCase) >= 0 || m2.Value.IndexOf(".jpeg", StringComparison.OrdinalIgnoreCase) >= 0 || m2.Value.IndexOf(".gif", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        previewUrl = m2.Value.TrimStart('[');
                        return true;
                    }
                }

                // Optionally, some raw items might contain object id like owner_photo which directly points to a photo resource "<owner>_<photoId>" in main_object_id
                var mainObj = ri.Value<string>("main_object_id") ?? ri.Value<string>("additional_object_id");
                if (!string.IsNullOrEmpty(mainObj))
                {
                    // If mainObj looks like owner_photo (photo owner_photoId) maybe it's already the photo id; but we can't derive direct image link without API — return false to let worker call API
                    // However some items may include direct photo url in other fields; already checked.
                }
            }
            catch { }
            return false;
        }

        private bool TryExtractPreviewFromContext(VKNotification n, List<VKUser> profiles, List<VKGroup> groups, out string previewUrl)
        {
            previewUrl = null;
            try
            {
                // Try raw fields first
                if (TryExtractPreviewFromRaw(n, out previewUrl))
                    return true;

                // If ParsedParent is a VKPhoto, use its sizes
                try
                {
                    var pp = n.ParsedParent as VKPhoto;
                    if (pp != null)
                    {
                        var url = !string.IsNullOrEmpty(pp.photo_130) ? pp.photo_130 : (!string.IsNullOrEmpty(pp.photo_200) ? pp.photo_200 : pp.photo_604);
                        if (!string.IsNullOrEmpty(url))
                        {
                            previewUrl = url;
                            return true;
                        }
                    }
                }
                catch { }

                // If Owner present, try owner's avatar (photo_100/photo_200)
                try
                {
                    if (n.Owner is VKUser u)
                    {
                        var url = !string.IsNullOrEmpty(u.photo_100) ? u.photo_100 : (!string.IsNullOrEmpty(u.photo_200) ? u.photo_200 : null);
                        if (!string.IsNullOrEmpty(url)) { previewUrl = url; return true; }
                    }
                    else if (n.Owner is VKGroup g)
                    {
                        var url = !string.IsNullOrEmpty(g.photo_200) ? g.photo_200 : (!string.IsNullOrEmpty(g.photo_100) ? g.photo_100 : null);
                        if (!string.IsNullOrEmpty(url)) { previewUrl = url; return true; }
                    }
                }
                catch { }

                // Fallback: try to locate actor in provided profiles/groups lists
                try
                {
                    long? actor = ExtractActorId(n);
                    if (actor.HasValue)
                    {
                        long a = actor.Value;
                        if (a > 0 && profiles != null)
                        {
                            var pu = profiles.Find(p => p.id == (uint)a);
                            if (pu != null)
                            {
                                var url = !string.IsNullOrEmpty(pu.photo_100) ? pu.photo_100 : (!string.IsNullOrEmpty(pu.photo_200) ? pu.photo_200 : null);
                                if (!string.IsNullOrEmpty(url)) { previewUrl = url; return true; }
                            }
                        }
                        else if (a < 0 && groups != null)
                        {
                            var gg = groups.Find(g => g.id == (uint)(-a));
                            if (gg != null)
                            {
                                var url = !string.IsNullOrEmpty(gg.photo_200) ? gg.photo_200 : (!string.IsNullOrEmpty(gg.photo_100) ? gg.photo_100 : null);
                                if (!string.IsNullOrEmpty(url)) { previewUrl = url; return true; }
                            }
                        }
                    }
                }
                catch { }
            }
            catch { }
            return false;
        }

        private void RequestPreviewForNotification(VKNotification item)
        {
            try { EnqueuePreview(item); } catch { }
        }

    }
}

