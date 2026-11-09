using Microsoft.EntityFrameworkCore;
using MuseoApi.Data;
using MuseoApi.Dtos;
using MuseoApi.Models;
using MuseoApi.Services;

namespace MuseoApi.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var gruppo = app.MapGroup("/api/auth");

        gruppo.MapPost("/register", async (RegistrazioneRequest req, MuseoDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return Results.BadRequest(new { messaggio = "Email e password sono obbligatorie." });

            if (req.Password.Length < 8)
                return Results.BadRequest(new { messaggio = "La password deve avere almeno 8 caratteri." });

            var email = req.Email.Trim().ToLowerInvariant();

            if (await db.Utenti.AnyAsync(u => u.Email == email))
                return Results.Conflict(new { messaggio = "Esiste già un account con questa email." });

            // La registrazione pubblica assegna sempre il ruolo piu basso: i ruoli
            // operativi si attribuiscono solo lato amministrazione.
            var ruoloVisitatore = await db.Ruoli.SingleAsync(r => r.Nome == "Visitatore");

            var utente = new Utente
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
                Nome = req.Nome.Trim(),
                Cognome = req.Cognome.Trim(),
                Telefono = req.Telefono,
                RuoloId = ruoloVisitatore.Id,
                DataRegistrazione = DateTime.UtcNow,
                Attivo = true
            };

            db.Utenti.Add(utente);
            await db.SaveChangesAsync();

            return Results.Created($"/api/utenti/{utente.Id}", new { utente.Id, utente.Email });
        });

        gruppo.MapPost("/login", async (LoginRequest req, MuseoDbContext db, TokenService tokenService) =>
        {
            var email = req.Email.Trim().ToLowerInvariant();

            var utente = await db.Utenti
                .Include(u => u.Ruolo)
                .SingleOrDefaultAsync(u => u.Email == email);

            // Stessa risposta per utente inesistente e password errata: non si rivela
            // quali email sono registrate.
            if (utente is null || !BCrypt.Net.BCrypt.Verify(req.Password, utente.PasswordHash))
                return Results.Unauthorized();

            if (!utente.Attivo)
                return Results.Problem(statusCode: 403, detail: "Account disattivato.");

            var (token, scadenza) = tokenService.CreaToken(utente);

            return Results.Ok(new LoginResponse(token, utente.Email, utente.Ruolo!.Nome, scadenza));
        });
    }
}
