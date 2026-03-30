import { Outlet } from 'react-router-dom';
import { Sidebar } from './Sidebar';
import { MobileNav } from './MobileNav';
import { useMediaQuery } from '../../hooks/useMediaQuery';

export function AppShell() {
  const isMobile = useMediaQuery('(max-width: 767px)');

  return (
    <div className="h-dvh flex flex-col md:flex-row bg-bg-primary">
      {!isMobile && <Sidebar />}
      <main className="flex-1 overflow-hidden">
        <Outlet />
      </main>
      {isMobile && <MobileNav />}
    </div>
  );
}
