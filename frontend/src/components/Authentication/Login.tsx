import { useState, useEffect } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import { login } from "../../Services/authService"
import "./Login.css";
import seconLogo from "../../assets/secon_logo.svg"

function Login() {
    const [email, setEmail] = useState<string>("");
    const [password, setPassword] = useState<string>("");
    
    const [loading, setLoading] = useState<boolean>(false);
    const [errorMessage, setErrorMessage] = useState("");

    const navigate = useNavigate();
    const location = useLocation();

    // Checks if the user was redirected after logging out
    const [loggedOut, setLoggedOut] = useState<boolean>(
        () => location.state?.loggedOut === true
    );

    // Hides the message after 3 seconds and clears navigation state
    useEffect(() => {
        if (location.state?.loggedOut) {
            navigate(location.pathname, {
                replace: true,
                state: null
            });
        }
    }, [location.state, location.pathname, navigate]);

    useEffect(() => {
        if (!loggedOut) return;

        const timer = setTimeout(() => {
            setLoggedOut(false);
        }, 3000);

        return () => clearTimeout(timer);
    }, [loggedOut]);

    const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        setLoading(true);
        setErrorMessage("");

        try {
            await login(email, password);
            navigate("/home", { replace: true });
        } catch (error) {
            if (error instanceof Error) {
                setErrorMessage(error.message);
            } else {
                setErrorMessage("Något gick fel vid inloggningen.");
            }
        } finally {
            setLoading(false);
        }
    };


    return (
        <div className="login-container">
            <form className="login-form" onSubmit={handleSubmit}>
                <img src={seconLogo} alt="SECON" className="secon-logo"/>

                {/* Confirmation message after logout */}
                {loggedOut && (
                    <div
                        className="alert alert-success logout-message"
                        role="status"
                    >Du har blivit utloggad.
                    </div>
                )}

                <div className="mb-3">
                    <label htmlFor="email" className="form-label">E-post</label>

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
                    <label htmlFor="password" className="form-label">Lösenord</label>

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