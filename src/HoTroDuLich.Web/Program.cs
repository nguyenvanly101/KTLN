using HoTroDuLich.Web.Data;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using System.Security.Claims;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(o=>{o.AddServerHeader=false;o.Limits.MaxRequestBodySize=30*1024*1024;});
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o=>o.MultipartBodyLengthLimit=30*1024*1024);
builder.Services.AddHsts(o=>o.MaxAge=TimeSpan.FromDays(365));
if(OperatingSystem.IsWindows()&&!builder.Environment.IsDevelopment())builder.Logging.AddEventLog();
// Add services to the container.
builder.Services.AddControllersWithViews(o =>
{
    o.ValueProviderFactories.Insert(0, new HoTroDuLich.Web.Models.FullTextValueProviderFactory());
    o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    o.Filters.Add<NhatKyAntiforgeryFilter>();
    o.ModelMetadataDetailsProviders.Add(new HoTroDuLich.Web.Models.VietnameseMetadataProvider());
    o.ModelBindingMessageProvider.SetAttemptedValueIsInvalidAccessor((v, f) => $"Giá trị nhập cho {f} không hợp lệ.");
    o.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor(_ => "Vui lòng nhập thông tin bắt buộc.");
    o.ModelBindingMessageProvider.SetValueMustBeANumberAccessor(f => $"{f} phải là một số hợp lệ.");
});
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("HoTroDuLich")));
builder.Services.AddMemoryCache();
builder.Logging.AddFilter("System.Net.Http.HttpClient.DiscoveryRouting",LogLevel.Warning);
var keyFolder=Path.GetFullPath(builder.Configuration["DataProtection:ThuMucKhoa"]??Path.Combine("App_Data","keys"),builder.Environment.ContentRootPath);
var protection = builder.Services.AddDataProtection().SetApplicationName("HoTroDuLichTuTuc").PersistKeysToFileSystem(new DirectoryInfo(keyFolder));
if (OperatingSystem.IsWindows())
    protection.ProtectKeysWithDpapi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<TravelService>();
