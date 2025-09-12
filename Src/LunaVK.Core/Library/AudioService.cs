using LunaVK.Core.DataObjects;
using LunaVK.Core.Framework;
using LunaVK.Core.Network;
using LunaVK.Core.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VkLib.Core.Audio; // Added for VkLib audio support
using VkLib.Error;
using LunaVK.Core;

namespace LunaVK.Core.Library
{
    public class AudioService
    {
        //private static readonly Func<string, List<VKAudio>> _deserializeAudioList = (Func<string, List<VKAudio>>)(jsonStr => JsonConvert.DeserializeObject<GenericRoot<VKList<VKAudio>>>(jsonStr).response.items);
        private Dictionary<string, string> _cachedResults = new Dictionary<string, string>();

        private static AudioService _instance;
        public static AudioService Instance
        {
            get
            {
                return AudioService._instance ?? (AudioService._instance = new AudioService());
            }
        }

        // Helper: execute a VkLib async call, retrying once after applying stored Settings.AccessToken if VkInvalidTokenException is thrown
        private async Task<T> ExecuteWithTokenRetry<T>(Func<Task<T>> func)
        {
            try
            {
                return await func();
            }
            catch (VkInvalidTokenException)
            {
                try
                {
                    // Apply stored token and retry once
                    if (!string.IsNullOrEmpty(Settings.AccessToken))
                        VkService.Instance.AccessToken.Token = Settings.AccessToken;
                    return await func();
                }
                catch (VkInvalidTokenException)
                {
                    // give up
                    return default(T);
                }
            }
        }

        public void GetAllTracksAndAlbums(int userOrGroupId, int offset, int count, Action<VKResponse<AudioPageGet>> callback)
        {
            // Using VkLib directly instead of custom execute method
            Task.Run(async () =>
            {
                try
                {
                    // Get audios
                    var audioResponse = await ExecuteWithTokenRetry(() => VkService.Instance.Audio.Get(userOrGroupId, 0, count, offset));

                    // Get albums/playlists
                    var albumResponse = await ExecuteWithTokenRetry(() => VkService.Instance.Audio.GetPlaylists(userOrGroupId, 0, 0));

                    if (audioResponse != null && audioResponse.Items != null)
                    {
                        var result = new AudioPageGet
                        {
                            audios_count = (uint)(audioResponse.TotalCount > 0 ? audioResponse.TotalCount : audioResponse.Items.Count),
                            audios = audioResponse.Items.Select(a => new VKAudio
                            {
                                id = (uint)a.Id,
                                owner_id = (int)a.OwnerId,
                                artist = a.Artist,
                                title = a.Title,
                                duration = (int)a.Duration.TotalSeconds,
                                url = a.Url,
                                // Map album_id instead of album object
                                album_id = a.Album != null ? (int)a.Album.Id : 0
                            }).ToList(),
                            albums_count = (uint)(albumResponse?.TotalCount ?? 0),
                            albums = albumResponse?.Items?.Select(p => new VKPlaylist
                            {
                                id = (int)p.Id,  // Cast long to int
                                owner_id = (int)p.OwnerId,  // Cast long to int
                                title = p.Title
                            }).ToList() ?? new List<VKPlaylist>()
                        };

                        Execute.ExecuteOnUIThread(() =>
                        {
                            callback(new VKResponse<AudioPageGet>
                            {
                                error = new VKError { error_code = Enums.VKErrors.None },
                                response = result
                            });
                        });
                    }
                    else
                    {
                        Execute.ExecuteOnUIThread(() =>
                        {
                            callback(new VKResponse<AudioPageGet>
                            {
                                error = new VKError { error_code = Enums.VKErrors.UnknownError },
                                response = null
                            });
                        });
                    }
                }
                catch (Exception ex)
                {
                    Execute.ExecuteOnUIThread(() =>
                    {
                        callback(new VKResponse<AudioPageGet>
                        {
                            error = new VKError { error_code = Enums.VKErrors.UnknownError, error_msg = ex.Message },
                            response = null
                        });
                    });
                }
            });
        }
        
