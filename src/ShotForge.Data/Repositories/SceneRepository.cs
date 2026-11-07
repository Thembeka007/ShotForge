using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ShotForge.Core.Models;

namespace ShotForge.Data.Repositories
{
    public class SceneRepository
    {
        private readonly ShotForgeDbContext _db;

        public SceneRepository(ShotForgeDbContext db) => _db = db;

        // CREATE
        public async Task<Scene> CreateAsync(int projectId, int sceneNumber, string title)
        {
            var scene = new Scene
            {
                ProjectId = projectId,
                SceneNumber = sceneNumber,
                Title = title
            };
            _db.Scenes.Add(scene);
            await _db.SaveChangesAsync();
            return scene;
        }

        // READ (all scenes for a project, ordered, with their shots also ordered)
        public async Task<List<Scene>> GetByProjectIdAsync(int projectId) =>
            await _db.Scenes
                .Where(s => s.ProjectId == projectId)
                .Include(s => s.Shots.OrderBy(sh => sh.ShotNumber))
                .OrderBy(s => s.SceneNumber)
                .AsNoTracking()
                .ToListAsync();

        // READ (single)
        public async Task<Scene?> GetByIdAsync(int id) =>
            await _db.Scenes
                .Include(s => s.Shots).ThenInclude(sh => sh.Characters)
                .FirstOrDefaultAsync(s => s.Id == id);

        // UPDATE
        public async Task UpdateAsync(Scene scene)
        {
            _db.Scenes.Update(scene);
            await _db.SaveChangesAsync();
        }

        // DELETE
        public async Task DeleteAsync(int id)
        {
            var scene = await _db.Scenes.FindAsync(id);
            if (scene is null) return;

            _db.Scenes.Remove(scene);
            await _db.SaveChangesAsync();
        }
    }
}
