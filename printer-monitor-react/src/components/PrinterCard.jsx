import React, { useEffect, useState } from "react";
import axios from "axios";
import { motion, AnimatePresence } from "framer-motion";
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

const getStatusColor = (status) => {
    switch (status?.toLowerCase()) {
        case "atenção":
            return "#FFD24D"; // amarelo
        case "indisponível":
        case "indisponivel":
            return "#FF4D4F"; // vermelho
        case "rodando":
            return "#22C55E"; // verde
        default:
            return "#94A3B8"; // neutro
    }
};

export default function PrinterCard({ printer, refreshInterval = 30000 }) {

    const [snmpData, setSnmpData] = useState({
        numeroSerie: printer.numeroSerie ?? "N/A",
        contadorTotal: printer.contadorTotal ?? "N/A",
        alertas: printer.alertas ?? [],
        status: printer.status ?? "N/A",
    });

    const [isLoading, setIsLoading] = useState(true);
    const [imageError, setImageError] = useState(false);
    const [expanded, setExpanded] = useState(false);

    const toners = printer.toners || {};
    const toneEntries = printer.eColorida
        ? Object.entries(toners)
        : toners.Black !== undefined
            ? [["Black", toners.Black]]
            : [];

    const fetchSnmp = async () => {
        try {
            setIsLoading(true);
            const res = await axios.get(`${API_BASE}/snmp/${printer.id}`);

            setSnmpData(prev => ({
                numeroSerie: res.data.numeroSerie ?? prev.numeroSerie,
                contadorTotal: res.data.contadorTotal ?? prev.contadorTotal,
                alertas: res.data.alertas ?? prev.alertas,
                status: res.data.status ?? prev.status,
            }));

        } catch (err) {
            console.log("Erro ao buscar SNMP:", err);
        } finally {
            setTimeout(() => setIsLoading(false), 500);
        }
    };

    useEffect(() => {
        fetchSnmp();
        const interval = setInterval(fetchSnmp, refreshInterval);
        return () => clearInterval(interval);
    }, [printer.id, refreshInterval]);

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

    const renderImage = () => {
        if (!imageError && printer.imagemUrl) {
            return (
                <img
                    className="printer-photo"
                    src={printer.imagemUrl}
                    alt={printer.nomeCustomizado}
                    onError={() => setImageError(true)}
                />
            );
        }

        return (
            <div className="printer-placeholder">
                <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                    <rect x="3" y="7" width="18" height="14" rx="2" />
                    <path d="M16 3H8v4h8V3z" />
                </svg>
            </div>
        );
    };

    return (
        <motion.article
            className="printer-card"
            onClick={() => setExpanded(!expanded)}
            animate={
                neonEffect
                    ? {
                        boxShadow: [
                            `0 0 0 ${neonEffect}`,
                            `0 0 12px ${neonEffect}`,
                            `0 0 22px ${neonEffect}`,
                            `0 0 12px ${neonEffect}`,
                        ],
                    }
                    : {}
            }
            transition={{ duration: 2, repeat: neonEffect ? Infinity : 0 }}
        >
            {isLoading && (
                <motion.div
                    style={{ position: "absolute", top: 10, right: 10 }}
                    animate={{ rotate: 360 }}
                    transition={{ duration: 1, repeat: Infinity, ease: "linear" }}
                >
                    <Loader2 size={18} />
                </motion.div>
            )}

            {/* ===== TOPO (SEMPRE VISÍVEL) ===== */}
            <div className="printer-top">
                {renderImage()}

                <div className="printer-meta">
                    <h3 className="printer-name">{printer.nomeCustomizado}</h3>

                    <div style={{ fontSize: 12 }}>
                        <strong>IP:</strong>{" "}
                        <a
                            href={`http://${printer.ip}`}
                            target="_blank"
                            rel="noreferrer"
                            style={{ color: "#4DA6FF" }}
                            onClick={(e) => e.stopPropagation()}
                        >
                            {printer.ip}
                        </a>
                    </div>

                    <div
                        style={{
                            fontSize: 12,
                            fontWeight: "bold",
                            color: getStatusColor(snmpData.status),
                        }}
                    >
                        {snmpData.status}
                    </div>
                </div>
            </div>

            {/* ===== EXPANSÃO ===== */}
            <AnimatePresence>
                {expanded && (
                    <motion.div
                        initial={{ opacity: 0, height: 0 }}
                        animate={{ opacity: 1, height: "auto" }}
                        exit={{ opacity: 0, height: 0 }}
                        transition={{ duration: 0.3 }}
                    >
                        <div className="printer-body">

                            <div className="printer-info">
                                <div><strong>Série:</strong> {snmpData.numeroSerie}</div>
                                <div><strong>Contador:</strong> {snmpData.contadorTotal}</div>
                                <div><strong>Colorida:</strong> {printer.eColorida ? "Sim" : "Não"}</div>
                            </div>

                            <div className="printer-toners">
                                {toneEntries.map(([k, v]) => (
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
                                ))}
                            </div>

                            <div className="printer-info">
                                {snmpData.alertas?.length
                                    ? snmpData.alertas.map((a, i) => (
                                        <div key={i}>⚠️ {a}</div>
                                    ))
                                    : "Nenhum alerta ativo"}
                            </div>
                        </div>
                    </motion.div>
                )}
            </AnimatePresence>
        </motion.article>
    );
}
