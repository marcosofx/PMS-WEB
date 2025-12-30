import React, { useState } from "react";
import { Routes, Route, Link } from "react-router-dom";
import Home from "./pages/Home";
import Admin from "./pages/Admin";

export default function App() {
    const [nocMode, setNocMode] = useState(false);

    const toggleNoc = () => {
        setNocMode(prev => !prev);
    };

    return (
        <div className={`app-root ${nocMode ? "noc-mode" : ""}`}>

            {/* HEADER — só aparece fora do NOC */}
            {!nocMode && (
                <header className="app-header">
                    <div className="brand">
                        <h1>Printer Monitor System</h1>
                        <span className="tag">Monitoramento em tempo real</span>
                    </div>

                    <button
                        className="noc-toggle"
                        onClick={toggleNoc}
                        title="Modo NOC"
                    >
                        <i className="fi fi-rs-monitor"></i>
                        <span>Modo NOC</span>
                    </button>

                    <nav>
                        <Link to="/">
                            <i className="fi fi-rs-home"></i>
                        </Link>

                        <Link to="/admin">
                            <i className="fi fi-rr-settings-sliders"></i>
                        </Link>
                    </nav>
                </header>
            )}

            {/* BOTÃO FLUTUANTE — só no NOC */}
            {nocMode && (
                <button
                    className="noc-exit"
                    onClick={toggleNoc}
                    title="Sair do Modo NOC"
                >
                    <i className="fi fi-rs-cross"></i>
                </button>
            )}

            {/* MAIN */}
            <main className="app-main">
                <Routes>
                    <Route path="/" element={<Home nocMode={nocMode} />} />
                    <Route path="/admin/*" element={<Admin />} />
                </Routes>
            </main>

        </div>
    );
}
