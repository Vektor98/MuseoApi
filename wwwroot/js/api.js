const BASE = "/api";
const CHIAVE_SESSIONE = "museo.sessione";

function leggiSessione() {
  const grezzo = localStorage.getItem(CHIAVE_SESSIONE);
  if (!grezzo) return null;
  try {
    const sessione = JSON.parse(grezzo);
    // Scadenza controllata lato client per evitare chiamate destinate a fallire.
    if (new Date(sessione.scadenza) <= new Date()) {
      localStorage.removeItem(CHIAVE_SESSIONE);
      return null;
    }
    return sessione;
  } catch {
    localStorage.removeItem(CHIAVE_SESSIONE);
    return null;
  }
}

function salvaSessione(sessione) {
  localStorage.setItem(CHIAVE_SESSIONE, JSON.stringify(sessione));
}

function esci() {
  localStorage.removeItem(CHIAVE_SESSIONE);
  location.href = "/index.html";
}

async function chiamata(percorso, opzioni = {}) {
  const sessione = leggiSessione();
  const intestazioni = { ...(opzioni.headers || {}) };

  if (opzioni.body) intestazioni["Content-Type"] = "application/json";
  if (sessione) intestazioni["Authorization"] = `Bearer ${sessione.token}`;

  const risposta = await fetch(`${BASE}${percorso}`, { ...opzioni, headers: intestazioni });

  if (risposta.status === 204) return null;

  if (risposta.status === 401) {
    localStorage.removeItem(CHIAVE_SESSIONE);
    throw new Error("Sessione scaduta: effettua di nuovo l'accesso.");
  }

  let dati = null;
  const testo = await risposta.text();
  if (testo) {
    try { dati = JSON.parse(testo); } catch { dati = null; }
  }

  // I 409 del backend portano un messaggio parlante: va mostrato all'utente cosi com'e.
  if (!risposta.ok) {
    throw new Error(dati?.messaggio || dati?.detail || `Errore ${risposta.status}.`);
  }

  return dati;
}

const api = {
  login: (email, password) => chiamata("/auth/login", { method: "POST", body: JSON.stringify({ email, password }) }),
  registra: (dati) => chiamata("/auth/register", { method: "POST", body: JSON.stringify(dati) }),

  mostre: (parametri = "") => chiamata(`/mostre${parametri}`),
  mostra: (id) => chiamata(`/mostre/${id}`),
  creaMostra: (dati) => chiamata("/mostre", { method: "POST", body: JSON.stringify(dati) }),
  annullaMostra: (id) => chiamata(`/mostre/${id}`, { method: "DELETE" }),
  turniMostra: (id, soloDisponibili = false) => chiamata(`/mostre/${id}/turni${soloDisponibili ? "?soloDisponibili=true" : ""}`),
  feedbackMostra: (id) => chiamata(`/mostre/${id}/feedback`),
  generaTurni: (id, dati) => chiamata(`/mostre/${id}/turni/genera`, { method: "POST", body: JSON.stringify(dati) }),

  turno: (id) => chiamata(`/turni/${id}`),
  prenotatiTurno: (id) => chiamata(`/turni/${id}/prenotazioni`),
  annullaTurno: (id) => chiamata(`/turni/${id}`, { method: "DELETE" }),

  prenota: (turnoVisitaId, numeroPosti) => chiamata("/prenotazioni", { method: "POST", body: JSON.stringify({ turnoVisitaId, numeroPosti }) }),
  miePrenotazioni: () => chiamata("/prenotazioni/mie"),
  annullaPrenotazione: (id) => chiamata(`/prenotazioni/${id}`, { method: "DELETE" }),

  inviaFeedback: (dati) => chiamata("/feedback", { method: "POST", body: JSON.stringify(dati) }),

  opere: (q = "", mostraId = null) => {
    const p = new URLSearchParams();
    if (q) p.set("q", q);
    if (mostraId) p.set("mostraId", mostraId);
    const query = p.toString();
    return chiamata(`/opere${query ? `?${query}` : ""}`);
  },

  creaOpera: (dati) => chiamata("/opere", { method: "POST", body: JSON.stringify(dati) }),
  eliminaOpera: (id) => chiamata(`/opere/${id}`, { method: "DELETE" }),
  spostaOpera: (id, mostraId) => chiamata(`/opere/${id}/mostra${mostraId ? `?mostraId=${mostraId}` : ""}`, { method: "PUT" }),

  sale: () => chiamata("/sale"),
  utenti: () => chiamata("/utenti"),
  ruoli: () => chiamata("/utenti/ruoli"),
  cambiaStatoUtente: (id, attivo) => chiamata(`/utenti/${id}/stato`, { method: "PUT", body: JSON.stringify({ attivo }) }),
  cambiaRuoloUtente: (id, ruoloId) => chiamata(`/utenti/${id}/ruolo?ruoloId=${ruoloId}`, { method: "PUT" }),
  dashboard: () => chiamata("/dashboard")
};

