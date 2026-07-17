using System;

namespace CaveroSalud.Domain.Entities
{
    public class UserNotification
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid? SenderUserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "info";
        public string? SourceKey { get; set; }
        public string? DetailUrl { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
    }
}
