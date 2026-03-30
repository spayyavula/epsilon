import { useState, useCallback, useRef } from 'react';
import { api } from '../api/client';

export function useSSE() {
  const [content, setContent] = useState('');
  const [isStreaming, setIsStreaming] = useState(false);
  const abortRef = useRef<AbortController | null>(null);

  const stream = useCallback(async (path: string, body: unknown) => {
    setContent('');
    setIsStreaming(true);
    abortRef.current = new AbortController();

    let full = '';
    try {
      for await (const chunk of api.stream(path, body, abortRef.current.signal)) {
        full += chunk.delta;
        setContent(full);
      }
    } catch (e) {
      if ((e as Error).name !== 'AbortError') throw e;
    } finally {
      setIsStreaming(false);
      abortRef.current = null;
    }
    return full;
  }, []);

  const stop = useCallback(() => {
    abortRef.current?.abort();
  }, []);

  return { content, isStreaming, stream, stop, setContent };
}
