using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

public class ChatHubPublisher
{
    private readonly IHubContext<ChatHub, IChatHub> _hubContext; // اضافه کردن این خط
    private readonly ChatDbContext _context;
    private readonly ChatListHubPublisher _chatListPublisher;

    // تزریق IHubContext به سازنده کلاس
    public ChatHubPublisher(
        IHubContext<ChatHub, IChatHub> hubContext,
        ChatDbContext context,
        ChatListHubPublisher chatListPublisher)
    {
        _hubContext = hubContext;
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
        // پیدا کردن پیام‌های خوانده نشده‌ای که این کاربر (readerUser) گیرنده آن‌ها بوده است
        var unreadMessages = await _context.Messages
            .Where(m => m.RoomName == roomName && m.Recipient == readerUser && !m.IsRead)
            .ToListAsync();

        foreach (var msg in unreadMessages)
        {
            msg.IsRead = true;

            // ۱. پیدا کردن فرستنده پیام و خبر دادن به او (برای تیک خوردن پیام‌هایی که او فرستاده بود)
            var senderUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == msg.Sender);
            if (senderUser != null && !string.IsNullOrEmpty(senderUser.ConnectionId))
            {
                await _hubContext.Clients.Client(senderUser.ConnectionId).MessageRead(msg.MessageId);
            }
        }
        await _context.SaveChangesAsync();

        // ۲. خبر دادن به خودِ شخصی که پیام‌ها را خوانده (readerUser) تا اگر پیامی از طرف مقابل در صفحه اش مانده که یک تیک است، دو تیک بخورد
        var readerClient = await _context.Users.FirstOrDefaultAsync(u => u.Username == readerUser);
        if (readerClient != null && !string.IsNullOrEmpty(readerClient.ConnectionId))
        {
            // تمام پیام‌هایی که در این اتاق فرستنده اش کاربر مقابل بوده و حالا خوانده شده‌اند را به این کلاینت هم اعلام می‌کنیم
            foreach (var msg in unreadMessages)
            {
                await _hubContext.Clients.Client(readerClient.ConnectionId).MessageRead(msg.MessageId);
            }
        }
    }
}