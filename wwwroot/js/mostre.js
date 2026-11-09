const elenco = document.getElementById("elenco");
const avviso = document.getElementById("avviso");

function disponibilita(m) {
  if (m.stato === 4) return '<span class="disponibilita chiusa">Mostra annullata</span>';
  if (m.stato === 3) return '<span class="disponibilita chiusa">Mostra conclusa</span>';
  if (m.turniDisponibili === 0) return '<span class="disponibilita esaurita">Nessun turno disponibile</span>';
  if (m.turniDisponibili === 1) return '<span class="disponibilita">Resta 1 turno con posti liberi</span>';
  return `<span class="disponibilita">${m.turniDisponibili} turni con posti liberi</span>`;
}

function riga(m) {
  const prenotabile = (m.stato === 1 || m.stato === 2) && m.turniDisponibili > 0;
  const classe = m.stato === 3 ? "conclusa" : m.stato === 4 ? "annullata" : "";

  return `
    <article class="riga-mostra ${classe}">
      <div>
        <h3><a href="/mostra.html?id=${m.id}">${m.titolo}</a></h3>
        <div class="dettagli">
          <span>${periodo(m.dataInizio, m.dataFine)}</span>
          <span>${m.sala}</span>
          ${m.curatore ? `<span>a cura di ${m.curatore}</span>` : ""}
        </div>
      </div>
      <div class="azione-riga">
        ${disponibilita(m)}
        <a class="bottone${prenotabile ? "" : " secondario"}" href="/mostra.html?id=${m.id}">
          ${prenotabile ? "Scegli un turno" : "Vedi la mostra"}
        </a>
      </div>
    </article>`;
}

async function carica() {
  nascondiAvviso(avviso);
  elenco.innerHTML = '<p class="vuoto">Caricamento…</p>';

  const stato = document.getElementById("filtroStato").value;

  try {
    const mostre = await api.mostre(stato ? `?stato=${stato}` : "");

    elenco.innerHTML = mostre.length
      ? mostre.map(riga).join("")
      : '<p class="vuoto">Nessuna mostra corrisponde al filtro scelto.</p>';
  } catch (errore) {
    elenco.innerHTML = "";
    mostraAvviso(avviso, errore.message);
  }
}

document.getElementById("applica").addEventListener("click", carica);

document.getElementById("azzera").addEventListener("click", () => {
  document.getElementById("filtroStato").value = "";
  carica();
});

carica();
