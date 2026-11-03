using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

public class ChatListHub : Hub<IChatListHub>
{
    private readonly ChatDbContext _context;
    private readonly ChatListHubPublisher _publisher;

    public ChatListHub(ChatDbContext context, ChatListHubPublisher publisher)
    {
        _context = context;
        _publisher = publisher;
    }

    public async Task GetUserChats(string username)
    {
        // ثبت یا آپدیت اتصال کاربر
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        bool isNew = false;
        if (user == null)
        {
            _context.Users.Add(new User { Username = username, ConnectionId = Context.ConnectionId });
            isNew = true;
        }
        else
        {
            user.ConnectionId = Context.ConnectionId;
        }
        await _context.SaveChangesAsync();

        // اگر کاربر جدید است، به همه اعلام کن
        if (isNew)
        {
            await _publisher.NotifyNewUserJoinedAsync(username);
        }

        // استخراج لیست مخاطبین و تعداد پیام‌های خوانده‌نشده
        var allUsers = await _context.Users
            .Where(u => u.Username != username)
            .ToListAsync();

        var chatList = new List<object>();

        foreach (var otherUser in allUsers)
        {
            string roomName = GetRoomName(username, otherUser.Username);

            int unreadCount = await _context.Messages
                .CountAsync(m => m.RoomName == roomName && m.Sender == otherUser.Username && !m.IsRead);

            var lastMessage = await _context.Messages
                .Where(m => m.RoomName == roomName)
                .OrderByDescending(m => m.SentAt)
                .FirstOrDefaultAsync();

            chatList.Add(new
            {
                username = otherUser.Username,
                unreadCount = unreadCount,
                lastMessage = lastMessage?.Content ?? "هنوز پیامی رد و بدل نشده است"
            });
        }

        // ارسال لیست به کلاینت درخواست‌دهنده
        await Clients.Caller.ReceiveChatList(chatList);
    }

    private string GetRoomName(string user1, string user2)
    {
        var comparison = string.Compare(user1, user2, StringComparison.OrdinalIgnoreCase);
        return comparison < 0 ? $"{user1}_{user2}" : $"{user2}_{user1}";
    }
}