// ================================
// 📌 Configuração da API
// ================================

// URL base da API definida no .env
export const API_BASE = import.meta.env.VITE_API_BASE
    ? `${import.meta.env.VITE_API_BASE}/api/printer`
    : `${location.origin}/api/printer`;

// ================================
// 📌 GET — Lista todas as impressoras
// ================================
export async function fetchPrinters() {
    const res = await fetch(API_BASE);
    if (!res.ok) throw new Error(`Erro ao buscar impressoras (status ${res.status})`);
    return await res.json();
}

// ================================
// 📌 POST — Adiciona impressora
// ================================
export async function addPrinter(payload) {
    const res = await fetch(API_BASE, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload),
    });
    if (!res.ok) {
        const err = await res.json().catch(() => ({}));
        throw new Error(err.error || "Erro ao adicionar impressora");
    }
    return await res.json();
}

// ================================
// 📌 DELETE — Remove impressora
// ================================
export async function removePrinter(id) {
    const res = await fetch(`${API_BASE}/${id}`, { method: "DELETE" });
    if (!res.ok) throw new Error("Erro ao remover impressora");
    return true;
}

// ================================
// 📌 PATCH — Atualiza impressora
// ================================
export async function patchPrinter(id, payload) {
    const res = await fetch(`${API_BASE}/${id}`, {
        method: "PATCH",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload),
    });
    if (!res.ok) {
        const err = await res.json().catch(() => ({}));
        throw new Error(err.error || "Erro ao atualizar impressora");
    }
    return await res.json();
}

// ================================
// 📌 POST — Upload de imagem
// ================================
export async function uploadImage(file) {
    const form = new FormData();
    form.append("file", file);

    const res = await fetch(`${API_BASE}/upload`, { method: "POST", body: form });
    if (!res.ok) {
        const err = await res.json().catch(() => ({}));
        throw new Error(err.error || "Erro ao enviar imagem");
    }
    return await res.json();
}
