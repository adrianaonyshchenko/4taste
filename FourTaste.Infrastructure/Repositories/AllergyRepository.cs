using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FourTaste.Infrastructure.Repositories
{
    public class AllergyRepository : IRepository<Allergy>
    {
        private readonly AppDbContext _context;

        public AllergyRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Allergy?> GetByIdAsync(int id) => await _context.Allergies.FindAsync(id);

        public async Task<IReadOnlyList<Allergy>> ListAsync() => await _context.Allergies.ToListAsync();

        public async Task AddAsync(Allergy entity)
        {
            await _context.Allergies.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Allergy entity)
        {
            _context.Allergies.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
