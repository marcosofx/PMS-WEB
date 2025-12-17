import React, { useEffect, useState } from "react";
import axios from "axios";
import { motion } from "framer-motion";
import { Loader2 } from "lucide-react";
import { API_BASE } from "../api/printerApi";

const getTonerColor = (k) => {
    switch (k?.toLowerCase()) {
        case "black": return "#0F0F0F";
        case "cyan": return "#00B8FF";
        case "magenta": return "#FF2DA6";
        case "yellow": return "#FFD24D";
        default: return "#9AA0A6";
    }
};

export default function PrinterCard({ printer, refreshInterval = 30000 }) {

    const [snmpData, setSnmpData] = useState({
        numeroSerie: printer.numeroSerie ?? "N/A",
        contadorTotal: printer.contadorTotal ?? "N/A",
        alertas: printer.alertas ?? [],
        status: printer.status ?? "N/A",
    });

    const [isAlertOpen, setIsAlertOpen] = useState(false);
    const [isLoading, setIsLoading] = useState(true);
    const [imageError, setImageError] = useState(false);

    const toners = printer.toners || {};
    const toneEntries = printer.eColorida
        ? Object.entries(toners)
        : toners.Black !== undefined
            ? [["Black", toners.Black]]
            : [];

    const fetchSnmp = async () => {
        try {
            setIsLoading(true);
            const url = `${API_BASE}/snmp/${printer.id}`;
            const res = await axios.get(url);

            setSnmpData(prev => ({
                numeroSerie: res.data.numeroSerie ?? prev.numeroSerie,
                contadorTotal: res.data.contadorTotal ?? prev.contadorTotal,
                alertas: res.data.alertas ?? prev.alertas,
                status: res.data.status ?? prev.status,
            }));

        } catch (err) {
            console.log("Erro ao buscar dados SNMP:", err);
        } finally {
            setTimeout(() => setIsLoading(false), 600);
        }
    };

    useEffect(() => {
        fetchSnmp();
        const interval = setInterval(fetchSnmp, refreshInterval);
        return () => clearInterval(interval);
    },  [printer.id, refreshInterval]);

    const renderPrinterImage = () => {
        if (!imageError && printer.imagemUrl && printer.imagemUrl.trim() !== "") {
            return (
                <img
                    className="printer-photo"
                    src={printer.imagemUrl}
                    alt={printer.nomeCustomizado || "Impressora"}
                    onError={() => {
                        console.warn("Erro ao carregar imagem:", printer.imagemUrl);
                        setImageError(true);
                    }}
                />
            );
        }

        return (
            <div className="printer-placeholder">
                <svg
                    width="64"
                    height="64"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2"
                >
                    <rect x="3" y="7" width="18" height="14" rx="2" ry="2" />
                    <path d="M16 3H8v4h8V3z" />
                </svg>
            </div>
        );
    };

    const tonerValues = printer.eColorida
        ? Object.values(printer.toners || {})
        : [printer.toners?.Black ?? 0];

    const empty = tonerValues.some(v => parseInt(v) === 0);
    const low = tonerValues.some(v => parseInt(v) > 0 && parseInt(v) < 11);

    const neonEffect = empty
        ? "rgba(255,0,0,0.8)"
        : low
            ? "rgba(255,215,0,0.8)"
            : null;

    return (
        <motion.article
            className="printer-card"
            animate={
                neonEffect
                    ? {
                        boxShadow: [
                            `0 0 0px ${neonEffect}`,
                            `0 0 10px ${neonEffect}`,
                            `0 0 20px ${neonEffect}`,
                            `0 0 10px ${neonEffect}`,
                            `0 0 0px ${neonEffect}`,
                        ],
                    }
                    : { boxShadow: "none" }
            }
            transition={{
                duration: 2,
                repeat: neonEffect ? Infinity : 0,
                repeatType: "mirror",
            }}
            style={{
                position: "relative",
                borderRadius: "12px",
                background: "#1a1d21",
                border: neonEffect
                    ? `1px solid ${neonEffect}`
                    : "1px solid rgba(255,255,255,0.1)",
                overflow: "hidden",
                padding: "1rem",
            }}
        >
            {isLoading && (
                <motion.div
                    style={{
                        position: "absolute",
                        top: "10px",
                        right: "10px",
                        color: "#00BFFF",
                    }}
                    animate={{ rotate: 360 }}
                    transition={{ duration: 1.2, repeat: Infinity, ease: "linear" }}
                >
                    <Loader2 size={22} />
                </motion.div>
            )}

            <div className="printer-top">
                {renderPrinterImage()}

                <div className="printer-meta">
                    <h3 className="printer-name">{printer.nomeCustomizado}</h3>
                    <p className="printer-desc">{printer.descricao || "Sem descrição"}</p>
                </div>
            </div>

            <div className="printer-body">
                <div className="printer-info">

                    {/* 🔗 LINK NO IP (ajuste solicitado) */}
                    <div>
                        <strong>IP:</strong>{" "}
                        <a
                            href={`http://${printer.ip}`}
                            target="_blank"
                            rel="noopener noreferrer"
                            style={{ color: "#4da6ff", textDecoration: "underline" }}
                        >
                            {printer.ip}
                        </a>
                    </div>

                    <div>
                        <strong>Série:</strong>{" "}
                        {isLoading ? <span className="skeleton w-80" /> : snmpData.numeroSerie}
                    </div>

                    <div>
                        <strong>Status:</strong>{" "}
                        {isLoading ? <span className="skeleton w-60" /> : snmpData.status}
                    </div>

                    <div><strong>Colorida:</strong> {printer.eColorida ? "Sim" : "Não"}</div>

                    <div>
                        <strong>Contador:</strong>{" "}
                        {isLoading ? <span className="skeleton w-40" /> : snmpData.contadorTotal}
                    </div>
                </div>

                <div className="printer-toners">
                    {isLoading ? (
                        <div className="skeleton-toner" />
                    ) : toneEntries.length ? (
                        toneEntries.map(([k, v]) => (
                            <div className="toner" key={k}>
                                <div className="toner-label">{k}</div>
                                <div className="toner-bar">
                                    <div
                                        className="toner-fill"
                                        style={{
                                            width: `${v}%`,
                                            background: getTonerColor(k),
                                        }}
                                    />
                                </div>
                                <div className="toner-percent">{v}%</div>
                            </div>
                        ))
                    ) : (
                        <div className="no-toner">Sem toner detectado</div>
                    )}
                </div>

                <div className="printer-info">
                    <button
                        className="btn btn-sm btn-outline-light"
                        onClick={() => setIsAlertOpen(!isAlertOpen)}
                        style={{ marginTop: "0.5rem" }}
                    >
                        {isAlertOpen ? "Ocultar alertas" : "Mostrar alertas"}
                    </button>

                    {isAlertOpen && (
                        <div
                            className="smooth-update"
                            style={{
                                marginTop: "0.5rem",
                                padding: "0.5rem",
                                background: "rgba(255,255,255,0.05)",
                                borderRadius: "6px",
                            }}
                        >
                            {snmpData.alertas?.length
                                ? snmpData.alertas.map((a, i) => <div key={i}>⚠️ {a}</div>)
                                : "Nenhum alerta ativo"}
                        </div>
                    )}
                </div>
            </div>
        </motion.article>
    );
}
