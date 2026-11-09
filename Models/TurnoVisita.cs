namespace MuseoApi.Models;

public class TurnoVisita
{
    public int Id { get; set; }
    public int MostraId { get; set; }
    public DateTime DataOra { get; set; }
    public int DurataMinuti { get; set; } = 60;

    // La capienza e del singolo turno, non della mostra: non puo superare quella della sala.
    public int PostiDisponibili { get; set; }
    public StatoTurno Stato { get; set; } = StatoTurno.Aperto;

    public Mostra? Mostra { get; set; }
    public ICollection<Prenotazione> Prenotazioni { get; set; } = new List<Prenotazione>();
}
