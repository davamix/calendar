"use strict";

// --- API layer -----------------------------------------------------------
// Maps a UI "kind" to its REST resource. Projects and tasks share the shape.
const RESOURCE = { project: "projects", task: "tasks" };

async function apiList(kind) {
  const res = await fetch(`/api/${RESOURCE[kind]}/`);
  if (!res.ok) throw new Error(`Failed to load ${kind}s`);
  return res.json();
}
async function apiSearch(kind, name) {
  const res = await fetch(`/api/${RESOURCE[kind]}/?name=${encodeURIComponent(name)}`);
  if (!res.ok) throw new Error(`Search failed`);
  return res.json();
}
async function apiGetById(kind, id) {
  const res = await fetch(`/api/${RESOURCE[kind]}/${id}`);
  if (res.status === 404) return null;
  if (!res.ok) throw new Error("Lookup failed");
  return res.json();
}
async function apiCreate(kind, body) {
  return send(`/api/${RESOURCE[kind]}/`, "POST", body);
}
async function apiUpdate(kind, id, body) {
  return send(`/api/${RESOURCE[kind]}/${id}`, "PUT", body);
}
async function apiDelete(kind, id) {
  const res = await fetch(`/api/${RESOURCE[kind]}/${id}`, { method: "DELETE" });
  if (!res.ok) throw new Error("Delete failed");
}
async function send(url, method, body) {
  const res = await fetch(url, {
    method,
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  if (res.status === 400) {
    const problem = await res.json().catch(() => null);
    const msg = problem?.errors
      ? Object.values(problem.errors).flat().join(" ")
      : "Validation failed";
    throw new Error(msg);
  }
  if (!res.ok) throw new Error("Request failed");
  return res.json();
}

// --- State ---------------------------------------------------------------
const state = {
  view: startOfMonth(new Date()), // first day of the month being shown
  items: [],                      // all projects + tasks, each tagged with .kind
};

const MONTHS = ["January", "February", "March", "April", "May", "June",
  "July", "August", "September", "October", "November", "December"];
const WEEKDAYS = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];

// --- Date helpers --------------------------------------------------------
function startOfMonth(d) { return new Date(d.getFullYear(), d.getMonth(), 1); }
function isoDate(d) {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, "0");
  const day = String(d.getDate()).padStart(2, "0");
  return `${y}-${m}-${day}`;
}
function todayIso() { return isoDate(new Date()); }
function prettyRange(start, end) {
  return start === end ? start : `${start} → ${end}`;
}

// --- Data loading + rendering -------------------------------------------
async function loadItems() {
  const [projects, tasks] = await Promise.all([apiList("project"), apiList("task")]);
  // Server already tags each item with a "kind" field ("Project"/"Task").
  state.items = [
    ...projects.map((p) => ({ ...p, kind: "project" })),
    ...tasks.map((t) => ({ ...t, kind: "task" })),
  ];
}

