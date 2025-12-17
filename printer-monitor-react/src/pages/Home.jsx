import React, { useEffect, useState } from "react";
import PrinterCard from "../components/PrinterCard";
import { fetchPrinters } from "../api/printerApi";
import { motion, AnimatePresence } from "framer-motion";

export default function Home() {
    const [printers, setPrinters] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    // 🔄 Carregamento progressivo
    const load = async () => {
        setLoading(true);
        setError(null);
        setPrinters([]);

        try {
            const data = await fetchPrinters();

            let buffer = [];
            for (const p of data) {
                buffer = [...buffer, p];
                setPrinters([...buffer]);
                await new Promise((res) => setTimeout(res, 120));
            }

        } catch (err) {
            console.error(err);
            setError(err.message);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        load();
    }, []);

    // Skeleton visual
    const SkeletonCard = ({ id }) => (
        <motion.div
            key={`skeleton-${id}`}
            className="printer-card"
            initial={{ opacity: 0.4 }}
            animate={{ opacity: [0.4, 1, 0.4] }}
            transition={{ duration: 1.4, repeat: Infinity }}
            style={{
                display: "flex",
                flexDirection: "column",
                gap: "12px",
                background:
                    "linear-gradient(90deg, #1f2328 25%, #2a2f36 50%, #1f2328 75%)",
                backgroundSize: "200% 100%",
                borderRadius: "12px",
                width: "220px",
                height: "380px",
                padding: "1rem",
            }}
        >
            <div style={{ display: "flex", gap: "1rem" }}>
                <div
                    style={{
                        width: "72px",
                        height: "72px",
                        borderRadius: "8px",
                        background: "#2d3238",
                    }}
                ></div>
                <div style={{ flex: 1 }}>
                    <div
                        style={{
                            width: "80%",
                            height: "14px",
                            background: "#2d3238",
                            marginBottom: "0.4rem",
                            borderRadius: "4px",
                        }}
                    ></div>
                    <div
                        style={{
                            width: "60%",
                            height: "12px",
                            background: "#2d3238",
                            borderRadius: "4px",
                        }}
                    ></div>
                </div>
            </div>

            <div>
                {[1, 2, 3].map((i) => (
                    <div
                        key={i}
                        style={{
                            width: "60%",
                            height: "12px",
                            marginBottom: "0.4rem",
                            background: "#2d3238",
                            borderRadius: "4px",
                        }}
                    ></div>
                ))}
            </div>

            <div>
                {[1, 2, 3, 4].map((i) => (
                    <div
                        key={i}
                        style={{
                            height: "12px",
                            background: "#2d3238",
                            borderRadius: "4px",
                            marginBottom: "0.4rem",
                        }}
                    ></div>
                ))}
            </div>

            <div
                style={{
                    height: "10px",
                    background: "#2d3238",
                    borderRadius: "4px",
                }}
            ></div>
        </motion.div>
    );

    return (
        <section
            className="cards-grid"
            style={{
                padding: "1rem",
                display: "flex",
                flexWrap: "wrap",
                justifyContent: "center",
                gap: "1rem",
            }}
        >
            {error && <div className="error">{error}</div>}

            <AnimatePresence mode="popLayout">
                {/* Skeleton enquanto carrega */}
                {loading &&
                    !error &&
                    [...Array(8)].map((_, i) => (
                        <SkeletonCard key={`skeleton-${i}`} id={i} />
                    ))}

                {/* Cards reais carregando progressivamente */}
                {printers.map((printer, i) => (
                    <motion.div
                        key={printer.id ?? i}
                        initial={{ opacity: 0, scale: 0.96 }}
                        animate={{ opacity: 1, scale: 1 }}
                        exit={{ opacity: 0, scale: 0.95 }}
                        transition={{ duration: 0.4, ease: "easeOut" }}
                    >
                        <PrinterCard printer={printer} />
                    </motion.div>
                ))}
            </AnimatePresence>
        </section>
    );
}
// ok