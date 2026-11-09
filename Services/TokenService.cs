using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MuseoApi.Models;

namespace MuseoApi.Services;

public class TokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    public (string Token, DateTime Scadenza) CreaToken(Utente utente)
    {
        var sezione = _config.GetSection("Jwt");
        var chiave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(sezione["Key"]!));
        var credenziali = new SigningCredentials(chiave, SecurityAlgorithms.HmacSha256);
        var scadenza = DateTime.UtcNow.AddMinutes(int.Parse(sezione["DurataMinuti"]!));

        // Il ruolo viaggia dentro il token: l'autorizzazione non interroga il database.
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, utente.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, utente.Email),
            new(ClaimTypes.NameIdentifier, utente.Id.ToString()),
            new(ClaimTypes.Role, utente.Ruolo!.Nome)
        };

        var token = new JwtSecurityToken(
            issuer: sezione["Issuer"],
            audience: sezione["Audience"],
            claims: claims,
            expires: scadenza,
            signingCredentials: credenziali);

        return (new JwtSecurityTokenHandler().WriteToken(token), scadenza);
    }
}