function render() {
  const view = state.view;
  document.getElementById("monthLabel").textContent =
    `${MONTHS[view.getMonth()]} ${view.getFullYear()}`;

  const daysInMonth = new Date(view.getFullYear(), view.getMonth() + 1, 0).getDate();
  const today = todayIso();
  const list = document.getElementById("dayList");
  list.innerHTML = "";

  for (let day = 1; day <= daysInMonth; day++) {
    const date = new Date(view.getFullYear(), view.getMonth(), day);
    const iso = isoDate(date);
    const weekday = date.getDay();

    const dayItems = state.items.filter((i) => iso >= i.startDate && iso <= i.endDate);

    const isToday = iso === today;

    const row = document.createElement("div");
    row.className = "day-row" + (isToday ? " today" : "") + (dayItems.length === 0 ? " empty" : "");

    const cell = document.createElement("div");
    cell.className = "day-cell";
    cell.innerHTML = `<span class="wd">${WEEKDAYS[weekday]}</span><span class="num">${day}</span>`;
    row.appendChild(cell);

    if (dayItems.length === 0) {
      const empty = document.createElement("div");
      empty.className = "day-empty";
      empty.textContent = "No events scheduled";
      row.appendChild(empty);
    } else {
      const items = document.createElement("div");
      items.className = "day-items";
      for (const item of dayItems) {
        // Today shows full colored bars; every other day shows a compact shape.
        const el = isToday ? buildDayBar(item) : buildShape(item);
        el.addEventListener("click", () => openEdit(item));
        items.appendChild(el);
      }
      row.appendChild(items);
    }

    if (isToday) {
      const badge = document.createElement("span");
      badge.className = "today-badge";
      badge.textContent = "Today";
      row.appendChild(badge);
    }

    list.appendChild(row);
  }

  // Legend aside: every element that overlaps the visible month.
  const firstIso = isoDate(new Date(view.getFullYear(), view.getMonth(), 1));
  const lastIso = isoDate(new Date(view.getFullYear(), view.getMonth(), daysInMonth));
  const monthItems = state.items
    .filter((i) => i.startDate <= lastIso && i.endDate >= firstIso)
    .sort((a, b) => a.startDate.localeCompare(b.startDate) || a.name.localeCompare(b.name));
  renderLegend(monthItems);
}

async function refresh() {
  try {
    await loadItems();
    render();
  } catch (err) {
    toast(err.message, true);
  }
}

// Full-width colored bar for items on the current day: P/T badge + name only.
function buildDayBar(item) {
  const color = elementColor(item);
  const bar = document.createElement("button");
  bar.className = `daybar ${item.kind}`;
  bar.type = "button";
  bar.style.backgroundColor = color;
  bar.style.color = contrastText(color);
  const badge = document.createElement("span");
  badge.className = "daybar-badge";
  badge.textContent = item.kind === "project" ? "P" : "T";
  bar.append(badge, document.createTextNode(item.name));
  return bar;
}

// Compact shape: square (P) for projects, circle (T) for tasks, filled with the
// element's colour. Returns a <button> by default; pass "span" for static use.
function buildShape(item, tag = "button") {
  const color = elementColor(item);
  const shape = document.createElement(tag);
  shape.className = `shape ${item.kind}`;
  shape.style.backgroundColor = color;
  shape.style.color = contrastText(color);
  shape.textContent = item.kind === "project" ? "P" : "T";
  shape.title = `${item.name} · ${prettyRange(item.startDate, item.endDate)}`;
  return shape;
}

// Legend aside: projects and tasks in the visible month, grouped by kind. Each
// row is the element's shape + name and opens it for editing.
function renderLegend(items) {
  const box = document.getElementById("legendList");
  box.innerHTML = "";
  if (items.length === 0) {
    box.innerHTML = `<p class="legend-empty">No projects or tasks this month.</p>`;
    return;
  }
  box.append(
    legendGroup("Active Projects", items.filter((i) => i.kind === "project")),
    legendGroup("Current Tasks", items.filter((i) => i.kind === "task")),
  );
}

function legendGroup(label, items) {
  const group = document.createElement("div");
  group.className = "legend-group";

  const heading = document.createElement("p");
  heading.className = "legend-group-label";
  heading.textContent = label;
  group.appendChild(heading);

  const rows = document.createElement("div");
  rows.className = "legend-rows";
  if (items.length === 0) {
    const none = document.createElement("p");
    none.className = "legend-empty";
    none.textContent = "None this month.";
    rows.appendChild(none);
  } else {
    for (const item of items) {
      const row = document.createElement("button");
      row.className = "legend-row";
      row.type = "button";
      const name = document.createElement("span");
      name.className = "legend-name";
      name.textContent = item.name;
      row.append(buildShape(item, "span"), name);
      row.addEventListener("click", () => openEdit(item));
      rows.appendChild(row);
    }
  }
  group.appendChild(rows);
  return group;
}

// Resolve a usable colour, falling back to the per-kind default.
function elementColor(item) {
  return item.color && HEX_RE.test(item.color) ? item.color : DEFAULT_COLOR[item.kind];
}

