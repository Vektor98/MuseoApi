import { creaCalendario } from "/js/calendario.js";

richiediAccesso(["Operatore", "Amministratore"]);

const avviso = document.getElementById("avviso");
const elencoMostre = document.getElementById("elencoMostre");
const areaTurni = document.getElementById("areaTurni");
const contenitoreCalendario = document.getElementById("calendario");
const contenitoreTurni = document.getElementById("turni");
const contenitorePrenotati = document.getElementById("prenotati");
const giornoScelto = document.getElementById("giornoScelto");
const legenda = document.getElementById("legenda");
const titoloTurni = document.getElementById("titoloTurni");
const titoloPrenotati = document.getElementById("titoloPrenotati");
const schede = document.getElementById("schede");

const VISTE = {
  corso: { filtro: m => m.stato === 2, vuoto: "Nessuna mostra aperta al pubblico in questo momento." },
  programma: { filtro: m => m.stato === 1, vuoto: "Nessuna mostra in programma." },
  archivio: { filtro: m => m.stato === 3 || m.stato === 4, vuoto: "L'archivio è vuoto: nessuna mostra conclusa o annullata." }
};

let vistaCorrente = "corso";
let mostreCaricate = [];
let mostraAperta = null;

// I valori corrispondono a DayOfWeek di .NET: domenica = 0.
const GIORNI_SETTIMANA = [
  { valore: 1, nome: "lun" }, { valore: 2, nome: "mar" }, { valore: 3, nome: "mer" },
  { valore: 4, nome: "gio" }, { valore: 5, nome: "ven" }, { valore: 6, nome: "sab" },
  { valore: 0, nome: "dom" }
];

document.getElementById("giorni").innerHTML = GIORNI_SETTIMANA.map(g => `
  <label class="scelta">
    <input type="checkbox" value="${g.valore}" ${g.valore !== 1 ? "checked" : ""}>
    ${g.nome}
  </label>`).join("");

function chiudiDettagli() {
    titoloTurni.hidden = true;
    areaTurni.hidden = true;
    titoloPrenotati.hidden = true;
    contenitoreCalendario.innerHTML = "";
    contenitoreTurni.innerHTML = "";
    contenitorePrenotati.innerHTML = "";
    legenda.innerHTML = "";
    giornoScelto.textContent = "";
    mostraAperta = null;
}

async function caricaSelezioni() {
  const sale = await api.sale();

  document.getElementById("sala").innerHTML = sale
    .map(s => `<option value="${s.id}">${s.nome}, capienza ${s.capienza}</option>`)
    .join("");
}

function aggiornaSelezioneTurni() {
  // La generazione ha senso solo su mostre non ancora concluse.
  const candidate = mostreCaricate.filter(m => m.stato === 1 || m.stato === 2);

  document.getElementById("mostraTurni").innerHTML = candidate.length
    ? candidate.map(m => `<option value="${m.id}">${m.titolo}</option>`).join("")
    : '<option value="">Nessuna mostra aperta</option>';
}

function rigaMostra(m) {
  const archiviata = m.stato === 3 || m.stato === 4;

  return `
    <tr>
      <td>${m.titolo}</td>
      <td>${periodo(m.dataInizio, m.dataFine)}</td>
      <td>${m.sala}</td>
      <td>${STATI_MOSTRA[m.stato]}</td>
      <td>${archiviata ? "—" : m.turniDisponibili}</td>
      <td>
        <button class="secondario" data-turni="${m.id}">Turni</button>
        ${archiviata ? "" : `<button class="secondario" data-annulla="${m.id}">Annulla</button>`}
      </td>
    </tr>`;
}

function disegnaMostre() {
  const vista = VISTE[vistaCorrente];
  const visibili = mostreCaricate.filter(vista.filtro);

  elencoMostre.innerHTML = visibili.length
    ? `<table>
        <thead><tr><th>Titolo</th><th>Periodo</th><th>Sala</th><th>Stato</th><th>Turni liberi</th><th></th></tr></thead>
        <tbody>${visibili.map(rigaMostra).join("")}</tbody>
      </table>`
    : `<p class="vuoto">${vista.vuoto}</p>`;
}

