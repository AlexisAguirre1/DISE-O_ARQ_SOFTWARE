
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using PyFinal_1.Data;

var builder = WebApplication.CreateBuilder(args);

// SERVICIOS MVC
builder.Services.AddControllersWithViews();

// CONEXION CON SQL SERVER
builder.Services.AddDbContext<ReclamosDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("ConexionSQL")
    ));

// SERVICIO DE DERIVACION
builder.Services.AddScoped<PyFinal_1.Services.DerivacionService>();

// AUTENTICACION
builder.Services
    .AddAuthentication(
        CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";

        options.AccessDeniedPath =
            "/Account/AccesoDenegado";

        options.Cookie.Name = "PyFinal_1.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;

        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// PERMISOS
builder.Services.AddAuthorization();

var app = builder.Build();

// COMPROBAR SQL SERVER
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<ReclamosDbContext>();

    try
    {
        if (await db.Database.CanConnectAsync())
        {
            app.Logger.LogInformation(
                "CONEXION EXITOSA CON SQL SERVER");

            int total = await db.Reclamos.CountAsync();

            app.Logger.LogInformation(
                "TOTAL DE RECLAMOS: {Total}", total);
        }
        else
        {
            app.Logger.LogWarning(
                "NO SE PUDO CONECTAR A SQL SERVER");
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError(
            ex, "ERROR DE CONEXION A LA BASE DE DATOS");
    }
}

// ERRORES
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// CONFIGURACION WEB
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// AUTENTICACION Y AUTORIZACION
app.UseAuthentication();
app.UseAuthorization();

// PANTALLA INICIAL: LOGIN
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