        public void MoveToAlbum(List<uint> aids, uint albumId, Action<VKResponse<object>> callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters["audio_ids"] = aids.GetCommaSeparated();
            parameters["album_id"] = albumId.ToString();
            VKRequestsDispatcher.DispatchRequestToVK<object>("audio.moveToAlbum", parameters, callback);
        }

        public void EditAlbum(int albumId, string albumName, Action<VKResponse<object>> callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters["title"] = albumName;
            parameters["album_id"] = albumId.ToString();
            VKRequestsDispatcher.DispatchRequestToVK<object>("audio.editAlbum", parameters, callback);
        }

        public void DeleteAlbum(int albumId, Action<VKResponse<object>> callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters["album_id"] = albumId.ToString();
            VKRequestsDispatcher.DispatchRequestToVK<object>("audio.deleteAlbum", parameters, callback);
        }

        public void CreateAlbum(string albumName, Action<VKResponse<VKPlaylist>> callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters["title"] = albumName;
            VKRequestsDispatcher.DispatchRequestToVK<VKPlaylist>("audio.addAlbum", parameters, callback);
        }

        public void GetAllAudio(Action<VKResponse<VKCountedItemsObject<VKAudio>>> callback, int? userOrGroupId = null, int? albumId = null, int offset = 0, int count = 0)
        {
            Task.Run(async () =>
            {
                try
                {
                    long ownerId = 0;
                    if (userOrGroupId.HasValue)
                        ownerId = userOrGroupId.Value;

                    long albumIdLong = 0;
                    if (albumId.HasValue)
                        albumIdLong = albumId.Value;

                    var response = await ExecuteWithTokenRetry(() => VkService.Instance.Audio.Get(ownerId, albumIdLong, count, offset));
                    
                    if (response != null)
                    {
                        var vkAudios = response.Items.Select(a => new VKAudio
                        {
                            id = (uint)a.Id,
                            owner_id = (int)a.OwnerId,
                            artist = a.Artist,
                            title = a.Title,
                            duration = (int)a.Duration.TotalSeconds,
                            url = a.Url,
                            // Map album_id instead of album object
                            album_id = a.Album != null ? (int)a.Album.Id : 0
                        }).ToList();

                        Execute.ExecuteOnUIThread(() =>
                        {
                            callback(new VKResponse<VKCountedItemsObject<VKAudio>>
                            {
                                error = new VKError { error_code = Enums.VKErrors.None },
                                response = new VKCountedItemsObject<VKAudio>
                                {
                                    count = (uint)response.TotalCount,
                                    items = vkAudios
                                }
                            });
                        });
                    }
                    else
                    {
                        Execute.ExecuteOnUIThread(() =>
                        {
                            callback(new VKResponse<VKCountedItemsObject<VKAudio>>
                            {
                                error = new VKError { error_code = Enums.VKErrors.UnknownError },
                                response = null
                            });
                        });
                    }
                }
                catch (Exception ex)
                {
                    Execute.ExecuteOnUIThread(() =>
                    {
                        callback(new VKResponse<VKCountedItemsObject<VKAudio>>
                        {
                            error = new VKError { error_code = Enums.VKErrors.UnknownError, error_msg = ex.Message },
                            response = null
                        });
                    });
                }
            });
        }

