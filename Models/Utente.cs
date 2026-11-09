namespace MuseoApi.Models;

public class Utente
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Cognome { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public int RuoloId { get; set; }
    public DateTime DataRegistrazione { get; set; }
    public bool Attivo { get; set; } = true;

    public Ruolo? Ruolo { get; set; }
    public ICollection<Prenotazione> Prenotazioni { get; set; } = new List<Prenotazione>();
}
