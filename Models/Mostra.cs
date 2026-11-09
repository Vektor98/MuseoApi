namespace MuseoApi.Models;

public class Mostra
{
    public int Id { get; set; }
    public string Titolo { get; set; } = string.Empty;
    public string? Descrizione { get; set; }
    public string? Curatore { get; set; }

    // Periodo di apertura: delimita le date ammesse per i turni di visita.
    public DateTime DataInizio { get; set; }
    public DateTime DataFine { get; set; }

    public int SalaId { get; set; }
    public StatoMostra Stato { get; set; } = StatoMostra.Programmata;

    public Sala? Sala { get; set; }
    public ICollection<TurnoVisita> Turni { get; set; } = new List<TurnoVisita>();
    public ICollection<Opera> Opere { get; set; } = new List<Opera>();
}
