using MuseoApi.Models;

namespace MuseoApi.Dtos;

public record RegistrazioneRequest(string Email, string Password, string Nome, string Cognome, string? Telefono);

public record LoginRequest(string Email, string Password);

public record LoginResponse(string Token, string Email, string Ruolo, DateTime Scadenza);

public record MostraListItem(
    int Id,
    string Titolo,
    string? Curatore,
    DateTime DataInizio,
    DateTime DataFine,
    string Sala,
    StatoMostra Stato,
    int TurniDisponibili);

public record MostraDettaglio(
    int Id,
    string Titolo,
    string? Descrizione,
    string? Curatore,
    DateTime DataInizio,
    DateTime DataFine,
    string Sala,
    int CapienzaSala,
    StatoMostra Stato,
    IEnumerable<OperaItem> Opere);

public record MostraRequest(
    string Titolo,
    string? Descrizione,
    string? Curatore,
    DateTime DataInizio,
    DateTime DataFine,
    int SalaId);

public record TurnoItem(
    int Id,
    DateTime DataOra,
    int DurataMinuti,
    int PostiDisponibili,
    int PostiOccupati,
    int PostiResidui,
    StatoTurno Stato);

public record TurnoRequest(DateTime DataOra, int DurataMinuti, int PostiDisponibili);

public record GenerazioneTurniRequest(
    IEnumerable<int> GiorniApertura,
    IEnumerable<string> OrariIngresso,
    int DurataMinuti,
    int PostiPerTurno);

public record GenerazioneTurniResponse(int TurniCreati, int TurniGiaPresenti);

public record PrenotazioneRequest(int TurnoVisitaId, int NumeroPosti);

public record PrenotazioneResponse(
    int Id,
    int TurnoVisitaId,
    string MostraTitolo,
    DateTime DataOra,
    int NumeroPosti,
    DateTime DataPrenotazione,
    StatoPrenotazione Stato,
    bool FeedbackInserito);

public record PrenotatoItem(
    int PrenotazioneId,
    string Nome,
    string Cognome,
    string Email,
    int NumeroPosti,
    StatoPrenotazione Stato);

public record FeedbackRequest(int PrenotazioneId, int Valutazione, string? Commento);

public record FeedbackResponse(int Id, int Valutazione, string? Commento, DateTime DataInserimento, string Autore);

public record OperaItem(int Id, string Titolo, string Autore, int? Anno, string? Tecnica, string? Inventario, int? MostraId);

public record OperaRequest(string Titolo, string Autore, int? Anno, string? Tecnica, string? Inventario, int? MostraId);

public record UtenteListItem(int Id, string Email, string Nome, string Cognome, string Ruolo, bool Attivo, DateTime DataRegistrazione);

public record CambioStatoRequest(bool Attivo);

public record IngressiPerMese(int Anno, int Mese, int Totale);

public record DashboardResponse(
    double TassoRiempimentoMedio,
    double TassoAnnullamento,
    double ValutazioneMedia,
    int IngressiTotali,
    IEnumerable<IngressiPerMese> IngressiPerMese);
