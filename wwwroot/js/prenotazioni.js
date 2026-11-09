richiediAccesso();

const elenco = document.getElementById("elenco");
const avviso = document.getElementById("avviso");

function riga(p) {
  const passata = new Date(p.dataOra) < new Date();
  const attiva = p.stato === 1;

  let azioni = "—";
  if (attiva && !passata) azioni = `<button class="secondario" data-annulla="${p.id}">Annulla</button>`;
  else if (attiva && passata && !p.feedbackInserito) azioni = `<button class="secondario" data-feedback="${p.id}">Lascia un giudizio</button>`;
  else if (attiva && passata && p.feedbackInserito) azioni = '<span class="etichetta grigia">Giudizio inviato</span>';

  return `
    <tr>
      <td>${p.mostraTitolo}</td>
      <td>${dataOraEstesa(p.dataOra)}</td>
      <td>${p.numeroPosti}</td>
      <td>${dataBreve(p.dataPrenotazione)}</td>
      <td>${attiva ? '<span class="etichetta verde">Confermata</span>' : '<span class="etichetta grigia">Annullata</span>'}</td>
      <td>${azioni}</td>
    </tr>`;
}

async function carica(mantieniAvviso = false) {
  if (!mantieniAvviso) nascondiAvviso(avviso);
  elenco.innerHTML = '<p class="vuoto">Caricamento…</p>';

  try {
    const prenotazioni = await api.miePrenotazioni();

    elenco.innerHTML = prenotazioni.length
      ? `<table>
          <thead>
            <tr><th>Mostra</th><th>Turno</th><th>Ingressi</th><th>Prenotato il</th><th>Stato</th><th></th></tr>
          </thead>
          <tbody>${prenotazioni.map(riga).join("")}</tbody>
        </table>`
      : '<p class="vuoto">Non hai ancora prenotazioni.</p>';
  } catch (errore) {
    elenco.innerHTML = "";
    mostraAvviso(avviso, errore.message);
  }
}

elenco.addEventListener("click", async e => {
  const annulla = e.target.closest("[data-annulla]");
  const feedback = e.target.closest("[data-feedback]");

  if (annulla) {
    if (!confirm("Vuoi annullare questa prenotazione?")) return;

    try {
      await api.annullaPrenotazione(Number(annulla.dataset.annulla));
      await carica(true);
      mostraAvviso(avviso, "Prenotazione annullata.", "ok");
    } catch (errore) {
      mostraAvviso(avviso, errore.message);
    }
    return;
  }

  if (feedback) {
    const voto = prompt("Che voto dai alla visita, da 1 a 5?");
    if (voto === null) return;

    const valutazione = Number(voto);
    if (!Number.isInteger(valutazione) || valutazione < 1 || valutazione > 5) {
      mostraAvviso(avviso, "Il voto deve essere un numero intero tra 1 e 5.");
      return;
    }

    const commento = prompt("Vuoi aggiungere un commento? (facoltativo)") || null;

    try {
      await api.inviaFeedback({ prenotazioneId: Number(feedback.dataset.feedback), valutazione, commento });
      await carica(true);
      mostraAvviso(avviso, "Grazie, il tuo giudizio è stato registrato.", "ok");
    } catch (errore) {
      mostraAvviso(avviso, errore.message);
    }
  }
});

carica();
