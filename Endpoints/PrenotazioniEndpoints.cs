using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MuseoApi.Data;
using MuseoApi.Dtos;
using MuseoApi.Models;
using MuseoApi.Services;

namespace MuseoApi.Endpoints;

public static class PrenotazioniEndpoints
{
    public static void MapPrenotazioniEndpoints(this IEndpointRouteBuilder app)
    {
        var gruppo = app.MapGroup("/api/prenotazioni").RequireAuthorization();

        gruppo.MapPost("", async (PrenotazioneRequest req, ClaimsPrincipal principal, MuseoDbContext db, IConfiguration config) =>
        {
            var utenteId = UtenteCorrente.Id(principal);
            var maxPosti = config.GetValue<int>("RegolePrenotazione:MaxPostiPerPrenotazione");

            if (req.NumeroPosti < 1 || req.NumeroPosti > maxPosti)
                return Results.BadRequest(new { messaggio = $"Puoi prenotare da 1 a {maxPosti} ingressi per turno." });

            var turno = await db.Turni
                .Include(t => t.Mostra)
                .Include(t => t.Prenotazioni)
                .SingleOrDefaultAsync(t => t.Id == req.TurnoVisitaId);

            if (turno is null)
                return Results.NotFound(new { messaggio = "Turno inesistente." });

            if (turno.Stato != StatoTurno.Aperto || turno.Mostra!.Stato == StatoMostra.Annullata)
                return Results.Conflict(new { messaggio = "Il turno non è prenotabile: risulta chiuso o annullato." });

            if (turno.DataOra <= DateTime.Now)
                return Results.Conflict(new { messaggio = "Il turno è già iniziato." });

            var esistente = turno.Prenotazioni.SingleOrDefault(p => p.UtenteId == utenteId);

            if (esistente is not null && esistente.Stato == StatoPrenotazione.Confermata)
                return Results.Conflict(new { messaggio = "Hai già una prenotazione confermata per questo turno." });

            // La capienza si misura in ingressi, non in prenotazioni.
            var occupati = turno.Prenotazioni
                .Where(p => p.Stato == StatoPrenotazione.Confermata)
                .Sum(p => p.NumeroPosti);

            if (occupati + req.NumeroPosti > turno.PostiDisponibili)
                return Results.Conflict(new { messaggio = $"Posti insufficienti: ne restano {turno.PostiDisponibili - occupati}." });

            // Una prenotazione annullata viene riattivata invece di crearne una nuova,
            // per non violare il vincolo di unicita su (utente, turno).
            if (esistente is not null)
            {
                esistente.Stato = StatoPrenotazione.Confermata;
                esistente.NumeroPosti = req.NumeroPosti;
                esistente.DataPrenotazione = DateTime.Now;
                await db.SaveChangesAsync();

                return Results.Ok(new { esistente.Id, messaggio = "Prenotazione riattivata." });
            }

            var prenotazione = new Prenotazione
            {
                UtenteId = utenteId,
                TurnoVisitaId = turno.Id,
                NumeroPosti = req.NumeroPosti,
                DataPrenotazione = DateTime.Now,
                Stato = StatoPrenotazione.Confermata
            };

            db.Prenotazioni.Add(prenotazione);
            await db.SaveChangesAsync();

            return Results.Created($"/api/prenotazioni/{prenotazione.Id}", new { prenotazione.Id });
        });

        gruppo.MapGet("/mie", async (ClaimsPrincipal principal, MuseoDbContext db) =>
        {
            var utenteId = UtenteCorrente.Id(principal);

            var prenotazioni = await db.Prenotazioni
                .AsNoTracking()
                .Where(p => p.UtenteId == utenteId)
                .Include(p => p.TurnoVisita)
                .ThenInclude(t => t!.Mostra)
                .OrderByDescending(p => p.TurnoVisita!.DataOra)
                .Select(p => new PrenotazioneResponse(
                    p.Id,
                    p.TurnoVisitaId,
                    p.TurnoVisita!.Mostra!.Titolo,
                    p.TurnoVisita.DataOra,
                    p.NumeroPosti,
                    p.DataPrenotazione,
                    p.Stato,
                    p.Feedback != null))
                .ToListAsync();

            return Results.Ok(prenotazioni);
        });

        gruppo.MapDelete("/{id:int}", async (int id, ClaimsPrincipal principal, MuseoDbContext db, IConfiguration config) =>
        {
            var utenteId = UtenteCorrente.Id(principal);

            var prenotazione = await db.Prenotazioni
                .Include(p => p.TurnoVisita)
                .SingleOrDefaultAsync(p => p.Id == id);

            if (prenotazione is null)
                return Results.NotFound();

            if (prenotazione.UtenteId != utenteId)
                return Results.Problem(statusCode: 403, detail: "La prenotazione appartiene a un altro utente.");

            if (prenotazione.Stato == StatoPrenotazione.Annullata)
                return Results.Conflict(new { messaggio = "La prenotazione risulta già annullata." });

            var oreLimite = config.GetValue<int>("RegolePrenotazione:OreLimiteAnnullamento");
            var limite = prenotazione.TurnoVisita!.DataOra.AddHours(-oreLimite);

            if (DateTime.Now > limite)
                return Results.Conflict(new { messaggio = $"L'annullamento è consentito fino a {oreLimite} ore prima del turno." });

            prenotazione.Stato = StatoPrenotazione.Annullata;
            await db.SaveChangesAsync();

            return Results.NoContent();
        });
    }
}
