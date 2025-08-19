using DoAn_LTW_Nhom15_22DTHG3.Models;
using DoAn_LTW_Nhom15_22DTHG3.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

public class CategoryController : Controller
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryController(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    // Hiển thị danh sách danh mục
    public async Task<IActionResult> Index()
    {
        var categories = await _categoryRepository.GetAllAsync();
        return View(categories);
    }

    // Hiển thị form thêm danh mục
    public IActionResult Create()
    {
        return View();
    }

    // Xử lý thêm danh mục
    [HttpPost]
    public async Task<IActionResult> Create(Category category)
    {
        if (ModelState.IsValid)
        {
            await _categoryRepository.AddAsync(category);
            return RedirectToAction(nameof(Index));
        }
        return View(category);
    }

    // Hiển thị form sửa danh mục
    public async Task<IActionResult> Edit(int id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category == null)
        {
            return NotFound();
        }
        return View(category);
    }

    // Xử lý sửa danh mục
    [HttpPost]
    public async Task<IActionResult> Edit(int id, Category category)
    {
        if (id != category.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            await _categoryRepository.UpdateAsync(category);
            return RedirectToAction(nameof(Index));
        }
        return View(category);
    }

    // Hiển thị form xác nhận xóa danh mục
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category == null)
        {
            return NotFound();
        }
        return View(category);
    }

    // Xử lý xóa danh mục
    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _categoryRepository.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<JsonResult> AutoCompleteCategory(string term)
    {
        var categories = await _categoryRepository.GetAllAsync();
        var result = categories
            .Where(c => !string.IsNullOrEmpty(c.Name) && c.Name.ToLower().Contains(term.ToLower()))
            .Select(c => c.Name)
            .Distinct()
            .Take(10)
            .ToList();
        return Json(result);
    }
}
