using DoAn_LTW_Nhom15_22DTHG3.Models;

namespace DoAn_LTW_Nhom15_22DTHG3.Repositories
{
    public interface ICategoryRepository
    {
        Task<IEnumerable<Category>> GetAllAsync();
        Task<Category> GetByIdAsync(int id);
        Task AddAsync(Category category);
        Task UpdateAsync(Category category);
        Task DeleteAsync(int id);

    }
}
