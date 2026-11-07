using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ShotForge.Core.Models;

namespace ShotForge.Data.Repositories
{
    public class ShotRepository
    {
        private readonly ShotForgeDbContext _db;

        public ShotRepository(ShotForgeDbContext db) => _db = db;

        // CREATE
        public async Task<Shot> CreateAsync(
            int sceneId,
            int shotNumber,
            string description,
            int durationSeconds,
            string styleLockTag,
            IEnumerable<int>? characterIds = null,
            string? negativePrompt = null)
        {
            var shot = new Shot
            {
                SceneId = sceneId,
                ShotNumber = shotNumber,
                Description = description,
                DurationSeconds = durationSeconds,
                StyleLockTag = styleLockTag,
                NegativePrompt = negativePrompt
            };

            if (characterIds is not null)
            {
                var characters = await _db.Characters
                    .Where(c => characterIds.Contains(c.Id))
                    .ToListAsync();
                shot.Characters = characters;
            }

            _db.Shots.Add(shot);
            await _db.SaveChangesAsync();
            return shot;
        }

        // READ (all shots for a scene, ordered)
        public async Task<List<Shot>> GetBySceneIdAsync(int sceneId) =>
            await _db.Shots
                .Where(s => s.SceneId == sceneId)
                .Include(s => s.Characters)
                .OrderBy(s => s.ShotNumber)
                .AsNoTracking()
                .ToListAsync();

        // READ (single)
        public async Task<Shot?> GetByIdAsync(int id) =>
            await _db.Shots
                .Include(s => s.Characters)
                .FirstOrDefaultAsync(s => s.Id == id);

        // UPDATE (full edit: scalars + character attachments)
        //
        // Deliberately NOT reusing the generic UpdateAsync(Shot) below for
        // this - Characters is a many-to-many relationship, and safely
        // re-diffing which characters are attached requires a properly
        // TRACKED query (Include + mutate + SaveChanges), not just calling
        // Update() on a detached shot object with a reassigned list. The
        // generic UpdateAsync is fine for Project/Scene, which have no
        // many-to-many relationships to worry about - Shot needs this
        // more careful version specifically because of Characters.
        public async Task<Shot?> UpdateDetailsAsync(
            int shotId,
            string description,
            string? negativePrompt,
            int durationSeconds,
            string styleLockTag,
            IEnumerable<int> characterIds)
        {
            var shot = await _db.Shots
                .Include(s => s.Characters)
                .FirstOrDefaultAsync(s => s.Id == shotId);

            if (shot is null) return null;

            shot.Description = description;
            shot.NegativePrompt = negativePrompt;
            shot.DurationSeconds = durationSeconds;
            shot.StyleLockTag = styleLockTag;

            var newCharacters = await _db.Characters
                .Where(c => characterIds.Contains(c.Id))
                .ToListAsync();

            shot.Characters.Clear();
            foreach (var character in newCharacters)
            {
                shot.Characters.Add(character);
            }

            await _db.SaveChangesAsync();
            return shot;
        }

        // UPDATE (scalar-only, e.g. from a context with no many-to-many
        // changes to worry about - prefer UpdateDetailsAsync above when
        // Characters might have changed)
        public async Task UpdateAsync(Shot shot)
        {
            _db.Shots.Update(shot);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateStatusAsync(int shotId, ShotStatus newStatus)
        {
            var shot = await _db.Shots.FindAsync(shotId);
            if (shot is null) return;

            shot.Status = newStatus;
            await _db.SaveChangesAsync();
        }

        // DELETE
        public async Task DeleteAsync(int id)
        {
            var shot = await _db.Shots.FindAsync(id);
            if (shot is null) return;

            _db.Shots.Remove(shot);
            await _db.SaveChangesAsync();
        }
    }
}
