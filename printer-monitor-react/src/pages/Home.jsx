import React, { useEffect, useState } from "react";
import PrinterList from "../components/PrinterList";
import { fetchPrinters } from "../api/printerApi";

export default function Home() {
    const [printers, setPrinters] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    const load = async () => {
        setLoading(true);
        setError(null);
        try {
            const data = await fetchPrinters();
            setPrinters(data);
        } catch (err) {
            console.error(err);
            setError(err.message);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        load();
        const interval = setInterval(load, 5000);
        return () => clearInterval(interval);
    }, []);

    return (
        <section>
            {loading && <div className="spinner-container"><div className="spinner"></div></div>}
            {error && <div className="error">{error}</div>}
            {!loading && !error && <PrinterList printers={printers} />}
        </section>
    );
}