// Pick black or white text for legibility on the given hex background.
function contrastText(hex) {
  let h = hex.replace("#", "");
  if (h.length === 3) h = h.split("").map((c) => c + c).join("");
  const r = parseInt(h.slice(0, 2), 16);
  const g = parseInt(h.slice(2, 4), 16);
  const b = parseInt(h.slice(4, 6), 16);
  // Relative luminance (sRGB) — threshold ~0.55 reads well for these chips.
  const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
  return luminance > 0.55 ? "#111827" : "#ffffff";
}

// --- Modal (create / edit) ----------------------------------------------
const modal = {
  backdrop: document.getElementById("modalBackdrop"),
  title: document.getElementById("modalTitle"),
  id: document.getElementById("fieldId"),
  kind: document.getElementById("fieldKind"),
  name: document.getElementById("fieldName"),
  description: document.getElementById("fieldDescription"),
  start: document.getElementById("fieldStart"),
  end: document.getElementById("fieldEnd"),
  color: document.getElementById("fieldColor"),
  error: document.getElementById("formError"),
  deleteBtn: document.getElementById("deleteBtn"),
};

// Default element colour per kind (from the curated palette) when none is set.
const DEFAULT_COLOR = { project: "#002366", task: "#004d40" };
const HEX_RE = /^#([0-9a-f]{3}|[0-9a-f]{6})$/i;

function openCreate(kind) {
  const firstOfView = isoDate(state.view);
  modal.title.textContent = kind === "project" ? "New project" : "New task";
  modal.id.value = "";
  modal.kind.value = kind;
  modal.name.value = "";
  modal.description.value = "";
  modal.start.value = firstOfView;
  modal.end.value = firstOfView;
  modal.color.value = DEFAULT_COLOR[kind];
  modal.error.classList.add("hidden");
  modal.deleteBtn.classList.add("hidden");
  showModal();
}

function openEdit(item) {
  modal.title.textContent = `Edit ${item.kind}`;
  modal.id.value = item.id;
  modal.kind.value = item.kind;
  modal.name.value = item.name;
  modal.description.value = item.description ?? "";
  modal.start.value = item.startDate;
  modal.end.value = item.endDate;
  modal.color.value = item.color ?? DEFAULT_COLOR[item.kind];
  modal.error.classList.add("hidden");
  modal.deleteBtn.classList.remove("hidden");
  showModal();
}

function showModal() {
  modal.backdrop.classList.remove("hidden");
  modal.name.focus();
}
function closeModal() { modal.backdrop.classList.add("hidden"); }

async function saveElement(evt) {
  evt.preventDefault();
  const kind = modal.kind.value;
  const id = modal.id.value;
  const color = modal.color.value.trim();
  const body = {
    name: modal.name.value.trim(),
    description: modal.description.value.trim() || null,
    startDate: modal.start.value,
    endDate: modal.end.value,
    color: color || null,
  };

  if (!body.name) return showFormError("Name is required.");
  if (body.endDate < body.startDate) return showFormError("End date must be on or after the start date.");
  if (color && !HEX_RE.test(color)) return showFormError("Color must be a hex code such as #4f46e5.");

  try {
    if (id) {
      await apiUpdate(kind, id, body);
      toast("Saved changes");
    } else {
      await apiCreate(kind, body);
      toast(`${kind === "project" ? "Project" : "Task"} created`);
    }
    closeModal();
    await refresh();
  } catch (err) {
    showFormError(err.message);
  }
}

async function deleteElement() {
  const id = modal.id.value;
  const kind = modal.kind.value;
  if (!id) return;
  if (!confirm("Delete this element? This cannot be undone.")) return;
  try {
    await apiDelete(kind, id);
    toast("Deleted");
    closeModal();
    await refresh();
  } catch (err) {
    showFormError(err.message);
  }
}

function showFormError(msg) {
  modal.error.textContent = msg;
  modal.error.classList.remove("hidden");
}

// --- Search --------------------------------------------------------------
const GUID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

