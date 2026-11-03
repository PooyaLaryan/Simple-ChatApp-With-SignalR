using Microsoft.EntityFrameworkCore;

public class ChatHubPublisher
{
    private readonly ChatDbContext _context;
    private readonly ChatListHubPublisher _chatListPublisher;

    public ChatHubPublisher(ChatDbContext context, ChatListHubPublisher chatListPublisher)
    {
        _context = context;
        _chatListPublisher = chatListPublisher;
    }

    public async Task SendMessageAsync(string roomName, string messageId, string sender, string recipient, string message)
    {
        // ۱. ذخیره پیام در دیتابیس
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

        // ۲. آپدیت تعداد پیام‌های خوانده‌نشده در لیست چت گیرنده
        var recipientUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == recipient);
        if (recipientUser != null && !string.IsNullOrEmpty(recipientUser.ConnectionId))
        {
            int unreadCount = await _context.Messages
                .CountAsync(m => m.RoomName == roomName && m.Sender == sender && !m.IsRead);

            await _chatListPublisher.NotifyChatListUpdateAsync(recipientUser.ConnectionId, sender, message, unreadCount);
        }
    }

    public async Task MarkMessagesAsReadAsync(string roomName, string readerUser)
    {
        var unreadMessages = await _context.Messages
            .Where(m => m.RoomName == roomName && m.Sender != readerUser && !m.IsRead)
            .ToListAsync();

        foreach (var msg in unreadMessages)
        {
            msg.IsRead = true;
        }
        await _context.SaveChangesAsync();
    }
}