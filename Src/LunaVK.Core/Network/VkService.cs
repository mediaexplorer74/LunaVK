using VkLib;

namespace LunaVK.Core.Network
{
    public class VkService
    {
        private const string VKAPP_ID = "2274003";//"6244854"; // LunaVK app ID
        private const string VKAPP_SECRET = "hHbZxrka2uZ6jB1inYsH"; // LunaVK app secret (empty for direct auth)

        public static Vk Instance { get; } = new Vk(
            VKAPP_ID,
            VKAPP_SECRET,
            "5.120",
            "VKAndroidApp/5.52-4543 (Android 5.1.1; SDK 22; x86_64; " +
                                  "unknown Android SDK built for x86_64; en; 320x240)"/*"LunaVK/1.0"*/
            );

        static VkService()
        {
            // Set the language based on app settings
            Instance.Language = "ru"; // Default to Russian, can be changed dynamically
        }
    }
}