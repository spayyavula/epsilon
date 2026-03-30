import { NavLink, useNavigate } from 'react-router-dom';
import { MessageSquare, Calculator, FlaskConical, FileText, BookOpen, Settings, LogOut, Plus, Sparkles } from 'lucide-react';
import { useAuthStore } from '../../stores/authStore';
import { useChatStore } from '../../stores/chatStore';

const navItems = [
  { to: '/chat', icon: MessageSquare, label: 'Chat' },
  { to: '/solver', icon: Calculator, label: 'Solver' },
  { to: '/research', icon: FlaskConical, label: 'Research' },
  { to: '/documents', icon: FileText, label: 'Documents' },
  { to: '/flashcards', icon: BookOpen, label: 'Flashcards' },
  { to: '/settings', icon: Settings, label: 'Settings' },
];

export function Sidebar() {
  const logout = useAuthStore((s) => s.logout);
  const conversations = useChatStore((s) => s.conversations);
  const activeConversationId = useChatStore((s) => s.activeConversationId);
  const selectConversation = useChatStore((s) => s.selectConversation);
  const createConversation = useChatStore((s) => s.createConversation);
  const navigate = useNavigate();

  const handleNewChat = async () => {
    const id = await createConversation();
    navigate(`/chat/${id}`);
  };

  return (
    <aside className="w-[260px] bg-bg-secondary/50 border-r border-border flex flex-col h-full glass">
      {/* Logo */}
      <div className="p-5 border-b border-border">
        <div className="flex items-center gap-2.5">
          <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-accent to-purple-500 flex items-center justify-center shadow-lg shadow-accent/20">
            <Sparkles size={16} className="text-white" />
          </div>
          <div>
            <h1 className="text-sm font-bold text-text-primary tracking-tight">Epsilon</h1>
            <p className="text-[10px] text-text-muted">Math Research Assistant</p>
          </div>
        </div>
      </div>

      {/* Navigation */}
      <nav className="flex-1 overflow-y-auto p-3 space-y-0.5">
        {navItems.map(({ to, icon: Icon, label }) => (
          <NavLink
            key={to}
            to={to}
            className={({ isActive }) =>
              `flex items-center gap-3 px-3 py-2 rounded-xl text-[13px] font-medium group ${
                isActive
                  ? 'bg-accent/10 text-accent'
                  : 'text-text-secondary hover:bg-black/[0.03] hover:text-text-primary'
              }`
            }
          >
            <Icon size={16} className="shrink-0" />
            {label}
          </NavLink>
        ))}

        {/* Recent chats */}
        <div className="pt-4 mt-4 border-t border-border/50">
          <div className="flex items-center justify-between px-3 mb-2">
            <span className="text-[10px] font-semibold text-text-muted uppercase tracking-wider">Recent</span>
            <button
              onClick={handleNewChat}
              className="w-5 h-5 rounded-md flex items-center justify-center text-text-muted hover:text-accent hover:bg-accent/10"
            >
              <Plus size={12} />
            </button>
          </div>
          <div className="space-y-0.5">
            {conversations.slice(0, 8).map((conv) => (
              <button
                key={conv.id}
                onClick={() => { selectConversation(conv.id); navigate(`/chat/${conv.id}`); }}
                className={`w-full text-left px-3 py-1.5 rounded-lg text-[12px] truncate ${
                  activeConversationId === conv.id
                    ? 'bg-accent/10 text-accent font-medium'
                    : 'text-text-muted hover:bg-black/[0.03] hover:text-text-secondary'
                }`}
              >
                {conv.title}
              </button>
            ))}
          </div>
        </div>
      </nav>

      {/* Footer */}
      <div className="p-3 border-t border-border/50">
        <button
          onClick={logout}
          className="flex items-center gap-2 w-full px-3 py-2 rounded-xl text-[12px] text-text-muted hover:text-danger hover:bg-danger/5"
        >
          <LogOut size={14} />
          Sign Out
        </button>
      </div>
    </aside>
  );
}
