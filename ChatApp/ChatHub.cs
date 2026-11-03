using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

public class ChatHub : Hub
{
    private readonly ChatDbContext _context;
    private readonly IHubContext<ChatListHub> _chatListHubContext;

    public ChatHub(ChatDbContext context, IHubContext<ChatListHub> chatListHubContext)
    {
        _context = context;
        _chatListHubContext = chatListHubContext;
    }

    public async Task JoinRoom(string user, string roomName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomName);

        // ارسال تاریخچه پیام‌ها
        var history = await _context.Messages
            .Where(m => m.RoomName == roomName)
            .OrderBy(m => m.SentAt)
            .ToListAsync();

        foreach (var msg in history)
        {
            await Clients.Caller.SendAsync("ReceiveMessage", msg.MessageId, msg.Sender, msg.Content);
            if (msg.IsRead)
            {
                await Clients.Caller.SendAsync("MessageRead", msg.MessageId);
            }
        }
    }

    public async Task SendMessage(string roomName, string messageId, string sender, string recipient, string message)
    {
        var newMessage = new Message
        {
            MessageId = messageId,
            RoomName = roomName,
            Sender = sender,
            Recipient = recipient,
            Content = message,
            IsRead = false
        };
        _context.Messages.Add(newMessage);
        await _context.SaveChangesAsync();

        // ارسال پیام به افراد داخل اتاق چت
        await Clients.OthersInGroup(roomName).SendAsync("ReceiveMessage", messageId, sender, message);

        // آپدیت لحظه‌ای لیست چت (تعداد پیام خوانده نشده) برای کاربر گیرنده
        var recipientUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == recipient);
        if (recipientUser != null && !string.IsNullOrEmpty(recipientUser.ConnectionId))
        {
            int unreadCount = await _context.Messages
                .CountAsync(m => m.RoomName == roomName && m.Sender == sender && !m.IsRead);

            await _chatListHubContext.Clients.Client(recipientUser.ConnectionId)
                .SendAsync("UpdateChatList", sender, message, unreadCount);
        }
    }

    // علامت‌گذاری پیام‌ها به عنوان خوانده شده وقتی کاربر وارد چت می‌شود
    public async Task MarkMessagesAsRead(string roomName, string readerUser)
    {
        var unreadMessages = await _context.Messages
            .Where(m => m.RoomName == roomName && m.Sender != readerUser && !m.IsRead)
            .ToListAsync();

        foreach (var msg in unreadMessages)
        {
            msg.IsRead = true;
            await Clients.Group(roomName).SendAsync("MessageRead", msg.MessageId);
        }
        await _context.Messages.Where(m => m.RoomName == roomName && m.Sender != readerUser && !m.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsRead, true));
    }

    public async Task SendTypingStatus(string roomName, string user, bool isTyping)
    {
        await Clients.OthersInGroup(roomName).SendAsync("ReceiveTypingStatus", user, isTyping);
    }
}