import { Navigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

// allowedRoles = e.g. ["Admin"] or ["Admin", "Mentor"]
export default function ProtectedRoute({ allowedRoles, children }) {
  const { isAuthenticated, role } = useAuth();

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  if (allowedRoles && !allowedRoles.includes(role)) {
    // Logged in, but wrong role for this page - send them to their own home
    return <Navigate to="/" replace />;
  }

  return children;
}
