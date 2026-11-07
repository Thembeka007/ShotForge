using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ShotForge.Core.Models;

namespace ShotForge.Data.Repositories
{
    public class CreditLogRepository
    {
        private readonly ShotForgeDbContext _db;

        public CreditLogRepository(ShotForgeDbContext db) => _db = db;

        // CREATE
        public async Task<PlatformCreditLog> LogUsageAsync(
            int projectId, string platformName, int creditsUsed, int creditsRemaining, CreditResetType resetType)
        {
            var entry = new PlatformCreditLog
            {
                ProjectId = projectId,
                PlatformName = platformName,
                Date = DateTime.UtcNow.Date,
                CreditsUsed = creditsUsed,
                CreditsRemaining = creditsRemaining,
                ResetType = resetType
            };
            _db.CreditLogs.Add(entry);
            await _db.SaveChangesAsync();
            return entry;
        }

        // READ (all logs for a project)
        public async Task<List<PlatformCreditLog>> GetByProjectIdAsync(int projectId) =>
            await _db.CreditLogs
                .Where(c => c.ProjectId == projectId)
                .OrderByDescending(c => c.Date)
                .AsNoTracking()
                .ToListAsync();

        // DELETE
        public async Task DeleteAsync(int id)
        {
            var entry = await _db.CreditLogs.FindAsync(id);
            if (entry is null) return;

            _db.CreditLogs.Remove(entry);
            await _db.SaveChangesAsync();
        }

        /// <summary>
        /// Rough "days remaining at current pace" calculation — the same
        /// math from the manual phased shot list, now automatic.
        /// </summary>
        public static int EstimateDaysRemaining(int shotsRemaining, int clipsPerDay) =>
            clipsPerDay <= 0 ? -1 : (int)Math.Ceiling(shotsRemaining / (double)clipsPerDay);
    }
}
