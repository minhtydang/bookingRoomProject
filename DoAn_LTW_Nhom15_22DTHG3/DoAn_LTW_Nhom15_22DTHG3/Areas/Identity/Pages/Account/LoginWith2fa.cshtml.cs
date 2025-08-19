using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using DoAn_LTW_Nhom15_22DTHG3.Models;
using Microsoft.AspNetCore.Http;

public class LoginWith2faModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<LoginWith2faModel> _logger;
    public LoginWith2faModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ILogger<LoginWith2faModel> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; }
    public string ReturnUrl { get; set; }
    [TempData]
    public string TwoFactorEmail { get; set; }
    [TempData]
    public string TwoFactorCodeSent { get; set; }
    [TempData]
    public bool RememberMe { get; set; }

    public class InputModel
    {
        [Required]
        [Display(Name = "Mã xác thực")]
        public string Code { get; set; }
    }

    public IActionResult OnGet(string returnUrl = null, bool rememberMe = false)
    {
        ReturnUrl = returnUrl;
        RememberMe = rememberMe;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string returnUrl = null)
    {
        ReturnUrl = returnUrl;
        if (!ModelState.IsValid)
        {
            return Page();
        }
        var codeSent = HttpContext.Session.GetString("TwoFactorCodeSent");
        var email = HttpContext.Session.GetString("TwoFactorEmail");
        var sentTimeStr = HttpContext.Session.GetString("TwoFactorCodeSentTime");
        DateTime sentTime;
        DateTime.TryParse(sentTimeStr, out sentTime);
        if (Input.Code == codeSent && !string.IsNullOrEmpty(email) && sentTime != default && (DateTime.UtcNow - sentTime).TotalMinutes <= 5)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                await _signInManager.SignInAsync(user, RememberMe);
                _logger.LogInformation("User {Email} logged in with 2FA.", user.Email);
                // Xóa mã khỏi Session sau khi dùng
                HttpContext.Session.Remove("TwoFactorCodeSent");
                HttpContext.Session.Remove("TwoFactorEmail");
                HttpContext.Session.Remove("TwoFactorCodeSentTime");
                return LocalRedirect(ReturnUrl ?? "/");
            }
        }
        ModelState.AddModelError(string.Empty, "Mã xác thực không đúng, đã hết hạn hoặc có lỗi hệ thống.");
        return Page();
    }
} 