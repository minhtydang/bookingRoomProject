using DoAn_LTW_Nhom15_22DTHG3.Models;
using Microsoft.EntityFrameworkCore;

namespace DoAn_LTW_Nhom15_22DTHG3.Repositories
{
    public class EFProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _context;
        public EFProductRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await _context.Products
            .Include(p => p.Category)
            .ToListAsync();

        }

        public async Task<Product> GetByIdAsync(int id)
        {
            return await _context.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task AddAsync(Product product)
        {
            product.Id = 0; // Đảm bảo không tự đặt ID
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
        }


        public async Task UpdateAsync(Product product)
        {
            var existingProduct = await _context.Products.FindAsync(product.Id);
            if (existingProduct == null) return;

            // Cập nhật từng thuộc tính
            existingProduct.Name = product.Name;
            existingProduct.Price = product.Price;
            existingProduct.Description = product.Description;
            existingProduct.ImageUrl = product.ImageUrl;
            existingProduct.ExtraImageUrls = product.ExtraImageUrls;
            existingProduct.CategoryId = product.CategoryId;
            
            // Cập nhật thông tin location
            existingProduct.Address = product.Address;
            existingProduct.Region = product.Region;
            existingProduct.Area = product.Area;
            existingProduct.Latitude = product.Latitude;
            existingProduct.Longitude = product.Longitude;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }
    }
}
