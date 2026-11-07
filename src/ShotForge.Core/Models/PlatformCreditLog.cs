using System;

namespace ShotForge.Core.Models
{
    public enum CreditResetType
    {
        Daily,
        Monthly,
        OneTime
    }

    /// <summary>
    /// One day's credit usage entry for a given generation platform
    /// (Kling, Vidu, Seedance, etc.), so spend and remaining allowance
    /// can be tracked across multiple tools in one place.
    /// </summary>
    public class PlatformCreditLog
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public Project? Project { get; set; }

        public string PlatformName { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.UtcNow.Date;
        public int CreditsUsed { get; set; }
        public int CreditsRemaining { get; set; }
        public CreditResetType ResetType { get; set; } = CreditResetType.Daily;
    }
}
