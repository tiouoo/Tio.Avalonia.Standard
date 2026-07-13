using Avalonia.Controls.Notifications;
using Tio.Avalonia.Standard.Tab.Extensions;
using TioUi.Common.Classes;
using TopLevel = Avalonia.Controls.TopLevel;

namespace Tio.Avalonia.Standard.Tab.Gateway;

public static class NotificationGateway
{
    public static Func<bool> IsToastFunc = null;
    
    public static void Notice(this TopLevel topLevel, NotificationOptions options)
    {
        if (IsToastFunc())
        {
            topLevel.TryGetToast()?.Show(options);
        }
        else
        {
            topLevel.TryGetNotification()?.Show(options);
        }
    }

    public static void Notice(this TopLevel topLevel, string msg, NotificationType type = NotificationType.Information)
    {
        if (IsToastFunc())
        {
            topLevel.TryGetToast()?.Show(msg, type);
        }
        else
        {
            topLevel.TryGetNotification()?.Show("Portal", msg, new NotificationOptions { Type = type });
        }
    }


    public static void Notice(this TopLevel topLevel, string msg, string title,
        NotificationType type = NotificationType.Information)
    {
        if (IsToastFunc())
        {
            topLevel.TryGetToast()?.Show(msg, type);
        }
        else
        {
            topLevel.TryGetNotification()?.Show(title, msg, new NotificationOptions { Type = type });
        }
    }
}