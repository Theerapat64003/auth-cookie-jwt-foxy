using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace web_test_jwt.Pages
{
    public class IndexModel : PageModel
    {

        [BindProperty]
        public string? Username { get; set; }

        [BindProperty]
        public string? Password { get; set; }

        public bool? LoginStatus { get; set; }
        public string? Message { get; set; }




        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {

        }

        public IActionResult OnPostLogin()
        {
            // ตัวอย่าง login แบบ hardcode
            if (Username == "admin" && Password == "1234")
            {
                //HttpContext.Session.SetString("User", Username);
                
                //return RedirectToPage("Index");






                var claims = new[]
                {
                    new Claim("UserId", "test"),
                    new Claim("Username", Username),
                    new Claim("Role", "Admin")
                };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("MySuperSecretKey_For_JWT_2026_SecretKey"));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                var expires = DateTime.UtcNow.AddMinutes(2);
                var token = new JwtSecurityToken(
                    issuer: "LoginWeb",
                    audience: "DashboardWeb",
                    claims: claims,
                    expires: expires,
                    signingCredentials: creds
                );

                string jwtToken = new JwtSecurityTokenHandler().WriteToken(token); // token ที่คุณสร้างได้

                Response.Cookies.Append("auth_token", jwtToken, new CookieOptions
                {
                    HttpOnly = true,              // JS อ่านไม่ได้ (ปลอดภัย)
                    Secure = true,                // ส่งเฉพาะ HTTPS
                    SameSite = SameSiteMode.None, // ถ้าเรียกข้าม domain
                    Path = "/",                   // ใช้ได้ทั้งเว็บ
                    Expires = expires
                });

                LoginStatus = true;
                Message = "Login สำเร็จ";
                return Page();
            }
            LoginStatus = false;
            Message = "Username หรือ Password ไม่ถูกต้อง";
            return Page();
        }
        public IActionResult OnPostLogout()
        {
            Response.Cookies.Delete("auth_token", new CookieOptions
            {
                Path = "/"
            });
            LoginStatus = true;
            Message = "Logout สำเร็จ";
            return Page();
        }
    }
}
