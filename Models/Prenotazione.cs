namespace MuseoApi.Models;

public class Prenotazione
{
    public int Id { get; set; }
    public int UtenteId { get; set; }
    public int TurnoVisitaId { get; set; }

    // Un visitatore puo prenotare piu ingressi nello stesso turno (famiglia, gruppo).
    public int NumeroPosti { get; set; } = 1;
    public DateTime DataPrenotazione { get; set; }
    public StatoPrenotazione Stato { get; set; } = StatoPrenotazione.Confermata;

    public Utente? Utente { get; set; }
    public TurnoVisita? TurnoVisita { get; set; }
    public Feedback? Feedback { get; set; }
}
