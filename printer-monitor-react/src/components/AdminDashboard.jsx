import React, { useEffect, useState } from "react";
import { fetchPrinters, removePrinter, patchPrinter } from "../api/printerApi";
import AddPrinterForm from "./AddPrinterForm";

export default function AdminDashboard() {
    const [printers, setPrinters] = useState([]);
    const [loading, setLoading] = useState(false);

    const load = async () => {
        setLoading(true);
        try {
            const data = await fetchPrinters();
            setPrinters(data);
        } catch (err) {
            console.error(err);
            alert("Erro ao carregar impressoras: " + err.message);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => { load(); }, []);

    const handleRemove = async (id) => {
        if (!confirm("Remover essa impressora?")) return;
        try {
            await removePrinter(id);
            setPrinters(prev => prev.filter(p => p.id !== id));
        } catch (err) {
            console.error(err);
            alert("Erro ao remover");
        }
    };

    const handleRename = async (id, value) => {
        try {
            const updated = await patchPrinter(id, { nomeCustomizado: value });
            setPrinters(prev => prev.map(p => p.id === id ? updated : p));
        } catch (err) {
            console.error(err);
            alert("Erro ao atualizar");
        }
    };

    const renderPreviewImage = (printer) => {
        if (printer.foto && printer.foto.trim() !== "") {
            return (
                <img
                    src={printer.foto}
                    alt={printer.nomeCustomizado || printer.nome}
                    className="preview-img"
                />
            );
        } else {
            return (
                <div className="preview-icon">
                    <svg
                        xmlns="http://www.w3.org/2000/svg"
                        viewBox="0 0 24 24"
                        fill="currentColor"
                        width="40"
                        height="40"
                    >
                        <path d="M6 9V2h12v7h3v13H3V9h3zm12 0V4H6v5h12zm-6 9h-4v-5h4v5zm6-5h-4v5h4v-5z" />
                    </svg>
                </div>
            );
        }
    };

    return (
        <div className="admin-area">
            <div className="admin-left">
                <h2>Gerenciar impressoras</h2>
                <AddPrinterForm onAdded={load} />
                {loading && <div className="small">Carregando...</div>}
                <ul className="admin-list">
                    {printers.map(p => (
                        <li key={p.id}>
                            <div className="admin-item">
                                {renderPreviewImage(p)}
                                <div className="admin-item-info">
                                    <input
                                        defaultValue={p.nomeCustomizado || p.nome}
                                        onBlur={(e) => handleRename(p.id, e.target.value)}
                                    />
                                    <div className="admin-actions">
                                        <button onClick={() => handleRemove(p.id)} className="danger">Remover</button>
                                    </div>
                                </div>
                            </div>
                        </li>
                    ))}
                </ul>
            </div>

            <div className="admin-right">
                <h3>Preview</h3>
                <div className="preview-grid">
                    {printers.map(p => (
                        <div key={p.id} className="preview-card">
                            {renderPreviewImage(p)}
                            <div className="preview-name">{p.nomeCustomizado || p.nome}</div>
                        </div>
                    ))}
                </div>
            </div>
        </div>
    );
}
