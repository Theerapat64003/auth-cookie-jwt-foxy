# 🔐 Authentication System (Web + API)

ระบบ Authentication แบบ JWT สำหรับโครงสร้างแยก Web และ API

---

## 🏗 Project Structure

```
Solution
│
├── web_login        → หน้า Login (สร้าง JWT + เก็บ Cookie)
├── web_dashboard    → หน้า Dashboard (อ่าน JWT จาก Cookie)
└── api_backend      → Web API (Validate JWT)
```

---

## 🧩 Architecture Flow

1. ผู้ใช้ Login ผ่าน `web_login`
2. `web_login` สร้าง JWT Token
3. เก็บ Token ลงใน Cookie (HttpOnly)
4. ผู้ใช้เข้า `web_dashboard`
5. `web_dashboard` ส่ง JWT ไปที่ `api_backend`
6. `api_backend` ตรวจสอบ Token
7. คืนข้อมูลกลับ

---

## 🔑 Authentication Strategy

- ใช้ JWT (Json Web Token)
- ใช้ ClaimTypes.Name สำหรับเก็บ Username
- ใช้ DateTime.UtcNow สำหรับ Expire Token
- Validate ด้วย JwtBearer

---

# ⚙️ Configuration

---

## 📌 1️⃣ web_login

### สร้าง JWT

```csharp
var claims = new List<Claim>
{
    new Claim(ClaimTypes.Name, user.Username),
    new Claim(ClaimTypes.Role, "Admin")
};

var key = new SymmetricSecurityKey(
    Encoding.UTF8.GetBytes("YOUR_SECRET_KEY"));

var creds = new SigningCredentials(
    key, SecurityAlgorithms.HmacSha256);

var token = new JwtSecurityToken(
    issuer: "LoginWeb",
    audience: "DashboardWeb",
    claims: claims,
    expires: DateTime.UtcNow.AddMinutes(15),
    signingCredentials: creds
);

var tokenString = new JwtSecurityTokenHandler()
    .WriteToken(token);
```

### เก็บลง Cookie

```csharp
Response.Cookies.Append("AuthToken", tokenString, new CookieOptions
{
    HttpOnly = true,
    Secure = true,
    SameSite = SameSiteMode.Strict,
    Expires = DateTime.UtcNow.AddMinutes(15)
});
```

---

## 📌 2️⃣ web_dashboard

### อ่านชื่อจาก Claim ใน Controller

```csharp
var name = User.Identity?.Name;
```

### แสดงใน Razor

```html
<h3>Welcome @User.Identity?.Name</h3>
```

---

## 📌 3️⃣ api_backend

### เปิดใช้ JWT Authentication

```csharp
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = "LoginWeb",
            ValidAudience = "DashboardWeb",
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("YOUR_SECRET_KEY"))
        };

        options.ClockSkew = TimeSpan.Zero;
    });

app.UseAuthentication();
app.UseAuthorization();
```

### ตัวอย่าง API

```csharp
[Authorize]
[HttpGet("me")]
public IActionResult Me()
{
    return Ok(new
    {
        Name = User.Identity?.Name
    });
}
```

---

# 🕒 Why Use DateTime.UtcNow ?

JWT มาตรฐานใช้เวลาแบบ UTC  
เพื่อป้องกันปัญหา Timezone mismatch ระหว่าง Server หลายประเทศ

กฎทอง:
- Server ทำงานด้วย UTC
- เวลาแสดงผล ค่อยแปลงเป็น Local Time

---

# 🚀 Run Project

1. รัน api_backend
2. รัน web_login
3. Login
4. เข้า web_dashboard
5. Dashboard เรียก API พร้อม JWT

---

# 🔐 Security Notes

- เก็บ Secret Key ใน appsettings.json
- ห้าม hardcode Key ใน Production
- ใช้ HTTPS เสมอ
- ตั้ง ClockSkew = TimeSpan.Zero ถ้าต้องการหมดอายุตรงเวลา

---

# 📌 Future Improvements

- Refresh Token
- Role-based Authorization
- Token Blacklist
- Redis Session Store
- Reverse Proxy (YARP)
- Centralized Identity Server
