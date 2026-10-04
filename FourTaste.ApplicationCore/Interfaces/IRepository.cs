namespace FourTaste.ApplicationCore.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IReadOnlyList<T>> ListAsync();
    Task AddAsync(T entity);
    Task DeleteAsync(T entity);
}

