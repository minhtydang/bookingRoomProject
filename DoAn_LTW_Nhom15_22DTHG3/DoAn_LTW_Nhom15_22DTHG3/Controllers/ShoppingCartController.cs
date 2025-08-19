using DoAn_LTW_Nhom15_22DTHG3.Extensions;
using DoAn_LTW_Nhom15_22DTHG3.Models;
using DoAn_LTW_Nhom15_22DTHG3.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Security.Cryptography;

namespace DoAn_LTW_Nhom15_22DTHG3.Controllers
{
    [Authorize]
    public class ShoppingCartController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public ShoppingCartController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IProductRepository productRepository, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _productRepository = productRepository;
            _context = context;
            _userManager = userManager;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<IActionResult> AddToCart(int productId, int quantity)
        {
            var product = await GetProductFromDatabase(productId);

            var cartItem = new CartItem
            {
                ProductId = productId,
                Name = product.Name,
                Price = product.Price,
                Quantity = quantity
            };

            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();
            cart.AddItem(cartItem);
            HttpContext.Session.SetObjectAsJson("Cart", cart);

            return RedirectToAction("Index");
        }

        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();
            return View(cart);
        }

        public IActionResult Checkout()
        {
            return View(new Order());
        }

        [HttpPost]
        public async Task<IActionResult> Checkout(Order order)
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");
            if (cart == null || !cart.Items.Any())
            {
                return RedirectToAction("Index");
            }

            var user = await _userManager.GetUserAsync(User);
            order.UserId = user.Id;
            order.OrderDate = DateTime.UtcNow;

            // Áp dụng mã khuyến mãi nếu có
            decimal totalPrice = cart.Items.Sum(i => (i.Price * 0.05m) * i.Quantity);
            decimal discountAmount = 0;
            if (!string.IsNullOrEmpty(cart.VoucherCode))
            {
                var voucher = await _context.Vouchers
                    .FirstOrDefaultAsync(v => v.Code == cart.VoucherCode
                                           && v.IsActive
                                           && v.StartDate <= DateTime.Now
                                           && v.EndDate >= DateTime.Now);
                if (voucher != null)
                {
                    var discount = totalPrice * voucher.DiscountPercent;
                    if (voucher.MaxDiscountAmount.HasValue)
                    {
                        discount = Math.Min(discount, voucher.MaxDiscountAmount.Value);
                    }
                    discountAmount = discount;
                    order.VoucherCode = cart.VoucherCode;
                }
            }
            order.TotalPrice = totalPrice - discountAmount;
            order.OrderDetails = cart.Items.Select(i => new OrderDetail
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                Price = i.Price,
                CheckInDate = i.CheckInDate ?? DateTime.Now,
                CheckOutDate = i.CheckOutDate ?? DateTime.Now.AddDays(1),
                Status = "Chờ xác nhận",
                CreatedAt = DateTime.Now
            }).ToList();

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("Cart");

            ViewBag.OrderId = order.Id;
            return View("OrderCompleted");
        }

        private async Task<Product> GetProductFromDatabase(int productId)
        {
            return await _productRepository.GetByIdAsync(productId);
        }

        public IActionResult RemoveFromCart(int productId)
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");
            if (cart is not null)
            {
                cart.RemoveItem(productId);
                HttpContext.Session.SetObjectAsJson("Cart", cart);
            }
            return RedirectToAction("Index");
        }

        // ✅ MyOrders: Xem danh sách đơn hàng của user
        public async Task<IActionResult> MyOrders()
        {
            var user = await _userManager.GetUserAsync(User);
            var orders = await _context.Orders
                .Where(o => o.UserId == user.Id)
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // ✅ Details: Xem chi tiết 1 đơn hàng
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return BadRequest();

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id.Value);

            if (order == null) return NotFound();

            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> ApplyVoucher(string voucherCode)
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();
            cart.VoucherCode = voucherCode;
            cart.DiscountAmount = 0;
            cart.DiscountPercent = 0; // Đặt lại mặc định

            if (!string.IsNullOrEmpty(voucherCode))
            {
                var voucher = await _context.Vouchers
                    .FirstOrDefaultAsync(v => v.Code == voucherCode
                                           && v.IsActive
                                           && v.StartDate <= DateTime.Now
                                           && v.EndDate >= DateTime.Now);
                if (voucher != null)
                {
                    var total = cart.Items.Sum(i => (i.Price * 0.05m) * i.Quantity); // Sử dụng tổng tiền thực tế với công thức mới
                    cart.DiscountPercent = voucher.DiscountPercent; // Gán phần trăm giảm vào cart
                    var discount = total * voucher.DiscountPercent;
                    if (voucher.MaxDiscountAmount.HasValue)
                    {
                        discount = Math.Min(discount, voucher.MaxDiscountAmount.Value);
                    }
                    cart.DiscountAmount = discount;
                }
                else
                {
                    TempData["VoucherError"] = "Mã khuyến mãi không hợp lệ hoặc đã hết hạn.";
                    cart.VoucherCode = null;
                }
            }
            HttpContext.Session.SetObjectAsJson("Cart", cart);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCart(List<CartItem> Items)
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();
            var updatedItems = new List<CartItem>();
            foreach (var item in Items)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId);
                if (product != null)
                {
                    updatedItems.Add(new CartItem
                    {
                        ProductId = item.ProductId,
                        Name = product.Name,
                        Price = product.Price,
                        Quantity = item.Quantity,
                        CheckInDate = item.CheckInDate,
                        CheckOutDate = item.CheckOutDate
                    });
                }
            }
            cart.Items = updatedItems;
            // Giữ lại VoucherCode, DiscountAmount, DiscountPercent
            HttpContext.Session.SetObjectAsJson("Cart", cart);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> PayWithMomo()
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");
            if (cart == null || !cart.Items.Any())
            {
                TempData["PaymentError"] = "Giỏ hàng trống.";
                return RedirectToAction("Index");
            }
            var user = await _userManager.GetUserAsync(User);
            var orderId = Guid.NewGuid().ToString();
            var amount = cart.TotalAfterDiscount.ToString("0");
            var orderInfo = $"Thanh toán phòng trọ cho {user.Email}, mã đơn: {orderId}";

            // MoMo config
            var accessKey = _configuration["MoMo:AccessKey"] ?? "F8BBA842ECF85";
            var secretKey = _configuration["MoMo:SecretKey"] ?? "K951B6PE1waDMi640xX08PD3vg6EkVlz";
            var partnerCode = _configuration["MoMo:PartnerCode"] ?? "MOMO";
            var redirectUrl = Url.Action("MomoCallback", "ShoppingCart", null, Request.Scheme);
            var ipnUrl = Url.Action("MomoCallback", "ShoppingCart", null, Request.Scheme);
            var requestType = "payWithMethod";
            var requestId = orderId;
            var extraData = "";
            var orderGroupId = "";
            var autoCapture = true;
            var lang = "vi";
            var rawSignature = $"accessKey={accessKey}&amount={amount}&extraData={extraData}&ipnUrl={ipnUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={redirectUrl}&requestId={requestId}&requestType={requestType}";
            using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
            var signature = BitConverter.ToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(rawSignature))).Replace("-", "").ToLower();
            var requestBody = new
            {
                partnerCode = partnerCode,
                partnerName = "Trọ Shop",
                storeId = "TrotShopStore",
                requestId = requestId,
                amount = amount,
                orderId = orderId,
                orderInfo = orderInfo,
                redirectUrl = redirectUrl,
                ipnUrl = ipnUrl,
                lang = lang,
                requestType = requestType,
                autoCapture = autoCapture,
                extraData = extraData,
                orderGroupId = orderGroupId,
                signature = signature
            };

            using var client = _httpClientFactory.CreateClient();
            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://test-payment.momo.vn/v2/gateway/api/create", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                TempData["PaymentError"] = "Không thể kết nối MoMo: " + responseContent;
                return RedirectToAction("Index");
            }

            var result = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(responseContent);
            if (result.TryGetProperty("payUrl", out var payUrl))
            {
                // Redirect người dùng sang trang thanh toán MoMo
                return Redirect(payUrl.GetString());
            }
            TempData["PaymentError"] = "Không nhận được link thanh toán từ MoMo.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> MomoCallback(string resultCode, string message, string orderId = null)
        {
            // Xác nhận thanh toán thành công
            if (resultCode == "0")
            {
                // Lưu đơn hàng như flow Checkout
                var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");
                var user = await _userManager.GetUserAsync(User);
                var order = new Order
                {
                    UserId = user.Id,
                    OrderDate = DateTime.UtcNow,
                    TotalPrice = cart.TotalAfterDiscount,
                    VoucherCode = cart.VoucherCode,
                    OrderDetails = cart.Items.Select(i => new OrderDetail
                    {
                        ProductId = i.ProductId,
                        Quantity = i.Quantity,
                        Price = (i.Price / 30m),
                        CheckInDate = i.CheckInDate ?? DateTime.Now,
                        CheckOutDate = i.CheckOutDate ?? DateTime.Now.AddDays(1),
                        Status = "Đã thanh toán qua MoMo",
                        CreatedAt = DateTime.Now
                    }).ToList()
                };
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                HttpContext.Session.Remove("Cart");
                TempData["PaymentSuccess"] = "Thanh toán MoMo thành công! Đơn hàng đã được ghi nhận.";
                return RedirectToAction("Index");
            }
            TempData["PaymentError"] = message ?? "Thanh toán thất bại hoặc bị hủy.";
            return RedirectToAction("Index");
        }
    }
}