        public void GetRecommended(int uid, int offset, int count, Action<VKResponse<List<VKAudio>>> callback)
        {
            Task.Run(async () =>
            {
                try
                {
                    var response = await VkService.Instance.Audio.GetRecommendations(null, count, offset, uid);
                    
                    if (response != null && response.Items != null)
                    {
                        var vkAudios = response.Items.Select(a => new VKAudio
                        {
                            id = (uint)a.Id,
                            owner_id = (int)a.OwnerId,
                            artist = a.Artist,
                            title = a.Title,
                            duration = (int)a.Duration.TotalSeconds,
                            url = a.Url,
                            // Map album_id instead of album object
                            album_id = a.Album != null ? (int)a.Album.Id : 0
                        }).ToList();

                        Execute.ExecuteOnUIThread(() =>
                        {
                            callback(new VKResponse<List<VKAudio>>
                            {
                                error = new VKError { error_code = Enums.VKErrors.None },
                                response = vkAudios
                            });
                        });
                    }
                    else
                    {
                        Execute.ExecuteOnUIThread(() =>
                        {
                            callback(new VKResponse<List<VKAudio>>
                            {
                                error = new VKError { error_code = Enums.VKErrors.UnknownError },
                                response = null
                            });
                        });
                    }
                }
                catch (Exception ex)
                {
                    Execute.ExecuteOnUIThread(() =>
                    {
                        callback(new VKResponse<List<VKAudio>>
                        {
                            error = new VKError { error_code = Enums.VKErrors.UnknownError, error_msg = ex.Message },
                            response = null
                        });
                    });
                }
            });
        }

        public void GetPopular(int offset, int count, Action<VKResponse<List<VKAudio>>> callback)
        {
            Task.Run(async () =>
            {
                try
                {
                    var response = await VkService.Instance.Audio.GetPopular(false, count, offset);
                    
                    if (response != null && response.Items != null)
                    {
                        var vkAudios = response.Items.Select(a => new VKAudio
                        {
                            id = (uint)a.Id,
                            owner_id = (int)a.OwnerId,
                            artist = a.Artist,
                            title = a.Title,
                            duration = (int)a.Duration.TotalSeconds,
                            url = a.Url,
                            // Map album_id instead of album object
                            album_id = a.Album != null ? (int)a.Album.Id : 0
                        }).ToList();

                        Execute.ExecuteOnUIThread(() =>
                        {
                            callback(new VKResponse<List<VKAudio>>
                            {
                                error = new VKError { error_code = Enums.VKErrors.None },
                                response = vkAudios
                            });
                        });
                    }
                    else
                    {
                        Execute.ExecuteOnUIThread(() =>
                        {
                            callback(new VKResponse<List<VKAudio>>
                            {
                                error = new VKError { error_code = Enums.VKErrors.UnknownError },
                                response = null
                            });
                        });
                    }
                }
                catch (Exception ex)
                {
                    Execute.ExecuteOnUIThread(() =>
                    {
                        callback(new VKResponse<List<VKAudio>>
                        {
                            error = new VKError { error_code = Enums.VKErrors.UnknownError, error_msg = ex.Message },
                            response = null
                        });
                    });
                }
            });
        }

        public void GetUserAlbums(Action<VKResponse<VKCountedItemsObject<VKPlaylist>>> callback, int? userOrGroupId = null, bool isGroup = false, int offset = 0, int count = 0)
        {
            Task.Run(async () =>
            {
                try
                {
                    long ownerId = 0;
                    if (userOrGroupId.HasValue)
                        ownerId = userOrGroupId.Value;

                    var response = await ExecuteWithTokenRetry(() => VkService.Instance.Audio.GetPlaylists(ownerId, count, offset));
                    
                    if (response != null)
                    {
                        var vkPlaylists = response.Items.Select(p => new VKPlaylist
                        {
                            id = (int)p.Id,  // Cast long to int
                            owner_id = (int)p.OwnerId,
                            title = p.Title
                        }).ToList();

                        Execute.ExecuteOnUIThread(() =>
                        {
                            callback(new VKResponse<VKCountedItemsObject<VKPlaylist>>
                            {
                                error = new VKError { error_code = Enums.VKErrors.None },
                                response = new VKCountedItemsObject<VKPlaylist>
                                {
                                    count = (uint)response.TotalCount,
                                    items = vkPlaylists
                                }
                            });
                        });
                    }
                    else
                    {
                        Execute.ExecuteOnUIThread(() =>
                        {
                            callback(new VKResponse<VKCountedItemsObject<VKPlaylist>>
                            {
                                error = new VKError { error_code = Enums.VKErrors.UnknownError },
                                response = null
                            });
                        });
                    }
                }
                catch (Exception ex)
                {
                    Execute.ExecuteOnUIThread(() =>
                    {
                        callback(new VKResponse<VKCountedItemsObject<VKPlaylist>>
                        {
                            error = new VKError { error_code = Enums.VKErrors.UnknownError, error_msg = ex.Message },
                            response = null
                        });
                    });
                }
            });
        }

