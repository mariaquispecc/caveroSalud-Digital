using System;
using CaveroSalud.Domain.Entities;
using CaveroSalud.Infrastructure.Identity;
using CaveroSalud.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Configuration
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
        else
        {
            policy.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration.GetConnectionString("Default");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("No database connection string was found. Set ConnectionStrings__DefaultConnection or ConnectionStrings__Default.");
}

// DbContext
builder.Services.AddDbContext<CaveroDbContext>(options =>
    options.UseNpgsql(connectionString));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
{
    options.User.RequireUniqueEmail = true;
})
    .AddEntityFrameworkStores<CaveroDbContext>()
    .AddDefaultTokenProviders();

// Email sender
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Seed roles and an admin user on startup
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var db = scope.ServiceProvider.GetRequiredService<CaveroDbContext>();

    await db.Database.MigrateAsync();
    logger.LogInformation("Database migrations applied successfully.");

    var roles = new[] { "Paciente", "Médico", "Laboratorista", "Farmacéutico", "Administrador" };
    foreach (var r in roles)
    {
        if (!await roleManager.RoleExistsAsync(r))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(r));
            logger.LogInformation("Role '{Role}' created.", r);
        }
    }

    var adminEmail = builder.Configuration["Admin:Email"] ?? "admin@cavero.local";
    var adminPassword = builder.Configuration["Admin:Password"] ?? "Admin123!";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            FullName = "Administrador Cavero",
            Dni = string.Empty,
            PhoneNumber = string.Empty,
            Speciality = string.Empty,
            IsTemporaryPassword = false,
            FirstLoginCompleted = true
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Administrador");
            logger.LogInformation("Admin user '{AdminEmail}' created and assigned to Administrador role.", adminEmail);
        }
        else
        {
            logger.LogError("Failed to create admin user '{AdminEmail}': {Errors}", adminEmail, string.Join("; ", result.Errors));
        }
    }
    else if (!await userManager.IsInRoleAsync(adminUser, "Administrador"))
    {
        await userManager.AddToRoleAsync(adminUser, "Administrador");
        logger.LogInformation("Existing user '{AdminEmail}' was assigned to Administrador role.", adminEmail);
    }
    else
    {
        logger.LogInformation("Admin user '{AdminEmail}' already exists and has Administrador role.", adminEmail);
    }

    if (!await db.Specialities.AnyAsync())
    {
        var defaults = new[]
        {
            new Speciality { Id = Guid.NewGuid(), Name = "Cardiología", Description = "Diagnóstico y control cardiovascular.", IsActive = true },
            new Speciality { Id = Guid.NewGuid(), Name = "Neurología", Description = "Atención de enfermedades del sistema nervioso.", IsActive = true },
            new Speciality { Id = Guid.NewGuid(), Name = "Pediatría", Description = "Cuidado integral para niños y adolescentes.", IsActive = true },
            new Speciality { Id = Guid.NewGuid(), Name = "Ginecología", Description = "Salud integral de la mujer.", IsActive = true },
            new Speciality { Id = Guid.NewGuid(), Name = "Traumatología", Description = "Evaluación y tratamiento musculoesquelético.", IsActive = true },
            new Speciality { Id = Guid.NewGuid(), Name = "Oftalmología", Description = "Prevención y tratamiento ocular.", IsActive = true },
            new Speciality { Id = Guid.NewGuid(), Name = "Odontología", Description = "Atención preventiva y restaurativa dental.", IsActive = true },
            new Speciality { Id = Guid.NewGuid(), Name = "Dermatología", Description = "Diagnóstico y cuidado de la piel.", IsActive = true }
        };

        await db.Specialities.AddRangeAsync(defaults);
        await db.SaveChangesAsync();
        logger.LogInformation("Default specialities seeded in database.");
    }
}

app.UseRouting();

// Serve static files from wwwroot (public pages)
app.UseStaticFiles();

app.UseCors("Default");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapRazorPages();

// If a request lands on /, redirect to the public index
// Root is served by Razor Pages / static files

app.Run();

public partial class Program { }
