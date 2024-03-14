using Orders.Shared.Responses;

namespace Orders.Backend.UnitsWork.Interfaces
{
    public interface IGenericUnitOfWork<T> where T : class
    {
        Task<ActionResponse<T>> GetAsync(int id);
        Task<ActionResponse<IEnumerable<T>>> GetAsync();
        Task<ActionResponse<T>> AddAsync(T model);
        Task<ActionResponse<T>> DeleteAsync(int id);
        Task<ActionResponse<T>> UpdateAsync(T model);
    }
}
