const risultati = document.getElementById("risultati");
const avviso = document.getElementById("avviso");
const areaOperatore = document.getElementById("areaOperatore");
const campoRicerca = document.getElementById("q");

const sessione = leggiSessione();
const operatore = sessione && (sessione.ruolo === "Operatore" || sessione.ruolo === "Amministratore");

let mostreDisponibili = [];

function collocazione(o) {
  if (!o.mostraId) return '<span class="etichetta grigia">Deposito</span>';
  const m = mostreDisponibili.find(x => x.id === o.mostraId);
  return `<span class="etichetta">${m ? m.titolo : "In mostra"}</span>`;
}

function azioni(o) {
  if (!operatore) return "";

  // Un'opera esposta si stacca dalla mostra; solo in deposito puo essere eliminata.
  return o.mostraId
    ? `<button class="secondario" data-deposito="${o.id}">Riporta in deposito</button>`
    : `<button class="secondario" data-elimina="${o.id}">Elimina</button>`;
}

function riga(o) {
  return `
    <tr>
      <td>${o.titolo}</td>
      <td>${o.autore}</td>
      <td>${o.anno ?? "—"}</td>
      <td>${o.tecnica ?? "—"}</td>
      <td>${o.inventario ?? "—"}</td>
      <td>${collocazione(o)}</td>
      ${operatore ? `<td>${azioni(o)}</td>` : ""}
    </tr>`;
}

async function cerca(mantieniAvviso = false) {
  if (!mantieniAvviso) nascondiAvviso(avviso);
  risultati.innerHTML = '<p class="vuoto">Caricamento…</p>';

  try {
    const opere = await api.opere(campoRicerca.value.trim());

    risultati.innerHTML = opere.length
      ? `<table>
          <thead>
            <tr>
              <th>Titolo</th><th>Autore</th><th>Anno</th><th>Tecnica</th><th>Inventario</th><th>Collocazione</th>
              ${operatore ? "<th></th>" : ""}
            </tr>
          </thead>
          <tbody>${opere.map(riga).join("")}</tbody>
        </table>`
      : '<p class="vuoto">Nessuna opera corrisponde alla ricerca.</p>';
  } catch (errore) {
    risultati.innerHTML = "";
    mostraAvviso(avviso, errore.message);
  }
}

async function preparaAreaOperatore() {
  if (!operatore) return;

  areaOperatore.hidden = false;

  mostreDisponibili = (await api.mostre()).filter(m => m.stato !== 4);

  document.getElementById("mostra").innerHTML =
    '<option value="">Nessuna, va in deposito</option>' +
    mostreDisponibili.map(m => `<option value="${m.id}">${m.titolo}</option>`).join("");
}

risultati.addEventListener("click", async e => {
  const elimina = e.target.closest("[data-elimina]");
  const deposito = e.target.closest("[data-deposito]");

  if (elimina) {
    if (!confirm("Eliminare definitivamente questa opera dalla collezione?")) return;

    try {
      await api.eliminaOpera(Number(elimina.dataset.elimina));
      await cerca(true);
      mostraAvviso(avviso, "Opera eliminata dalla collezione.", "ok");
    } catch (errore) {
      mostraAvviso(avviso, errore.message);
    }
    return;
  }

  if (deposito) {
    try {
      await api.spostaOpera(Number(deposito.dataset.deposito), null);
      await cerca(true);
      mostraAvviso(avviso, "Opera riportata in deposito. Ora può essere eliminata.", "ok");
    } catch (errore) {
      mostraAvviso(avviso, errore.message);
    }
  }
});

const modulo = document.getElementById("moduloOpera");

if (modulo) {
  modulo.addEventListener("submit", async e => {
    e.preventDefault();

    const mostraId = document.getElementById("mostra").value;

    const dati = {
      titolo: document.getElementById("titolo").value.trim(),
      autore: document.getElementById("autore").value.trim(),
      anno: document.getElementById("anno").value ? Number(document.getElementById("anno").value) : null,
      tecnica: document.getElementById("tecnica").value.trim() || null,
      inventario: document.getElementById("inventario").value.trim() || null,
      mostraId: mostraId ? Number(mostraId) : null
    };

    try {
      await api.creaOpera(dati);
      e.target.reset();
      await cerca(true);
      mostraAvviso(avviso, "Opera aggiunta alla collezione.", "ok");
    } catch (errore) {
      mostraAvviso(avviso, errore.message);
    }
  });
}

document.getElementById("cerca").addEventListener("click", () => cerca());
campoRicerca.addEventListener("keydown", e => { if (e.key === "Enter") cerca(); });

preparaAreaOperatore().then(() => cerca());
