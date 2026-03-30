import { create } from 'zustand';
import { persist } from 'zustand/middleware';

interface UiState {
  sidebarOpen: boolean;
  selectedProviderId: string;
  selectedModelId: string;
  toggleSidebar: () => void;
  setSidebarOpen: (open: boolean) => void;
  setProvider: (id: string) => void;
  setModel: (id: string) => void;
}

export const useUiStore = create<UiState>()(
  persist(
    (set) => ({
      sidebarOpen: false,
      selectedProviderId: 'openai',
      selectedModelId: 'gpt-4o',
      toggleSidebar: () => set((s) => ({ sidebarOpen: !s.sidebarOpen })),
      setSidebarOpen: (open) => set({ sidebarOpen: open }),
      setProvider: (id) => set({ selectedProviderId: id }),
      setModel: (id) => set({ selectedModelId: id }),
    }),
    { name: 'epsilon-ui' }
  )
);
