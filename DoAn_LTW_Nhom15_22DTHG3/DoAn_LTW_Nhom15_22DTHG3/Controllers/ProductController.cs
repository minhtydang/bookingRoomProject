using Microsoft.AspNetCore.Mvc;
using DoAn_LTW_Nhom15_22DTHG3.Repositories;
using Microsoft.AspNetCore.Mvc.Rendering;
using DoAn_LTW_Nhom15_22DTHG3.Models;
using Microsoft.AspNetCore.Http;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DoAn_LTW_Nhom15_22DTHG3.Helpers;

namespace DoAn_LTW_Nhom15_22DTHG3.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;

        public ProductController(IProductRepository productRepository, ICategoryRepository categoryRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
        }

        public async Task<IActionResult> Index(string searchTerm, string sortOrder, string regionFilter, decimal? minPrice, decimal? maxPrice, double? minArea, double? maxArea)
        {
            var products = await _productRepository.GetAllAsync();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                string keyword = searchTerm.RemoveDiacritics().ToLower();

                products = products.Where(p =>
                    (!string.IsNullOrEmpty(p.Name) && p.Name.RemoveDiacritics().ToLower().Contains(keyword)) ||
                    (!string.IsNullOrEmpty(p.Description) && p.Description.RemoveDiacritics().ToLower().Contains(keyword)) ||
                    (p.Category != null && !string.IsNullOrEmpty(p.Category.Name) && p.Category.Name.RemoveDiacritics().ToLower().Contains(keyword))
                ).ToList();
            }

            if (!string.IsNullOrEmpty(regionFilter))
            {
                string region = regionFilter.RemoveDiacritics().ToLower();
                products = products.Where(p => !string.IsNullOrEmpty(p.Description) && p.Description.RemoveDiacritics().ToLower().Contains(region)).ToList();
            }

            if (minPrice.HasValue)
            {
                products = products.Where(p => p.Price >= minPrice.Value).ToList();
            }

            if (maxPrice.HasValue)
            {
                products = products.Where(p => p.Price <= maxPrice.Value).ToList();
            }

            if (minArea.HasValue || maxArea.HasValue)
            {
                products = products.Where(p =>
                {
                    if (string.IsNullOrEmpty(p.Description)) return false;
                    var match = System.Text.RegularExpressions.Regex.Match(p.Description, @"\b(Diện tích|dien tich)[:\s]*(\d+\.?\d*)\s*m");
                    if (match.Success && double.TryParse(match.Groups[2].Value, out double area))
                    {
                        return (!minArea.HasValue || area >= minArea.Value) && (!maxArea.HasValue || area <= maxArea.Value);
                    }
                    return false;
                }).ToList();
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
            ViewBag.RegionFilter = regionFilter;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.MinArea = minArea;
            ViewBag.MaxArea = maxArea;

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

        [HttpGet]
        public async Task<JsonResult> AutoCompleteProduct(string term)
        {
            var products = await _productRepository.GetAllAsync();
            var result = products
                .Where(p => !string.IsNullOrEmpty(p.Name) && p.Name.ToLower().Contains(term.ToLower()))
                .Select(p => p.Name.Trim())
                .GroupBy(name => name.ToLower())
                .Select(g => g.First())
                .Take(10)
                .ToList();
            return Json(result);
        }

        [HttpGet]
        public async Task<JsonResult> AutoCompleteProductAndRegion(string term)
        {
            var products = await _productRepository.GetAllAsync();
            var nameSuggestions = products
                .Where(p => !string.IsNullOrEmpty(p.Name) && p.Name.ToLower().Contains(term.ToLower()))
                .Select(p => p.Name.Trim())
                .GroupBy(name => name.ToLower())
                .Select(g => g.First());

            var regionSuggestions = products
                .Where(p => !string.IsNullOrEmpty(p.Region) && p.Region.ToLower().Contains(term.ToLower()))
                .Select(p => p.Region.Trim())
                .GroupBy(region => region.ToLower())
                .Select(g => g.First());

            var result = nameSuggestions
                .Concat(regionSuggestions)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(10)
                .ToList();

            return Json(result);
        }
    }
}