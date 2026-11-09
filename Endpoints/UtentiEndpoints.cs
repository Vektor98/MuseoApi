using Microsoft.EntityFrameworkCore;
using MuseoApi.Data;
using MuseoApi.Dtos;

namespace MuseoApi.Endpoints;

public static class UtentiEndpoints
{
    public static void MapUtentiEndpoints(this IEndpointRouteBuilder app)
    {
        var gruppo = app.MapGroup("/api/utenti").RequireAuthorization("Amministratore");

        gruppo.MapGet("", async (MuseoDbContext db) =>
        {
            var utenti = await db.Utenti
                .AsNoTracking()
                .Include(u => u.Ruolo)
                .OrderBy(u => u.Cognome)
                .Select(u => new UtenteListItem(
                    u.Id, u.Email, u.Nome, u.Cognome, u.Ruolo!.Nome, u.Attivo, u.DataRegistrazione))
                .ToListAsync();

            return Results.Ok(utenti);
        });

        gruppo.MapPut("/{id:int}/stato", async (int id, CambioStatoRequest req, MuseoDbContext db) =>
        {
            var utente = await db.Utenti.FindAsync(id);
            if (utente is null)
                return Results.NotFound();

            utente.Attivo = req.Attivo;
            await db.SaveChangesAsync();

            return Results.NoContent();
        });

        // Promozione di ruolo riservata all'amministratore: colma il limite della
        // registrazione pubblica, che crea solo visitatori.
        gruppo.MapPut("/{id:int}/ruolo", async (int id, int ruoloId, MuseoDbContext db) =>
        {
            var utente = await db.Utenti.FindAsync(id);
            if (utente is null)
                return Results.NotFound();

            if (!await db.Ruoli.AnyAsync(r => r.Id == ruoloId))
                return Results.BadRequest(new { messaggio = "Ruolo inesistente." });

            utente.RuoloId = ruoloId;
            await db.SaveChangesAsync();

            return Results.NoContent();
        });

        gruppo.MapGet("/ruoli", async (MuseoDbContext db) =>
            await db.Ruoli.AsNoTracking().OrderBy(r => r.Id).Select(r => new { r.Id, r.Nome }).ToListAsync());
    }
}
