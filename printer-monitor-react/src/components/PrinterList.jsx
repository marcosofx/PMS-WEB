import React from "react";
import PrinterCard from "./PrinterCard";

export default function PrinterList({ printers }) {
    if (!printers || printers.length === 0) {
        return <div className="empty-message">Nenhuma impressora cadastrada.</div>;
    }

    return (
        <section className="cards-grid">
            {printers.map(p => <PrinterCard key={p.id || p.id} printer={p} />)}
        </section>
    );
}