        public void GetAllAudioForUser(int uid, int guid, int albumId, List<int> aids, int count, int offset, Action<VKResponse<List<VKAudio>>> callback)
        {
            // This method is not used, but keeping it for compatibility
            VKRequestsDispatcher.DispatchRequestToVK<List<VKAudio>>("audio.get", new Dictionary<string, string>(), callback);
        }

        public void SearchTracks(string query, int offset, int count, Action<VKResponse<AudioPageSearch>> callback)
        {
            Task.Run(async () =>
            {
                try
                {
                    var response = await VkService.Instance.Audio.Search(query, count, offset);
                    
                    if (response != null)
                    {
                        var result = new AudioPageSearch
                        {
                            audios_count = (uint)response.TotalCount,  // Fixed type conversion
                            audios = response.Items.Select(a => new VKAudio
                            {
                                id = (uint)a.Id,
                                owner_id = (int)a.OwnerId,
                                artist = a.Artist,
                                title = a.Title,
                                duration = (int)a.Duration.TotalSeconds,
                                url = a.Url,
                                // Map album_id instead of album object
                                album_id = a.Album != null ? (int)a.Album.Id : 0
                            }).ToList(),
                            // Note: VkLib's search doesn't return albums or artists directly
                            albums_count = 0,
                            albums = new List<VKPlaylist>(),
                            artists_count = 0,
                            artists = new List<VKGroup>()
                        };

                        Execute.ExecuteOnUIThread(() =>
                        {
                            callback(new VKResponse<AudioPageSearch>
                            {
                                error = new VKError { error_code = Enums.VKErrors.None },
                                response = result
                            });
                        });
                    }
                    else
                    {
                        Execute.ExecuteOnUIThread(() =>
                        {
                            callback(new VKResponse<AudioPageSearch>
                            {
                                error = new VKError { error_code = Enums.VKErrors.UnknownError },
                                response = null
                            });
                        });
                    }
                }
                catch (Exception ex)
                {
                    Execute.ExecuteOnUIThread(() =>
                    {
                        callback(new VKResponse<AudioPageSearch>
                        {
                            error = new VKError { error_code = Enums.VKErrors.UnknownError, error_msg = ex.Message },
                            response = null
                        });
                    });
                }
            });
        }
        
        public void GetAudio(int ownerId, uint aid, Action<VKResponse<VKAudio>> callback)
        {
            Task.Run(async () =>
            {
                try
                {
                    var response = await VkService.Instance.Audio.GetById(new List<string> { $"{ownerId}_{aid}" });
                    
                    if (response != null && response.Count > 0)
                    {
                        var a = response[0];
                        var vkAudio = new VKAudio
                        {
                            id = (uint)a.Id,
                            owner_id = (int)a.OwnerId,
                            artist = a.Artist,
                            title = a.Title,
                            duration = (int)a.Duration.TotalSeconds,
                            url = a.Url,
                            // Map album_id instead of album object
                            album_id = a.Album != null ? (int)a.Album.Id : 0
                        };

                        Execute.ExecuteOnUIThread(() =>
                        {
                            callback(new VKResponse<VKAudio>
                            {
                                error = new VKError { error_code = Enums.VKErrors.None },
                                response = vkAudio
                            });
                        });
                    }
                    else
                    {
                        Execute.ExecuteOnUIThread(() =>
                        {
                            callback(new VKResponse<VKAudio>
                            {
                                error = new VKError { error_code = Enums.VKErrors.UnknownError },
                                response = null
                            });
                        });
                    }
                }
                catch (Exception ex)
                {
                    Execute.ExecuteOnUIThread(() =>
                    {
                        callback(new VKResponse<VKAudio>
                        {
                            error = new VKError { error_code = Enums.VKErrors.UnknownError, error_msg = ex.Message },
                            response = null
                        });
                    });
                }
            });
        }
        
