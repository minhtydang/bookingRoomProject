using DoAn_LTW_Nhom15_22DTHG3.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using DoAn_LTW_Nhom15_22DTHG3.Areas.Admin.ViewModels;

namespace DoAn_LTW_Nhom15_22DTHG3.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // Danh sách người dùng
        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users.ToList();
            var userRoleList = new List<UserRoleViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoleList.Add(new UserRoleViewModel
                {
                    User = user,
                    Roles = roles
                });
            }

            return View(userRoleList);
        }

        // GET: Chỉnh sửa người dùng
        public async Task<IActionResult> EditUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Cập nhật người dùng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(ApplicationUser model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                return NotFound();
            }

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.UserName = model.UserName;
            user.IsEnabled = model.IsEnabled;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["Success"] = "Cập nhật thông tin người dùng thành công.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        // POST: Xóa người dùng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                TempData["Success"] = "Xóa người dùng thành công.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = "Xóa người dùng thất bại.";
            return RedirectToAction(nameof(Index));
        }

        // ✅ BẬT / TẮT người dùng
        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.IsEnabled = !user.IsEnabled;
            await _userManager.UpdateAsync(user);

            return RedirectToAction(nameof(Index));
        }

        public class UserRoleViewModel
        {
            public ApplicationUser User { get; set; }
            public IList<string> Roles { get; set; }
        }
    }
}
