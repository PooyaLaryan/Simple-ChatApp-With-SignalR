public interface IChatListHub
{
    Task ReceiveChatList(object chatList);
    Task NewUserJoined(string newUsername);
    Task UpdateChatList(string sender, string message, int unreadCount);
}