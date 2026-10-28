    
using Microsoft.EntityFrameworkCore;
using PyFinal_1.Data;

var builder = WebApplication.CreateBuilder(args);

// Agregar servicios MVC
builder.Services.AddControllersWithViews();

// CONEXIÓN A SQL SERVER
builder.Services.AddDbContext<ReclamosDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("ConexionSQL")
    ));

builder.Services.AddScoped<PyFinal_1.Services.DerivacionService>();

var app = builder.Build();

// VERIFICAR CONEXIÓN CON LA BASE DE DATOS
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<ReclamosDbContext>();

    bool conectado = await db.Database.CanConnectAsync();

    if (conectado)
    {
        app.Logger.LogInformation(
            "CONEXIÓN EXITOSA CON SQL SERVER"
        );

        // Consultar reclamos desde SQL Server
        var reclamos = await db.Reclamos
            .AsNoTracking()
            .OrderBy(r => r.IdReclamo)
            .ToListAsync();

        // Mostrar cantidad de reclamos
        app.Logger.LogInformation(
            "TOTAL DE RECLAMOS: {Total}",
            reclamos.Count
        );

        // Mostrar los reclamos registrados
        foreach (var reclamo in reclamos)
        {
            app.Logger.LogInformation(
                "RECLAMO REGISTRADO: {Codigo}",
                reclamo.CodigoReclamo
            );
        }
    }
    else
    {
        app.Logger.LogWarning(
            "ERROR: NO SE PUDO CONECTAR A SQL SERVER"
        );
    }
}

// Configurar HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
