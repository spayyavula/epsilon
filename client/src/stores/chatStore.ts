import { create } from 'zustand';
import { api } from '../api/client';
import type { ConversationDto, MessageDto } from '../types/api';

interface ChatState {
  conversations: ConversationDto[];
  activeConversationId: string | null;
  messages: MessageDto[];
  isStreaming: boolean;
  streamingContent: string;
  abortController: AbortController | null;
  loadConversations: () => Promise<void>;
  selectConversation: (id: string) => Promise<void>;
  createConversation: (title?: string) => Promise<string>;
  deleteConversation: (id: string) => Promise<void>;
  renameConversation: (id: string, title: string) => Promise<void>;
  sendMessage: (content: string, providerId: string, modelId: string, webSearchEnabled?: boolean) => Promise<void>;
  stopStreaming: () => void;
}

export const useChatStore = create<ChatState>((set, get) => ({
  conversations: [],
  activeConversationId: null,
  messages: [],
  isStreaming: false,
  streamingContent: '',
  abortController: null,

  loadConversations: async () => {
    const conversations = await api.get<ConversationDto[]>('/conversations');
    set({ conversations });
  },

  selectConversation: async (id) => {
    set({ activeConversationId: id, streamingContent: '' });
    const messages = await api.get<MessageDto[]>(`/conversations/${id}/messages`);
    set({ messages });
  },

  createConversation: async (title = 'New Chat') => {
    const conv = await api.post<ConversationDto>('/conversations', { title });
    set((s) => ({ conversations: [conv, ...s.conversations], activeConversationId: conv.id, messages: [] }));
    return conv.id;
  },

  deleteConversation: async (id) => {
    await api.delete(`/conversations/${id}`);
    set((s) => ({
      conversations: s.conversations.filter((c) => c.id !== id),
      activeConversationId: s.activeConversationId === id ? null : s.activeConversationId,
      messages: s.activeConversationId === id ? [] : s.messages,
    }));
  },

  renameConversation: async (id, title) => {
    await api.patch(`/conversations/${id}`, { title });
    set((s) => ({
      conversations: s.conversations.map((c) => (c.id === id ? { ...c, title } : c)),
    }));
  },

  sendMessage: async (content, providerId, modelId, webSearchEnabled = false) => {
    const { activeConversationId } = get();
    if (!activeConversationId) return;

    const userMsg: MessageDto = {
      id: crypto.randomUUID(),
      role: 'user',
      content,
      model: null,
      createdAt: new Date().toISOString(),
    };
    set((s) => ({ messages: [...s.messages, userMsg], isStreaming: true, streamingContent: '' }));

    const controller = new AbortController();
    set({ abortController: controller });

    let fullContent = '';
    try {
      for await (const chunk of api.stream(
        `/conversations/${activeConversationId}/messages`,
        { content, providerId, modelId, webSearchEnabled },
        controller.signal
      )) {
        fullContent += chunk.delta;
        set({ streamingContent: fullContent });
      }

      const assistantMsg: MessageDto = {
        id: crypto.randomUUID(),
        role: 'assistant',
        content: fullContent,
        model: modelId,
        createdAt: new Date().toISOString(),
      };
      set((s) => ({
        messages: [...s.messages, assistantMsg],
        streamingContent: '',
        isStreaming: false,
        abortController: null,
      }));
    } catch (e) {
      if ((e as Error).name !== 'AbortError') {
        set({ isStreaming: false, abortController: null });
        throw e;
      }
      set({ isStreaming: false, abortController: null });
    }

    get().loadConversations();
  },

  stopStreaming: () => {
    const { abortController, streamingContent } = get();
    abortController?.abort();
    if (streamingContent) {
      const partialMsg: MessageDto = {
        id: crypto.randomUUID(),
        role: 'assistant',
        content: streamingContent,
        model: null,
        createdAt: new Date().toISOString(),
      };
      set((s) => ({
        messages: [...s.messages, partialMsg],
        streamingContent: '',
        isStreaming: false,
        abortController: null,
      }));
    }
  },
}));
