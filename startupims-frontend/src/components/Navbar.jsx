import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export default function Navbar() {
  const { role, isAuthenticated, logout } = useAuth();
  const navigate = useNavigate();

  function handleLogout() {
    logout();
    navigate('/login');
  }

  return (
    <nav className="navbar navbar-expand-lg px-4 py-3">
      <span className="navbar-brand mb-0 h1">StartupIMS</span>
      {isAuthenticated && (
        <div className="d-flex align-items-center gap-3 ms-auto">
          <span className="badge bg-secondary me-2">{role}</span>
          <button className="btn btn-outline-primary btn-sm" onClick={handleLogout}>
            Log out
          </button>
        </div>
      )}
    </nav>
  );
}
