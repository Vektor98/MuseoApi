richiediAccesso(["Amministratore"]);

const avviso = document.getElementById("avviso");
const indicatori = document.getElementById("indicatori");
const perMese = document.getElementById("perMese");
const tabellaUtenti = document.getElementById("utenti");

let ruoliDisponibili = [];

function riquadro(valore, nome) {
  return `<div class="indicatore"><div class="valore">${valore}</div><div class="nome">${nome}</div></div>`;
}

async function caricaDashboard() {
  try {
    const d = await api.dashboard();

    indicatori.innerHTML = [
      riquadro(`${d.tassoRiempimentoMedio}%`, "Riempimento medio dei turni svolti"),
      riquadro(`${d.tassoAnnullamento}%`, "Prenotazioni annullate"),
      riquadro(d.valutazioneMedia.toFixed(2), "Valutazione media"),
      riquadro(d.ingressiTotali, "Ingressi prenotati")
    ].join("");

    // Barre proporzionali disegnate in CSS: evitano una libreria di grafici
    // per un indicatore cosi semplice.
    const massimo = Math.max(...d.ingressiPerMese.map(m => m.totale), 1);

    perMese.innerHTML = d.ingressiPerMese.length
      ? `<table>
          <thead><tr><th>Mese</th><th>Ingressi</th><th style="width:50%"></th></tr></thead>
          <tbody>${d.ingressiPerMese.map(m => `
            <tr>
              <td>${MESI[m.mese - 1]} ${m.anno}</td>
              <td>${m.totale}</td>
              <td>
                <span style="display:block;height:10px;background:var(--sala);width:${(m.totale / massimo) * 100}%"></span>
              </td>
            </tr>`).join("")}</tbody>
        </table>`
      : '<p class="vuoto">Nessun ingresso registrato.</p>';
  } catch (errore) {
    mostraAvviso(avviso, errore.message);
  }
}

async function caricaUtenti() {
  try {
    const [utenti, ruoli] = await Promise.all([api.utenti(), api.ruoli()]);
    ruoliDisponibili = ruoli;

    tabellaUtenti.innerHTML = `
      <table>
        <thead><tr><th>Cognome</th><th>Nome</th><th>Email</th><th>Ruolo</th><th>Stato</th><th></th></tr></thead>
        <tbody>${utenti.map(u => `
          <tr>
            <td>${u.cognome}</td>
            <td>${u.nome}</td>
            <td>${u.email}</td>
            <td>
              <select data-ruolo="${u.id}">
                ${ruoli.map(r => `<option value="${r.id}" ${r.nome === u.ruolo ? "selected" : ""}>${r.nome}</option>`).join("")}
              </select>
            </td>
            <td>${u.attivo ? '<span class="etichetta verde">Attivo</span>' : '<span class="etichetta grigia">Disattivato</span>'}</td>
            <td><button class="secondario" data-utente="${u.id}" data-attivo="${u.attivo}">${u.attivo ? "Disattiva" : "Riattiva"}</button></td>
          </tr>`).join("")}</tbody>
      </table>`;
  } catch (errore) {
    mostraAvviso(avviso, errore.message);
  }
}

tabellaUtenti.addEventListener("click", async e => {
  const bottone = e.target.closest("[data-utente]");
  if (!bottone) return;

  nascondiAvviso(avviso);

  try {
    await api.cambiaStatoUtente(Number(bottone.dataset.utente), bottone.dataset.attivo !== "true");
    await caricaUtenti();
  } catch (errore) {
    mostraAvviso(avviso, errore.message);
  }
});

tabellaUtenti.addEventListener("change", async e => {
  const select = e.target.closest("[data-ruolo]");
  if (!select) return;

  nascondiAvviso(avviso);

  try {
    await api.cambiaRuoloUtente(Number(select.dataset.ruolo), Number(select.value));
    mostraAvviso(avviso, "Ruolo aggiornato. L'utente dovrà rifare l'accesso per ottenere i nuovi permessi.", "ok");
  } catch (errore) {
    mostraAvviso(avviso, errore.message);
    await caricaUtenti();
  }
});

caricaDashboard();
caricaUtenti();
