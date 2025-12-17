import React, { useState } from "react";
import { Routes, Route } from "react-router-dom";
import AdminLogin from "../components/AdminLogin";
import AdminDashboard from "../components/AdminDashboard";

export default function Admin() {
    const [auth, setAuth] = useState(() => !!localStorage.getItem("admin_auth"));

    const logout = () => {
        localStorage.removeItem("admin_auth");
        setAuth(false);
    };

    if (!auth) {
        return <AdminLogin onAuth={() => setAuth(true)} />;
    }

    return (
        <Routes>
            {/* Rota principal do painel admin */}
            <Route
                path="*"
                element={
                    <div>
                        <div className="admin-top">
                            <i
                                onClick={logout}
                                className="fi fi-rr-address-card"
                                style={{ cursor: "pointer" }}
                            ></i>
                        </div>

                        <AdminDashboard />
                    </div>
                }
            />
        </Routes>
    );
}
