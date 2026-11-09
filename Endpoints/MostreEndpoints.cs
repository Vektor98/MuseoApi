using Microsoft.EntityFrameworkCore;
using MuseoApi.Data;
using MuseoApi.Dtos;
using MuseoApi.Models;
using MuseoApi.Services;

namespace MuseoApi.Endpoints;

public static class MostreEndpoints
{
    public static void MapMostreEndpoints(this IEndpointRouteBuilder app)
    {
        var gruppo = app.MapGroup("/api/mostre");

        gruppo.MapGet("", async (MuseoDbContext db, DateTime? dal, DateTime? al, StatoMostra? stato) =>
        {
            var query = db.Mostre.AsNoTracking().Include(m => m.Sala).AsQueryable();

            // Una mostra rientra nel filtro se il suo periodo interseca l'intervallo richiesto.
            if (dal.HasValue)
                query = query.Where(m => m.DataFine >= dal.Value);

            if (al.HasValue)
                query = query.Where(m => m.DataInizio <= al.Value);

            if (stato.HasValue)
                query = query.Where(m => m.Stato == stato.Value);

            var adesso = DateTime.Now;

            var mostre = await query
                .OrderBy(m => m.DataInizio)
                .Select(m => new MostraListItem(
                    m.Id,
                    m.Titolo,
                    m.Curatore,
                    m.DataInizio,
                    m.DataFine,
                    m.Sala!.Nome,
                    m.Stato,
                    m.Turni.Count(t => t.Stato == StatoTurno.Aperto
                        && t.DataOra > adesso
                        && t.Prenotazioni.Where(p => p.Stato == StatoPrenotazione.Confermata).Sum(p => (int?)p.NumeroPosti) < t.PostiDisponibili)))
                .ToListAsync();

            return Results.Ok(mostre);
        });

        gruppo.MapGet("/{id:int}", async (int id, MuseoDbContext db) =>
        {
            var mostra = await db.Mostre
                .AsNoTracking()
                .Include(m => m.Sala)
                .Include(m => m.Opere)
                .Where(m => m.Id == id)
                .Select(m => new MostraDettaglio(
                    m.Id,
                    m.Titolo,
                    m.Descrizione,
                    m.Curatore,
                    m.DataInizio,
                    m.DataFine,
                    m.Sala!.Nome,
                    m.Sala.Capienza,
                    m.Stato,
                    m.Opere.Select(o => new OperaItem(o.Id, o.Titolo, o.Autore, o.Anno, o.Tecnica, o.Inventario, o.MostraId))))
                .SingleOrDefaultAsync();

            return mostra is null ? Results.NotFound() : Results.Ok(mostra);
        });

        gruppo.MapGet("/{id:int}/turni", async (int id, MuseoDbContext db, bool soloDisponibili = false) =>
        {
            if (!await db.Mostre.AnyAsync(m => m.Id == id))
                return Results.NotFound();

            var query = db.Turni.AsNoTracking().Where(t => t.MostraId == id);

            if (soloDisponibili)
                query = query.Where(t => t.Stato == StatoTurno.Aperto && t.DataOra > DateTime.Now);

            var turni = await query
                .OrderBy(t => t.DataOra)
                .Select(t => new
                {
                    t.Id,
                    t.DataOra,
                    t.DurataMinuti,
                    t.PostiDisponibili,
                    Occupati = t.Prenotazioni.Where(p => p.Stato == StatoPrenotazione.Confermata).Sum(p => (int?)p.NumeroPosti) ?? 0,
                    t.Stato
                })
                .ToListAsync();

            var risultato = turni.Select(t => new TurnoItem(
                t.Id, t.DataOra, t.DurataMinuti, t.PostiDisponibili, t.Occupati,
                t.PostiDisponibili - t.Occupati, t.Stato));

            return Results.Ok(risultato);
        });

        gruppo.MapGet("/{id:int}/feedback", async (int id, MuseoDbContext db) =>
        {
            if (!await db.Mostre.AnyAsync(m => m.Id == id))
                return Results.NotFound();

            var feedback = await db.Feedback
                .AsNoTracking()
                .Where(f => f.Prenotazione!.TurnoVisita!.MostraId == id)
                .OrderByDescending(f => f.DataInserimento)
                .Select(f => new FeedbackResponse(
                    f.Id,
                    f.Valutazione,
                    f.Commento,
                    f.DataInserimento,
                    f.Prenotazione!.Utente!.Nome + " " + f.Prenotazione.Utente.Cognome.Substring(0, 1) + "."))
                .ToListAsync();

            return Results.Ok(feedback);
        });

        gruppo.MapPost("", async (MostraRequest req, MuseoDbContext db) =>
        {
            var sala = await db.Sale.FindAsync(req.SalaId);
            if (sala is null)
                return Results.BadRequest(new { messaggio = "Sala inesistente." });

            if (req.DataFine < req.DataInizio)
                return Results.BadRequest(new { messaggio = "La data di chiusura non può precedere quella di apertura." });

            // Una sala non puo ospitare due mostre con periodi sovrapposti.
            var sovrapposta = await db.Mostre.AnyAsync(m =>
                m.SalaId == req.SalaId &&
                m.Stato != StatoMostra.Annullata &&
                m.DataInizio <= req.DataFine &&
                m.DataFine >= req.DataInizio);

            if (sovrapposta)
                return Results.Conflict(new { messaggio = "La sala ospita già un'altra mostra in questo periodo." });

            var mostra = new Mostra
            {
                Titolo = req.Titolo.Trim(),
                Descrizione = req.Descrizione,
                Curatore = req.Curatore,
                DataInizio = req.DataInizio.Date,
                DataFine = req.DataFine.Date,
                SalaId = req.SalaId,
                Stato = req.DataInizio.Date <= DateTime.Today ? StatoMostra.InCorso : StatoMostra.Programmata
            };

            db.Mostre.Add(mostra);
            await db.SaveChangesAsync();

            return Results.Created($"/api/mostre/{mostra.Id}", new { mostra.Id });
        }).RequireAuthorization("Operatore");

        gruppo.MapPut("/{id:int}", async (int id, MostraRequest req, MuseoDbContext db) =>
        {
            var mostra = await db.Mostre.FindAsync(id);
            if (mostra is null)
                return Results.NotFound();

            if (req.DataFine < req.DataInizio)
                return Results.BadRequest(new { messaggio = "La data di chiusura non può precedere quella di apertura." });

            // Restringere il periodo lascerebbe turni orfani fuori dalle date della mostra.
            var turniFuori = await db.Turni.CountAsync(t =>
                t.MostraId == id &&
                t.Stato != StatoTurno.Annullato &&
                (t.DataOra < req.DataInizio.Date || t.DataOra >= req.DataFine.Date.AddDays(1)));

            if (turniFuori > 0)
                return Results.Conflict(new { messaggio = $"Ci sono {turniFuori} turni fuori dal nuovo periodo: annullali prima di modificare le date." });

            mostra.Titolo = req.Titolo.Trim();
            mostra.Descrizione = req.Descrizione;
            mostra.Curatore = req.Curatore;
            mostra.DataInizio = req.DataInizio.Date;
            mostra.DataFine = req.DataFine.Date;
            mostra.SalaId = req.SalaId;

            await db.SaveChangesAsync();

            return Results.NoContent();
        }).RequireAuthorization("Operatore");

        gruppo.MapDelete("/{id:int}", async (int id, MuseoDbContext db) =>
        {
            var mostra = await db.Mostre
                .Include(m => m.Turni)
                .ThenInclude(t => t.Prenotazioni)
                .SingleOrDefaultAsync(m => m.Id == id);

            if (mostra is null)
                return Results.NotFound();

            // Annullamento a cascata logica: la mostra trascina turni e prenotazioni,
            // senza cancellare nulla dal database.
            mostra.Stato = StatoMostra.Annullata;

            foreach (var turno in mostra.Turni)
            {
                turno.Stato = StatoTurno.Annullato;

                foreach (var prenotazione in turno.Prenotazioni.Where(p => p.Stato == StatoPrenotazione.Confermata))
                    prenotazione.Stato = StatoPrenotazione.Annullata;
            }

            await db.SaveChangesAsync();

            return Results.NoContent();
        }).RequireAuthorization("Operatore");

        gruppo.MapPost("/{id:int}/turni/genera", async (int id, GenerazioneTurniRequest req, MuseoDbContext db) =>
        {
            var mostra = await db.Mostre.Include(m => m.Sala).SingleOrDefaultAsync(m => m.Id == id);
            if (mostra is null)
                return Results.NotFound();

            if (mostra.Stato == StatoMostra.Annullata)
                return Results.Conflict(new { messaggio = "La mostra è annullata." });

            if (req.PostiPerTurno < 1 || req.PostiPerTurno > mostra.Sala!.Capienza)
                return Results.BadRequest(new { messaggio = $"I posti per turno devono essere compresi tra 1 e {mostra.Sala.Capienza}." });

            if (!req.OrariIngresso.Any() || !req.GiorniApertura.Any())
                return Results.BadRequest(new { messaggio = "Indica almeno un giorno di apertura e un orario di ingresso." });

            List<TimeOnly> orari;
            try
            {
                orari = req.OrariIngresso.Select(o => TimeOnly.Parse(o)).ToList();
            }
            catch (FormatException)
            {
                return Results.BadRequest(new { messaggio = "Gli orari devono essere nel formato HH:mm." });
            }

            var esistenti = await db.Turni
                .Where(t => t.MostraId == id)
                .Select(t => t.DataOra)
                .ToListAsync();

            var richiesta = new RichiestaGenerazione(
                req.GiorniApertura.Select(g => (DayOfWeek)g),
                orari,
                req.DurataMinuti,
                req.PostiPerTurno);

            var nuovi = GeneratoreTurni.Genera(mostra, richiesta, esistenti);

            db.Turni.AddRange(nuovi);
            await db.SaveChangesAsync();

            var attesi = nuovi.Count + esistenti.Count;

            return Results.Ok(new GenerazioneTurniResponse(nuovi.Count, attesi - nuovi.Count));
        }).RequireAuthorization("Operatore");
    }
}
