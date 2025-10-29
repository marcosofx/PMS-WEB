// Ajuste aqui para o IP/porta da sua API
export const API_BASE = 'http://131.10.3.50:5000/api/printer';

// FETCH: lista todas as impressoras
export async function fetchPrinters() {
    const res = await fetch(API_BASE);
    if (!res.ok) throw new Error(`API error ${res.status}`);
    return res.json();
}

// POST: adiciona nova impressora
export async function addPrinter(payload) {
    const res = await fetch(API_BASE, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload),
    });
    if (!res.ok) throw new Error("Erro ao adicionar impressora");
    return res.json();
}

// DELETE: remove impressora
export async function removePrinter(id) {
    const res = await fetch(`${API_BASE}/${id}`, { method: "DELETE" });
    if (!res.ok) throw new Error("Erro ao remover impressora");
    return res;
}

// PATCH: atualiza info da impressora
export async function patchPrinter(id, payload) {
    const res = await fetch(`${API_BASE}/${id}`, {
        method: "PATCH",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload),
    });
    if (!res.ok) throw new Error("Erro ao atualizar impressora");
    return res.json();
}

// POST: upload de imagem
export async function uploadImage(file) {
    const form = new FormData();
    form.append("file", file);

    const res = await fetch(`${API_BASE}/upload`, {
        method: "POST",
        body: form,
    });

    if (!res.ok) {
        const err = await res.json().catch(() => ({}));
        throw new Error(err.error || "Erro ao enviar imagem");
    }

    return res.json(); // retorna { url: "http://IP:PORT/uploads/arquivo.jpg" }
}
