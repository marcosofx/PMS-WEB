import { useState, useEffect } from "react";
import { addPrinter, uploadImage } from "../api/printerApi";

export default function AddPrinterForm({ onAdded }) {
    const [nome, setNome] = useState("");
    const [ip, setIp] = useState("");
    const [descricao, setDescricao] = useState("");
    const [file, setFile] = useState(null);
    const [loading, setLoading] = useState(false);
    const [previewUrl, setPreviewUrl] = useState("");

    useEffect(() => {
        if (!file) {
            setPreviewUrl("");
            return;
        }
        const url = URL.createObjectURL(file);
        setPreviewUrl(url);
        return () => URL.revokeObjectURL(url);
    }, [file]);

    const handleSubmit = async (e) => {
        e.preventDefault();
        setLoading(true);

        try {
            let imagemUrl = "";
            if (file) {
                const res = await uploadImage(file);
                imagemUrl = res.url;
            }

            const newPrinter = {
                NomeCustomizado: nome,
                Ip: ip,
                Descricao: descricao,
                ImagemUrl: imagemUrl
            };

            const created = await addPrinter(newPrinter);

            setNome("");
            setIp("");
            setDescricao("");
            setFile(null);
            setPreviewUrl("");

            if (onAdded) onAdded(created);

        } catch (err) {
            console.error("Erro ao adicionar impressora:", err);
            alert(err.message);
        } finally {
            setLoading(false);
        }
    };

    return (
        <form
            onSubmit={handleSubmit}
            style={{ display: "flex", flexDirection: "column", gap: "12px", maxWidth: "400px" }}
        >
            {/* Card-style container */}
            <div
                style={{
                    display: "flex",
                    gap: "12px",
                    padding: "12px",
                    background: "rgba(255,255,255,0.03)",
                    borderRadius: "12px",
                    boxShadow: "0 6px 18px rgba(2,6,23,0.6)",
                    alignItems: "center"
                }}
            >
                {/* Preview / Icon */}
                <div
                    style={{
                        width: "72px",
                        height: "72px",
                        borderRadius: "8px",
                        border: "1px solid rgba(255,255,255,0.08)",
                        background: "rgba(255,255,255,0.03)",
                        display: "flex",
                        justifyContent: "center",
                        alignItems: "center",
                        flexShrink: 0
                    }}
                >
                    {previewUrl ? (
                        <img src={previewUrl} alt="preview" style={{ width: "72px", height: "72px", borderRadius: "8px", objectFit: "cover" }} />
                    ) : (
                        <span style={{ fontSize: "32px", color: "#5eead4" }}>🖼️</span>
                    )}
                </div>

                {/* Inputs */}
                <div style={{ display: "flex", flexDirection: "column", gap: "8px", flex: 1 }}>
                    <input
                        type="text"
                        placeholder="Nome da impressora"
                        value={nome}
                        onChange={e => setNome(e.target.value)}
                        required
                        style={{
                            padding: "8px",
                            borderRadius: "8px",
                            border: "1px solid rgba(255,255,255,0.04)",
                            background: "rgba(255,255,255,0.03)",
                            color: "#e6eef8"
                        }}
                    />
                    <input
                        type="text"
                        placeholder="IP da impressora"
                        value={ip}
                        onChange={e => setIp(e.target.value)}
                        required
                        style={{
                            padding: "8px",
                            borderRadius: "8px",
                            border: "1px solid rgba(255,255,255,0.04)",
                            background: "rgba(255,255,255,0.03)",
                            color: "#e6eef8"
                        }}
                    />
                    <input
                        type="text"
                        placeholder="Descrição"
                        value={descricao}
                        onChange={e => setDescricao(e.target.value)}
                        style={{
                            padding: "8px",
                            borderRadius: "8px",
                            border: "1px solid rgba(255,255,255,0.04)",
                            background: "rgba(255,255,255,0.03)",
                            color: "#e6eef8"
                        }}
                    />
                    <input
                        type="file"
                        accept="image/*"
                        onChange={e => setFile(e.target.files[0])}
                    />
                </div>
            </div>

            <button
                type="submit"
                disabled={loading}
                style={{
                    padding: "10px 16px",
                    background: "#5eead4",
                    color: "#042",
                    borderRadius: "8px",
                    border: "none",
                    cursor: "pointer"
                }}
            >
                {loading ? "Enviando..." : "Adicionar Impressora"}
            </button>
        </form>
    );
}
