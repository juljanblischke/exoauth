import { useTranslation } from 'react-i18next'
import { Link, useLocation } from '@tanstack/react-router'
import { ChevronLeft, ChevronRight } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Separator } from '@/components/ui/separator'
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@/components/ui/tooltip'
import { useSidebar, usePermissions } from '@/contexts'
import { navigation } from '@/config/navigation'
import { cn } from '@/lib/utils'

export function Sidebar() {
  const { t } = useTranslation()
  const location = useLocation()
  const { isCollapsed, toggle } = useSidebar()
  const { hasPermission } = usePermissions()

  return (
    <aside
      className={cn(
        'fixed left-0 top-0 z-40 h-screen border-r bg-sidebar transition-all duration-300',
        isCollapsed ? 'w-16' : 'w-64'
      )}
    >
      <div className="flex h-full flex-col">
        {/* Logo */}
        <div className="flex h-16 items-center justify-between border-b px-4">
          {!isCollapsed && (
            <Link to="/dashboard" className="flex items-center gap-2">
              <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-primary text-primary-foreground font-bold">
                E
              </div>
              <span className="font-semibold text-sidebar-foreground">
                {t('common:app.name')}
              </span>
            </Link>
          )}
          {isCollapsed && (
            <Link to="/dashboard" className="mx-auto">
              <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-primary text-primary-foreground font-bold">
                E
              </div>
            </Link>
          )}
        </div>

        {/* Navigation */}
        <ScrollArea className="flex-1 py-4">
          <TooltipProvider delayDuration={0}>
            <nav className="space-y-6 px-2">
              {navigation.map((section, sectionIndex) => {
                // Filter items by permission
                const visibleItems = section.items.filter(
                  (item) => !item.permission || hasPermission(item.permission)
                )

                if (visibleItems.length === 0) return null

                return (
                  <div key={sectionIndex}>
                    {/* Section Header */}
                    {!isCollapsed && (
                      <h3 className="mb-2 px-3 text-xs font-semibold uppercase tracking-wider text-sidebar-foreground/60">
                        {t(section.label)}
                      </h3>
                    )}
                    {isCollapsed && sectionIndex > 0 && (
                      <Separator className="my-2" />
                    )}

                    {/* Section Items */}
                    <div className="space-y-1">
                      {visibleItems.map((item) => {
                        const isActive = location.pathname === item.href
                        const Icon = item.icon

                        const linkContent = (
                          <Link
                            to={item.href}
                            className={cn(
                              'flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors',
                              isActive
                                ? 'bg-sidebar-accent text-sidebar-accent-foreground'
                                : 'text-sidebar-foreground hover:bg-sidebar-accent hover:text-sidebar-accent-foreground',
                              isCollapsed && 'justify-center px-2'
                            )}
                          >
                            <Icon className="h-5 w-5 shrink-0" />
                            {!isCollapsed && <span>{t(item.label)}</span>}
                            {!isCollapsed && item.badge && (
                              <span className="ml-auto flex h-5 min-w-5 items-center justify-center rounded-full bg-primary px-1.5 text-xs text-primary-foreground">
                                {item.badge}
                              </span>
                            )}
                          </Link>
                        )

                        if (isCollapsed) {
                          return (
                            <Tooltip key={item.href}>
                              <TooltipTrigger asChild>
                                {linkContent}
                              </TooltipTrigger>
                              <TooltipContent side="right">
                                {t(item.label)}
                              </TooltipContent>
                            </Tooltip>
                          )
                        }

                        return <div key={item.href}>{linkContent}</div>
                      })}
                    </div>
                  </div>
                )
              })}
            </nav>
          </TooltipProvider>
        </ScrollArea>

        {/* Collapse Toggle */}
        <div className="border-t p-2">
          <Button
            variant="ghost"
            size="sm"
            onClick={toggle}
            className={cn('w-full', isCollapsed && 'px-2')}
          >
            {isCollapsed ? (
              <ChevronRight className="h-4 w-4" />
            ) : (
              <>
                <ChevronLeft className="h-4 w-4 mr-2" />
                <span>{t('navigation:sidebar.collapse')}</span>
              </>
            )}
          </Button>
        </div>
      </div>
    </aside>
  )
}
