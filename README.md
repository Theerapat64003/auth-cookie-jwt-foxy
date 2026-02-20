# 🔐 ASP.NET Core Authentication System

### Razor Pages + JWT Cookie + YARP Reverse Proxy

ระบบ Authentication สำหรับสถาปัตยกรรมที่แยก **Web** และ **API** ออกจากกัน
ใช้ JWT เก็บใน Cookie และเรียก API ผ่าน Reverse Proxy (YARP)

---

## 📌 Project Overview

ระบบประกอบด้วย 3 โปรเจคหลัก

* 🌐 **web_login** → หน้า Login (สร้าง JWT + เก็บ Cookie)
* 🌐 **web_dashboard** → หน้า Dashboard (เรียก API ผ่าน YARP)
* 🔧 **api_backend** → Web API (ตรวจสอบ JWT)

> web_dashboard ใช้ **YARP Reverse Proxy** เรียก API ผ่าน path `/api/*`
> ทำให้ไม่ต้องเปิด CORS และสามารถส่ง Cookie ได้โดยตรง

---

## 🏗 Project Structure

```
Solution
│
├── web_login        → Login (สร้าง JWT + Cookie)
├── web_dashboard    → Dashboard + Reverse Proxy
└── api_backend      → Web API (JWT Validation)
```

---

## 🧩 Architecture

```
Browser
   ↓
https://localhost:7290/api/WeatherForecast
   ↓
YARP Reverse Proxy
   ↓
https://localhost:7060/WeatherForecast
   ↓
JWT Validate from Cookie (auth_token)
```

---

## 🔐 Authentication Strategy

* ใช้ JWT (HS256)
* เก็บ JWT ใน HttpOnly Cookie ชื่อ `auth_token`
* ใช้ `DateTime.UtcNow` สำหรับหมดอายุ Token
* Validate ด้วย `JwtBearer`
* ใช้ YARP แทนการเปิด CORS

---

## 📦 Required Packages

### web_dashboard

```
Microsoft.ReverseProxy
```

### api_backend

```
Microsoft.AspNetCore.Authentication.JwtBearer
```

---

## ⚙️ API Configuration (api_backend)

### Program.cs

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
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

## 🌐 Reverse Proxy Configuration (web_dashboard)

### appsettings.json

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

## 🔑 JWT Creation (web_login)

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

## 🌐 Frontend Call API Example

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
.then(data => console.log(data));
```

---

## 🔓 Logout

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

## 🔄 Authentication Flow

1. User Login
2. web_login สร้าง JWT
3. เก็บ JWT ใน Cookie
4. web_dashboard เรียก `/api/*`
5. YARP forward ไป API
6. API อ่าน JWT จาก Cookie
7. Validate Token
8. ถ้า Token หมดอายุ → 401
9. Web redirect ไปหน้า Error / Login

---

## 🧪 Test Endpoint

```
GET https://localhost:7290/api/WeatherForecast
```

---

## 🔐 Security Notes

* Cookie ต้องเป็น:

  * HttpOnly = true
  * Secure = true
  * SameSite = Strict
* ห้าม hardcode Secret Key ใน Production
* ใช้ HTTPS เท่านั้น
* ใช้ `DateTime.UtcNow`
* ตั้ง `ClockSkew = TimeSpan.Zero`

---

## 🏁 Run Order

1. Start api_backend (7060)
2. Start web_dashboard (7290)
3. Start web_login
4. Login
5. เรียก API ผ่าน `/api/*`

---

## 🚀 Benefits

✅ ไม่ต้องใช้ CORS
✅ Cookie ปลอดภัยกว่า localStorage
✅ ซ่อน backend port
✅ รองรับ Scale
✅ Production-ready
✅ ต่อ Microservices ได้ง่าย

---

## 📈 Future Improvements

* Refresh Token
* Role-based Authorization
* Token Blacklist
* Redis Session Store
* Centralized Identity Server
* Rate Limiting
* Load Balancing
* Health Checks

---

🎉 **Pattern นี้เหมาะกับระบบ Web + API ที่ต้องการความปลอดภัยสูง และรองรับ Production**

---
