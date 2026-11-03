using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

public class ChatHub : Hub
{
    private readonly ChatDbContext _context;

    public ChatHub(ChatDbContext context)
    {
        _context = context;
    }

    public async Task JoinRoom(string user, string roomName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomName);

        // ذخیره یا به‌روزرسانی اطلاعات کاربر در دیتابیس
        var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == user);
        if (dbUser == null)
        {
            _context.Users.Add(new User { Username = user, ConnectionId = Context.ConnectionId, CurrentRoom = roomName });
        }
        else
        {
            dbUser.ConnectionId = Context.ConnectionId;
            dbUser.CurrentRoom = roomName;
        }
        await _context.SaveChangesAsync();

        // ارسال تاریخچه پیام‌های قبلی این اتاق به کاربری که تازه وارد شده
        var history = await _context.Messages
            .Where(m => m.RoomName == roomName)
            .OrderBy(m => m.SentAt)
            .ToListAsync();

        foreach (var msg in history)
        {
            // ارسال پیام‌های قدیمی به کلاینت
            await Clients.Caller.SendAsync("ReceiveMessage", msg.MessageId, msg.Sender, msg.Content);
            if (msg.IsRead)
            {
                await Clients.Caller.SendAsync("MessageRead", msg.MessageId);
            }
        }

        await Clients.OthersInGroup(roomName).SendAsync("ReceiveMessage", "sys_" + Guid.NewGuid(), "سیستم", $"{user} وارد اتاق شد.");
    }

    public async Task SendMessage(string roomName, string messageId, string user, string message)
    {
        // ذخیره پیام در دیتابیس
        var newMessage = new Message
        {
            MessageId = messageId,
            RoomName = roomName,
            Sender = user,
            Content = message,
            IsRead = false
        };
        _context.Messages.Add(newMessage);
        await _context.SaveChangesAsync();

        await Clients.OthersInGroup(roomName).SendAsync("ReceiveMessage", messageId, user, message);
    }

    public async Task MarkAsRead(string roomName, string messageId)
    {
        // به‌روزرسانی وضعیت پیام در دیتابیس به خوانده شده
        var msg = await _context.Messages.FirstOrDefaultAsync(m => m.MessageId == messageId);
        if (msg != null)
        {
            msg.IsRead = true;
            await _context.SaveChangesAsync();
        }

        await Clients.Group(roomName).SendAsync("MessageRead", messageId);
    }

    public async Task SendTypingStatus(string roomName, string user, bool isTyping)
    {
        await Clients.OthersInGroup(roomName).SendAsync("ReceiveTypingStatus", user, isTyping);
    }
}