async function runSearch(evt) {
  evt.preventDefault();
  const query = document.getElementById("searchInput").value.trim();
  if (!query) return;

  let results = [];
  try {
    if (GUID_RE.test(query)) {
      // Search by ID across both kinds.
      const [p, t] = await Promise.all([apiGetById("project", query), apiGetById("task", query)]);
      if (p) results.push({ ...p, kind: "project" });
      if (t) results.push({ ...t, kind: "task" });
    } else {
      // Search by name across both kinds.
      const [p, t] = await Promise.all([apiSearch("project", query), apiSearch("task", query)]);
      results = [
        ...p.map((x) => ({ ...x, kind: "project" })),
        ...t.map((x) => ({ ...x, kind: "task" })),
      ];
    }
  } catch (err) {
    return toast(err.message, true);
  }
  renderSearchResults(query, results);
}

function renderSearchResults(query, results) {
  const box = document.getElementById("searchResults");
  box.innerHTML = "";
  if (results.length === 0) {
    box.innerHTML = `<p class="result-empty">No matches for “${escapeHtml(query)}”.</p>`;
  } else {
    for (const item of results) {
      const color = elementColor(item);
      const row = document.createElement("div");
      row.className = "result";
      row.style.backgroundColor = color;
      row.style.color = contrastText(color);
      row.innerHTML =
        `<span class="kind-tag ${item.kind}">${item.kind}</span>` +
        `<span class="r-name">${escapeHtml(item.name)}</span>` +
        `<span class="r-dates">${prettyRange(item.startDate, item.endDate)}</span>`;
      row.addEventListener("click", () => {
        document.getElementById("searchBackdrop").classList.add("hidden");
        // Jump the calendar to the item's start month, then open it for editing.
        state.view = startOfMonth(new Date(item.startDate + "T00:00:00"));
        render();
        openEdit(item);
      });
      box.appendChild(row);
    }
  }
  document.getElementById("searchBackdrop").classList.remove("hidden");
}

// --- Utilities -----------------------------------------------------------
let toastTimer;
function toast(message, isError = false) {
  const el = document.getElementById("toast");
  el.textContent = message;
  el.className = `toast${isError ? " error" : ""}`;
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => el.classList.add("hidden"), 2600);
}
function escapeHtml(s) {
  return String(s).replace(/[&<>"']/g, (c) =>
    ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
}

// --- Wiring --------------------------------------------------------------
function init() {
  document.getElementById("prevMonth").addEventListener("click", () => {
    state.view = new Date(state.view.getFullYear(), state.view.getMonth() - 1, 1);
    render();
  });
  document.getElementById("nextMonth").addEventListener("click", () => {
    state.view = new Date(state.view.getFullYear(), state.view.getMonth() + 1, 1);
    render();
  });
  document.getElementById("todayBtn").addEventListener("click", () => {
    state.view = startOfMonth(new Date());
    render();
  });

  document.getElementById("newProject").addEventListener("click", () => openCreate("project"));
  document.getElementById("newTask").addEventListener("click", () => openCreate("task"));
  document.getElementById("fab").addEventListener("click", () => openCreate("project"));

  document.getElementById("elementForm").addEventListener("submit", saveElement);
  document.getElementById("deleteBtn").addEventListener("click", deleteElement);
  document.getElementById("cancelBtn").addEventListener("click", closeModal);
  document.getElementById("modalClose").addEventListener("click", closeModal);
  modal.backdrop.addEventListener("click", (e) => { if (e.target === modal.backdrop) closeModal(); });

  document.getElementById("searchForm").addEventListener("submit", runSearch);
  document.getElementById("searchClose").addEventListener("click", () =>
    document.getElementById("searchBackdrop").classList.add("hidden"));
  document.getElementById("searchBackdrop").addEventListener("click", (e) => {
    if (e.target.id === "searchBackdrop") e.currentTarget.classList.add("hidden");
  });

  document.addEventListener("keydown", (e) => {
    if (e.key === "Escape") {
      closeModal();
      document.getElementById("searchBackdrop").classList.add("hidden");
    }
  });

  refresh();
}

document.addEventListener("DOMContentLoaded", init);
