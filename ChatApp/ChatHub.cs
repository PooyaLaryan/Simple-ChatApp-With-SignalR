using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

public class ChatHub : Hub<IChatHub>
{
    private readonly ChatDbContext _context;
    private readonly ChatHubPublisher _publisher;

    public ChatHub(ChatDbContext context, ChatHubPublisher publisher)
    {
        _context = context;
        _publisher = publisher;
    }

    public async Task JoinRoom(string user, string roomName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomName);

        var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == user);
        bool isNew = false;
        if (dbUser == null)
        {
            _context.Users.Add(new User { Username = user, ConnectionId = Context.ConnectionId });
            isNew = true;
        }
        else
        {
            dbUser.ConnectionId = Context.ConnectionId;
        }
        await _context.SaveChangesAsync();

        if (isNew)
        {
            await Clients.Others.NewUserJoined(user);
        }

        var history = await _context.Messages
            .Where(m => m.RoomName == roomName)
            .OrderBy(m => m.SentAt)
            .ToListAsync();

        foreach (var msg in history)
        {
            await Clients.Caller.ReceiveMessage(msg.MessageId, msg.Sender, msg.Content);
            if (msg.IsRead)
            {
                await Clients.Caller.MessageRead(msg.MessageId);
            }
        }
    }

    public async Task GetUserChats(string username)
    {
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

        await Clients.Caller.ReceiveChatList(chatList);
    }

    public async Task SendMessage(string roomName, string messageId, string sender, string recipient, string message)
    {
        // ۱. ذخیره پیام در دیتابیس توسط پابلشر
        await _publisher.SendMessageAsync(roomName, messageId, sender, recipient, message);

        // ۲. ارسال پیام فقط به سایر اعضای اتاق (به جز فرستنده) - بدون خطا و کاملاً بومی در Hub
        await Clients.OthersInGroup(roomName).ReceiveMessage(messageId, sender, message);
    }

    public async Task MarkMessagesAsRead(string roomName, string readerUser)
    {
        await _publisher.MarkMessagesAsReadAsync(roomName, readerUser);
        await Clients.Group(roomName).MessageRead(readerUser); // اطلاع‌رسانی به گروه
    }

    public async Task SendTypingStatus(string roomName, string user, bool isTyping)
    {
        // استفاده از OthersInGroup در اینجا به راحتی و بدون خطا کار می‌کند
        await Clients.OthersInGroup(roomName).ReceiveTypingStatus(user, isTyping);
    }

    private string GetRoomName(string user1, string user2)
    {
        var comparison = string.Compare(user1, user2, StringComparison.OrdinalIgnoreCase);
        return comparison < 0 ? $"{user1}_{user2}" : $"{user2}_{user1}";
    }
}