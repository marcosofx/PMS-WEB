import React from "react";
import { Routes, Route, Link } from "react-router-dom";
import Home from "./pages/Home";
import Admin from "./pages/Admin";

export default function App() {
    return (
        <div className="app-root">
            <header className="app-header">
                <div className="brand">
                    <h1>Printer Monitor System</h1>
                    <span className="tag">Monitoramento em tempo real</span>
                </div>
                <nav>
                    <Link to="/"><i class="fi fi-rs-home"></i></Link>
                    
                    <Link to="/admin"><i class="fi fi-rr-settings-sliders"></i></Link>
                </nav>
            </header>

            <main className="app-main">
                <Routes>
                    <Route path="/" element={<Home />} />
                    <Route path="/admin/*" element={<Admin />} />
                </Routes>
            </main>

            <footer className="app-footer">
                <small>  © {new Date().getFullYear()} Printer Monitor System</small>
            </footer>
        </div>
    );
}
