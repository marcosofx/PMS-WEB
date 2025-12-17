import React from "react";
import PrinterCard from "./PrinterCard";

export default function PrinterList({ printers = [] }) {

    if (!Array.isArray(printers) || printers.length === 0) {
        return (
            <div className="empty-message" style={{ textAlign: "center", opacity: 0.7 }}>
                Nenhuma impressora cadastrada.
            </div>
        );
    }

    return (
        <section className="cards-grid">
            {printers.map((p) => (
                <PrinterCard
                    key={p.id ?? p.Id ?? Math.random()}
                    printer={p}
                />
            ))}
        </section>
    );
}
