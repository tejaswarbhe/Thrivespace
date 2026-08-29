import { createContext, useContext, useState } from 'react';
import api from '../api/axiosInstance';

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [role, setRole] = useState(localStorage.getItem('role'));
  const [userId, setUserId] = useState(localStorage.getItem('userId'));

  async function login(email, password) {
    const { data } = await api.post('/Auth/login', { email, password });
    saveSession(data);
  }

  async function register(payload) {
    const { data } = await api.post('/Auth/register', payload);
    saveSession(data);
  }

  function saveSession(data) {
    localStorage.setItem('accessToken', data.accessToken);
    localStorage.setItem('refreshToken', data.refreshToken);
    localStorage.setItem('role', data.role);
    localStorage.setItem('userId', data.userId);
    setRole(data.role);
    setUserId(data.userId);
  }

  function logout() {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('role');
    localStorage.removeItem('userId');
    setRole(null);
    setUserId(null);
  }

  const isAuthenticated = Boolean(role);

  return (
    <AuthContext.Provider value={{ role, userId, isAuthenticated, login, register, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used inside an AuthProvider');
  return ctx;
}
