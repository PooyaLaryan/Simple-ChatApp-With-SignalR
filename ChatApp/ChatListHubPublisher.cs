using Microsoft.AspNetCore.SignalR;

public class ChatListHubPublisher
{
    private readonly IHubContext<ChatListHub, IChatListHub> _hubContext;

    public ChatListHubPublisher(IHubContext<ChatListHub, IChatListHub> hubContext)
    {
        _hubContext = hubContext;
    }

    // اطلاع‌رسانی به همه کاربران که یک کاربر جدید وارد سیستم شده است
    public async Task NotifyNewUserJoinedAsync(string newUsername)
    {
        await _hubContext.Clients.All.NewUserJoined(newUsername);
    }

    // به‌روزرسانی لحظه‌ای تعداد پیام‌های خوانده‌نشده در لیست چت گیرنده
    public async Task NotifyChatListUpdateAsync(string recipientConnectionId, string sender, string message, int unreadCount)
    {
        await _hubContext.Clients.Client(recipientConnectionId).UpdateChatList(sender, message, unreadCount);
    }
}