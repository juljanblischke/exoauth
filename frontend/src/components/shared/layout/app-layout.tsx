import { type ReactNode } from 'react'
import { useIsMobile } from '@/hooks'
import { useSidebar } from '@/contexts'
import { Sidebar } from './sidebar'
import { Header } from './header'
import { Footer } from './footer'
import { MobileNav } from './mobile-nav'
import { cn } from '@/lib/utils'

interface AppLayoutProps {
  children: ReactNode
}

export function AppLayout({ children }: AppLayoutProps) {
  const isMobile = useIsMobile()
  const { isCollapsed } = useSidebar()

  return (
    <div className="min-h-screen bg-background">
      {/* Mobile Navigation */}
      {isMobile && <MobileNav />}

      {/* Desktop Sidebar */}
      {!isMobile && <Sidebar />}

      {/* Main Content Area */}
      <div
        className={cn(
          'flex min-h-screen flex-col transition-all duration-300',
          !isMobile && (isCollapsed ? 'ml-16' : 'ml-64')
        )}
      >
        <Header />

        <main className="flex-1 p-6">
          {children}
        </main>

        <Footer />
      </div>
    </div>
  )
}
