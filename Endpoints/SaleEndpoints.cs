using Microsoft.EntityFrameworkCore;
using MuseoApi.Data;

namespace MuseoApi.Endpoints;

public static class SaleEndpoints
{
    public static void MapSaleEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/sale", async (MuseoDbContext db) =>
            await db.Sale
                .AsNoTracking()
                .OrderBy(s => s.Nome)
                .Select(s => new { s.Id, s.Nome, s.Capienza, s.Ubicazione })
                .ToListAsync());
    }
}
