using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MuseoApi.Data;
using MuseoApi.Dtos;
using MuseoApi.Models;
using MuseoApi.Services;

namespace MuseoApi.Endpoints;

public static class FeedbackEndpoints
{
    public static void MapFeedbackEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/feedback", async (FeedbackRequest req, ClaimsPrincipal principal, MuseoDbContext db) =>
        {
            var utenteId = UtenteCorrente.Id(principal);

            if (req.Valutazione < 1 || req.Valutazione > 5)
                return Results.BadRequest(new { messaggio = "La valutazione deve essere compresa tra 1 e 5." });

            var prenotazione = await db.Prenotazioni
                .Include(p => p.TurnoVisita)
                .Include(p => p.Feedback)
                .SingleOrDefaultAsync(p => p.Id == req.PrenotazioneId);

            if (prenotazione is null)
                return Results.NotFound(new { messaggio = "Prenotazione inesistente." });

            if (prenotazione.UtenteId != utenteId)
                return Results.Json(new { messaggio = "La prenotazione appartiene a un altro utente." }, statusCode: 403);

            if (prenotazione.Stato != StatoPrenotazione.Confermata)
                return Results.Conflict(new { messaggio = "La prenotazione risulta annullata." });

            // Il feedback presuppone la visita: si apre solo a turno terminato.
            var fineTurno = prenotazione.TurnoVisita!.DataOra.AddMinutes(prenotazione.TurnoVisita.DurataMinuti);

            if (fineTurno > DateTime.Now)
                return Results.Conflict(new { messaggio = "Il feedback è consentito solo a visita conclusa." });

            if (prenotazione.Feedback is not null)
                return Results.Conflict(new { messaggio = "Hai già lasciato un feedback per questa visita." });

            var feedback = new Feedback
            {
                PrenotazioneId = prenotazione.Id,
                Valutazione = req.Valutazione,
                Commento = req.Commento,
                DataInserimento = DateTime.Now
            };

            db.Feedback.Add(feedback);
            await db.SaveChangesAsync();

            return Results.Created($"/api/feedback/{feedback.Id}", new { feedback.Id });
        }).RequireAuthorization();
    }
}
