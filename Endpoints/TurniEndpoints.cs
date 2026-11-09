using Microsoft.EntityFrameworkCore;
using MuseoApi.Data;
using MuseoApi.Dtos;
using MuseoApi.Models;

namespace MuseoApi.Endpoints;

public static class TurniEndpoints
{
    public static void MapTurniEndpoints(this IEndpointRouteBuilder app)
    {
        var gruppo = app.MapGroup("/api/turni");

        gruppo.MapGet("/{id:int}", async (int id, MuseoDbContext db) =>
        {
            var turno = await db.Turni
                .AsNoTracking()
                .Where(t => t.Id == id)
                .Select(t => new
                {
                    t.Id,
                    t.DataOra,
                    t.DurataMinuti,
                    t.PostiDisponibili,
                    Occupati = t.Prenotazioni.Where(p => p.Stato == StatoPrenotazione.Confermata).Sum(p => (int?)p.NumeroPosti) ?? 0,
                    t.Stato,
                    Mostra = t.Mostra!.Titolo
                })
                .SingleOrDefaultAsync();

            if (turno is null)
                return Results.NotFound();

            return Results.Ok(new
            {
                turno.Id,
                turno.Mostra,
                turno.DataOra,
                turno.DurataMinuti,
                turno.PostiDisponibili,
                PostiOccupati = turno.Occupati,
                PostiResidui = turno.PostiDisponibili - turno.Occupati,
                turno.Stato
            });
        });

        gruppo.MapGet("/{id:int}/prenotazioni", async (int id, MuseoDbContext db) =>
        {
            if (!await db.Turni.AnyAsync(t => t.Id == id))
                return Results.NotFound();

            var prenotati = await db.Prenotazioni
                .AsNoTracking()
                .Where(p => p.TurnoVisitaId == id)
                .Include(p => p.Utente)
                .OrderBy(p => p.DataPrenotazione)
                .Select(p => new PrenotatoItem(p.Id, p.Utente!.Nome, p.Utente.Cognome, p.Utente.Email, p.NumeroPosti, p.Stato))
                .ToListAsync();

            return Results.Ok(prenotati);
        }).RequireAuthorization("Operatore");

        gruppo.MapPost("", async (int mostraId, TurnoRequest req, MuseoDbContext db) =>
        {
            var mostra = await db.Mostre.Include(m => m.Sala).SingleOrDefaultAsync(m => m.Id == mostraId);
            if (mostra is null)
                return Results.BadRequest(new { messaggio = "Mostra inesistente." });

            // Il turno deve cadere dentro il periodo di apertura della mostra.
            if (req.DataOra.Date < mostra.DataInizio.Date || req.DataOra.Date > mostra.DataFine.Date)
                return Results.Conflict(new { messaggio = "Il turno cade fuori dal periodo della mostra." });

            if (req.PostiDisponibili < 1 || req.PostiDisponibili > mostra.Sala!.Capienza)
                return Results.BadRequest(new { messaggio = $"I posti devono essere compresi tra 1 e {mostra.Sala.Capienza}." });

            if (await db.Turni.AnyAsync(t => t.MostraId == mostraId && t.DataOra == req.DataOra))
                return Results.Conflict(new { messaggio = "Esiste già un turno per questa mostra in quell'orario." });

            var turno = new TurnoVisita
            {
                MostraId = mostraId,
                DataOra = req.DataOra,
                DurataMinuti = req.DurataMinuti,
                PostiDisponibili = req.PostiDisponibili,
                Stato = StatoTurno.Aperto
            };

            db.Turni.Add(turno);
            await db.SaveChangesAsync();

            return Results.Created($"/api/turni/{turno.Id}", new { turno.Id });
        }).RequireAuthorization("Operatore");

        gruppo.MapDelete("/{id:int}", async (int id, MuseoDbContext db) =>
        {
            var turno = await db.Turni
                .Include(t => t.Prenotazioni)
                .SingleOrDefaultAsync(t => t.Id == id);

            if (turno is null)
                return Results.NotFound();

            turno.Stato = StatoTurno.Annullato;

            foreach (var prenotazione in turno.Prenotazioni.Where(p => p.Stato == StatoPrenotazione.Confermata))
                prenotazione.Stato = StatoPrenotazione.Annullata;

            await db.SaveChangesAsync();

            return Results.NoContent();
        }).RequireAuthorization("Operatore");
    }
}
