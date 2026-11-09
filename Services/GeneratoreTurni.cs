using MuseoApi.Models;

namespace MuseoApi.Services;

public record RichiestaGenerazione(
    IEnumerable<DayOfWeek> GiorniApertura,
    IEnumerable<TimeOnly> OrariIngresso,
    int DurataMinuti,
    int PostiPerTurno);

public static class GeneratoreTurni
{
    /// <summary>
    /// Genera i turni di una mostra combinando il suo periodo di apertura con i giorni
    /// e gli orari indicati. I turni gia esistenti non vengono duplicati.
    /// </summary>
    public static List<TurnoVisita> Genera(
        Mostra mostra,
        RichiestaGenerazione richiesta,
        IEnumerable<DateTime> turniEsistenti)
    {
        var giorni = richiesta.GiorniApertura.ToHashSet();
        var orari = richiesta.OrariIngresso.OrderBy(o => o).ToList();
        var esistenti = turniEsistenti.ToHashSet();
        var nuovi = new List<TurnoVisita>();

        for (var giorno = mostra.DataInizio.Date; giorno <= mostra.DataFine.Date; giorno = giorno.AddDays(1))
        {
            if (!giorni.Contains(giorno.DayOfWeek))
                continue;

            foreach (var orario in orari)
            {
                var dataOra = giorno.Add(orario.ToTimeSpan());

                // Salta i turni gia presenti: la generazione e ripetibile senza effetti collaterali.
                if (esistenti.Contains(dataOra))
                    continue;

                nuovi.Add(new TurnoVisita
                {
                    MostraId = mostra.Id,
                    DataOra = dataOra,
                    DurataMinuti = richiesta.DurataMinuti,
                    PostiDisponibili = richiesta.PostiPerTurno,
                    Stato = StatoTurno.Aperto
                });
            }
        }

        return nuovi;
    }
}
