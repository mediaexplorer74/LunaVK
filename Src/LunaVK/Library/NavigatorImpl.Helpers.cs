using System;
using System.Collections.Generic;
using LunaVK.Pages;
using System.Diagnostics;

namespace LunaVK.Library
{
    public partial class NavigatorImpl
    {
        // Helper navigation methods moved to a dedicated partial file for clarity.
        public void NavigateToProfileAppPage(uint appId, int ownerId, string appName, string utmParamsStr = "")
        {
            Dictionary<string, object> QueryString = new Dictionary<string, object>();
            QueryString.Add("AppId", appId);
            QueryString.Add("OwnerId", ownerId);
            QueryString.Add("AppName", appName);
            if (!string.IsNullOrEmpty(utmParamsStr))
                QueryString.Add("UtmParams", utmParamsStr);

            Debug.WriteLine($"NavigateToProfileAppPage: AppId={appId} OwnerId={ownerId} AppName={appName} Utm={utmParamsStr}");
            this.Navigate(typeof(ProfileAppPage), QueryString);
        }

        public void NavigateToGroupRecommendations(object sender)
        {
            Debug.WriteLine("NavigateToGroupRecommendations");
            this.Navigate(typeof(RecommendedGroupsPage));
        }

        public void NavigateToUsersSearch(string query = "")
        {
            Debug.WriteLine($"NavigateToUsersSearch: query={query}");
            this.Navigate(typeof(SearchResultsPage), query);
        }
    }
}
