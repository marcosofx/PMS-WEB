import React, { useEffect, useState } from "react";
import axios from "axios";

const getTonerColor = (k) => {
    switch (k?.toLowerCase()) {
        case "black": return "#222";
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
        alertas: printer.alertas ?? "N/A",
        status: printer.status ?? "N/A"
    });

    const toners = printer.toners || {};
    const toneEntries = printer.eColorida
        ? Object.entries(toners)
        : (toners.Black !== undefined ? [["Black", toners.Black]] : []);

    const fetchSnmp = async () => {
        try {
            const res = await axios.get(`/api/printer/snmp/${printer.id}`);
            setSnmpData(prev => ({
                numeroSerie: res.data.numeroSerie ?? prev.numeroSerie,
                contadorTotal: res.data.contadorTotal ?? prev.contadorTotal,
                alertas: res.data.alertas ?? prev.alertas,
                status: res.data.status ?? prev.status
            }));
        } catch (err) {
            console.log("Erro ao buscar dados SNMP:", err);
        }
    };

    useEffect(() => {
        let isMounted = true;

        const updateData = async () => {
            if (isMounted) await fetchSnmp();
        };

        updateData(); // primeira atualização
        const interval = setInterval(updateData, refreshInterval);

        return () => {
            isMounted = false;
            clearInterval(interval);
        };
    }, [printer.id, refreshInterval]);

    const renderPrinterImage = () => {
        if (printer.foto && printer.foto.trim() !== "") {
            return (
                <img
                    className="printer-photo"
                    src={printer.foto}
                    alt={printer.nomeCustomizado || "Impressora"}
                />
            );
        } else {
            return (
                <div className="printer-placeholder">
                    <svg
                        width="64"
                        height="64"
                        viewBox="0 0 24 24"
                        fill="none"
                        stroke="currentColor"
                        strokeWidth="2"
                        strokeLinecap="round"
                        strokeLinejoin="round"
                    >
                        <rect x="3" y="7" width="18" height="14" rx="2" ry="2" />
                        <path d="M16 3H8v4h8V3z" />
                    </svg>
                </div>
            );
        }
    };

    return (
        <article className="printer-card">
            <div className="printer-top">
                {renderPrinterImage()}
                <div className="printer-meta">
                    <h3 className="printer-name">{printer.nomeCustomizado}</h3>
                    <p className="printer-desc">{printer.descricao || "Sem descrição"}</p>
                </div>
            </div>

            <div className="printer-body">
                <div className="printer-info">
                    <div><strong>IP:</strong> {printer.ip ?? "N/A"}</div>
                    <div><strong>Série:</strong> <span className="smooth-update">{snmpData.numeroSerie}</span></div>
                    <div><strong>Status:</strong> <span className="status-text smooth-update">{snmpData.status}</span></div>
                    <div><strong>Colorida:</strong> {printer.eColorida ? "Sim" : "Não"}</div>
                    <div><strong>Contador:</strong> <span className="smooth-update">{snmpData.contadorTotal}</span></div>
                </div>

                <div className="printer-toners">
                    {toneEntries.length ? toneEntries.map(([k, v]) => (
                        <div className="toner" key={k}>
                            <div className="toner-label">{k}</div>
                            <div className="toner-bar">
                                <div
                                    className="toner-fill"
                                    style={{
                                        width: `${v}%`,
                                        background: getTonerColor(k),
                                        transition: "width 0.6s ease"
                                    }}
                                />
                            </div>
                            <div className="toner-percent">{v}%</div>
                        </div>
                    )) : <div className="no-toner">Sem toner detectado</div>}
                </div>

                <div className="printer-info">
                    <strong>Alertas:</strong> <span className="smooth-update">{snmpData.alertas}</span>
                </div>
            </div>
        </article>
    );
}
