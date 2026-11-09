using Microsoft.EntityFrameworkCore;
using MuseoApi.Data;
using MuseoApi.Dtos;
using MuseoApi.Models;

namespace MuseoApi.Endpoints;

public static class OpereEndpoints
{
    public static void MapOpereEndpoints(this IEndpointRouteBuilder app)
    {
        var gruppo = app.MapGroup("/api/opere");

        gruppo.MapGet("", async (MuseoDbContext db, string? q, int? mostraId) =>
        {
            var query = db.Opere.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var testo = q.Trim();
                query = query.Where(o => o.Titolo.Contains(testo) || o.Autore.Contains(testo));
            }

            if (mostraId.HasValue)
                query = query.Where(o => o.MostraId == mostraId.Value);

            var opere = await query
                .OrderBy(o => o.Autore)
                .ThenBy(o => o.Titolo)
                .Select(o => new OperaItem(o.Id, o.Titolo, o.Autore, o.Anno, o.Tecnica, o.Inventario, o.MostraId))
                .ToListAsync();

            return Results.Ok(opere);
        });

        gruppo.MapPost("", async (OperaRequest req, MuseoDbContext db) =>
        {
            if (req.MostraId.HasValue && !await db.Mostre.AnyAsync(m => m.Id == req.MostraId.Value))
                return Results.BadRequest(new { messaggio = "Mostra inesistente." });

            var opera = new Opera
            {
                Titolo = req.Titolo.Trim(),
                Autore = req.Autore.Trim(),
                Anno = req.Anno,
                Tecnica = req.Tecnica,
                Inventario = req.Inventario,
                MostraId = req.MostraId
            };

            db.Opere.Add(opera);
            await db.SaveChangesAsync();

            return Results.Created($"/api/opere/{opera.Id}", new { opera.Id });
        }).RequireAuthorization("Operatore");

        gruppo.MapPut("/{id:int}/mostra", async (int id, int? mostraId, MuseoDbContext db) =>
        {
            var opera = await db.Opere.FindAsync(id);
            if (opera is null)
                return Results.NotFound();

            if (mostraId.HasValue && !await db.Mostre.AnyAsync(m => m.Id == mostraId.Value))
                return Results.BadRequest(new { messaggio = "Mostra inesistente." });

            // mostraId assente: l'opera rientra in deposito.
            opera.MostraId = mostraId;
            await db.SaveChangesAsync();

            return Results.NoContent();
        }).RequireAuthorization("Operatore");

        gruppo.MapDelete("/{id:int}", async (int id, MuseoDbContext db) =>
        {
            var opera = await db.Opere.FindAsync(id);
            if (opera is null)
                return Results.NotFound();

            // Un'opera esposta non si elimina: va prima rimossa dalla mostra.
            if (opera.MostraId is not null)
                return Results.Conflict(new { messaggio = "L'opera è esposta in una mostra: rimuovila dalla mostra prima di eliminarla." });

            db.Opere.Remove(opera);
            await db.SaveChangesAsync();

            return Results.NoContent();
        }).RequireAuthorization("Operatore");
    }
}
