using System.Security.Claims;

namespace MuseoApi.Services;

public static class UtenteCorrente
{
    public static int Id(ClaimsPrincipal principal)
    {
        var valore = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.Parse(valore!);
    }
}
