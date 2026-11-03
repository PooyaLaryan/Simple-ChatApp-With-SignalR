using System;
using System.ComponentModel.DataAnnotations;
public class User
{
    [Key]
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public string ConnectionId { get; set; } = null!;
}

public class Message
{
    [Key]
    public int Id { get; set; }
    public string MessageId { get; set; } = null!; // شناسه یکتا برای هر پیام
    public string RoomName { get; set; } = null!; // نام اتاق ترکیبی (مثلا UserA_UserB)
    public string Sender { get; set; } = null!;
    public string Recipient { get; set; } = null!; // گیرنده پیام
    public string Content { get; set; } = null!;
    public bool IsRead { get; set; } = false;
    public DateTime SentAt { get; set; } = DateTime.Now;
}