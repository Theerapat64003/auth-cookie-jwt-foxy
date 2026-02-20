using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// มีแล้วไม่ต้องทำ
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // ดึง token จาก cookie
            context.Token = context.Request.Cookies["auth_token"];
            return Task.CompletedTask;
        },

        OnAuthenticationFailed = context =>
        {
            if (context.Exception is SecurityTokenExpiredException)
            {
                context.HttpContext.Items["TokenExpired"] = true;
            }

            return Task.CompletedTask;
        },

        OnChallenge = context =>
        {
            // กรณีไม่มี token เลย
            //context.Response.Redirect("/Error");
            //context.HandleResponse();
            //return Task.CompletedTask;

            context.HandleResponse();

            var expired = context.HttpContext.Items.ContainsKey("TokenExpired");

            if (expired)
                context.Response.Redirect("/Error?type=expired");
            else
                context.Response.Redirect("/Error?type=invalid");

            return Task.CompletedTask;
        }
    };

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true, // ⭐ ต้องเปิด
        ClockSkew = TimeSpan.Zero, // ไม่เผื่อเวลา
        ValidIssuer = "LoginWeb",
        ValidAudience = "DashboardWeb",
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("MySuperSecretKey_For_JWT_2026_SecretKey"))
    };
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapReverseProxy();
app.Run();
