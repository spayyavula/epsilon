import { NavLink } from 'react-router-dom';
import { MessageSquare, Calculator, FlaskConical, FileText, MoreHorizontal } from 'lucide-react';

const items = [
  { to: '/chat', icon: MessageSquare, label: 'Chat' },
  { to: '/solver', icon: Calculator, label: 'Solver' },
  { to: '/research', icon: FlaskConical, label: 'Research' },
  { to: '/documents', icon: FileText, label: 'Library' },
  { to: '/settings', icon: MoreHorizontal, label: 'More' },
];

export function MobileNav() {
  return (
    <nav className="bg-bg-secondary/80 glass border-t border-border/50 flex justify-around py-1.5 px-2 safe-area-pb">
      {items.map(({ to, icon: Icon, label }) => (
        <NavLink
          key={to}
          to={to}
          className={({ isActive }) =>
            `flex flex-col items-center gap-0.5 px-3 py-1.5 rounded-xl min-w-[56px] ${
              isActive ? 'text-accent' : 'text-text-muted active:scale-95'
            }`
          }
        >
          {({ isActive }) => (
            <>
              <div className="relative">
                <Icon size={20} strokeWidth={isActive ? 2.5 : 1.5} />
                {isActive && (
                  <div className="absolute -bottom-1 left-1/2 -translate-x-1/2 w-1 h-1 rounded-full bg-accent" />
                )}
              </div>
              <span className="text-[10px] font-medium">{label}</span>
            </>
          )}
        </NavLink>
      ))}
    </nav>
  );
}
