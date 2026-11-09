using Microsoft.EntityFrameworkCore;
using MuseoApi.Models;
using MuseoApi.Services;

namespace MuseoApi.Data;

public static class SeedData
{
    public static async Task InizializzaAsync(MuseoDbContext db)
    {
        await db.Database.MigrateAsync();

        if (await db.Ruoli.AnyAsync())
            return;

        var visitatore = new Ruolo { Nome = "Visitatore" };
        var operatore = new Ruolo { Nome = "Operatore" };
        var amministratore = new Ruolo { Nome = "Amministratore" };
        db.Ruoli.AddRange(visitatore, operatore, amministratore);

        var salaGrande = new Sala { Nome = "Sala delle Colonne", Capienza = 60, Ubicazione = "Piano terra" };
        var salaNovecento = new Sala { Nome = "Sala Novecento", Capienza = 35, Ubicazione = "Primo piano" };
        var salaGrafica = new Sala { Nome = "Gabinetto dei disegni", Capienza = 15, Ubicazione = "Secondo piano" };
        db.Sale.AddRange(salaGrande, salaNovecento, salaGrafica);

        await db.SaveChangesAsync();

        var oggi = DateTime.Today;

        var mostre = new List<Mostra>
        {
            new()
            {
                Titolo = "Luce del Mediterraneo",
                Descrizione = "Pittura di paesaggio tra Otto e Novecento nelle collezioni civiche.",
                Curatore = "Elena Marchetti",
                DataInizio = oggi.AddDays(-5),
                DataFine = oggi.AddDays(40),
                SalaId = salaGrande.Id,
                Stato = StatoMostra.InCorso
            },
            new()
            {
                Titolo = "Geometrie del Novecento",
                Descrizione = "Astrattismo italiano dal secondo dopoguerra agli anni Settanta.",
                Curatore = "Paolo Ferrari",
                DataInizio = oggi.AddDays(20),
                DataFine = oggi.AddDays(80),
                SalaId = salaNovecento.Id,
                Stato = StatoMostra.Programmata
            },
            new()
            {
                Titolo = "Segni su carta",
                Descrizione = "Disegni e incisioni dal fondo grafico del museo.",
                Curatore = "Chiara Lombardi",
                DataInizio = oggi.AddDays(-60),
                DataFine = oggi.AddDays(-10),
                SalaId = salaGrafica.Id,
                Stato = StatoMostra.Conclusa
            }
        };
        db.Mostre.AddRange(mostre);

        await db.SaveChangesAsync();

        db.Opere.AddRange(
            new Opera { Titolo = "Marina al tramonto", Autore = "Giuseppe De Nittis", Anno = 1878, Tecnica = "Olio su tela", Inventario = "INV-0121", MostraId = mostre[0].Id },
            new Opera { Titolo = "Scogliera", Autore = "Francesco Netti", Anno = 1881, Tecnica = "Olio su tavola", Inventario = "INV-0134", MostraId = mostre[0].Id },
            new Opera { Titolo = "Composizione n. 7", Autore = "Alberto Burri", Anno = 1956, Tecnica = "Tecnica mista", Inventario = "INV-0402", MostraId = mostre[1].Id },
            new Opera { Titolo = "Struttura modulare", Autore = "Carla Accardi", Anno = 1964, Tecnica = "Acrilico su tela", Inventario = "INV-0415", MostraId = mostre[1].Id },
            new Opera { Titolo = "Studio di figura", Autore = "Anonimo lombardo", Anno = 1720, Tecnica = "Sanguigna su carta", Inventario = "INV-0033", MostraId = null },
            new Opera { Titolo = "Veduta del porto", Autore = "Luigi Ricci", Anno = 1890, Tecnica = "Acquaforte", Inventario = "INV-0087", MostraId = null });

        // Turni generati con lo stesso servizio usato dall'endpoint: il seed esercita la logica reale.
        var richiesta = new RichiestaGenerazione(
            GiorniApertura: new[] { DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday },
            OrariIngresso: new[] { new TimeOnly(10, 0), new TimeOnly(12, 0), new TimeOnly(16, 0) },
            DurataMinuti: 90,
            PostiPerTurno: 25);

        foreach (var mostra in mostre)
            db.Turni.AddRange(GeneratoreTurni.Genera(mostra, richiesta, Array.Empty<DateTime>()));

        var hash = BCrypt.Net.BCrypt.HashPassword("Password123!");

        var utenti = new List<Utente>
        {
            new() { Email = "admin@museo.it", PasswordHash = hash, Nome = "Anna", Cognome = "Rossi", RuoloId = amministratore.Id, DataRegistrazione = oggi.AddMonths(-8) },
            new() { Email = "operatore@museo.it", PasswordHash = hash, Nome = "Marco", Cognome = "Bianchi", RuoloId = operatore.Id, DataRegistrazione = oggi.AddMonths(-5) },
            new() { Email = "giulia.verdi@example.it", PasswordHash = hash, Nome = "Giulia", Cognome = "Verdi", Telefono = "3331112233", RuoloId = visitatore.Id, DataRegistrazione = oggi.AddMonths(-3) },
            new() { Email = "luca.neri@example.it", PasswordHash = hash, Nome = "Luca", Cognome = "Neri", RuoloId = visitatore.Id, DataRegistrazione = oggi.AddMonths(-1) }
        };
        db.Utenti.AddRange(utenti);

        await db.SaveChangesAsync();

        // Prenotazioni di esempio: alcune su turni passati, cosi la dashboard ha dati e
        // almeno una prenotazione e idonea al feedback.
        var turniConclusi = await db.Turni
            .Where(t => t.MostraId == mostre[2].Id && t.DataOra < DateTime.Now)
            .OrderByDescending(t => t.DataOra)
            .Take(2)
            .ToListAsync();

        var turnoFuturo = await db.Turni
            .Where(t => t.MostraId == mostre[0].Id && t.DataOra > DateTime.Now)
            .OrderBy(t => t.DataOra)
            .FirstAsync();

        var prenotazioni = new List<Prenotazione>();

        if (turniConclusi.Count > 0)
        {
            prenotazioni.Add(new Prenotazione { UtenteId = utenti[2].Id, TurnoVisitaId = turniConclusi[0].Id, NumeroPosti = 2, DataPrenotazione = turniConclusi[0].DataOra.AddDays(-6), Stato = StatoPrenotazione.Confermata });
            prenotazioni.Add(new Prenotazione { UtenteId = utenti[3].Id, TurnoVisitaId = turniConclusi[0].Id, NumeroPosti = 1, DataPrenotazione = turniConclusi[0].DataOra.AddDays(-4), Stato = StatoPrenotazione.Confermata });
        }

        if (turniConclusi.Count > 1)
            prenotazioni.Add(new Prenotazione { UtenteId = utenti[2].Id, TurnoVisitaId = turniConclusi[1].Id, NumeroPosti = 1, DataPrenotazione = turniConclusi[1].DataOra.AddDays(-3), Stato = StatoPrenotazione.Annullata });

        prenotazioni.Add(new Prenotazione { UtenteId = utenti[2].Id, TurnoVisitaId = turnoFuturo.Id, NumeroPosti = 3, DataPrenotazione = oggi.AddDays(-2), Stato = StatoPrenotazione.Confermata });

        db.Prenotazioni.AddRange(prenotazioni);

        await db.SaveChangesAsync();

        var daRecensire = prenotazioni
            .Where(p => p.Stato == StatoPrenotazione.Confermata && turniConclusi.Any(t => t.Id == p.TurnoVisitaId))
            .ToList();

        if (daRecensire.Count > 0)
            db.Feedback.Add(new Feedback { PrenotazioneId = daRecensire[0].Id, Valutazione = 5, Commento = "Allestimento curato e personale disponibile.", DataInserimento = oggi.AddDays(-9) });

        if (daRecensire.Count > 1)
            db.Feedback.Add(new Feedback { PrenotazioneId = daRecensire[1].Id, Valutazione = 4, Commento = "Molto interessante, sale un po' affollate nel turno di punta.", DataInserimento = oggi.AddDays(-8) });

        await db.SaveChangesAsync();
    }
}
