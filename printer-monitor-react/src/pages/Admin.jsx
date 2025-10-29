import React, { useState } from "react";
import AdminLogin from "../components/AdminLogin";
import AdminDashboard from "../components/AdminDashboard";

export default function Admin() {
    const [auth, setAuth] = useState(!!localStorage.getItem("admin_auth"));

    const logout = () => {
        localStorage.removeItem("admin_auth");
        setAuth(false);
    };

    if (!auth) return <AdminLogin onAuth={() => setAuth(true)} />;

    return (
        <div>
            <div className="admin-top">
                <i onClick={logout} class="fi fi-rr-address-card"></i>
            </div>
            <AdminDashboard />
        </div>
    );
}
