import { useTranslation } from 'react-i18next'
import { Link, useLocation } from '@tanstack/react-router'
import { Menu } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetTrigger,
} from '@/components/ui/sheet'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Separator } from '@/components/ui/separator'
import { useSidebar, usePermissions } from '@/contexts'
import { navigation } from '@/config/navigation'
import { cn } from '@/lib/utils'

export function MobileNav() {
  const { t } = useTranslation()
  const location = useLocation()
  const { isOpen, setOpen } = useSidebar()
  const { hasPermission } = usePermissions()

  return (
    <Sheet open={isOpen} onOpenChange={setOpen}>
      <SheetTrigger asChild>
        <Button
          variant="ghost"
          size="icon"
          className="fixed left-4 top-4 z-50 md:hidden"
        >
          <Menu className="h-5 w-5" />
          <span className="sr-only">{t('navigation:sidebar.toggleMenu')}</span>
        </Button>
      </SheetTrigger>
      <SheetContent side="left" className="w-64 p-0">
        <SheetHeader className="border-b px-4 py-4">
          <SheetTitle asChild>
            <Link
              to="/system/dashboard"
              className="flex items-center gap-2"
              onClick={() => setOpen(false)}
            >
              <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-primary text-primary-foreground font-bold">
                E
              </div>
              <span className="font-semibold">{t('common:app.name')}</span>
            </Link>
          </SheetTitle>
        </SheetHeader>

        <ScrollArea className="h-[calc(100vh-5rem)]">
          <nav className="space-y-6 p-4">
            {navigation.map((section, sectionIndex) => {
              // Filter items by permission
              const visibleItems = section.items.filter(
                (item) => !item.permission || hasPermission(item.permission)
              )

              if (visibleItems.length === 0) return null

              return (
                <div key={sectionIndex}>
                  {/* Section Header */}
                  <h3 className="mb-2 px-3 text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                    {t(section.label)}
                  </h3>

                  {sectionIndex > 0 && <Separator className="my-2" />}

                  {/* Section Items */}
                  <div className="space-y-1">
                    {visibleItems.map((item) => {
                      const isActive = location.pathname === item.href
                      const Icon = item.icon

                      return (
                        <Link
                          key={item.href}
                          to={item.href}
                          onClick={() => setOpen(false)}
                          className={cn(
                            'flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors',
                            isActive
                              ? 'bg-accent text-accent-foreground'
                              : 'text-foreground hover:bg-accent hover:text-accent-foreground'
                          )}
                        >
                          <Icon className="h-5 w-5 shrink-0" />
                          <span>{t(item.label)}</span>
                          {item.badge && (
                            <span className="ml-auto flex h-5 min-w-5 items-center justify-center rounded-full bg-primary px-1.5 text-xs text-primary-foreground">
                              {item.badge}
                            </span>
                          )}
                        </Link>
                      )
                    })}
                  </div>
                </div>
              )
            })}
          </nav>
        </ScrollArea>
      </SheetContent>
    </Sheet>
  )
}
