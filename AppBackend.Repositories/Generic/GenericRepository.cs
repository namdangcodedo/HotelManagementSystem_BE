using System.Linq.Expressions;
using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Repositories.Generic
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly HotelManagementContext Context;

        public GenericRepository(HotelManagementContext context)
        {
            Context = context;
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await Context.Set<T>().ToListAsync();
        }

        public async Task<T?> GetByIdAsync(object id)
        {
            return await Context.Set<T>().FindAsync(id);
        }

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await Context.Set<T>().Where(predicate).ToListAsync();
        }

        public async Task AddAsync(T entity)
        {
            await Context.Set<T>().AddAsync(entity);
        }

        public async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await Context.Set<T>().AddRangeAsync(entities);
        }

        public Task UpdateAsync(T entity)
        {
            Context.Set<T>().Update(entity);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(T entity)
        {
            Context.Set<T>().Remove(entity);
            await Task.CompletedTask;
        }

        public async Task DeleteByIdAsync(object id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                Context.Set<T>().Remove(entity);
            }
        }

        public async Task<T?> GetSingleAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = Context.Set<T>();
            if (includes != null)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }
            return await query.FirstOrDefaultAsync(predicate);
        }
    }
}
