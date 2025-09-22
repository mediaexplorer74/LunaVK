using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using LunaVK.Framework;
using System.Text.RegularExpressions;
using LunaVK.Core.DataObjects;
using LunaVK.Core;
using LunaVK.Core.Enums;
using LunaVK.Core.Utils;
using System.Linq;
using LunaVK.ViewModels;
using LunaVK.Core.Library;
using LunaVK.Pages;
using System.Diagnostics;
using LunaVK.Core.Framework;
using LunaVK.UC;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Xaml.Media;
using Windows.Foundation.Metadata;

namespace LunaVK.Library
{
    public partial class NavigatorImpl
    {
        private static NavigatorImpl _instance;
        private bool _isNavigating = false;

        // Store last attempted navigation URI for debugging purposes
        public string LastAttemptedUri { get; private set; }

        public static NavigatorImpl Instance
        {
            get
            {
                if (NavigatorImpl._instance == null)
                {
                    NavigatorImpl._instance = new NavigatorImpl();
                    CustomFrame.Instance.Navigating += NavigatorImpl._instance.NavigationService_Navigating;
                }
                return NavigatorImpl._instance;
            }
        }

        void NavigationService_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            if (e.Parameter == null && CustomFrame.Instance.Content != null)//страница без параметров
            {

                string temp = CustomFrame.Instance.Content.ToString();
                if (temp == e.SourcePageType.FullName)
                {
                    e.Cancel = true;
                    return;
                }
            }
        }

        public void NavigateToAudio(int ownerId, string ownerName)
        {
            Dictionary<string, object> QueryString = new Dictionary<string, object>();
            QueryString.Add("OwnerId", ownerId);
            QueryString.Add("OwnerName", ownerName);
            this.Navigate(typeof(MusicPage), QueryString);
        }
        
        public void NavigateToConversations(uint? groupId = null)
        {
            this.NavigateToConversation(0, groupId);
        }
        
        public void NavigateToConversation(int peerId, uint? groupId = null)
        {
            if(CustomFrame.Instance.Content is DialogsConversationPage2 page)
            {
                if(peerId==0)
                {
                    page.BackAction();
                }
                else
                {
                    page.SelectConversation(peerId);
                }
                
            }
            else
            {
                Dictionary<string, int> QueryString = new Dictionary<string, int>();
                if(peerId!=0)
                    QueryString["PeerId"] = peerId;
                if(groupId.HasValue)
                    QueryString["GroupId"] = (int)groupId.Value;
                this.Navigate(typeof(DialogsConversationPage2), QueryString);
            }
        }

        /// <summary>
        /// Навигация в сообщество или пользователя
        /// </summary>
        /// <param name="Id"></param>
        public void NavigateToProfilePage(int Id)
        {            
            if(Id<0)
                this.Navigate(typeof(LunaVK.Pages.Group.GroupPage), (uint)(-Id));
            else
                this.Navigate(typeof(ProfilePage), (uint)Id);
        }

