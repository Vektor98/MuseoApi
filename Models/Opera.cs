namespace MuseoApi.Models;

public class Opera
{
    public int Id { get; set; }
    public string Titolo { get; set; } = string.Empty;
    public string Autore { get; set; } = string.Empty;
    public int? Anno { get; set; }
    public string? Tecnica { get; set; }
    public string? Inventario { get; set; }

    // Un'opera appartiene alla collezione permanente (null) oppure e esposta in una mostra.
    public int? MostraId { get; set; }

    public Mostra? Mostra { get; set; }
}
