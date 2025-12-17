import React, { useState } from "react";
import { motion } from "framer-motion";

export default function AdminLogin({ onAuth }) {
    const [password, setPassword] = useState("");
    const [error, setError] = useState("");

    const ADMIN_PASSWORD = import.meta.env.VITE_ADMIN_PASSWORD;

    const handleLogin = (e) => {
        e.preventDefault();

        if (password === ADMIN_PASSWORD) {
            onAuth();
        } else {
            setError("Senha incorreta");
        }
    };

    return (
        <motion.div
            className="admin-login-container"
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.6 }}
        >
            <div className="admin-login-box soft-neon-box">
                <h2 className="admin-login-title">Área Administrativa</h2>

                <form onSubmit={handleLogin} className="admin-login-form">
                    <input
                        type="password"
                        placeholder="Digite a senha"
                        value={password}
                        onChange={(e) => setPassword(e.target.value)}
                        className="admin-login-input soft-neon-input"
                    />

                    {error && <p className="error-text">{error}</p>}

                    <button type="submit" className="admin-login-button soft-neon-button">
                        Entrar
                    </button>
                </form>
            </div>


        </motion.div>
    );
}
