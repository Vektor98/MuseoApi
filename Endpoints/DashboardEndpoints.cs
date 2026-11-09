using Microsoft.EntityFrameworkCore;
using MuseoApi.Data;
using MuseoApi.Dtos;
using MuseoApi.Models;

namespace MuseoApi.Endpoints;

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/dashboard", async (MuseoDbContext db) =>
        {
            // Il riempimento si calcola sui soli turni gia svolti: quelli futuri
            // abbasserebbero la media senza significato.
            var adesso = DateTime.Now;

            var turni = await db.Turni
                .AsNoTracking()
                .Where(t => t.Stato != StatoTurno.Annullato && t.DataOra < adesso)
                .Select(t => new
                {
                    t.PostiDisponibili,
                    Occupati = t.Prenotazioni.Where(p => p.Stato == StatoPrenotazione.Confermata).Sum(p => (int?)p.NumeroPosti) ?? 0
                })
                .ToListAsync();

            var tassoRiempimento = turni.Count == 0
                ? 0
                : turni.Average(t => t.PostiDisponibili == 0 ? 0 : (double)t.Occupati / t.PostiDisponibili);

            var totalePrenotazioni = await db.Prenotazioni.CountAsync();
            var annullate = await db.Prenotazioni.CountAsync(p => p.Stato == StatoPrenotazione.Annullata);

            var tassoAnnullamento = totalePrenotazioni == 0
                ? 0
                : (double)annullate / totalePrenotazioni;

            var valutazioneMedia = await db.Feedback.AnyAsync()
                ? await db.Feedback.AverageAsync(f => (double)f.Valutazione)
                : 0;

            var ingressiTotali = await db.Prenotazioni
                .Where(p => p.Stato == StatoPrenotazione.Confermata)
                .SumAsync(p => (int?)p.NumeroPosti) ?? 0;

            var perMese = await db.Prenotazioni
                .AsNoTracking()
                .Where(p => p.Stato == StatoPrenotazione.Confermata)
                .GroupBy(p => new { p.TurnoVisita!.DataOra.Year, p.TurnoVisita.DataOra.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new IngressiPerMese(g.Key.Year, g.Key.Month, g.Sum(p => p.NumeroPosti)))
                .ToListAsync();

            return Results.Ok(new DashboardResponse(
                Math.Round(tassoRiempimento * 100, 1),
                Math.Round(tassoAnnullamento * 100, 1),
                Math.Round(valutazioneMedia, 2),
                ingressiTotali,
                perMese));
        }).RequireAuthorization("Amministratore");
    }
}
