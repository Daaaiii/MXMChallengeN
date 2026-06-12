using System.Text.Json;

namespace MXMChallenge.DTOs
{
    public class FinanceStateResponseDTO
    {
        public bool Exists { get; set; }
        public int ServerVersion { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public JsonElement State { get; set; }
    }

    public class FinanceSyncRequestDTO
    {
        public int BaseVersion { get; set; }
        public JsonElement LocalState { get; set; }
    }

    public class FinanceSyncResponseDTO
    {
        public string Source { get; set; } = null!;
        public int ServerVersion { get; set; }
        public JsonElement State { get; set; }
        public List<FinanceSyncConflictDTO> Conflicts { get; set; } = [];
    }

    public class FinanceSyncConflictDTO
    {
        public string Entity { get; set; } = null!;
        public string EntityId { get; set; } = null!;
        public string Field { get; set; } = null!;
        public JsonElement LocalValue { get; set; }
        public JsonElement RemoteValue { get; set; }
    }
}
