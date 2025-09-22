using System.Threading.Tasks;

namespace LunaVK
{
    public static class MessageBox
    {
        public enum MessageBoxButton
        {
            OK,
            OKCancel,
            YesNo,
            YesNoCancel
        }

        // Async overload used with await (two args)
        public static Task<MessageBoxButton> Show(string titleKey, string messageKey)
        {
            // Minimal implementation: always return OK
            return Task.FromResult(MessageBoxButton.OK);
        }

        // Async overload used with await (three args)
        public static Task<MessageBoxButton> Show(string titleKey, string messageKey, MessageBoxButton buttons)
        {
            // Minimal implementation: always return OK
            return Task.FromResult(MessageBoxButton.OK);
        }

        // Sync overload used in some places
        public static MessageBoxButton Show(string titleKey, string messageKey, MessageBoxButton buttons, bool sync)
        {
            return MessageBoxButton.OK;
        }
    }
}
