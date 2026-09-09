import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider, useAuth } from './context/AuthContext';
import ProtectedRoute from './components/ProtectedRoute';
import Navbar from './components/Navbar';
import LoginPage from './pages/auth/LoginPage';
import RegisterPage from './pages/auth/RegisterPage';
import AdminDashboard from './pages/admin/AdminDashboard';
import MentorDashboard from './pages/mentor/MentorDashboard';
import FounderDashboard from './pages/founder/FounderDashboard';

// Sends a logged-in user to the dashboard that matches their role
function HomeRedirect() {
  const { role } = useAuth();
  if (role === 'Admin') return <Navigate to="/admin" replace />;
  if (role === 'Mentor') return <Navigate to="/mentor" replace />;
  if (role === 'Founder') return <Navigate to="/founder" replace />;
  return <Navigate to="/login" replace />;
}

function AppRoutes() {
  return (
    <BrowserRouter>
      <Navbar />
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />

        <Route path="/" element={<HomeRedirect />} />

        <Route
          path="/admin"
          element={
            <ProtectedRoute allowedRoles={['Admin']}>
              <AdminDashboard />
            </ProtectedRoute>
          }
        />
        <Route
          path="/mentor"
          element={
            <ProtectedRoute allowedRoles={['Mentor']}>
              <MentorDashboard />
            </ProtectedRoute>
          }
        />
        <Route
          path="/founder"
          element={
            <ProtectedRoute allowedRoles={['Founder']}>
              <FounderDashboard />
            </ProtectedRoute>
          }
        />
      </Routes>
    </BrowserRouter>
  );
}

export default function App() {
  return (
    <AuthProvider>
      <AppRoutes />
    </AuthProvider>
  );
}