builder.Services.AddScoped<TripWorkspaceService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<JourneyService>();
builder.Services.AddScoped<ReviewService>();
builder.Services.AddScoped<AvailabilityService>();
builder.Services.AddScoped<CompanionService>();
builder.Services.AddScoped<DiscoveryService>();
builder.Services.AddScoped<BranchService>();
builder.Services.AddScoped<PlaceSearchService>();
builder.Services.AddSingleton<GoogleMapsClient>();
builder.Services.AddHttpClient("GoogleMaps",c=>c.Timeout=TimeSpan.FromSeconds(12));
builder.Logging.AddFilter("System.Net.Http.HttpClient.GoogleMaps",LogLevel.Warning);
builder.Services.AddSingleton<OpenMapPlaces>();
builder.Services.AddHttpClient("OpenMapPlaces",c=>{c.DefaultRequestHeaders.UserAgent.ParseAdd("HanhTrinhTravel/1.0");c.Timeout=TimeSpan.FromSeconds(8);});
builder.Logging.AddFilter("System.Net.Http.HttpClient.OpenMapPlaces",LogLevel.Warning);
builder.Services.AddHostedService<GoogleLocationCleanup>();
builder.Services.AddSingleton<DiscoveryRouter>();
builder.Services.AddHttpClient("DiscoveryRouting",c=>{c.DefaultRequestHeaders.UserAgent.ParseAdd("HanhTrinhTravel/1.0");c.Timeout=TimeSpan.FromSeconds(15);});
builder.Services.AddHostedService<DiscoveryIndexWorker>();
builder.Services.AddFullTextComponents();
builder.Services.AddFullTextContracts();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IDongHo, DongHo>();
builder.Services.AddScoped<IGiaoDich, GiaoDich>();
builder.Services.AddScoped<ILuuTep>(sp => (LuuTepService)sp.GetRequiredService<ILuuTepService>());
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o =>
{
    o.LoginPath = "/Account/Login";
    o.AccessDeniedPath = "/Account/Denied";
    o.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    o.SlidingExpiration = true;
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.Cookie.Name = builder.Environment.IsDevelopment() ? "DLTT.Auth" : "__Host-DLTT.Auth";
    o.Cookie.Path = "/";
    o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    o.Events.OnRedirectToAccessDenied = c =>
    {
        c.Response.StatusCode = StatusCodes.Status403Forbidden;
        c.Response.ContentType = "text/plain; charset=utf-8";
        return c.Response.WriteAsync("Bạn không có quyền truy cập chức năng này theo vai trò hiện tại.");
    };
    o.Events.OnValidatePrincipal = async c =>
    {
        var id = long.TryParse(c.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var v) ? v : 0;
        var db = c.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
        var a = await db.TaiKhoan.AsNoTracking().SingleOrDefaultAsync(x => x.MaTaiKhoan == id);
        c.HttpContext.Items["MustChangePassword"] = a?.PhaiDoiMatKhau == true;
        if (a == null || a.TrangThai != HoTroDuLich.Web.Domain.TrangThaiTaiKhoan.HoatDong || a.DauPhien.ToString() != c.Principal?.FindFirstValue("stamp") || !long.TryParse(c.Principal?.FindFirstValue("issued"), out var issued) || DateTime.UtcNow.Ticks - issued > TimeSpan.FromHours(8).Ticks)
            c.RejectPrincipal();
    };
});
builder.Services.AddAuthorization(SucAccess.Configure);
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.AddPolicy("discovery",c=>RateLimitPartition.GetFixedWindowLimiter(c.Connection.RemoteIpAddress?.ToString()??"local",_=>new FixedWindowRateLimiterOptions{PermitLimit=90,Window=TimeSpan.FromMinutes(1),QueueLimit=0}));
    o.AddPolicy("maps",c=>RateLimitPartition.GetFixedWindowLimiter(c.Connection.RemoteIpAddress?.ToString()??"local",_=>new FixedWindowRateLimiterOptions{PermitLimit=12,Window=TimeSpan.FromMinutes(1),QueueLimit=0}));
    o.AddPolicy("maps-lookup",c=>RateLimitPartition.GetFixedWindowLimiter(c.Connection.RemoteIpAddress?.ToString()??"local",_=>new FixedWindowRateLimiterOptions{PermitLimit=40,Window=TimeSpan.FromMinutes(1),QueueLimit=0}));
    o.AddPolicy("maps-search",c=>RateLimitPartition.GetFixedWindowLimiter(c.Connection.RemoteIpAddress?.ToString()??"local",_=>new FixedWindowRateLimiterOptions{PermitLimit=30,Window=TimeSpan.FromMinutes(1),QueueLimit=0}));
    o.AddPolicy("auth", c => RateLimitPartition.GetFixedWindowLimiter(c.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.AddHostedService<NhacDenHanDinhChiService>();
builder.Services.AddHostedService<PreparationReminderService>();
var app = builder.Build();
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/loi");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.Use(async (c, next) =>
{
    c.Response.Headers["X-Content-Type-Options"] = "nosniff";
    c.Response.Headers["X-Frame-Options"] = "DENY";
    c.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    c.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(self)";
    var discoveryTiles=builder.Configuration["Discovery:Map:TileUrl"]??"https://tile.openstreetmap.org/{z}/{x}/{y}.png";
    var discoveryTileOrigin=Uri.TryCreate(discoveryTiles,UriKind.Absolute,out var tileUri)&&tileUri.Scheme=="https"?tileUri.GetLeftPart(UriPartial.Authority):"https://tile.openstreetmap.org";
    var mapsNonce=Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24));
    c.Items["MapsNonce"]=mapsNonce;
    c.Response.Headers["Content-Security-Policy"] = "default-src 'self'; img-src 'self' data: blob: "+discoveryTileOrigin+" https://*.googleapis.com https://*.gstatic.com https://*.google.com https://*.googleusercontent.com; connect-src 'self' https://*.googleapis.com https://*.google.com https://*.gstatic.com; style-src 'self' 'nonce-"+mapsNonce+"' https://fonts.googleapis.com; style-src-attr 'unsafe-inline'; script-src 'self' 'nonce-"+mapsNonce+"' https://maps.googleapis.com https://maps.gstatic.com; font-src 'self' https://fonts.gstatic.com; worker-src 'self' blob:; frame-src https://www.google.com; object-src 'none'; base-uri 'self'; frame-ancestors 'none'; form-action 'self'";
    await next();
});
app.UseMiddleware<NhatKyBaoMatMiddleware>();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.Use(async (c, next) =>
{
    var path = c.Request.Path.Value ?? "/";
    if (c.User.Identity?.IsAuthenticated == true && c.Items["MustChangePassword"] is true && !new[]
    {
        "/tai-khoan/doi-mat-khau",
        "/Account/Password",
        "/Account/Logout",
        "/tai-khoan/dang-xuat"
    }.Contains(path, StringComparer.OrdinalIgnoreCase))
    {
        c.Response.Redirect("/tai-khoan/doi-mat-khau");
        return;
    }

    if (c.User.Identity?.IsAuthenticated == true && new[]
    {
        "/Account/Login",
        "/Account/Register",
        "/tai-khoan/dang-nhap/mo-dang-nhap",
        "/tai-khoan/dang-ky/mo-dang-ky",
        "/tai-khoan/dang-nhap/dang-nhap",
        "/tai-khoan/dang-ky/dang-ky",
        "/tai-khoan/dang-ky/chon-vai-tro"
    }.Contains(path, StringComparer.OrdinalIgnoreCase))
    {
        c.Response.Redirect(SucAccess.Home(c.User.FindFirstValue(ClaimTypes.Role) ?? ""));
        return;
    }

    await next();
});
app.UseAuthorization();
app.MapStaticAssets();
app.MapControllerRoute(name: "default", pattern: "{controller=Explore}/{action=Index}/{id?}").WithStaticAssets();
if (args.Contains("--initialize") || app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await Seed.Initialize(scope.ServiceProvider, app.Environment.ContentRootPath);
    await DiscoveryIndexWorker.Initialize(scope.ServiceProvider);
    if (args.Contains("--initialize"))
        return;
}

app.Run();
