using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;
using LunaVK.Core.DataObjects;
using LunaVK.Core.Network;
using LunaVK.Core.Enums;
using LunaVK.Core.Framework;
using LunaVK.Core.Library;
using System.Threading.Tasks;
using System.Linq;

namespace LunaVK.Core.ViewModels
{
    public class NotificationsViewModel : ISupportUpDownIncrementalLoading
    {
        public ObservableCollection<VKNotification> Notifications { get; private set; }

        public bool HasMoreUpItems
        {
            get { return false; }
        }

        public bool HasMoreDownItems
        {
            get
            {
                if (this.nextFrom == null && this.Notifications.Count == 0)
                    return true;

                return !string.IsNullOrEmpty(this.nextFrom);
            }
        }

        private string nextFrom = null;

        public NotificationsViewModel()
        {
            this.Notifications = new ObservableCollection<VKNotification>();
        }

        public async Task LoadUpAsync()
        {

        }

        public Action<ProfileLoadingStatus> LoadingStatusUpdated;

        public async Task<object> Reload()
        {
            this.nextFrom = "";
            this.Notifications.Clear();
            this.LoadingStatusUpdated?.Invoke(ProfileLoadingStatus.Reloading);
            await LoadDownAsync(true);

            await Task.Delay(1000);

            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters = new Dictionary<string, string>();
            var temp2 = await RequestsDispatcher.GetResponse<int>("notifications.markAsViewed", parameters);

            return null;
        }

        public async Task LoadDownAsync(bool InReload = false)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();

            if (!String.IsNullOrEmpty(this.nextFrom))
            {
                parameters["start_from"] = this.nextFrom;
                parameters["count"] = "30";
            }
            else
            {
                parameters["count"] = "15";
            }

            VKResponse<NotificationData> temp = await RequestsDispatcher.GetResponse<NotificationData>("notifications.get", parameters);

            if (temp.error.error_code == VKErrors.None)
            {
                this.nextFrom = temp.response.next_from;

                GroupsService.Instance.SetCachedGroups(temp.response.groups);
                UsersService.Instance.SetCachedUsers(temp.response.profiles);

                var profiles = temp.response.profiles ?? new List<VKUser>();
                var groups = temp.response.groups ?? new List<VKGroup>();

                // Try fill Owner from response lists or cache
                foreach (VKNotification p in temp.response.items)
                {
                    if (p.Owner != null)
                        continue;

                    long? aid = null;
                    try { aid = NewsFeedService.ExtractActorId(p); } catch { aid = null; }

                    if (aid.HasValue)
                    {
                        if (aid.Value > 0)
                        {
                            uint uid = (uint)aid.Value;
                            var up = profiles.FirstOrDefault(x => x.id == uid);
                            if (up != null)
                            {
                                p.Owner = up;
                                continue;
                            }

                            var cached = UsersService.Instance.GetCachedUser(uid);
                            if (cached != null)
                            {
                                p.Owner = cached;
                                continue;
                            }
                        }
                        else if (aid.Value < 0)
                        {
                            uint gid = (uint)(-aid.Value);
                            var gp = groups.FirstOrDefault(x => x.id == gid);
                            if (gp != null)
                            {
                                p.Owner = gp;
                                continue;
                            }

                            var cachedGroup = GroupsService.Instance.GetCachedGroup(gid);
                            if (cachedGroup != null)
                            {
                                p.Owner = cachedGroup;
                                continue;
                            }
                        }
                    }
                }

                // collect missing ids
                var missingUserIds = new HashSet<uint>();
                var missingGroupIds = new HashSet<uint>();
                foreach (var p in temp.response.items)
                {
                    if (p.Owner != null)
                        continue;
                    long? aid = null;
                    try { aid = NewsFeedService.ExtractActorId(p); } catch { aid = null; }
                    if (!aid.HasValue)
                        continue;
                    if (aid.Value > 0)
                    {
                        uint uid = (uint)aid.Value;
                        if (!UsersService.Instance.GetCachedUser(uid).IsNotNull())
                            missingUserIds.Add(uid);
                    }
                    else if (aid.Value < 0)
                    {
                        uint gid = (uint)(-aid.Value);
                        if (!GroupsService.Instance.GetCachedGroup(gid).IsNotNull())
                            missingGroupIds.Add(gid);
                    }
                }

                int pending = 0;
                int invoked = 0;

                Action invokeOnce = () =>
                {
                    if (System.Threading.Interlocked.Exchange(ref invoked, 1) == 0)
                    {
                        // assign Owners from merged caches
                        foreach (var p in temp.response.items)
                        {
                            if (p.Owner != null)
                                continue;
                            long? aid = null;
                            try { aid = NewsFeedService.ExtractActorId(p); } catch { aid = null; }
                            if (!aid.HasValue)
                                continue;
                            if (aid.Value > 0)
                            {
                                uint uid = (uint)aid.Value;
                                var u = UsersService.Instance.GetCachedUser(uid);
                                if (u != null)
                                    p.Owner = u;
                            }
                            else if (aid.Value < 0)
                            {
                                uint gid = (uint)(-aid.Value);
                                var g = GroupsService.Instance.GetCachedGroup(gid);
                                if (g != null)
                                    p.Owner = g;
                            }
                        }

                        // add items to collection on UI thread
                        Execute.ExecuteOnUIThread(() =>
                        {
                            foreach (VKNotification p in temp.response.items)
                                this.Notifications.Add(p);
                        });
                    }
                };

                if (missingUserIds.Count == 0 && missingGroupIds.Count == 0)
                {
                    // nothing to fetch
                    foreach (VKNotification p in temp.response.items)
                        this.Notifications.Add(p);
                    return;
                }

                if (missingUserIds.Count > 0)
                {
                    pending++;
                    UsersService.Instance.GetUsers(missingUserIds.ToList(), (usersRes) =>
                    {
                        if (usersRes != null && usersRes.Count > 0)
                        {
                            UsersService.Instance.SetCachedUsers(usersRes);
                        }
                        if (System.Threading.Interlocked.Decrement(ref pending) == 0)
                            invokeOnce();
                    });
                }

                if (missingGroupIds.Count > 0)
                {
                    pending++;
                    Dictionary<string, string> gpParams = new Dictionary<string, string>();
                    gpParams["group_ids"] = string.Join(",", missingGroupIds);
                    gpParams["fields"] = "photo_50,photo_100";
                    VKRequestsDispatcher.DispatchRequestToVK<List<VKGroup>>("groups.getById", gpParams, (groupsRes) =>
                    {
                        if (groupsRes != null && groupsRes.error.error_code == VKErrors.None && groupsRes.response != null)
                        {
                            GroupsService.Instance.SetCachedGroups(groupsRes.response);
                        }
                        if (System.Threading.Interlocked.Decrement(ref pending) == 0)
                            invokeOnce();
                    });
                }

            }

        }

        public class NotificationData
        {
            public List<VKNotification> items { get; set; }
            public List<VKUser> profiles { get; set; }
            public List<VKGroup> groups { get; set; }
            public string next_from { get; set; }
        }
    }

    static class Extensions
    {
        public static bool IsNotNull<T>(this T obj) where T: class
        {
            return obj != null;
        }
    }
}
