using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Models;
using FourTaste.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FourTaste.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public UserRepository(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            using var context = _contextFactory.CreateDbContext();
            context.EnsureDatabaseReady();
            return await context.Users.FindAsync(id);
        }

        public async Task<IReadOnlyList<User>> ListAsync()
        {
            using var context = _contextFactory.CreateDbContext();
            context.EnsureDatabaseReady();
            return await context.Users.ToListAsync();
        }

        public async Task AddAsync(User entity)
        {
            using var context = _contextFactory.CreateDbContext();
            context.EnsureDatabaseReady();
            await context.Users.AddAsync(entity);
            await context.SaveChangesAsync();
        }

        public async Task DeleteAsync(User entity)
        {
            using var context = _contextFactory.CreateDbContext();
            context.EnsureDatabaseReady();
            context.Users.Remove(entity);
            await context.SaveChangesAsync();
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            using var context = _contextFactory.CreateDbContext();
            context.EnsureDatabaseReady();
            return await context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task UpdateAsync(User entity)
        {
            using var context = _contextFactory.CreateDbContext();
            context.EnsureDatabaseReady();
            context.Users.Update(entity);
            await context.SaveChangesAsync();
        }

        public async Task<List<UserSummaryDto>> SearchUsersAsync(string? searchTerm, UserSearchFilter filter)
        {
            using var context = _contextFactory.CreateDbContext();
            context.EnsureDatabaseReady();

            var term = (searchTerm ?? string.Empty).Trim().ToLower().TrimStart('@');
            var users = context.Users.AsNoTracking();

            users = term.Length == 0 ? users : filter switch
            {
                UserSearchFilter.Name => users.Where(u => u.Name.ToLower().Contains(term)),
                UserSearchFilter.Username => users.Where(u => u.Username.ToLower().Contains(term)),
                UserSearchFilter.Email => users.Where(u => u.Email.ToLower().Contains(term)),
                _ => users.Where(u =>
                    u.Name.ToLower().Contains(term) ||
                    u.Username.ToLower().Contains(term) ||
                    u.Email.ToLower().Contains(term)),
            };

            return await users
                .OrderByDescending(u => u.Role)
                .ThenByDescending(u => u.CreatedAt ?? DateTime.MinValue)
                .ThenBy(u => u.Username)
                .Select(u => new UserSummaryDto
                {
                    Id = u.ID,
                    Name = u.Name,
                    Username = u.Username,
                    Email = u.Email,
                    Avatar = u.Avatar,
                    RecipesCount = context.Recipes.Count(r => r.UserId == u.ID),
                    CreatedAt = u.CreatedAt,
                    IsAdmin = u.Role == UserRole.Admin,
                    IsBlocked = u.IsBlocked,
                })
                .ToListAsync();
        }

        public async Task<int> CountAdminsAsync()
        {
            using var context = _contextFactory.CreateDbContext();
            context.EnsureDatabaseReady();
            return await context.Users.CountAsync(u => u.Role == UserRole.Admin);
        }
    }
}
