using System.ComponentModel.DataAnnotations;

namespace MxmChallenge.Models
{
    public class FinanceSyncConflict
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public string Entity { get; set; } = null!;

        [Required]
        public string EntityId { get; set; } = null!;

        [Required]
        public string Field { get; set; } = null!;

        [Required]
        public string LocalValueJson { get; set; } = null!;

        [Required]
        public string RemoteValueJson { get; set; } = null!;

        public bool Resolved { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual User? User { get; set; }
    }
}
