import { useEffect, useRef, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Send, Square, Plus, Sparkles } from 'lucide-react';
import { useChatStore } from '../stores/chatStore';
import { useUiStore } from '../stores/uiStore';
import { MarkdownMath } from '../components/math/MarkdownMath';
import { Button } from '../components/ui/Button';
import { Textarea } from '../components/ui/Textarea';

export function ChatPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [input, setInput] = useState('');
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  const {
    conversations, messages, isStreaming, streamingContent,
    selectConversation, createConversation, sendMessage, stopStreaming, loadConversations,
  } = useChatStore();
  const { selectedProviderId, selectedModelId } = useUiStore();

  useEffect(() => { loadConversations(); }, [loadConversations]);
  useEffect(() => { if (id) selectConversation(id); }, [id, selectConversation]);
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, streamingContent]);

  const handleSend = async () => {
    if (!input.trim() || isStreaming) return;
    const content = input;
    setInput('');
    if (textareaRef.current) textareaRef.current.style.height = 'auto';

    let convId = id;
    if (!convId) {
      convId = await createConversation(content.slice(0, 50));
      navigate(`/chat/${convId}`, { replace: true });
    }
    await sendMessage(content, selectedProviderId, selectedModelId);
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  const suggestions = [
    'Prove that √2 is irrational',
    'Solve x³ - 6x² + 11x - 6 = 0',
    'Explain eigenvalues intuitively',
    'What is the Riemann Hypothesis?',
  ];

  return (
    <div className="flex flex-col h-full">
      {/* Header */}
      <header className="flex items-center gap-3 px-4 py-3 border-b border-border/50 glass bg-bg-secondary/30">
        <h2 className="text-sm font-medium text-text-primary truncate flex-1">
          {id ? conversations.find(c => c.id === id)?.title || 'Chat' : 'New Chat'}
        </h2>
        <Button variant="ghost" size="sm" onClick={async () => {
          const newId = await createConversation();
          navigate(`/chat/${newId}`);
        }}>
          <Plus size={15} />
        </Button>
      </header>

      {/* Messages */}
      <div className="flex-1 overflow-y-auto px-4 py-6">
        {messages.length === 0 && !isStreaming ? (
          <div className="flex items-center justify-center h-full">
            <div className="text-center space-y-6 max-w-md animate-fade-in">
              <div className="w-16 h-16 rounded-2xl bg-gradient-to-br from-accent/20 to-purple-500/20 flex items-center justify-center mx-auto border border-accent/10">
                <Sparkles size={28} className="text-accent" />
              </div>
              <div>
                <h2 className="text-xl font-semibold text-text-primary">What would you like to explore?</h2>
                <p className="text-sm text-text-muted mt-2">Ask about equations, proofs, concepts, or anything math</p>
              </div>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
                {suggestions.map((s) => (
                  <button
                    key={s}
                    onClick={() => setInput(s)}
                    className="text-left text-xs bg-bg-secondary/50 border border-border/50 rounded-xl px-3.5 py-2.5 text-text-secondary hover:text-accent hover:border-accent/30 hover:bg-accent/5"
                  >
                    {s}
                  </button>
                ))}
              </div>
            </div>
          </div>
        ) : (
          <div className="max-w-3xl mx-auto space-y-5">
            {messages.map((msg, i) => (
              <div key={msg.id} className={`flex ${msg.role === 'user' ? 'justify-end' : 'justify-start'} animate-fade-in`} style={{ animationDelay: `${i * 30}ms` }}>
                <div className={`max-w-[85%] md:max-w-[75%] rounded-2xl px-4 py-3 ${
                  msg.role === 'user'
                    ? 'bg-accent text-white rounded-br-lg'
                    : 'bg-bg-secondary border border-border rounded-bl-lg'
                }`}>
                  {msg.role === 'user' ? (
                    <p className="text-sm whitespace-pre-wrap leading-relaxed">{msg.content}</p>
                  ) : (
                    <MarkdownMath content={msg.content} />
                  )}
                </div>
              </div>
            ))}

            {isStreaming && streamingContent && (
              <div className="flex justify-start animate-fade-in">
                <div className="max-w-[85%] md:max-w-[75%] bg-bg-secondary border border-border rounded-2xl rounded-bl-lg px-4 py-3">
                  <MarkdownMath content={streamingContent} />
                </div>
              </div>
            )}

            {isStreaming && !streamingContent && (
              <div className="flex justify-start animate-fade-in">
                <div className="bg-bg-secondary border border-border rounded-2xl rounded-bl-lg px-5 py-4">
                  <div className="flex gap-1.5">
                    {[0, 1, 2].map((i) => (
                      <div key={i} className="w-2 h-2 bg-accent/60 rounded-full" style={{
                        animation: 'pulseDot 1.4s ease-in-out infinite',
                        animationDelay: `${i * 0.2}s`,
                      }} />
                    ))}
                  </div>
                </div>
              </div>
            )}
            <div ref={messagesEndRef} />
          </div>
        )}
      </div>

      {/* Input */}
      <div className="border-t border-border/50 bg-bg-secondary/20 glass p-4">
        <div className="flex gap-2.5 items-end max-w-3xl mx-auto">
          <Textarea
            ref={textareaRef}
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyDown={handleKeyDown}
            placeholder="Ask about math..."
            rows={1}
            className="min-h-[46px] max-h-32 bg-bg-secondary/50"
            onInput={(e) => {
              const t = e.target as HTMLTextAreaElement;
              t.style.height = 'auto';
              t.style.height = `${Math.min(t.scrollHeight, 128)}px`;
            }}
          />
          {isStreaming ? (
            <Button variant="danger" size="md" onClick={stopStreaming} className="shrink-0 h-[46px] w-[46px] p-0">
              <Square size={16} />
            </Button>
          ) : (
            <Button size="md" onClick={handleSend} disabled={!input.trim()} className="shrink-0 h-[46px] w-[46px] p-0">
              <Send size={16} />
            </Button>
          )}
        </div>
      </div>
    </div>
  );
}
