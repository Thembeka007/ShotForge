using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ShotForge.Core.Models;

namespace ShotForge.Data.Repositories
{
    public class CharacterRepository
    {
        private readonly ShotForgeDbContext _db;

        public CharacterRepository(ShotForgeDbContext db) => _db = db;

        // CREATE
        public async Task<Character> CreateAsync(
            int projectId, string name, string standingDescription, string? referenceImagePath = null)
        {
            var character = new Character
            {
                ProjectId = projectId,
                Name = name,
                StandingDescription = standingDescription,
                ReferenceImagePath = referenceImagePath
            };
            _db.Characters.Add(character);
            await _db.SaveChangesAsync();
            return character;
        }

        // READ (all for a project)
        public async Task<List<Character>> GetByProjectIdAsync(int projectId) =>
            await _db.Characters
                .Where(c => c.ProjectId == projectId)
                .AsNoTracking()
                .ToListAsync();

        // READ (single)
        public async Task<Character?> GetByIdAsync(int id) =>
            await _db.Characters.FindAsync(id);

        // UPDATE
        public async Task UpdateAsync(Character character)
        {
            _db.Characters.Update(character);
            await _db.SaveChangesAsync();
        }

        // DELETE
        public async Task DeleteAsync(int id)
        {
            var character = await _db.Characters.FindAsync(id);
            if (character is null) return;

            _db.Characters.Remove(character);
            await _db.SaveChangesAsync();
        }
    }
}