        public void AddAudio(int ownerId, int aid, Action<VKResponse<int>> callback)
        {
            Task.Run(async () =>
            {
                try
                {
                    var response = await VkService.Instance.Audio.Add(aid, ownerId);
                    
                    if (response > 0)
                    {
                        Execute.ExecuteOnUIThread(() =>
                        {
                            callback(new VKResponse<int>
                            {
                                error = new VKError { error_code = Enums.VKErrors.None },
                                response = (int)response // Cast long to int
                            });
                        });
                    }
                    else
                    {
                        Execute.ExecuteOnUIThread(() =>
                        {
                            callback(new VKResponse<int>
                            {
                                error = new VKError { error_code = Enums.VKErrors.UnknownError },
                                response = 0
                            });
                        });
                    }
                }
                catch (Exception ex)
                {
                    Execute.ExecuteOnUIThread(() =>
                    {
                        callback(new VKResponse<int>
                        {
                            error = new VKError { error_code = Enums.VKErrors.UnknownError, error_msg = ex.Message },
                            response = 0
                        });
                    });
                }
            });
        }

        public void DeleteAudios(int aid, int ownerId, Action<VKResponse<int>> callback)
        {
            /*
            string format = "API.audio.delete({{ \"audio_id\":{0}, \"owner_id\":{1} }});";
            int loggedInUserId = AppGlobalStateManager.Current.LoggedInUserId;
            string str = "";
            foreach (int num in list)
                str = str + string.Format(format, num, loggedInUserId) + Environment.NewLine;
            
            VKRequestsDispatcher.Execute<VKClient.Common.Backend.DataObjects.ResponseWithId>(str, callback, (Func<string, VKClient.Common.Backend.DataObjects.ResponseWithId>)(jsonStr => new VKClient.Common.Backend.DataObjects.ResponseWithId()), false, true, cancellationToken);
            */
            Task.Run(async () =>
            {
                try
                {
                    var response = await VkService.Instance.Audio.Delete(aid, ownerId);
                    
                    Execute.ExecuteOnUIThread(() =>
                    {
                        callback(new VKResponse<int>
                        {
                            error = new VKError { error_code = Enums.VKErrors.None },
                            response = response ? 1 : 0
                        });
                    });
                }
                catch (Exception ex)
                {
                    Execute.ExecuteOnUIThread(() =>
                    {
                        callback(new VKResponse<int>
                        {
                            error = new VKError { error_code = Enums.VKErrors.UnknownError, error_msg = ex.Message },
                            response = 0
                        });
                    });
                }
            });
        }

        public void ReorderAudio(int aid, int oid, int album_id, int after, int before, Action<VKResponse<int>> callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters["audio_id"] = aid.ToString();
            if (oid != 0)
                parameters["owner_id"] = oid.ToString();
            if (album_id != 0L)
                parameters["album_id"] = album_id.ToString();
            parameters["after"] = after.ToString();
            parameters["before"] = before.ToString();
            VKRequestsDispatcher.DispatchRequestToVK<int>("audio.reorder", parameters, callback);
        }

        public void StatusSet(string audio, Action<VKResponse<int>> callback)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters["audio"] = audio;
            VKRequestsDispatcher.DispatchRequestToVK<int>("audio.setBroadcast", parameters, callback);
        }

        public void ResetBroadcast(Action<VKResponse<int>> callback)
        {
            VKRequestsDispatcher.DispatchRequestToVK<int>("audio.setBroadcast", new Dictionary<string, string>(), callback);
        }

        public void EditAudio(int ownerId, int id, string artist, string title, Action<VKResponse<int>> callback)
        {
            VKRequestsDispatcher.DispatchRequestToVK<int>("audio.edit", new Dictionary<string, string>()
            {
                { "owner_id", ownerId.ToString() },
                { "audio_id", id.ToString() },
                { "artist", artist },
                { "title", title }
            }, callback);
        }

