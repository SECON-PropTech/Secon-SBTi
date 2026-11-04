import { useState } from "react";
import { useNavigate } from "react-router-dom";
import "./Login.css";
import seconLogo from "../assets/secon_logo.svg";

function Login() {
    const [email, setEmail] = useState<string>("");
    const [password, setPassword] = useState<string>("");
    
    const [loading, setLoading] = useState<boolean>(false);
    const [errorMessage, setErrorMessage] = useState("");

    const navigate = useNavigate();

const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setLoading(true);
    setErrorMessage("");

    try {
        const response = await fetch("/api/auth/login", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
            credentials: "include",
            body: JSON.stringify({
                email,
                password,
            }),
        });

        const data = await response.json();

        if (!response.ok) {
            setErrorMessage(data.message || "Inloggningen misslyckades.");
            return;
        }

        navigate("/home");

    } catch {
        setErrorMessage("Kunde inte kontakta servern.");
    } finally {
        setLoading(false);
    }
};

    return (
        
        <div className="login-container">
            <form className="login-form" onSubmit={handleSubmit}>
                <img src={seconLogo} alt="SECON" className="secon-logo"/>
                <div className="mb-3">
                    <label htmlFor="email" className="form-label">
                        Email address
                    </label>

                    <input
                        type="email"
                        className="form-control"
                        id="email"
                        value={email}
                        onChange={(e) => setEmail(e.target.value)}
                        required
                        autoComplete="email"
                        disabled={loading}
                    />
                </div>

                <div className="mb-3">
                    <label htmlFor="password" className="form-label">
                        Password
                    </label>

                    <input
                        type="password"
                        className="form-control"
                        id="password"
                        value={password}
                        onChange={(e) => setPassword(e.target.value)}
                        required
                        disabled={loading}
                    />
                </div>
                {errorMessage && (
                    <div className="alert alert-danger" role="alert">
                        {errorMessage}
                    </div>
                )}

                <button
                    type="submit"
                    className="btn btn-primary"
                    disabled={loading}
                >
                    {loading ? (
                        <>
                            <span
                                className="spinner-border spinner-border-sm me-2"
                                role="status"
                                aria-hidden="true"
                            ></span>
                            Loggar in..
                        </>
                    ) : (
                        "Login"
                    )}
                </button>
            </form>
        </div>
    );
}

export default Login;