namespace MuseoApi.Models;

public class Sala
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;

    // Capienza fisica della sala: limite massimo per i posti di ogni turno.
    public int Capienza { get; set; }
    public string? Ubicazione { get; set; }

    public ICollection<Mostra> Mostre { get; set; } = new List<Mostra>();
}
