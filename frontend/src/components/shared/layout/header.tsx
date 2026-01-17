import { useTranslation } from 'react-i18next'
import { Search, Bell } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { useIsMobile } from '@/hooks'
import { Breadcrumbs } from './breadcrumbs'
import { ThemeToggle } from './theme-toggle'
import { LanguageSwitcher } from './language-switcher'
import { UserMenu } from './user-menu'

export function Header() {
  const { t } = useTranslation()
  const isMobile = useIsMobile()

  return (
    <header className="sticky top-0 z-30 flex h-16 items-center gap-4 border-b bg-background px-6">
      {/* Left side - Breadcrumbs (desktop) or spacer (mobile) */}
      <div className="flex-1">
        {isMobile ? (
          <div className="w-10" /> // Spacer for hamburger menu
        ) : (
          <Breadcrumbs />
        )}
      </div>

      {/* Right side - Actions */}
      <div className="flex items-center gap-2">
        {/* Search Button */}
        <Button
          variant="ghost"
          size="icon"
          className="hidden sm:flex"
          aria-label={t('navigation:search.placeholder')}
        >
          <Search className="h-5 w-5" />
        </Button>

        {/* Notifications (placeholder) */}
        <Button
          variant="ghost"
          size="icon"
          aria-label={t('navigation:notifications.title')}
        >
          <Bell className="h-5 w-5" />
        </Button>

        {/* Theme Toggle */}
        <ThemeToggle />

        {/* Language Switcher */}
        <LanguageSwitcher />

        {/* User Menu */}
        <UserMenu />
      </div>
    </header>
  )
}
