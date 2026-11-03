public interface IChatHub
{
    Task ReceiveMessage(string messageId, string sender, string message);
    Task MessageRead(string messageId);
    Task ReceiveTypingStatus(string user, bool isTyping);
    Task NewUserJoined(string newUsername);
    Task UpdateChatList(string sender, string message, int unreadCount);
    Task ReceiveChatList(object chatList);
}