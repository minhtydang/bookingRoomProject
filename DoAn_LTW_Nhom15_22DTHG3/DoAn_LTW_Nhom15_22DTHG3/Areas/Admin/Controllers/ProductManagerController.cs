using DoAn_LTW_Nhom15_22DTHG3.Areas.Admin.Models;
using DoAn_LTW_Nhom15_22DTHG3.Helpers;
using DoAn_LTW_Nhom15_22DTHG3.Models;
using DoAn_LTW_Nhom15_22DTHG3.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DoAn_LTW_Nhom15_22DTHG3.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductManagerController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;

        public ProductManagerController(IProductRepository productRepository, ICategoryRepository categoryRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
        }

        public IActionResult Index(string searchTerm, string sortOrder)
        {
            var products = _productRepository.GetAllAsync().Result;

            if (!string.IsNullOrEmpty(searchTerm))
            {
                string normalizedSearch = searchTerm.RemoveDiacritics().ToLower();
                products = products.Where(p => p.Name.RemoveDiacritics().ToLower().Contains(normalizedSearch)).ToList();
            }

            switch (sortOrder)
            {
                case "name_asc":
                    products = products.OrderBy(p => p.Name).ToList();
                    break;
                case "name_desc":
                    products = products.OrderByDescending(p => p.Name).ToList();
                    break;
                case "price_asc":
                    products = products.OrderBy(p => p.Price).ToList();
                    break;
                case "price_desc":
                    products = products.OrderByDescending(p => p.Price).ToList();
                    break;
            }

            ViewBag.SearchTerm = searchTerm;
            return View(products);
        }


        public async Task<IActionResult> View(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = new SelectList(await _categoryRepository.GetAllAsync(), "Id", "Name");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Product product, List<string> ExternalVideoUrlList)
        {
            if (ModelState.IsValid)
            {
                await HandleImageUpload(product);

                // Xử lý URL video nhập tay
                if (ExternalVideoUrlList != null && ExternalVideoUrlList.Any())
                {
                    product.ExternalVideoUrls = string.Join(",", ExternalVideoUrlList.Where(url => !string.IsNullOrWhiteSpace(url)));
                }

                await _productRepository.AddAsync(product);
                return RedirectToAction("Index");
            }

            ViewBag.Categories = new SelectList(await _categoryRepository.GetAllAsync(), "Id", "Name", product.CategoryId);
            return View(product);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();

            ViewBag.Categories = new SelectList(await _categoryRepository.GetAllAsync(), "Id", "Name", product.CategoryId);
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Product product, List<string> KeepExtraImages, List<string> KeepVideoFiles, List<string> KeepExternalVideoUrls, List<string> ExternalVideoUrlList)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = new SelectList(await _categoryRepository.GetAllAsync(), "Id", "Name", product.CategoryId);
                return View(product);
            }

            var oldProduct = await _productRepository.GetByIdAsync(id);
            if (oldProduct == null) return NotFound();

            // === Ảnh chính ===
            if (product.ImageFile != null && product.ImageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(product.ImageFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await product.ImageFile.CopyToAsync(stream);
                }

                product.ImageUrl = "images/" + uniqueFileName;
            }
            else
            {
                product.ImageUrl = oldProduct.ImageUrl;
            }

            // === Ảnh phụ ===
            var remainingImageUrls = new List<string>();

            if (!string.IsNullOrEmpty(oldProduct.ExtraImageUrls))
            {
                foreach (var url in oldProduct.ExtraImageUrls.Split(","))
                {
                    var fileName = Path.GetFileName(url);
                    if (KeepExtraImages.Contains(fileName))
                    {
                        remainingImageUrls.Add(url); // giữ lại ảnh được chọn
                    }
                }
            }

            if (product.ExtraImageFiles != null && product.ExtraImageFiles.Any())
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                Directory.CreateDirectory(uploadsFolder);

                foreach (var file in product.ExtraImageFiles)
                {
                    if (file.Length > 0)
                    {
                        var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        remainingImageUrls.Add("images/" + uniqueFileName);
                    }
                }
            }

            product.ExtraImageUrls = string.Join(",", remainingImageUrls);

            // === Video từ file ===
            var remainingVideoUrls = new List<string>();

            if (!string.IsNullOrEmpty(oldProduct.VideoUrls))
            {
                foreach (var url in oldProduct.VideoUrls.Split(","))
                {
                    var fileName = Path.GetFileName(url);
                    if (KeepVideoFiles.Contains(fileName))
                    {
                        remainingVideoUrls.Add(url); // giữ lại video được chọn
                    }
                }
            }

            if (product.VideoFiles != null && product.VideoFiles.Any())
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/videos");
                Directory.CreateDirectory(uploadsFolder);

                foreach (var file in product.VideoFiles)
                {
                    if (file.Length > 0)
                    {
                        var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        remainingVideoUrls.Add("videos/" + uniqueFileName);
                    }
                }
            }

            product.VideoUrls = string.Join(",", remainingVideoUrls);

            // === Video từ URL ===
            var remainingExternalVideoUrls = new List<string>();

            if (!string.IsNullOrEmpty(oldProduct.ExternalVideoUrls))
            {
                foreach (var url in oldProduct.ExternalVideoUrls.Split(","))
                {
                    if (KeepExternalVideoUrls.Contains(url))
                    {
                        remainingExternalVideoUrls.Add(url); // giữ lại URL được chọn
                    }
                }
            }

            if (ExternalVideoUrlList != null && ExternalVideoUrlList.Any())
            {
                remainingExternalVideoUrls.AddRange(ExternalVideoUrlList.Where(url => !string.IsNullOrWhiteSpace(url)));
            }

            product.ExternalVideoUrls = string.Join(",", remainingExternalVideoUrls);

            await _productRepository.UpdateAsync(product);
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _productRepository.DeleteAsync(id);
            return RedirectToAction("Index");
        }

        // ---------- Helper methods ----------

        private async Task HandleImageUpload(Product product)
        {
            // Ảnh chính
            if (product.ImageFile != null && product.ImageFile.Length > 0)
            {
                product.ImageUrl = await SaveFile(product.ImageFile, "images");
            }

            // Ảnh phụ
            if (product.ExtraImageFiles != null && product.ExtraImageFiles.Any())
            {
                var extraImageUrls = new List<string>();
                foreach (var file in product.ExtraImageFiles)
                {
                    if (file.Length > 0)
                    {
                        var fileUrl = await SaveFile(file, "images");
                        extraImageUrls.Add(fileUrl);
                    }
                }
                product.ExtraImageUrls = string.Join(",", extraImageUrls);
            }

            // Video từ file
            if (product.VideoFiles != null && product.VideoFiles.Any())
            {
                var videoUrls = new List<string>();
                foreach (var file in product.VideoFiles)
                {
                    if (file.Length > 0)
                    {
                        var fileUrl = await SaveFile(file, "videos");
                        videoUrls.Add(fileUrl);
                    }
                }
                product.VideoUrls = string.Join(",", videoUrls);
            }
        }

        private async Task<string> SaveFile(IFormFile file, string folder)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), $"wwwroot/{folder}");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"{folder}/{uniqueFileName}";
        }
    }
}
