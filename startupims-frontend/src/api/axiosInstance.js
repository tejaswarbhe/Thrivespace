import axios from 'axios';

// Base URL of your ASP.NET Core API - update the port to match what
// Visual Studio shows you when you run the backend (check launchSettings.json
// or the console output when you hit F5).
const API_BASE_URL = 'https://localhost:61901/api';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Cache-Control': 'no-cache',
    Pragma: 'no-cache',
  },
});

// --- Request interceptor: attach the access token to every outgoing call ---
api.interceptors.request.use((config) => {
  const accessToken = localStorage.getItem('accessToken');
  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`;
  }
  return config;
});

// --- Response interceptor: if a request comes back 401 (access token expired),
// try refreshing once, then retry the original request. If refresh also
// fails, log the user out. ---
let isRefreshing = false;
let pendingRequests = [];

function resolvePendingRequests(newAccessToken) {
  pendingRequests.forEach((cb) => cb(newAccessToken));
  pendingRequests = [];
}

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    // Only attempt this once per request, and only for 401s, and never for
    // the refresh call itself (avoids an infinite loop if refresh also 401s).
    if (
      error.response?.status === 401 &&
      !originalRequest._retry &&
      !originalRequest.url.includes('/Auth/refresh')
    ) {
      if (isRefreshing) {
        // Another request already triggered a refresh - wait for it instead
        // of firing a second parallel refresh call.
        return new Promise((resolve) => {
          pendingRequests.push((newAccessToken) => {
            originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;
            resolve(api(originalRequest));
          });
        });
      }

      originalRequest._retry = true;
      isRefreshing = true;

      try {
        const refreshToken = localStorage.getItem('refreshToken');
        const { data } = await axios.post(`${API_BASE_URL}/Auth/refresh`, {
          refreshToken,
        });

        localStorage.setItem('accessToken', data.accessToken);
        localStorage.setItem('refreshToken', data.refreshToken);

        resolvePendingRequests(data.accessToken);
        originalRequest.headers.Authorization = `Bearer ${data.accessToken}`;
        return api(originalRequest);
      } catch (refreshError) {
        // Refresh token is also invalid/expired - the user needs to log in again
        localStorage.removeItem('accessToken');
        localStorage.removeItem('refreshToken');
        localStorage.removeItem('role');
        localStorage.removeItem('userId');
        window.location.href = '/login';
        return Promise.reject(refreshError);
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  }
);

export default api;
