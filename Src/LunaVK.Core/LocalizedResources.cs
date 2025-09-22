using System.Collections.Generic;
using System.Diagnostics;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.Resources.Core;
using Windows.Globalization;

namespace LunaVK.Core
{
   
    public class LocalizedStrings
    {
        private static ResourceLoader resourceLoader = new ResourceLoader();
        public string this[string key]
        {
            get
            {
                return resourceLoader.GetString(key);
            }
        }

        /// <summary>
        /// Returns the localized value of the specified key.
        /// </summary>
        /// <param name="key">The resource identifier.</param>
        /// <returns>The appropriate string value of the localized resources.</returns>
        public static string GetString(string key)
        {
#if DEBUG
            string temp = resourceLoader.GetString(key);
            //TODO
            //Debug.Assert(!string.IsNullOrEmpty(temp));
            if (string.IsNullOrEmpty(temp))
                Debug.WriteLine("[warn] LocalizedStrings class - Missing resource: " + key);
#endif
            return resourceLoader.GetString(key);
        }
    }

}
