using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

public class ChatListHub : Hub
{
    private readonly ChatDbContext _context;

    public ChatListHub(ChatDbContext context)
    {
        _context = context;
    }

    public async Task GetUserChats(string username)
    {
        // ثبت یا آپدیت اتصال کاربر
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
        {
            _context.Users.Add(new User { Username = username, ConnectionId = Context.ConnectionId });
            await _context.SaveChangesAsync();

            // اطلاع به سایر کاربران آنلاین که یک کاربر جدید وارد سیستم شده است
            await Clients.Others.SendAsync("NewUserJoined", username);
        }
        else
        {
            user.ConnectionId = Context.ConnectionId;
            await _context.SaveChangesAsync();
        }

        // دریافت تمام کاربران دیگر به عنوان مخاطبین چت
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

        await Clients.Caller.SendAsync("ReceiveChatList", chatList);
    }

    private string GetRoomName(string user1, string user2)
    {
        var comparison = string.Compare(user1, user2, StringComparison.OrdinalIgnoreCase);
        return comparison < 0 ? $"{user1}_{user2}" : $"{user2}_{user1}";
    }
}