const STATI_MOSTRA = { 1: "Programmata", 2: "In corso", 3: "Conclusa", 4: "Annullata" };
const STATI_TURNO = { 1: "Aperto", 2: "Chiuso", 3: "Annullato" };
const STATI_PRENOTAZIONE = { 1: "Confermata", 2: "Annullata" };
const MESI = ["gennaio", "febbraio", "marzo", "aprile", "maggio", "giugno", "luglio", "agosto", "settembre", "ottobre", "novembre", "dicembre"];
const GIORNI = ["domenica", "lunedì", "martedì", "mercoledì", "giovedì", "venerdì", "sabato"];

function dataEstesa(iso) {
  const d = new Date(iso);
  return `${d.getDate()} ${MESI[d.getMonth()]} ${d.getFullYear()}`;
}

function oraBreve(iso) {
  const d = new Date(iso);
  return `${String(d.getHours()).padStart(2, "0")}:${String(d.getMinutes()).padStart(2, "0")}`;
}

function dataOraEstesa(iso) {
  const d = new Date(iso);
  return `${GIORNI[d.getDay()]} ${d.getDate()} ${MESI[d.getMonth()]}, ore ${oraBreve(iso)}`;
}

function dataBreve(iso) {
  const d = new Date(iso);
  return `${String(d.getDate()).padStart(2, "0")}/${String(d.getMonth() + 1).padStart(2, "0")}/${d.getFullYear()}`;
}

function periodo(inizio, fine) {
  return `${dataEstesa(inizio)} — ${dataEstesa(fine)}`;
}

function mostraAvviso(elemento, testo, tipo = "ko") {
  elemento.textContent = testo;
  elemento.className = `avviso visibile ${tipo}`;
  window.scrollTo({ top: 0, behavior: "smooth" });
}

function nascondiAvviso(elemento) {
  elemento.className = "avviso";
}

function disegnaMenu() {
  const sessione = leggiSessione();
  const menu = document.querySelector("nav.menu");
  if (!menu) return;

  const voci = ['<a href="/index.html">Mostre</a>', '<a href="/collezione.html">Collezione</a>'];

  if (sessione) {
    voci.push('<a href="/prenotazioni.html">Le mie visite</a>');
    if (sessione.ruolo === "Operatore" || sessione.ruolo === "Amministratore")
      voci.push('<a href="/gestione.html">Gestione</a>');
    if (sessione.ruolo === "Amministratore")
      voci.push('<a href="/dashboard.html">Dashboard</a>');
    voci.push(`<a href="#" id="esci">Esci (${sessione.email})</a>`);
  } else {
    voci.push('<a href="/login.html">Accedi</a>');
    voci.push('<a href="/registrati.html">Registrati</a>');
  }

  menu.innerHTML = voci.join("");

  const pagina = location.pathname.split("/").pop() || "index.html";
  menu.querySelectorAll("a").forEach(a => {
    if (a.getAttribute("href") === `/${pagina}`) a.classList.add("attivo");
  });

  const bottoneEsci = document.getElementById("esci");
  if (bottoneEsci) bottoneEsci.addEventListener("click", e => { e.preventDefault(); esci(); });
}

function richiediAccesso(ruoli = null) {
  const sessione = leggiSessione();
  if (!sessione) {
    location.href = "/login.html";
    return null;
  }
  if (ruoli && !ruoli.includes(sessione.ruolo)) {
    location.href = "/index.html";
    return null;
  }
  return sessione;
}

function parametroUrl(nome) {
  return new URLSearchParams(location.search).get(nome);
}

document.addEventListener("DOMContentLoaded", disegnaMenu);
