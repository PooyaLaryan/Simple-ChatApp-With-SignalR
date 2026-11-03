using System;
using System.ComponentModel.DataAnnotations;

public class User
{
    [Key]
    public int Id { get; set; }
    public string Username { get; set; }
    public string ConnectionId { get; set; }
    public string CurrentRoom { get; set; }
}

public class Message
{
    [Key]
    public int Id { get; set; }
    public string MessageId { get; set; } // همان شناسه یکتای پیام برای تیک خوردن
    public string RoomName { get; set; }
    public string Sender { get; set; }
    public string Content { get; set; }
    public bool IsRead { get; set; } = false; // وضعیت خوانده شده
    public DateTime SentAt { get; set; } = DateTime.Now;
}