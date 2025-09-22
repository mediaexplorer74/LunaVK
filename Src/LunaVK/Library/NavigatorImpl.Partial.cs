using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LunaVK.Library
{
    // Partial companion to NavigatorImpl: provide NavType enum and helpers used by large file.
    public partial class NavigatorImpl
    {
        private enum NavType
        {
            none,
            friends,
            communities,
            dialogs,
            news,
            tagPhoto,
            albums,
            dialog,
            profile,
            community,
            board,
            album,
            video,
            audios,
            topic,
            photo,
            wallPost,
            namedObject,
            stickersSettings,
            settings,
            feedback,
            videos,
            fave,
            apps,
            marketAlbum,
            market,
            product,
            stickers,
            stickersPack,
            recommendedNews,
            app,
            gifts,
            giftsCatalog,
            podcasts
        }

        private class NavigationTypeMatch
        {
            public NavType MatchType { get; set; }
            public int Id1 { get; set; }
            public int Id2 { get; set; }
            public int Id3 { get; set; }
            public string ObjName { get; set; }
            public string ObjSubName { get; set; }
            public List<string> SubTypes { get; } = new List<string>();

            public bool Check(string uri)
            {
                return false; // placeholder
            }
        }

        private List<NavigationTypeMatch> _navTypesList = new List<NavigationTypeMatch>();

        // Regexes used elsewhere in the project to format mentions.
        public static readonly Regex Regex_DomainMention = new Regex("\\[((id|club)([0-9]+)(?:[:][^\\]]+)?)\\|([^\\]]+)\\]", RegexOptions.Compiled);

        // This regex captures several mention forms; the display name is in capture group 4 so callers can use $4.
        public static readonly Regex Regex_Mention = new Regex("\\[(?:id(\\d+):bp-\\d+_\\d+|id(\\d+)|club(\\d+))\\|([^\\]]+)\\]", RegexOptions.Compiled);
    }
}
