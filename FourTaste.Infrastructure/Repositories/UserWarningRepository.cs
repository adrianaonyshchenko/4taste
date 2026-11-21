using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FourTaste.Infrastructure.Repositories
{
    public class UserWarningRepository : IUserWarningRepository
    {
        private readonly IDbContextFactory<AppDbContext> contextFactory;

        public UserWarningRepository(IDbContextFactory<AppDbContext> contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        public async Task AddAsync(UserWarning warning)
        {
            using var context = await this.contextFactory.CreateDbContextAsync();
            context.EnsureDatabaseReady();
            context.UserWarnings.Add(warning);
            await context.SaveChangesAsync();
        }

        public async Task<List<UserWarning>> GetUnreadAsync(int userId)
        {
            using var context = await this.contextFactory.CreateDbContextAsync();
            context.EnsureDatabaseReady();
            return await context.UserWarnings
                .AsNoTracking()
                .Where(w => w.UserId == userId && !w.IsRead)
                .OrderByDescending(w => w.CreatedAt)
                .ThenByDescending(w => w.Id)
                .ToListAsync();
        }

        public async Task MarkAllReadAsync(int userId)
        {
            using var context = await this.contextFactory.CreateDbContextAsync();
            context.EnsureDatabaseReady();

            var unread = await context.UserWarnings
                .Where(w => w.UserId == userId && !w.IsRead)
                .ToListAsync();

            foreach (var warning in unread)
            {
                warning.IsRead = true;
            }

            await context.SaveChangesAsync();
        }
    }
}