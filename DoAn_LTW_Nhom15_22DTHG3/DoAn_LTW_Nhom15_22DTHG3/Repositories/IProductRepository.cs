using DoAn_LTW_Nhom15_22DTHG3.Models;

namespace DoAn_LTW_Nhom15_22DTHG3.Repositories
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllAsync();
        Task<Product> GetByIdAsync(int id);
        Task AddAsync(Product product);
        Task UpdateAsync(Product product);
        Task DeleteAsync(int id);
    }
}
