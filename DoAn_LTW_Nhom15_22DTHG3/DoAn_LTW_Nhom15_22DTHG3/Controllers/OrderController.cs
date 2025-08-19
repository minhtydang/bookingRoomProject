using DoAn_LTW_Nhom15_22DTHG3.Extensions;
using DoAn_LTW_Nhom15_22DTHG3.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace DoAn_LTW_Nhom15_22DTHG3.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrderController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ✅ GET: /Order/Checkout
        public IActionResult Checkout()
        {
            return View(new Order());
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder(Order order)
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");
            if (cart == null || !cart.Items.Any())
            {
                return RedirectToAction("Index", "ShoppingCart");
            }

            var user = await _userManager.GetUserAsync(User);
            order.UserId = user.Id;
            order.OrderDate = DateTime.UtcNow;
            order.TotalPrice = cart.Items.Sum(i => (i.Price * 0.05m) * i.Quantity);

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("Cart");
            return RedirectToAction("Index", "ShoppingCart");
        }

        // ✅ GET: /Order/OrderCompleted?id=1
        public IActionResult OrderCompleted(int id)
        {
            ViewBag.OrderId = id;
            return View();
        }

        // (Optional) Xem danh sách đơn hàng của người dùng
        public async Task<IActionResult> MyOrders()
        {
            var user = await _userManager.GetUserAsync(User);
            var orders = await _context.Orders
                .Include(o => o.OrderDetails) // Ensure Include is recognized
                .ThenInclude(d => d.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // (Optional) Chi tiết đơn hàng
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return BadRequest();

            var order = await _context.Orders
                .Include(o => o.OrderDetails) // Ensure Include is recognized
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id.Value);

            if (order == null) return NotFound();

            return View(order);
        }


    }
}
