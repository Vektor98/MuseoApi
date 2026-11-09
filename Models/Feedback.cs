namespace MuseoApi.Models;

public class Feedback
{
    public int Id { get; set; }
    public int PrenotazioneId { get; set; }
    public int Valutazione { get; set; }
    public string? Commento { get; set; }
    public DateTime DataInserimento { get; set; }

    public Prenotazione? Prenotazione { get; set; }
}
