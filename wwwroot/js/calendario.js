// Calendario mensile condiviso tra la pagina pubblica e la gestione.
// Riceve i turni gia caricati e si limita a disporli per giornata.

const GIORNI_INTESTAZIONE = ["lun", "mar", "mer", "gio", "ven", "sab", "dom"];

function chiave(data) {
  return `${data.getFullYear()}-${String(data.getMonth() + 1).padStart(2, "0")}-${String(data.getDate()).padStart(2, "0")}`;
}

function primoLunedi(anno, mese) {
  const primo = new Date(anno, mese, 1);
  // getDay() mette domenica a 0: qui la settimana inizia di lunedi.
  const scarto = (primo.getDay() + 6) % 7;
  return new Date(anno, mese, 1 - scarto);
}

export function creaCalendario(opzioni) {
  const {
    contenitore,
    turni,
    dataInizio,
    dataFine,
    onSeleziona,
    soloConPrenotati = false
  } = opzioni;

  const inizio = new Date(dataInizio);
  const fine = new Date(dataFine);
  const adesso = new Date();

  // Indice giorno -> turni di quel giorno.
  const perGiorno = new Map();
  for (const t of turni) {
    const k = chiave(new Date(t.dataOra));
    if (!perGiorno.has(k)) perGiorno.set(k, []);
    perGiorno.get(k).push(t);
  }

  const giorniConTurni = [...perGiorno.keys()].sort();

  let selezionato = null;
  let mese, anno;

  // Si apre sul primo giorno utile, non sul mese corrente: una mostra puo
  // iniziare fra settimane.
  const primoUtile = soloConPrenotati
    ? (giorniConTurni.find(k => perGiorno.get(k).some(t => t.postiOccupati > 0)) ?? giorniConTurni[0])
    : (giorniConTurni.find(k => new Date(k) >= new Date(adesso.toDateString())) ?? giorniConTurni[0]);
  const riferimento = primoUtile ? new Date(primoUtile) : inizio;
  mese = riferimento.getMonth();
  anno = riferimento.getFullYear();

  function statoGiorno(k, data) {
    if (data < new Date(inizio.toDateString()) || data > new Date(fine.toDateString()))
      return "fuori";

    const delGiorno = perGiorno.get(k) ?? [];
    if (!delGiorno.length) return "chiuso";

    // In archivio tutte le giornate di apertura restano consultabili:
    // quelle senza alcun visitatore si distinguono dalle altre.
    if (soloConPrenotati)
      return delGiorno.some(t => t.postiOccupati > 0) ? "visitato" : "senza-visite";

    const prenotabili = delGiorno.filter(t =>
      t.stato === 1 && t.postiResidui > 0 && new Date(t.dataOra) > adesso);

    if (prenotabili.length) return "liberi";
    return data < new Date(adesso.toDateString()) ? "passato" : "esaurito";
  }

  function disegna() {
    const celle = [];
    const partenza = primoLunedi(anno, mese);

    for (let i = 0; i < 42; i++) {
      const data = new Date(partenza.getFullYear(), partenza.getMonth(), partenza.getDate() + i);
      const k = chiave(data);
      const fuoriMese = data.getMonth() !== mese;
      const stato = statoGiorno(k, data);
      const selezionabili = ["liberi", "esaurito", "passato", "visitato", "senza-visite"];
      const attivo = !fuoriMese && selezionabili.includes(stato);

      celle.push(`
        <button type="button"
          class="cal-giorno ${fuoriMese ? "fuori-mese" : stato} ${k === selezionato ? "scelto" : ""}"
          data-giorno="${k}" ${attivo ? "" : "disabled"}>
          ${data.getDate()}
        </button>`);
    }

    // Le frecce si fermano ai mesi coperti dalla mostra.
    const indietro = new Date(anno, mese, 1) > new Date(inizio.getFullYear(), inizio.getMonth(), 1);
    const avanti = new Date(anno, mese, 1) < new Date(fine.getFullYear(), fine.getMonth(), 1);

    contenitore.innerHTML = `
      <div class="calendario">
        <div class="cal-testa">
          <button type="button" class="cal-nav" data-passo="-1" ${indietro ? "" : "disabled"} aria-label="Mese precedente">‹</button>
          <span class="cal-mese">${MESI[mese]} ${anno}</span>
          <button type="button" class="cal-nav" data-passo="1" ${avanti ? "" : "disabled"} aria-label="Mese successivo">›</button>
        </div>
        <div class="cal-settimana">${GIORNI_INTESTAZIONE.map(g => `<span>${g}</span>`).join("")}</div>
        <div class="cal-griglia">${celle.join("")}</div>
      </div>`;
  }

  contenitore.addEventListener("click", e => {
    const nav = e.target.closest("[data-passo]");
    if (nav) {
      const data = new Date(anno, mese + Number(nav.dataset.passo), 1);
      mese = data.getMonth();
      anno = data.getFullYear();
      disegna();
      return;
    }

    const giorno = e.target.closest("[data-giorno]");
    if (!giorno || giorno.disabled) return;

    selezionato = giorno.dataset.giorno;
    disegna();
    onSeleziona(selezionato, perGiorno.get(selezionato) ?? []);
  });

  disegna();

  // Apertura sul primo giorno utile, cosi la pagina non parte vuota.
  if (primoUtile) {
    selezionato = primoUtile;
    disegna();
    onSeleziona(primoUtile, perGiorno.get(primoUtile) ?? []);
  } else {
    onSeleziona(null, []);
  }
}
