using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ShotForge.Core.Models;

namespace ShotForge.Data.Repositories
{
    public class ProjectRepository
    {
        private readonly ShotForgeDbContext _db;

        public ProjectRepository(ShotForgeDbContext db) => _db = db;

        // CREATE
        public async Task<Project> CreateAsync(string name)
        {
            var project = new Project { Name = name };
            _db.Projects.Add(project);
            await _db.SaveChangesAsync();
            return project;
        }

        // READ (all)
        public async Task<List<Project>> GetAllAsync() =>
            await _db.Projects
                .Include(p => p.Scenes)
                .Include(p => p.Characters)
                .AsNoTracking()
                .ToListAsync();

        // READ (single, with full graph for editing)
        public async Task<Project?> GetByIdAsync(int id) =>
            await _db.Projects
                .Include(p => p.Scenes).ThenInclude(s => s.Shots).ThenInclude(sh => sh.Characters)
                .Include(p => p.Characters)
                .FirstOrDefaultAsync(p => p.Id == id);

        // UPDATE
        public async Task UpdateAsync(Project project)
        {
            _db.Projects.Update(project);
            await _db.SaveChangesAsync();
        }

        // DELETE
        public async Task DeleteAsync(int id)
        {
            var project = await _db.Projects.FindAsync(id);
            if (project is null) return;

            _db.Projects.Remove(project);
            await _db.SaveChangesAsync();
        }
    }
}
