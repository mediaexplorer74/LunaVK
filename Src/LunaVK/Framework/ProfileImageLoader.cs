using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;

using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Threading.Tasks;
using Windows.Storage;
using System.Net.Http;
using Windows.UI.Xaml.Media;
using LunaVK.Core.Utils;
using Windows.UI.Xaml.Media.Imaging;
using LunaVK.Library;
using System.Threading;
using Windows.Foundation;
using LunaVK.Core.Framework;
using System.Text.RegularExpressions;

namespace LunaVK.Framework
{
    public static class ProfileImageLoader
    {
        public static Dictionary<string, string> _downloadedDictionary = new Dictionary<string, string>();
        private static readonly Task _task = new Task(ProfileImageLoader.WorkerThreadProc);
        private static readonly object _syncBlock = new object();
        private static ProfileImageLoader.PendingRequest _currentRequest;

        /// <summary>
        /// Ожидающие запросф
        /// </summary>
        private static readonly Queue<ProfileImageLoader.PendingRequest> _pendingRequests = new Queue<ProfileImageLoader.PendingRequest>();

        static ProfileImageLoader()
        {
            ProfileImageLoader._task.Start();
        }

        private static void WorkerThreadProc()
        {
            while (true)
            {
                lock (ProfileImageLoader._syncBlock)
                {
                    if (ProfileImageLoader._pendingRequests.Count == 0 || ProfileImageLoader._currentRequest != null)
                    {
                        Monitor.Wait(ProfileImageLoader._syncBlock, 300);
                    }
                    else
                    {
                        ProfileImageLoader._currentRequest = ProfileImageLoader._pendingRequests.Dequeue();
                        ProfileImageLoader.DownloadFileAndCache(ProfileImageLoader._currentRequest.Uri);
                    }
                }
            }
        }

        private static async void DownloadFileAndCache(string uri)
        {
            string ret = null;
            //Windows.Storage.Streams.IRandomAccessStream rs = null;

            if (string.IsNullOrWhiteSpace(uri))
            {
                // nothing to do
                return;
            }

            if (ProfileImageLoader._downloadedDictionary.ContainsKey(uri))
            {
                ret = ProfileImageLoader._downloadedDictionary[uri];
            }
            else
            {
                try
                {
                    if (!Uri.TryCreate(uri, UriKind.Absolute, out Uri remoteUri))
                    {
                        return;
                    }

                    HttpClient http = new HttpClient();
                    byte[] image_from_web_as_bytes = await http.GetByteArrayAsync(remoteUri);

                    var sf = await CacheManager2.WriteToCache(remoteUri, "Cache/Stickers/" + uri.GetHashCode().ToString() + ".jpg");
                    ret = sf?.Path;
                }
                catch
                {
                    // ignore network/cache errors
                    ret = null;
                }
            }
            if (ret == null )
            {
                ProfileImageLoader._currentRequest = null;
                return;
            }
            Execute.ExecuteOnUIThread(()=>
                {
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache;

                    // set UriSource only if valid
                    if (Uri.TryCreate(ret, UriKind.Absolute, out Uri localUri))
                    {
                        bitmapImage.UriSource = localUri;
                        ProfileImageLoader._currentRequest.Image.Source = bitmapImage;
                    }
                    else
                    {
                        ProfileImageLoader._currentRequest.Image.Source = null;
                    }

                    ProfileImageLoader._currentRequest = null;

                    if (!ProfileImageLoader._downloadedDictionary.ContainsKey(uri) && ret != null)
                        ProfileImageLoader._downloadedDictionary.Add(uri, ret);
                });

        }

        public static void SetUriSource(Image image, string value)
        {
            if (image == null)
                throw new ArgumentNullException("obj");

            // Картинка ложится в кеш
            if (value == null)
                return;

            if(ProfileImageLoader._downloadedDictionary.ContainsKey(value))
            {
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                if(Uri.TryCreate(ProfileImageLoader._downloadedDictionary[value], UriKind.Absolute, out Uri cachedUri))
                {
                    bitmapImage.UriSource = cachedUri;
                    image.Source = bitmapImage;
                }
                else
                {
                    image.Source = null;
                }
            }
            else
            {
                ProfileImageLoader.AddPendingRequest(image, value);
            }
        }

        private static void AddPendingRequest(Image image, string uri)
        {
            lock (ProfileImageLoader._syncBlock)
            {
                ProfileImageLoader._pendingRequests.Enqueue(new ProfileImageLoader.PendingRequest(image, uri/*, currentAttempt*/));
                Monitor.Pulse(ProfileImageLoader._syncBlock);
            }
        }

        private class PendingRequest
        {
            public Image Image { get; private set; }

            public string Uri { get; private set; }

            //public DateTime CreatedTimstamp { get; private set; }

            //public DateTime DownloadStaredTimestamp { get; set; }

            //public Guid UniqueId { get; private set; }

            //public int CurrentAttempt { get; set; }

            public PendingRequest(Image image, string uri)
            {
                this.Image = image;
                this.Uri = uri;
            }
        }
    }
}