        public void NavigateToFeedback()
        {
            this.Navigate(typeof(NotificationsPage));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id">ИД чата</param>
        public void NavigateToChatEditPage(int id)
        {
            //Dictionary<string, int> QueryString = new Dictionary<string, long>();
            //QueryString.Add("Id", Id);
            this.Navigate(typeof(ChatEditPage), id);
        }

        public void NavigateToFriends(int userId, string userName = null)
        {
            Dictionary<string, object> QueryString = new Dictionary<string, object>();
            QueryString.Add("Id", userId);
            if(userName!=null)
                QueryString.Add("UserName", userName);
            this.Navigate(typeof(FriendsPage), QueryString);
        }

        public void NavigateToGroups(int userId)
        {
            Dictionary<string, int> QueryString = new Dictionary<string, int>();
            QueryString.Add("Id", userId);
            this.Navigate(typeof(GroupsPage), QueryString);
        }

        /// <summary>
        /// На страницу с фото-альбомами
        /// </summary>
        /// <param name="userId"></param>
        public void NavigateToPhotoAlbums(int ownerId, string ownerName)
        {
            Dictionary<string, object> QueryString = new Dictionary<string, object>();
            QueryString.Add("Id", ownerId);
            QueryString.Add("OwnerName", ownerName);
            this.Navigate(typeof(PhotoAlbumPage), QueryString);
        }

        public void NavigateToPhotosOfAlbum(int ownerId, int albumId, string albumName)
        {
            Dictionary<string, object> QueryString = new Dictionary<string, object>();
            QueryString.Add("OwnerId", ownerId);
            QueryString.Add("AlbumId", albumId);
            QueryString.Add("AlbumName", albumName);
            this.Navigate(typeof(PhotosPage), QueryString);
        }

        public void NavigateToAllPhotos(int ownerId, string ownerName)
        {
            Dictionary<string, object> QueryString = new Dictionary<string, object>();
            QueryString.Add("Id", ownerId);
            QueryString.Add("OwnerName", ownerName);
            this.Navigate(typeof(AllPhotosPage), QueryString);
        }

        public void NavigateToVideoCatalog()
        {
            this.Navigate(typeof(VideoCatalogPage));
        }

        /// <summary>
        /// Переход к списку видео у группы/пользователя
        /// </summary>
        /// <param name="userOrGroupId"></param>
        public void NavigateToVideos(int ownerId, string ownerName = "")
        {
            Dictionary<string, object> QueryString = new Dictionary<string, object>();
            QueryString.Add("OwnerId", ownerId);
            QueryString.Add("OwnerName", ownerName);
            //this.Navigate(typeof(Pages.Group.GroupVideosPage),QueryString);
            this.Navigate(typeof(Pages.VideoAlbumsListPage), QueryString);
        }
        /*
        public void NavigateToVideoAlbumsList(int ownerId,string ownerName = "")
        {
            Dictionary<string, object> QueryString = new Dictionary<string, object>();
            QueryString.Add("OwnerId", ownerId);
            QueryString.Add("OwnerName", ownerName);
            this.Navigate(typeof(Pages.VideoAlbumsListPage), QueryString);
        }
        */
        public void NavigateToVideoAlbum(int albumId, string albumName, int ownerId = 0)
        {
            //string navStr = string.Format("/VKClient.Video;component/VideoPage.xaml?PickMode={0}&UserOrGroupId={1}&IsGroup={2}&AlbumId={3}&AlbumName={4}", pickMode.ToString(), userOrGroupId, isGroup, albumId.ToString(), Extensions.ForURL(albumName));
            


            Dictionary<string, object> QueryString = new Dictionary<string, object>();
            QueryString.Add("AlbumId", albumId);
            QueryString.Add("OwnerId", ownerId);
            QueryString.Add("AlbumName", albumName);
            this.Navigate(typeof(VideoPage), QueryString);
        }

        public void NavigateToSettings()
        {
            this.Navigate(typeof(SettingsPage));
        }

        public void NavigateToFavorites()
        {
            this.Navigate(typeof(FavoritesPage));
        }

        public void NavigateToLikes()
        {
            this.Navigate(typeof(MyLikesPage));
        }

        public void NavigateToDownloads()
        {
            this.Navigate(typeof(MediaDownloadPage));
        }

        public static bool GoBack()
        {
            if (CustomFrame.Instance.CanGoBack)
            {
                CustomFrame.Instance.GoBack();
                return true;
            }
            return false;
        }

        private void Navigate(Type navStr, object parameter = null)
        {
            // Prevent re-entrancy / concurrent navigation
            if (this._isNavigating)
                return;

            Execute.ExecuteOnUIThread(() =>
            {
                // quick double-check
                if (this._isNavigating)
                    return;

                try
                {
                    this._isNavigating = true;

                    var frame = CustomFrame.Instance;
                    if (frame == null)
                    {
                        try { Logger.Instance?.Error("Navigator.Navigate: CustomFrame.Instance is null"); } catch { Debug.WriteLine("Navigator.Navigate: CustomFrame.Instance is null"); }
                        return;
                    }

                    // If already on the same page type with no parameter, skip navigation
                    try
                    {
                        if (frame.Content != null && frame.Content.GetType() == navStr && parameter == null)
                            return;
                    }
                    catch { }

                    // Perform navigation
                    frame.Navigate(navStr, parameter/*,new DrillInNavigationTransitionInfo()*/);
                    frame.OpenCloseMenu(false);
                }
                catch (Exception ex)
                {
                    try
                    {
                        // Special handling for COMException to capture HRESULT and extra context
                        var comEx = ex as System.Runtime.InteropServices.COMException;
                        if (comEx != null)
                        {
                            string frameInfo = "";
                            try { frameInfo = CustomFrame.Instance?.Content?.GetType()?.FullName ?? "<no-frame>"; } catch { frameInfo = "<err-getting-frame>"; }
                            Logger.Instance?.Error($"Navigator.Navigate COMException HResult=0x{comEx.HResult:X8} Message={comEx.Message} Frame={frameInfo} Stack={comEx.StackTrace} EnvStack={System.Environment.StackTrace}", comEx);
                            Debug.WriteLine($"Navigator.Navigate COMException HResult=0x{comEx.HResult:X8} Message={comEx.Message}");
                        }
                        else
                        {
                            Logger.Instance?.Error("Navigator.Navigate failed", ex);
                        }
                    }
                    catch
                    {
                        Debug.WriteLine(ex.ToString());
                    }
                    // swallow - don't crash UI thread
                }
                finally
                {
                    this._isNavigating = false;
                }
            });
        }

        public void ClearBackStack()
        {
            CustomFrame.Instance.BackStack.Clear();
        }

        public void NavigateToNewsFeed(string query = null)
        {
            this.Navigate(typeof(NewsPage), query);
        }

        public void NavigateToWallPostComments(int ownerId, uint postId, uint commentId = 0, object postData = null)
        {
            try { Debug.WriteLine($"NavigateToWallPostComments: owner={ownerId} post={postId} commentId={commentId}"); } catch { }

            // Try to prefetch post to detect whether owner id sign is correct (user vs group).
            WallService.Instance.GetWallPostByIdWithComments(ownerId, postId, 0, (int)commentId, true, (result) =>
            {
                if (result.error.error_code == VKErrors.None && result.response != null)
                {
                    // Success with given ownerId
                    Execute.ExecuteOnUIThread(() =>
                    {
                        Dictionary<string, object> QueryString = new Dictionary<string, object>();
                        QueryString.Add("OwnerId", ownerId);
                        QueryString.Add("ItemId", postId);
                        QueryString.Add("CommentId", commentId);
                        if (postData != null)
                            QueryString.Add("Data", postData);
                        this.Navigate(typeof(CommentsPage), QueryString);
                    });
                    return;
                }

                // If access denied or content unavailable, and ownerId positive, try negated owner (group)
                if ((result.error.error_code == VKErrors.AccessDenied || result.error.error_code == VKErrors.ContentUnavailable || result.error.error_code == VKErrors.PermissionIsDenied) && ownerId > 0)
                {
                    int swapped = -ownerId;
                    WallService.Instance.GetWallPostByIdWithComments(swapped, postId, 0, (int)commentId, true, (result2) =>
                    {
                        if (result2.error.error_code == VKErrors.None && result2.response != null)
                        {
                            Execute.ExecuteOnUIThread(() =>
                            {
                                Dictionary<string, object> QueryString = new Dictionary<string, object>();
                                QueryString.Add("OwnerId", swapped);
                                QueryString.Add("ItemId", postId);
                                QueryString.Add("CommentId", commentId);
                                if (postData != null)
                                    QueryString.Add("Data", postData);
                                this.Navigate(typeof(CommentsPage), QueryString);
                            });
                        }
                        else
                        {
                            // fallback: navigate with original ownerId so page shows error
                            Execute.ExecuteOnUIThread(() =>
                            {
                                Dictionary<string, object> QueryString = new Dictionary<string, object>();
                                QueryString.Add("OwnerId", ownerId);
                                QueryString.Add("ItemId", postId);
                                QueryString.Add("CommentId", commentId);
                                if (postData != null)
                                    QueryString.Add("Data", postData);
                                this.Navigate(typeof(CommentsPage), QueryString);
                            });
                        }
                    });
                    return;
                }

                // Other errors - navigate with original ownerId so CommentsPage can handle/display appropriate message
                Execute.ExecuteOnUIThread(() =>
                {
                    Dictionary<string, object> QueryString = new Dictionary<string, object>();
                    QueryString.Add("OwnerId", ownerId);
                    QueryString.Add("ItemId", postId);
                    QueryString.Add("CommentId", commentId);
                    if (postData != null)
                        QueryString.Add("Data", postData);
                    this.Navigate(typeof(CommentsPage), QueryString);
                });
            });
         }

        /// <summary>
        /// Добавляем плеер во фрейм
        /// </summary>
        /// <param name="ownerId">Владелец видеозаписи</param>
        /// <param name="videoId">ИД видеозаписи</param>
        /// <param name="accessKey"></param>
        /// <param name="video"></param>
        /// <param name="sender">Изображение на фоне</param>
        public void NavigateToVideoWithComments(int ownerId, uint videoId, string accessKey = "", VKVideoBase video = null, object sender = null)
        {
            VideoViewerUC.Show(ownerId, videoId, accessKey, video, sender);
        }

        public void NavigateToPhotoWithComments(int ownerId, uint photoId, string accessKey = "", VKPhoto photo = null)
        {
            Dictionary<string, object> QueryString = new Dictionary<string, object>();
            QueryString.Add("OwnerId", ownerId);
            QueryString.Add("ItemId", photoId);

            if (!string.IsNullOrEmpty(accessKey))
                QueryString.Add("AccessKey", accessKey);

            if (photo != null)
                QueryString.Add("Data", photo);
//            this.Navigate(typeof(PhotoCommentsPage), QueryString);
            this.Navigate(typeof(CommentsPage), QueryString);
        }

        public void NavigateToCommunityManagement(uint communityId, VKGroupType communityType, VKAdminLevel adminLevel)
        {
            Dictionary<string, object> QueryString = new Dictionary<string, object>();
            QueryString.Add("CommunityId", communityId);
            QueryString.Add("CommunityType", communityType);
            QueryString.Add("AdminLevel", adminLevel);
            this.Navigate(typeof(GroupManagementPage), QueryString);//Management/MainPage.xaml
        }

        public void NavigateToCommunityManagementRequests(uint communityId)
        {
            this.Navigate(typeof(Pages.Group.Management.RequestsPage), communityId);
        }

        /// <summary>
        /// Переход к подписчикам группы
        /// </summary>
        /// <param name="communityId"></param>
        public void NavigateToCommunitySubscribers(uint communityId, VKGroupType communityType/*, bool isManagement = false, bool isPicker = false, bool isBlockingPicker = false*/)
        {
            Dictionary<string, object> QueryString = new Dictionary<string, object>();
            QueryString.Add("CommunityId", communityId);
            QueryString.Add("CommunityType", communityType);
            this.Navigate(typeof(Pages.Group.CommunitySubscribersPage), QueryString);
        }

        /// <summary>
        /// Переход к руководителям группы
        /// </summary>
        /// <param name="communityId"></param>
        public void NavigateToCommunityManagementManagers(uint communityId/*, GroupType communityType*/)
        {
            this.Navigate(typeof(Pages.Group.Management.ManagersPage), communityId/*, (int)communityType)*/);
        }

        public void NavigateToCommunityManagementInformation(uint communityId)
        {
//            this.Navigate(typeof(Pages.Group.Management.CommunityInformationPage),communityId);
        }

        public void NavigateToCommunityManagementServices(uint communityId)
        {
            this.Navigate(typeof(Pages.Group.Management.ServicesPage),communityId);
        }

        public void NavigateToWebUri(string uri, bool forceWebNavigation = false)
        {
            if (string.IsNullOrWhiteSpace(uri))
                return;

            // Save last attempted uri for diagnostics
            try { this.LastAttemptedUri = uri; } catch { }

            if (uri.StartsWith("tel:"))
            {
                // phone call
            }
            else if (uri.StartsWith("vk.cc/"))
            {
                AccountService.Instance.CheckLink(uri, (result) =>
                {
                    if(result.error.error_code == VKErrors.None)
                    {
                        this.NavigateToWebUri(result.response.link);
                    }
                    else
                    {
                        this.NavigateToWebUri(uri);
                    }
                });
                return;
            }
            else
            {
                // If uri refers to a story and is a relative or hostless path, convert to https://vk.ru/story... to avoid malformed http://story...
                bool isSchemeMissing = !uri.StartsWith("http://", StringComparison.CurrentCultureIgnoreCase) && !uri.StartsWith("https://", StringComparison.CurrentCultureIgnoreCase);
                if (isSchemeMissing)
                {
                    string lower = uri.ToLowerInvariant();
                    if (lower.StartsWith("/story") || lower.StartsWith("story") || lower.Contains("/story/"))
                    {
                        // ensure leading slash
                        string path = uri.StartsWith("/") ? uri : "/" + uri;
                        uri = "https://vk.ru" + path;

                        // update last attempted uri
                        try { this.LastAttemptedUri = uri; } catch { }
                    }
                }

                if (!uri.StartsWith("http://", StringComparison.CurrentCultureIgnoreCase) && !uri.StartsWith("https://", StringComparison.CurrentCultureIgnoreCase))
                    uri = "http://" + uri;

                bool flag = false;
                if (!forceWebNavigation)
                    flag = this.GetWithinAppNavigationUri(uri);
                if (flag)
                    return;

                // Normalize URL: unescape JSON slashes and HTML entities, then URL-decode
                try
                {
                    uri = uri.Replace("\\/", "/").Replace("&amp;", "&").Trim();
                    try { uri = System.Net.WebUtility.UrlDecode(uri); } catch { }
                }
                catch { }
                
                // Retry in-app navigation on normalized URI
                try
                {
                    if (!forceWebNavigation)
                    {
                        bool handled = this.GetWithinAppNavigationUri(uri);
                        if (handled)
                            return;
                    }
                }
                catch (Exception ex)
                {
                    try { Logger.Instance?.Error("GetWithinAppNavigationUri failed on normalized uri", ex); } catch { Debug.WriteLine(ex.ToString()); }
                }

                // Try extract wall-<owner>_<post> and navigate internally if found
                try
                {
                    var m = System.Text.RegularExpressions.Regex.Match(uri, @"wall-?(?<owner>-?\d+)_(?<post>\d+)");
                    if (m.Success)
                    {
                        int ownerId = int.Parse(m.Groups["owner"].Value);
                        uint postId = uint.Parse(m.Groups["post"].Value);
                        try
                        {
                            this.NavigateToWallPostComments(ownerId, postId);
                            return;
                        }
                        catch (Exception navEx)
                        {
                            try { Logger.Instance?.Error("NavigateToWallPostComments failed for extracted wall link", navEx); } catch { Debug.WriteLine(navEx.ToString()); }
                        }
                    }
                }
                catch (Exception ex)
                {
                    try { Logger.Instance?.Error("Failed to extract wall link from uri", ex); } catch { Debug.WriteLine(ex.ToString()); }
                }
                // Fallback: open in external browser on UI thread
                Execute.ExecuteOnUIThread(async () =>
                {
                    try
                    {
                        LauncherOptions options = new LauncherOptions();
                        if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 3))
                            options.IgnoreAppUriHandlers = true;

                        await Launcher.LaunchUriAsync(new Uri(uri), options);
                    }
                    catch (System.Runtime.InteropServices.COMException comEx)
                    {
                        try { Logger.Instance?.Error($"Launcher.LaunchUriAsync COMException HResult=0x{comEx.HResult:X8} Message={comEx.Message} Uri={uri}", comEx); } catch { Debug.WriteLine(comEx.ToString()); }
                        try
                        {
                            await Launcher.LaunchUriAsync(new Uri(uri));
                        }
                        catch (Exception ex2)
                        {
                            try { Logger.Instance?.Error("Fallback LaunchUriAsync failed", ex2); } catch { Debug.WriteLine(ex2.ToString()); }
                        }
                    }
                    catch (Exception ex)
                    {
                        try { Logger.Instance?.Error("Launcher.LaunchUriAsync failed", ex); } catch { Debug.WriteLine(ex.ToString()); }
                    }
                });
            }
        }

        private void NavigateToGame(VKGame app, int sourceId, string uri)
        {
            if (app == null)
                return;
            int id = app.id;
            bool num1 = this.TryOpenGame(app);
            string utmParamsStr = "";
            if (!string.IsNullOrEmpty(uri))
            {
                int num2 = uri.IndexOf("?");
                int num3 = uri.IndexOf("#");
                if (num2 > -1 || num3 > -1)
                {
                    int startIndex = -1;
                    if (num2 > -1)
                    {
                        if (num3 > -1)
                            startIndex = num2 >= num3 ? num3 : num2 + 1;
                    }
                    else
                        startIndex = num3;
                    if (startIndex > -1)
                        utmParamsStr = uri.Substring(startIndex);
                }
                else
                {
                    utmParamsStr = uri;//my
                }
            }
            if (num1 == false && id != 0)
            {
               this.NavigateToProfileAppPage((uint)id, sourceId, app.title, utmParamsStr);
                
            }
        }

        private bool TryOpenGame(VKGame game)
        {
            bool result = false;
            
            if (!string.IsNullOrEmpty(game.platform_id) && game.is_in_catalog)
            {
                result = true;
                /*
                Execute.ExecuteOnUIThread(delegate
                {
                    PageBase currentPage = FramePageUtils.CurrentPage;
                    if (currentPage == null || currentPage is OpenUrlPage)
                    {
                        this.NavigateToGames(game.id, false);
                        return;
                    }
                    Grid grid = currentPage.Content as Grid;
                    FrameworkElement root = null;
                    if (((grid != null) ? grid.Children : null) != null && grid.Children.Count > 0)
                    {
                        root = (grid.Children[0] as FrameworkElement);
                    }
                    PageBase arg_94_0 = currentPage;
                    List<object> expr_70 = new List<object>();
                    expr_70.Add(game);
                    arg_94_0.OpenGamesPopup(expr_70, fromPush ? GamesClickSource.push : GamesClickSource.catalog, "", 0, root);
                });*/
                
            }
            return result;
        }

        private bool _isNavigatingToGame;

        private void NavigateToGame(int appId, int sourceId, string uri)
        {
            if (this._isNavigatingToGame)
                return;
            this._isNavigatingToGame = true;

            AppsService.Instance.GetApp(appId, (result =>
            {
                this._isNavigatingToGame = false;
                if (result.error.error_code == VKErrors.None)
                {
                    VKGame app;
                    if (result.response == null)
                    {
                        app = null;
                    }
                    else
                    {
                        app = result.response.items.FirstOrDefault();
                    }
                    Execute.ExecuteOnUIThread(() =>
                    { 
                        this.NavigateToGame(app, sourceId, uri);
                    });
                }
                //else
               //     GenericInfoUC.ShowBasedOnResult((int)result.ResultCode, "", null);
            }));
        }

        public void NavigateToDocuments(int ownerId = 0/*, bool isOwnerCommunityAdmined = false*/)
        {
            this.Navigate(typeof(DocumentsPage), ownerId);
        }

        public void NavigateToAlbum(VKPlaylist playlist)
        {
            Dictionary<string, object> QueryString = new Dictionary<string, object>();
            QueryString.Add("Playlist", playlist);
            this.Navigate(typeof(Pages.Audio.AlbumPage), QueryString);
        }

        public void NavigateToGroupDiscussions(int gid, string name, VKAdminLevel adminLevel, bool isPublicPage, bool canCreateTopic)
        {
            Dictionary<string, object> QueryString = new Dictionary<string, object>();
            QueryString.Add("GroupId", gid);
            QueryString.Add("Name", name);
            QueryString.Add("AdminLevel", adminLevel);
            QueryString.Add("IsPublicPage", isPublicPage);
            QueryString.Add("CanCreateTopic", canCreateTopic);
            this.Navigate(typeof(Pages.Group.GroupDiscussionsPage), QueryString);
        }

        /// <summary>
        /// Переход к обсуждению
        /// </summary>
        /// <param name="groupId">ИД группы без минуса</param>
        /// <param name="topicId">ИД обсуждения</param>
        /// <param name="topicName">Название обсуждения</param>
        /// <param name="canComment"></param>
        public void NavigateToGroupDiscussion(uint groupId, uint topicId, string topicName = "", bool canComment = true, uint commentId = 0)
        {
            Dictionary<string, object> QueryString = new Dictionary<string, object>();
            QueryString.Add("GroupId", groupId);
            QueryString.Add("TopicId", topicId);
            QueryString.Add("TopicName", topicName);
            //QueryString.Add("KnownCommentsCount", knownCommentsCount);
            //QueryString.Add("LoadFromTheEnd", loadFromEnd);
            QueryString.Add("CanComment", canComment);
            QueryString.Add("CommentId", commentId);
            this.Navigate(typeof(Pages.Group.GroupDiscussionPage), QueryString);
        }

        public void NavigateToNewWallPost(WallPostViewModel.Mode mode1 = WallPostViewModel.Mode.NewWallPost, int userOrGroupId = 0, VKAdminLevel adminLevel = 0, bool isPublicPage = false, VKWallPost data = null)
        {
            //if (userOrGroupId == 0)
            //    userOrGroupId = (int)Settings.UserId;
            
            Dictionary<string, object> QueryString = new Dictionary<string, object>();
            QueryString.Add("UserOrGroupId", userOrGroupId);
            QueryString.Add("AdminLevel", adminLevel);
            QueryString.Add("IsPublicPage", isPublicPage);
            
            QueryString.Add("Mode", mode1);
            //QueryString.Add("FromWallPostPage", isFromWallPostPage);
            //QueryString.Add("IsPostponed", isPostponed);
            if(data!=null)
                QueryString.Add("Data", data);
            //IsPopupNavigation

            this.Navigate(typeof(NewPostPage), QueryString);
        }

        // Navigation helpers moved to NavigatorImpl.Helpers.cs

       /* public void NavigateToGroupRecommendations(object sender)//(int categoryId, string categoryName)
        {
            this.Navigate(typeof(RecommendedGroupsPage));
        }

        public void NavigateToSuggestedSourcesPage(object sender)
        {
            this.Navigate(typeof(SuggestedSourcesPage));
        }

        public void NavigateToUsersSearch(string query = "")
        {
            this.Navigate(typeof(SearchResultsPage), query); //UsersSearchResultsPage
        }

        public void NavigateToCommunityManagement(uint communityId, VKGroupType communityType, VKAdminLevel adminLevel)
        {
            Dictionary<string, object> QueryString = new Dictionary<string, object>();
            QueryString.Add("CommunityId", communityId);
            QueryString.Add("CommunityType", communityType);
            QueryString.Add("AdminLevel", adminLevel);
            this.Navigate(typeof(GroupManagementPage), QueryString);//Management/MainPage.xaml
        }

        public void NavigateToCommunityManagementManagers(uint communityId)
        {
            this.Navigate(typeof(Pages.Group.Management.ManagersPage), communityId);
        }

        public void NavigateToCommunityManagementInformation(uint communityId)
        {
//            this.Navigate(typeof(Pages.Group.Management.CommunityInformationPage),communityId);
        }

        public void NavigateToCommunityManagementServices(uint communityId)
        {
            this.Navigate(typeof(Pages.Group.Management.ServicesPage),communityId);
        }*/

        #region WEB_PARCER
        /// <summary>
        /// Навигация внутри приложение, если ссылка ВК. Возвращает правду, если навигация будет внутри.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="fromPush"></param>
        /// <param name="customCallback"></param>
        /// <returns></returns>
        public bool GetWithinAppNavigationUri(string uri, bool fromPush = false, Action<bool> customCallback = null)
        {
            if (!NavigatorImpl.IsVKUri(uri))
                return false;
            
            // Normalize incoming URI: unescape JSON slashes and HTML entities, then URL-decode to increase chance of matching
            try
            {
                uri = uri.Replace("\\/", "/").Replace("&amp;", "&").Trim();
                try { uri = System.Net.WebUtility.UrlDecode(uri); } catch { }
            }
            catch { }
            
            // Fix malformed pattern like "/id-12345" which sometimes появляется вместо "/club12345"
            try { if (uri.Contains("/id-")) uri = uri.Replace("/id-", "/club"); } catch { }

            string uri1 = uri;

            // Aggressive early extraction: if URI (or decoded forms) contains a wall link, navigate to it immediately
            try
            {
                var wallMatch = System.Text.RegularExpressions.Regex.Match(uri, @"wall-?(?<owner>-?\d+)_(?<post>\d+)", RegexOptions.IgnoreCase);
                if (!wallMatch.Success)
                    wallMatch = System.Text.RegularExpressions.Regex.Match(uri1, @"wall-?(?<owner>-?\d+)_(?<post>\d+)", RegexOptions.IgnoreCase);

                if (wallMatch.Success)
                {
                    int parsedOwner = 0;
                    uint parsedPost = 0;
                    try
                    {
                        parsedOwner = int.Parse(wallMatch.Groups["owner"].Value);
                        parsedPost = uint.Parse(wallMatch.Groups["post"].Value);
                    }
                    catch { }

                    if (parsedPost != 0)
                    {
                        try
                        {
                            this.NavigateToWallPostComments(parsedOwner, parsedPost);
                            return true;
                        }
                        catch (Exception navEx)
                        {
                            try { Logger.Instance?.Error("NavigateToWallPostComments failed for extracted wall link", navEx); } catch { Debug.WriteLine(navEx.ToString()); }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                try { Logger.Instance?.Error("GetWithinAppNavigationUri early wall extraction failed", ex); } catch { Debug.WriteLine(ex.ToString()); }
            }

            int count = uri1.IndexOf("/");
            if (count > -1)
                uri1 = uri1.Remove(0, count);
            if (uri1.StartsWith("dev/") || uri1.StartsWith("dev") && uri1.Length == 3)
                return false;
            Dictionary<string, string> queryString = uri.ParseQueryString();
            if (uri1.StartsWith("/feed") && queryString.ContainsKey("section") && queryString["section"] == "search")
            {
                //bug: если ссылка в конце обрезанного текста, то в неё имеются три точки (...) на конце
                if (queryString.ContainsKey("q") && queryString["q"].Contains("@"))
                {
                    string[] s = queryString["q"].Split('@');
                    this.NavigateToPostsSearch(0, "", s[0], s[1]);
                }
                else
                {
                    this.NavigateToNewsFeed(queryString.ContainsKey("q") ? queryString["q"] : "");
                    //this.NavigateToNewsSearch(HttpUtility.UrlDecode(queryString.ContainsKey("q") ? queryString["q"] : ""));
                }

                return true;
            }
            int ownerId;
            int id2;
            int id3;
            string objName;
            string objSub;
            NavigatorImpl.NavType navigationType = this.GetNavigationType(uri1, out ownerId, out id2, out id3, out objName, out objSub);
            if (navigationType == NavigatorImpl.NavType.none)
                return false;
            if (ownerId == 0)
                ownerId = (int)Settings.UserId;
            //bool flag = true;
            switch (navigationType)
            {
                case NavigatorImpl.NavType.friends:
              //      this.NavigateToFriends(id1, "", false, FriendsPageMode.Default);
                    break;
                case NavigatorImpl.NavType.communities:
            //        this.NavigateToGroups(AppGlobalStateManager.Current.LoggedInUserId, "", false, 0, 0, "", false, "", 0L);
                    break;
                case NavigatorImpl.NavType.dialogs:
                    this.NavigateToConversations();
                    break;
                case NavigatorImpl.NavType.news:
             //       this.NavigateToNewsFeed(0, false);
                    break;
                case NavigatorImpl.NavType.tagPhoto:
             //       this.NavigateToPhotoAlbum(Math.Abs(id1), id1 < 0, "2", "0", "", 0, "", "", false, 0, false);
                    break;
                case NavigatorImpl.NavType.albums:
            //        this.NavigateToPhotoAlbums(false, Math.Abs(id1), id1 < 0, 0);
                    break;
                
                case NavigatorImpl.NavType.dialog:
              //      this.NavigateToConversation(id1, false, false, "", 0, false);
                    break;
                case NavigatorImpl.NavType.profile:
                    this.NavigateToProfilePage(ownerId);
                    break;
                case NavigatorImpl.NavType.community:
                    this.NavigateToProfilePage(-ownerId);
                    break;
                case NavigatorImpl.NavType.board:
             //       this.NavigateToGroupDiscussions(id1, "", 0, false, false);
                    break;
                case NavigatorImpl.NavType.album:
                    //long albumIdLong = AlbumTypeHelper.GetAlbumIdLong(id2String);
                    //AlbumType albumType = AlbumTypeHelper.GetAlbumType(albumIdLong);
                    //this.NavigateToPhotoAlbum(Math.Abs(id1), id1 < 0, albumType.ToString(), albumIdLong.ToString(), "", 0, "", "", false, 0, false);
                    this.NavigateToPhotosOfAlbum(ownerId, id2,"");
                    break;
                case NavigatorImpl.NavType.video:
                    this.NavigateToVideoWithComments(ownerId, (uint)id2);
                    break;
                case NavigatorImpl.NavType.audios:
             //       this.NavigateToAudio(0, Math.Abs(id1), id1 < 0, 0, 0, "");
                    break;
                case NavigatorImpl.NavType.topic:
                    this.NavigateToGroupDiscussion((uint)-ownerId, (uint)id2);
                    break;
                case NavigatorImpl.NavType.photo:
                    this.NavigateToPhotoWithComments(ownerId, (uint)id2);
                    break;
                case NavigatorImpl.NavType.wallPost:
                    this.NavigateToWallPostComments(ownerId,(uint)id2, (uint)id3);
                    break;
                case NavigatorImpl.NavType.namedObject:
                    this.ResolveScreenNameNavigationObject(uri, objName);
                    break;
                case NavigatorImpl.NavType.stickersSettings:
               //     this.NavigateToStickersManage();
                    break;
                case NavigatorImpl.NavType.settings:
                    this.NavigateToSettings();
                    break;
                case NavigatorImpl.NavType.feedback:
                    this.NavigateToFeedback();
                    break;
                case NavigatorImpl.NavType.videos:
            //        this.NavigateToVideo(false, Math.Abs(id1), id1 < 0, false);
                    break;
                case NavigatorImpl.NavType.fave:
                    this.NavigateToFavorites();
                    break;
                case NavigatorImpl.NavType.apps:
                   this.NavigateToGames(0/*, fromPush*/);
                    //flag = false;
                    break;
                case NavigatorImpl.NavType.marketAlbum:
              //      this.NavigateToMarketAlbumProducts(id1, id2, null);
                    break;
                case NavigatorImpl.NavType.market:
                    this.NavigateToMarket(ownerId);
                    break;
                case NavigatorImpl.NavType.product:
                    this.NavigateToProduct(ownerId, (uint)id2);
                    break;
                case NavigatorImpl.NavType.stickers:
                    this.NavigateToStickersStore(0, false);
                    break;
                case NavigatorImpl.NavType.stickersPack:
                    NavigatorImpl.ShowStickersPack(objSub);
                    break;
                case NavigatorImpl.NavType.recommendedNews:
               //     this.NavigateToNewsFeed(NewsSources.Suggestions.PickableItem.ID, false);
                    break;
                case NavigatorImpl.NavType.app:
                    this.NavigateToGame(ownerId, id2, uri);
                    break;
                case NavigatorImpl.NavType.gifts:
                //    EventAggregator.Current.Publish(new GiftsPurchaseStepsEvent(GiftPurchaseStepsSource.link, GiftPurchaseStepsAction.gifts_page));
                //    this.NavigateToGifts(id1, "", "");
                    break;
                case NavigatorImpl.NavType.giftsCatalog:
              //      EventAggregator.Current.Publish(new GiftsPurchaseStepsEvent(GiftPurchaseStepsSource.link, GiftPurchaseStepsAction.store));
               //     this.NavigateToGiftsCatalog(0, false);
                    break;
                case NavigatorImpl.NavType.podcasts:
                    {
                        this.NavigateToPodcasts(ownerId, "");
                        break;
                    }
            }
            //return flag;
            return true;
        }

        public void NavigateToPostsSearch(int v1, string v2, string v3, string v4)
        {
            throw new NotImplementedException();
        }

        public void NavigateToMarket(int ownerId)
        {
            throw new NotImplementedException();
        }

        public void NavigateToProduct(int ownerId, uint id2)
        {
            throw new NotImplementedException();
        }

        public void NavigateToStickersStore(int v1, bool v2)
        {
            throw new NotImplementedException();
        }

        public static void ShowStickersPack(string objSub)
        {
            throw new NotImplementedException();
        }

        private static bool IsVKUri(string uri)
        {
            uri = uri.ToLowerInvariant();
            uri = uri.Replace("http://", "").Replace("https://", "");
            if (uri.StartsWith("m.") || uri.StartsWith("t.") || uri.StartsWith("0."))
                uri = uri.Remove(0, 2);
            if (uri.StartsWith("www.") || uri.StartsWith("new."))
                uri = uri.Remove(0, 4);
            if (!uri.StartsWith("vk.ru/") && !uri.StartsWith("vkontakte.ru/"))
                return uri.StartsWith("vk.me/");
            return true;
        }

        

        private bool _isResolvingScreenName;

        private void ResolveScreenNameNavigationObject(string uri, string objName)
        {
            if (this._isResolvingScreenName)
                return;

            this._isResolvingScreenName = true;
            AccountService.Instance.ResolveScreenName(objName.Replace("/", ""), (result) => {

                this._isResolvingScreenName = false;

                if (result.error.error_code == VKErrors.None)
                {
                    Execute.ExecuteOnUIThread(() => { 
                        bool flag = false;
                        if (!string.IsNullOrEmpty(uri) && uri.StartsWith("http://vk.me/"))
                        {
                            flag = true;
                        }

                        string type = result.response.type;
                        int object_id = result.response.object_id;
                        if (type == "user")
                        {
                            if (flag)
                                this.NavigateToConversation(object_id);
                            else
                                this.NavigateToProfilePage(object_id);
                        }
                        else if (type == "group")
                        {
                            if (flag)
                                this.NavigateToConversation(-object_id);
                            else
                                this.NavigateToProfilePage(-object_id);
                        }
                        else if (type == "application" || type == "vk_app" /*&& AppGlobalStateManager.Current.GlobalState.GamesSectionEnabled*/)
                        {
                            //Game app = res.ResultData.app;
                            //this.NavigateToGame(app, 0, uri, fromPush, customCallback);
                            //return;

                            this.NavigateToGame(object_id, 0, uri);
                        }
                        else
                            this.NavigateToWebUri(uri, true);
                    });
                }
                else
                    this.NavigateToWebUri(uri, true);
            });

            
        }

        private NavigatorImpl.NavType GetNavigationType(string uri, out int id1, out int id2, out int id3, out string obj, out string objSub)
        {
            id1 = id2 = id3 = 0;
            obj = objSub = "";
            foreach (NavigatorImpl.NavigationTypeMatch navTypes1 in this._navTypesList)
            {
                if (navTypes1.Check(uri))
                {
                    if (navTypes1.SubTypes.Count > 0)
                    {
                        foreach (string subType in navTypes1.SubTypes)
                        {
                            foreach (NavigatorImpl.NavigationTypeMatch navTypes2 in this._navTypesList)
                            {
                                if (navTypes2.Check(subType))
                                {
                                    id1 = navTypes2.Id1;
                                    id2 = navTypes2.Id2;
                                    //id3 = navTypes2.Id3;
                                    obj = navTypes2.ObjName;
                                    objSub = navTypes2.ObjSubName;
                                    return navTypes2.MatchType;
                                }
                            }
                        }
                    }
                    id1 = navTypes1.Id1;
                    id2 = navTypes1.Id2;
                    id3 = navTypes1.Id3;
                    obj = navTypes1.ObjName;
                    objSub = navTypes1.ObjSubName;
                    return navTypes1.MatchType;
                }
            }
            return NavigatorImpl.NavType.none;
        }



#endregion

        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="aid">ИД альбома</param>
        /// <param name="albumType">Тип альбома (wall,profile,saved)</param>
        /// <param name="userOrGroupId">Владелец</param>
        /// <param name="photosCount">Всего изображений</param>
        /// <param name="selectedPhotoIndex">Что показываем?</param>
        /// <param name="photos"></param>
        /// <param name="getImageByIdFunc"></param>
        public void NavigateToImageViewer(string aid, ImageViewerViewModel.AlbumType albumType, int userOrGroupId, uint photosCount, int selectedPhotoIndex, List<VKPhoto> photos, Func<int, Border> getImageByIdFunc)
        {
            UC.ImageViewerDecoratorUC.ShowPhotosFromAlbum(aid, albumType, userOrGroupId, photosCount, selectedPhotoIndex, photos, getImageByIdFunc);
        }

        public void NavigateToImageViewer(uint photosCount, int initialOffset, int selectedPhotoIndex, List<VKPhoto> photos, ImageViewerViewModel.ViewerMode viewerMode, Func<int, Border> getImageByIdFunc = null, bool hideActions = false)
        {
            UC.ImageViewerDecoratorUC.ShowPhotosById(photosCount, initialOffset, selectedPhotoIndex, photos, viewerMode, getImageByIdFunc, hideActions);
        }

        public void NaviateToImageViewerPhotoFeed(int userOrGroupId, string aid, uint photosCount, int selectedPhotoIndex, DateTime date, List<VKPhoto> photos, string mode, Func<int, Border> getImageByIdFunc)
        {
            UC.ImageViewerDecoratorUC.ShowPhotosFromFeed(userOrGroupId, aid, photosCount, selectedPhotoIndex, date, photos, mode, getImageByIdFunc);
        }
    
        //public void NavigateToImageViewer(uint photosCount, int selectedPhotoIndex, ObservableCollection<VKPhoto> photos, Action<Action<bool>> loadMoreFunc, Func<int, Image> getImageByIdFunc)
        //{
        //    UC.ImageViewerDecoratorUC.ShowPhotosById(photosCount, initialOffset, selectedPhotoIndex, photos, getImageByIdFunc, hideActions);
        //}

        public void NavigateToCommunityManagementBlacklist(uint communityId/*, GroupType communityType*/)
        {
            this.Navigate(typeof(Pages.Group.Management.BlacklistPage), communityId);
        }

        public void NavigateToSubscriptions(uint userId)
        {
            this.Navigate(typeof(SubscriptionsPage), userId);
        }

        public void NavigateToLikesPage(int ownerId, uint itemId, LikeObjectType type, int knownCount = 0, bool selectFriendLikes = false)
        {
            Dictionary<string, object> QueryString = new Dictionary<string, object>();
            QueryString.Add("OwnerId", ownerId);
            QueryString.Add("ItemId", itemId);
            QueryString.Add("Type", (byte)type);
            QueryString.Add("KnownCount", knownCount);
            QueryString.Add("SelectFriendLikes", selectFriendLikes);
            this.Navigate(typeof(LikesPage), QueryString);
        }

        public void NavigateToSuggestedPostponedPostsPage(int userOrGroupId, int mode)
        {
            Dictionary<string, int> QueryString = new Dictionary<string, int>();
            QueryString.Add("UserOrGroupId", userOrGroupId);
            QueryString.Add("Mode", mode);
            this.Navigate(typeof(SuggestedPostponedPostsPage), QueryString);
        }

        public void NavigateToGames(long gameId = 0)
        {
            this.Navigate(typeof(GamesMainPage), gameId);
        }

        public void NavigateToStickersManage()
        {
            this.Navigate(typeof(StickersManagePage));
        }

        public void NavigateShareExternalContentpage(Windows.ApplicationModel.DataTransfer.ShareTarget.ShareOperation shareOperation)
        {
            this.Navigate(typeof(ShareExternalContentPage), shareOperation);
        }

        public void NavigateToPodcasts(int ownerId, string ownerName)
        {
            this.Navigate(typeof(PodcastsPage), ownerId);
        }

        public void NavigateToArticles(int ownerId, string ownerName)
        {
            this.Navigate(typeof(ArticlesPage), ownerId);
        }

        public void NavigateToEditProfile()
        {
            this.Navigate(typeof(SettingsEditProfilePage));
        }

        public void NavigateToPhotoPickerPhotos(int maxAllowedToSelect, Windows.Storage.StorageFile pickToStorageFile = null)
        {
            Dictionary<string, object> QueryString = new Dictionary<string, object>();
            QueryString.Add("MaxAllowedToSelect", maxAllowedToSelect);
            //QueryString.Add("OwnPhotoPick", ownPhotoPick);

            if(pickToStorageFile!=null)
                QueryString.Add("PickToStorageFile", pickToStorageFile);
            //QueryString.Add("IsPopupNavigation", true);
            this.Navigate(typeof(Pages.PhotoPickerPhotos), QueryString);
        }

        public void NavigateToChangePassword()
        {
            this.Navigate(typeof(ChangePasswordPage));
        }

        public void NavigateToBirthdaysPage()
        {
            this.Navigate(typeof(BirthdaysPage));
        }

        public void NavigateToAddNewVideo(string filePath, int ownerId)
        {
            Dictionary<string, object> QueryString = new Dictionary<string, object>();
            QueryString.Add("VideoToUploadPath", filePath);
            QueryString.Add("OwnerId", ownerId);

            this.Navigate(typeof(AddEditVideoPage) , QueryString);
        }

        public void NavigateToEditVideo(int ownerId, uint videoId, VKVideoBase video = null)
        {
            Dictionary<string, object> QueryString = new Dictionary<string, object>();
            QueryString.Add("OwnerId", ownerId);
            QueryString.Add("VideoId", videoId);
            QueryString.Add("VideoForEdit", video);

            //if (video != null)
            //    ParametersRepository.SetParameterForId("VideoForEdit", video);

            this.Navigate(typeof(AddEditVideoPage), QueryString);
        }

        public void NavigateToGifts(uint userId, string firstName = "", string firstNameGen = "")
        {
            Dictionary<string, object> QueryString = new Dictionary<string, object>();
            QueryString.Add("UserId", userId);
            QueryString.Add("FirstName", firstName);
            QueryString.Add("FirstNameGen", firstNameGen);

            this.Navigate(typeof(GiftsPage), QueryString);
        }

        public void NavigateToConversationMaterials(int id)
        {
            throw new NotImplementedException();
        }

        public void NavigateToManageSources(bool v)
        {
            throw new NotImplementedException();
        }

        public void NavigateToGroupWikiPages(uint id, string wikiPageText)
        {
            throw new NotImplementedException();
        }

        public void NavigateToStoryCreate()
        {
            throw new NotImplementedException();
        }

        public void NavigateToNotificationDetail(VKNotification notification)
        {
            if (notification == null)
                return;

            // First: try to navigate using explicit TargetUrl / ActionUrl if present.
            try
            {
                string target = notification.TargetUrl ?? notification.ActionUrl;
                if (!string.IsNullOrWhiteSpace(target))
                {
                    try
                    {
                        // Normalize and decode
                        string uri = target.Replace("\\/", "/").Replace("&amp;", "&").Trim();
                        try { uri = System.Net.WebUtility.UrlDecode(uri); } catch { }

                        // Try in-app navigation first (best effort)
                        bool handled = false;
                        try
                        {
                            handled = this.GetWithinAppNavigationUri(uri);
                        }
                        catch { handled = false; }

                        if (handled)
                            return;

                        // Sometimes URIs lack scheme or are hostless; try adding https:// if it helps
                        try
                        {
                            if (!uri.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !uri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                            {
                                string withScheme = "https://" + uri.TrimStart('/');
                                if (this.GetWithinAppNavigationUri(withScheme))
                                    return;
                            }
                        }
                        catch { }

                        // Final fallback: open as web uri (this will again attempt in-app handling inside NavigateToWebUri)
                        this.NavigateToWebUri(uri);
                        return;
                    }
                    catch { /* swallow and continue to parsed-parent flow */ }
                }
            }
            catch { /* ignore target-url pre-check failures and continue */ }

            try
            {
                var parsedParent = notification.ParsedParent;
                var parsedFeedback = notification.ParsedFeedback;

                // Wall post parent -> open comments
                if (parsedParent is VKWallPost wp)
                {
                    int ownerId = wp.owner_id;
                    uint postId = (wp.reply_post_id != 0) ? (uint)wp.reply_post_id : wp.id;
                    uint commentId = 0;
                    if (parsedFeedback is VKComment fb)
                        commentId = fb.id;

                    this.NavigateToWallPostComments(ownerId, postId, commentId, wp);
                    return;
                }

                // Parent is a comment -> open post comments and scroll to comment
                if (parsedParent is VKComment parentComment)
                {
                    int ownerId = parentComment.owner_id != 0 ? parentComment.owner_id : parentComment.from_id;
                    uint postId = parentComment.post_id != 0 ? parentComment.post_id : parentComment.id;
                    uint commentId = parentComment.id;

                    this.NavigateToWallPostComments(ownerId, postId, commentId);
                    return;
                }

                // Parent is a topic (discussion)
                if (parsedParent is VKTopic topic)
                {
                    // Try to get topic id and group id if available
                    uint topicId = 0;
                    uint groupIdParam = 0;
                    try { topicId = topic.id; } catch { }
                    try { groupIdParam = (topic.owner_id < 0) ? (uint)(-topic.owner_id) : 0; } catch { }

                    uint commentId = 0;
                    if (parsedFeedback is VKComment fb2)
                        commentId = fb2.id;

                    if (groupIdParam != 0 && topicId != 0)
                    {
                        this.NavigateToGroupDiscussion(groupIdParam, topicId, topic.title ?? string.Empty, true, commentId);
                        return;
                    }
                }

                // Parent is photo
                if (parsedParent is VKPhoto photo)
                {
                    int ownerId = 0;
                    uint photoId = 0;
                    try { ownerId = photo.owner_id; } catch { }
                    try { photoId = photo.id; } catch { }

                    if (photoId != 0)
                    {
                        this.NavigateToPhotoWithComments(ownerId, photoId, string.Empty, photo);
                        return;
                    }
                }

                // Parent is video
                if (parsedParent is VKVideoBase video)
                {
                    int ownerId = 0;
                    uint videoId = 0;
                    try { ownerId = video.owner_id; } catch { }
                    try { videoId = video.id; } catch { }

                    if (videoId != 0)
                    {
                        this.NavigateToVideoWithComments(ownerId, videoId);
                        return;
                    }
                }

                // If feedback itself is a comment, try routing based on it
                if (parsedFeedback is VKComment feedbackComment)
                {
                    uint commentId = feedbackComment.id;
                    int ownerId = feedbackComment.owner_id != 0 ? feedbackComment.owner_id : feedbackComment.from_id;
                    uint postId = feedbackComment.post_id != 0 ? feedbackComment.post_id : 0;

                    if (postId != 0)
                    {
                        this.NavigateToWallPostComments(ownerId, postId, commentId);
                        return;
                    }

                    // try photo/video ids on comment if present
                    try
                    {
                        if (feedbackComment.photo != null)
                        {
                            uint photoId = feedbackComment.photo.id;
                            if (photoId != 0)
                            {
                                this.NavigateToPhotoWithComments(ownerId, photoId);
                                return;
                            }
                        }
                    }
                    catch { }

                    try
                    {
                        if (feedbackComment.video != null)
                        {
                            uint videoId = feedbackComment.video.id;
                            if (videoId != 0)
                            {
                                this.NavigateToVideoWithComments(ownerId, videoId);
                                return;
                            }
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("NavigateToNotificationDetail error: " + ex);
            }

            // Fallback to notifications list
            this.NavigateToFeedback();
        }
    }
}

