import React, { useState } from "react";

export default function AdminLogin({ onAuth }) {
    const [pw, setPw] = useState("");

    const submit = (e) => {
        e.preventDefault();
        // Mude aqui sua senha ou coloque verificação no back
        if (pw === "senha123") {
            localStorage.setItem("admin_auth", "1");
            onAuth(true);
        } else {
            alert("Senha incorreta");
        }
    };

    return (
        <form className="admin-login" onSubmit={submit}>
            <h2>Administrador</h2>
            <input type="password" placeholder="Senha" value={pw} onChange={e => setPw(e.target.value)} />
            <button type="submit">Entrar</button>
        </form>
    );
}