async function caricaMostre() {
  elencoMostre.innerHTML = '<p class="vuoto">Caricamento…</p>';

  try {
    mostreCaricate = await api.mostre();
    disegnaMostre();
    aggiornaSelezioneTurni();
  } catch (errore) {
    elencoMostre.innerHTML = "";
    mostraAvviso(avviso, errore.message);
  }
}

function disegnaTurniDelGiorno(giorno, turniDelGiorno) {
  const archiviata = mostraAperta.stato === 3 || mostraAperta.stato === 4;

  if (!giorno) {
    giornoScelto.textContent = "";
    contenitoreTurni.innerHTML = archiviata
      ? '<p class="vuoto">Nessun turno di questa mostra ha avuto prenotazioni.</p>'
      : '<p class="vuoto">Nessun turno generato per questa mostra.</p>';
    return;
  }

  const d = new Date(giorno);
  giornoScelto.textContent = `${GIORNI[d.getDay()]} ${d.getDate()} ${MESI[d.getMonth()]} ${d.getFullYear()}`;

  // In archivio restano solo i turni che hanno avuto visitatori.
  const turni = (archiviata ? turniDelGiorno.filter(t => t.postiOccupati > 0) : turniDelGiorno)
    .sort((a, b) => new Date(a.dataOra) - new Date(b.dataOra));

  contenitoreTurni.innerHTML = turni.length
    ? `<table>
        <thead><tr><th>Ora</th><th>Durata</th><th>Occupati</th><th>Stato</th><th></th></tr></thead>
        <tbody>${turni.map(t => {
          const futuro = new Date(t.dataOra) > new Date();

          return `
            <tr>
              <td>${oraBreve(t.dataOra)}</td>
              <td>${t.durataMinuti} min</td>
              <td>${t.postiOccupati} su ${t.postiDisponibili}</td>
              <td>${STATI_TURNO[t.stato]}</td>
              <td>
                <button class="secondario" data-prenotati="${t.id}">Prenotati</button>
                ${!archiviata && t.stato === 1 && futuro ? `<button class="secondario" data-annullaturno="${t.id}">Annulla</button>` : ""}
              </td>
            </tr>`;
        }).join("")}</tbody>
      </table>`
    : `<p class="vuoto">${archiviata ? "In questa giornata non si è presentato nessun visitatore." : "Nessun turno in questa giornata."}</p>`;
}

async function apriTurni(mostra) {
  mostraAperta = mostra;
  const archiviata = mostra.stato === 3 || mostra.stato === 4;

  try {
    const turni = await api.turniMostra(mostra.id);

    titoloTurni.textContent = archiviata ? `Turni visitati di ${mostra.titolo}` : `Turni di ${mostra.titolo}`;
    titoloTurni.hidden = false;
    areaTurni.hidden = false;
    titoloPrenotati.hidden = true;
    contenitorePrenotati.innerHTML = "";

    legenda.innerHTML = archiviata
      ? '<span class="l-liberi">giornate con visitatori</span><span class="l-vuoto">aperto, nessun visitatore</span>'
      : '<span class="l-liberi">posti liberi</span><span class="l-esaurito">esaurito</span>';

    creaCalendario({
      contenitore: contenitoreCalendario,
      turni,
      dataInizio: mostra.dataInizio,
      dataFine: mostra.dataFine,
      soloConPrenotati: archiviata,
      onSeleziona: disegnaTurniDelGiorno
    });

    titoloTurni.scrollIntoView({ behavior: "smooth" });
  } catch (errore) {
    mostraAvviso(avviso, errore.message);
  }
}

schede.addEventListener("click", e => {
  const bottone = e.target.closest("[data-vista]");
  if (!bottone) return;

  vistaCorrente = bottone.dataset.vista;
  schede.querySelectorAll(".scheda").forEach(s => s.classList.toggle("attiva", s === bottone));

  // Il dettaglio aperto appartiene alla vista precedente: va chiuso.
  chiudiDettagli();
  disegnaMostre();
});

