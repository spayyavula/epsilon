import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { api } from '../api/client';
import type { TokenResponse } from '../types/api';

interface AuthState {
  token: string | null;
  refreshToken: string | null;
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string, displayName?: string) => Promise<void>;
  logout: () => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      token: null,
      refreshToken: null,
      isAuthenticated: false,

      login: async (email, password) => {
        const res = await api.post<TokenResponse>('/auth/login', { email, password });
        api.setToken(res.accessToken);
        set({ token: res.accessToken, refreshToken: res.refreshToken, isAuthenticated: true });
      },

      register: async (email, password, displayName) => {
        const res = await api.post<TokenResponse>('/auth/register', { email, password, displayName });
        api.setToken(res.accessToken);
        set({ token: res.accessToken, refreshToken: res.refreshToken, isAuthenticated: true });
      },

      logout: () => {
        api.setToken(null);
        set({ token: null, refreshToken: null, isAuthenticated: false });
      },
    }),
    {
      name: 'epsilon-auth',
    }
  )
);

// Rehydrate token from localStorage synchronously on load
// Zustand persist hydrates async, but we can read localStorage directly
try {
  const raw = localStorage.getItem('epsilon-auth');
  if (raw) {
    const parsed = JSON.parse(raw);
    const token = parsed?.state?.token;
    if (token) {
      api.setToken(token);
    }
  }
} catch { /* ignore parse errors */ }

// Also subscribe to future state changes to keep api client in sync
useAuthStore.subscribe((state) => {
  api.setToken(state.token);
});
