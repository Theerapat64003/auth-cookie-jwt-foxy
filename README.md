# 🔐 ASP.NET Core Razor + API + JWT Cookie + YARP Reverse Proxy

---

## 📌 Project Overview

ระบบนี้ประกอบด้วย 2 โปรเจคหลัก

- 🌐 Web App (Razor Pages + YARP) → https://localhost:7290  
- 🔧 API Backend (JWT Protected) → https://localhost:7060  

Web App ใช้ **YARP Reverse Proxy** เพื่อเรียก API ผ่าน path `/api/*`  
ทำให้ไม่ต้องใช้ CORS และสามารถส่ง Cookie (JWT) ได้โดยตรง

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

# 🏗 Architecture

```
Browser
   ↓
https://localhost:7290/api/WeatherForecast
   ↓
YARP Reverse Proxy
   ↓
https://localhost:7060/WeatherForecast
   ↓
JWT ตรวจสอบจาก Cookie (auth_token)
```

---

# 📦 Required Packages

## Web Project (7290)

```
Microsoft.ReverseProxy
```

## API Project (7060)

```
Microsoft.AspNetCore.Authentication.JwtBearer
```

---

# 🔐 Authentication Strategy

- ใช้ JWT (HS256)
- เก็บ JWT ใน HttpOnly Cookie ชื่อ `auth_token`
- ใช้ `DateTime.UtcNow` สำหรับ Expire Token
- Validate ผ่าน `JwtBearer`
- ใช้ Reverse Proxy แทน CORS

---

# ⚙️ API Configuration (7060)

## Program.cs

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    // อ่าน JWT จาก Cookie
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            context.Token = context.Request.Cookies["auth_token"];
            return Task.CompletedTask;
        }
    };

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ClockSkew = TimeSpan.Zero,

        ValidIssuer = "LoginWeb",
        ValidAudience = "DashboardWeb",

        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("YOUR_SECRET_KEY"))
    };
});

app.UseAuthentication();
app.UseAuthorization();
```

---

## ตัวอย่าง API Endpoint

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

# 🌐 Web Configuration (7290)

## appsettings.json

```json
{
  "ReverseProxy": {
    "Routes": {
      "apiRoute": {
        "ClusterId": "apiCluster",
        "Match": {
          "Path": "/api/{**catch-all}"
        },
        "Transforms": [
          { "PathRemovePrefix": "/api" }
        ]
      }
    },
    "Clusters": {
      "apiCluster": {
        "Destinations": {
          "destination1": {
            "Address": "https://localhost:7060/"
          }
        }
      }
    }
  }
}
```

---

## Program.cs (Web)

```csharp
builder.Services.AddRazorPages();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts(); // Production Security
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();
app.MapReverseProxy();

app.Run();
```

---

# 🧠 Frontend Fetch Example

```javascript
fetch('/api/WeatherForecast', {
    method: 'GET',
    credentials: 'include'
})
.then(res => {
    if (res.status === 401) {
        window.location.href = "/Error?type=expired";
        return;
    }
    return res.json();
})
.then(data => {
    console.log(data);
});
```

---

# 🔐 JWT Creation (Login Example)

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

var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

Response.Cookies.Append("auth_token", tokenString, new CookieOptions
{
    HttpOnly = true,
    Secure = true,
    SameSite = SameSiteMode.Strict,
    Path = "/",
    Expires = DateTime.UtcNow.AddMinutes(15)
});
```

---

# 🔓 Logout Implementation

```csharp
public IActionResult OnPostLogout()
{
    Response.Cookies.Delete("auth_token", new CookieOptions
    {
        Path = "/"
    });

    return RedirectToPage("/Login");
}
```

---

# 🔄 Authentication Flow

1. User Login
2. Web สร้าง JWT
3. JWT ถูกเก็บใน HttpOnly Cookie ชื่อ `auth_token`
4. Web เรียก `/api/*`
5. YARP Forward ไป API
6. API อ่าน JWT จาก Cookie
7. Validate Token
8. ถ้า token หมดอายุ → 401
9. Web redirect ไป `/Error?type=expired`

---

# 🚀 Benefits of This Architecture

✅ ไม่ต้องใช้ CORS  
✅ Cookie ส่งได้ปกติ  
✅ ซ่อน backend port  
✅ ปลอดภัยกว่า localStorage  
✅ รองรับ Scale และ Load Balancing  
✅ Production Ready Pattern  

---

# 🧪 Test Endpoint

```
GET https://localhost:7290/api/WeatherForecast
```

---

# 📌 Important Notes

- Cookie ต้องตั้งค่า:
  - HttpOnly = true
  - Secure = true
  - SameSite = Strict (หรือ None ถ้าข้าม domain จริง)
- JWT Secret ต้องเก็บใน Environment Variable ใน Production
- ใช้ DateTime.UtcNow เสมอ
- ตั้ง ClockSkew = TimeSpan.Zero เพื่อไม่เผื่อเวลา

---

# 🏁 Run Order

1. Start API (7060)
2. Start Web (7290)
3. Login เพื่อสร้าง Cookie
4. เรียก API ผ่าน `/api/*`

---

# 📈 Future Improvements

- Refresh Token
- Role-based Authorization
- Token Blacklist
- Redis Session Store
- Centralized Identity Server
- Rate Limiting
- Load Balancing
- Health Checks

---

🎉 ระบบนี้เป็น Razor + JWT Cookie + Reverse Proxy Pattern  
ระดับ Production พร้อมต่อยอด Microservices ได้
