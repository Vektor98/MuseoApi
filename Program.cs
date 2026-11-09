using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MuseoApi.Data;
using MuseoApi.Endpoints;
using MuseoApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MuseoDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<TokenService>();

var jwt = builder.Configuration.GetSection("Jwt");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!))
        };
    });

// L'amministratore eredita i permessi dell'operatore: una sola policy per entrambi.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Operatore", policy => policy.RequireRole("Operatore", "Amministratore"));
    options.AddPolicy("Amministratore", policy => policy.RequireRole("Amministratore"));
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

// Migrazioni e dati iniziali applicati all'avvio: l'ambiente si ricrea da zero
// con un solo comando.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MuseoDbContext>();
    await SeedData.InizializzaAsync(db);
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors();

// L'ordine conta: prima si stabilisce chi e l'utente, poi cosa puo fare.
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapMostreEndpoints();
app.MapTurniEndpoints();
app.MapPrenotazioniEndpoints();
app.MapFeedbackEndpoints();
app.MapOpereEndpoints();
app.MapSaleEndpoints();
app.MapUtentiEndpoints();
app.MapDashboardEndpoints();

app.Run();
