import { creaCalendario } from "/js/calendario.js";

const id = Number(parametroUrl("id"));
const avviso = document.getElementById("avviso");
const intestazione = document.getElementById("intestazione");
const contenitoreCalendario = document.getElementById("calendario");
const contenitoreTurni = document.getElementById("turni");
const giornoScelto = document.getElementById("giornoScelto");
const contenitoreOpere = document.getElementById("opere");
const contenitoreFeedback = document.getElementById("feedback");

let mostraCorrente = null;

function chipTurno(t, autenticato) {
    const prenotabile = t.stato === 1 && t.postiResidui > 0 && new Date(t.dataOra) > new Date();
    const passato = new Date(t.dataOra) <= new Date();

    const nota = t.stato === 3
        ? "annullato"
        : passato
            ? "concluso"
            : t.postiResidui > 0
                ? `${t.postiResidui} posti`
                : "esaurito";

    if (!prenotabile)
        return `<button class="turno" disabled><span class="ora">${oraBreve(t.dataOra)}</span><span class="liberi">${nota}</span></button>`;

  // Senza sessione il turno porta all'accesso invece di essere inerte.
  return autenticato
    ? `<button class="turno" data-prenota="${t.id}"><span class="ora">${oraBreve(t.dataOra)}</span><span class="liberi">${nota}</span></button>`
    : `<a class="turno" href="/login.html"><span class="ora">${oraBreve(t.dataOra)}</span><span class="liberi">accedi per prenotare</span></a>`;
}

function mostraGiorno(giorno, turniDelGiorno) {
  if (!giorno) {
    giornoScelto.textContent = "";
    contenitoreTurni.innerHTML = '<p class="vuoto">Questa mostra non ha turni di visita.</p>';
    return;
  }

  const d = new Date(giorno);
  giornoScelto.textContent = `${GIORNI[d.getDay()]} ${d.getDate()} ${MESI[d.getMonth()]} ${d.getFullYear()}`;

  const autenticato = leggiSessione() !== null;

  contenitoreTurni.innerHTML = turniDelGiorno.length
    ? `<div class="turni-giorno">${turniDelGiorno
        .sort((a, b) => new Date(a.dataOra) - new Date(b.dataOra))
        .map(t => chipTurno(t, autenticato))
        .join("")}</div>`
    : '<p class="vuoto">Nessun turno in questa giornata.</p>';
}

async function caricaCalendario() {
  try {
    const turni = await api.turniMostra(id);

    creaCalendario({
      contenitore: contenitoreCalendario,
      turni,
      dataInizio: mostraCorrente.dataInizio,
      dataFine: mostraCorrente.dataFine,
      onSeleziona: mostraGiorno
    });
  } catch (errore) {
    mostraAvviso(avviso, errore.message);
  }
}

async function caricaMostra() {
  try {
    mostraCorrente = await api.mostra(id);
    const m = mostraCorrente;

    intestazione.innerHTML = `
      <h1>${m.titolo}</h1>
      <p class="sottotitolo">
        ${periodo(m.dataInizio, m.dataFine)}, ${m.sala}.
        ${m.curatore ? `A cura di ${m.curatore}.` : ""}
        ${m.stato === 3 ? " La mostra è conclusa." : m.stato === 4 ? " La mostra è stata annullata." : ""}
      </p>
      ${m.descrizione ? `<p>${m.descrizione}</p>` : ""}`;

    contenitoreOpere.innerHTML = m.opere.length
      ? `<table>
          <thead><tr><th>Titolo</th><th>Autore</th><th>Anno</th><th>Tecnica</th><th>Inventario</th></tr></thead>
          <tbody>${m.opere.map(o => `
            <tr>
              <td>${o.titolo}</td>
              <td>${o.autore}</td>
              <td>${o.anno ?? "—"}</td>
              <td>${o.tecnica ?? "—"}</td>
              <td>${o.inventario ?? "—"}</td>
            </tr>`).join("")}</tbody>
        </table>`
      : '<p class="vuoto">Nessuna opera associata a questa mostra.</p>';
  } catch (errore) {
    mostraAvviso(avviso, errore.message);
  }
}

async function caricaFeedback() {
  try {
    const feedback = await api.feedbackMostra(id);

    contenitoreFeedback.innerHTML = feedback.length
      ? `<table>
          <thead><tr><th>Voto</th><th>Commento</th><th>Visitatore</th><th>Data</th></tr></thead>
          <tbody>${feedback.map(f => `
            <tr>
              <td>${f.valutazione} su 5</td>
              <td>${f.commento ?? "—"}</td>
              <td>${f.autore}</td>
              <td>${dataBreve(f.dataInserimento)}</td>
            </tr>`).join("")}</tbody>
        </table>`
      : '<p class="vuoto">Ancora nessun giudizio per questa mostra.</p>';
  } catch {
    contenitoreFeedback.innerHTML = "";
  }
}

contenitoreTurni.addEventListener("click", async e => {
  const bottone = e.target.closest("[data-prenota]");
  if (!bottone) return;

  const posti = prompt("Quanti ingressi vuoi prenotare? (massimo 5)", "1");
  if (posti === null) return;

  const numeroPosti = Number(posti);
  if (!Number.isInteger(numeroPosti) || numeroPosti < 1) {
    mostraAvviso(avviso, "Indica un numero intero di ingressi.");
    return;
  }

  bottone.disabled = true;
  nascondiAvviso(avviso);

  try {
    await api.prenota(Number(bottone.dataset.prenota), numeroPosti);
    await caricaCalendario();
    mostraAvviso(avviso, "Prenotazione effettuata. La trovi in \"Le mie visite\".", "ok");
  } catch (errore) {
    mostraAvviso(avviso, errore.message);
    bottone.disabled = false;
  }
});

caricaMostra().then(caricaCalendario).then(caricaFeedback);