        public void UploadAudio(byte[] data, string artist, string title, Action<VKResponse<VKAudio>> callback, Action<double> progressCallback = null, CancellationToken? cancellation = null)
        {
            VKRequestsDispatcher.DispatchRequestToVK<UploadServerAddress>("audio.getUploadServer", new Dictionary<string, string>(), (result) =>
            {
                if (result.error.error_code != Enums.VKErrors.None)
                    callback(new VKResponse<VKAudio>() { error = result.error });
                else
                {
                    JsonWebRequest.Upload(result.response.upload_url, data, "file", "audio", (JsonString, jsonResult) =>
                    {
                        if (!jsonResult)
                            callback(null);
                        else
                        {
                            UploadResponseData uploadResponseData = JsonConvert.DeserializeObject<UploadResponseData>(JsonString);
                            this.SaveAudio(uploadResponseData.audio, uploadResponseData.hash, uploadResponseData.server, callback);
                        }
                    }, "track.mp3", progressCallback, cancellation);
                }
            });
        }

        private void SaveAudio(string audio, string hash, string server, Action<VKResponse<VKAudio>> callback)
        {
            VKRequestsDispatcher.DispatchRequestToVK<VKAudio>("audio.save", new Dictionary<string, string>()
            {
                {  "server", server },
                { "audio", audio },
                { "hash", hash },
                //{ "artist", artist },
                //{ "title", title }
            }, callback);
        }
        
        public void GetAlbumArtwork(string search, Action<string> callback)
        {
            string format = "https://itunes.apple.com/search?media=music&limit=1&version=2&term={0}";
            search = System.Net.WebUtility.UrlEncode(search);
            if (this._cachedResults.ContainsKey(search))
                callback(this._cachedResults[search]);
            else
            {
                JsonWebRequest.SendHTTPRequestAsync(string.Format(format, search), (jsonResp, IsSucceeded) =>
                {
                    if (IsSucceeded)
                    {
                        try
                        {
                            AudioService.ItunesList itunesList = JsonConvert.DeserializeObject<AudioService.ItunesList>(jsonResp);
                            if (itunesList.results.Count > 0)
                            {
                                AudioService.ItunesAlbumArt result = itunesList.results[0];
                                var albArt = result.artworkUrl100.Replace("100x100", "600x600");
                                this._cachedResults[search] = albArt;
                                callback(albArt);
                            }
                            else
                                callback("");
                        }
                        catch (Exception)
                        {
                            callback(null);
                        }
                    }
                    else
                        callback(null);
                });
            }
        }

        public void ReloadAudio(IReadOnlyList<VKAudio> audios, Action<VKResponse<List<VKAudio>>> callback)
        {
            List<string> list = new List<string>();
            foreach(var audio in audios)
            {
                list.Add(audio.owner_id + "_" + audio.id + "_" + audio.actionHash + "_" + audio.urlHash);
                if (list.Count == 3)
                    break;
            }

            string ids = string.Join(",", list);

            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters["ids"] = ids;
            VKRequestsDispatcher.DispatchRequestToVK<List<VKAudio>>("audio.reload", parameters, callback);
        }

        private class ItunesAlbumArt
        {
            public string wrapperType { get; set; }
            public string kind { get; set; }
            public string artistName { get; set; }
            public string collectionName { get; set; }
            public string trackName { get; set; }

            public string previewUrl { get; set; }

            public string artworkUrl30 { get; set; }
            public string artworkUrl60 { get; set; }
            public string artworkUrl100 { get; set; }
        }

        private class ItunesList
        {
            public int resultCount { get; set; }

            public List<AudioService.ItunesAlbumArt> results { get; set; }
        }

        public class AudioPageGet
        {
            public uint audios_count { get; set; }
            public List<VKAudio> audios { get; set; }

            public uint albums_count { get; set; }
            public List<VKPlaylist> albums { get; set; }
        }

        public class AudioPageSearch
        {
            public uint audios_count { get; set; }
            public List<VKAudio> audios { get; set; }

            public uint albums_count { get; set; }
            public List<VKPlaylist> albums { get; set; }

            public uint artists_count { get; set; }
            public List<VKGroup> artists { get; set; }
        }
    }
}