elencoMostre.addEventListener("click", async e => {
  const turni = e.target.closest("[data-turni]");
  const annulla = e.target.closest("[data-annulla]");

  if (turni) {
    nascondiAvviso(avviso);
    const mostra = mostreCaricate.find(m => m.id === Number(turni.dataset.turni));
    if (mostra) await apriTurni(mostra);
    return;
  }

  if (annulla) {
    if (!confirm("Annullare la mostra? Verranno annullati tutti i suoi turni e le relative prenotazioni.")) return;

    try {
      await api.annullaMostra(Number(annulla.dataset.annulla));
      chiudiDettagli();
      await caricaMostre();
      mostraAvviso(avviso, "Mostra annullata insieme a turni e prenotazioni. La trovi in archivio.", "ok");
    } catch (errore) {
      mostraAvviso(avviso, errore.message);
    }
  }
});

contenitoreTurni.addEventListener("click", async e => {
  const prenotati = e.target.closest("[data-prenotati]");
  const annullaTurno = e.target.closest("[data-annullaturno]");

  if (prenotati) {
    nascondiAvviso(avviso);
    try {
      const lista = await api.prenotatiTurno(Number(prenotati.dataset.prenotati));
      titoloPrenotati.hidden = false;

      contenitorePrenotati.innerHTML = lista.length
        ? `<table>
            <thead><tr><th>Cognome</th><th>Nome</th><th>Email</th><th>Ingressi</th><th>Stato</th></tr></thead>
            <tbody>${lista.map(p => `
              <tr>
                <td>${p.cognome}</td>
                <td>${p.nome}</td>
                <td>${p.email}</td>
                <td>${p.numeroPosti}</td>
                <td>${STATI_PRENOTAZIONE[p.stato]}</td>
              </tr>`).join("")}</tbody>
          </table>`
        : '<p class="vuoto">Nessun prenotato per questo turno.</p>';

      titoloPrenotati.scrollIntoView({ behavior: "smooth" });
    } catch (errore) {
      mostraAvviso(avviso, errore.message);
    }
    return;
  }

  if (annullaTurno) {
    if (!confirm("Annullare il turno? Le prenotazioni confermate verranno annullate.")) return;

    try {
      await api.annullaTurno(Number(annullaTurno.dataset.annullaturno));
      mostraAvviso(avviso, "Turno annullato.", "ok");
      const riferimento = mostraAperta;
      await caricaMostre();
      if (riferimento) await apriTurni(riferimento);
    } catch (errore) {
      mostraAvviso(avviso, errore.message);
    }
  }
});

document.getElementById("moduloMostra").addEventListener("submit", async e => {
  e.preventDefault();
  nascondiAvviso(avviso);

  const dati = {
    titolo: document.getElementById("titolo").value.trim(),
    descrizione: document.getElementById("descrizione").value.trim() || null,
    curatore: document.getElementById("curatore").value.trim() || null,
    dataInizio: document.getElementById("dataInizio").value,
    dataFine: document.getElementById("dataFine").value,
    salaId: Number(document.getElementById("sala").value)
  };

  try {
    await api.creaMostra(dati);
    e.target.reset();
    await caricaMostre();
    mostraAvviso(avviso, "Mostra creata. Ora puoi generarne i turni.", "ok");
  } catch (errore) {
    mostraAvviso(avviso, errore.message);
  }
});

document.getElementById("moduloTurni").addEventListener("submit", async e => {
  e.preventDefault();
  nascondiAvviso(avviso);

  const mostraId = Number(document.getElementById("mostraTurni").value);
  if (!mostraId) {
    mostraAvviso(avviso, "Non ci sono mostre aperte per cui generare turni.");
    return;
  }

  const giorni = [...document.querySelectorAll("#giorni input:checked")].map(i => Number(i.value));
  const orari = document.getElementById("orari").value.split(",").map(o => o.trim()).filter(Boolean);

  const dati = {
    giorniApertura: giorni,
    orariIngresso: orari,
    durataMinuti: Number(document.getElementById("durata").value),
    postiPerTurno: Number(document.getElementById("posti").value)
  };

  try {
    const esito = await api.generaTurni(mostraId, dati);
    chiudiDettagli();
    await caricaMostre();
    mostraAvviso(avviso, `Generati ${esito.turniCreati} turni. Ne erano già presenti ${esito.turniGiaPresenti}.`, "ok");
  } catch (errore) {
    mostraAvviso(avviso, errore.message);
  }
});

caricaSelezioni().then(caricaMostre);
