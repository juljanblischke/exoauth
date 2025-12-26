import { useEffect, useState, useCallback } from 'react'
import { useNavigate } from '@tanstack/react-router'
import { useTranslation } from 'react-i18next'
import {
  CommandDialog,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
  CommandSeparator,
} from '@/components/ui/command'
import {
  LayoutDashboard,
  Users,
  Shield,
  FileText,
  Settings,
  Building2,
  FolderKanban,
  Moon,
  Sun,
  Monitor,
  LogOut,
} from 'lucide-react'
import { useTheme } from '@/contexts/theme-context'
import { useAuth } from '@/contexts/auth-context'

export function CommandMenu() {
  const [open, setOpen] = useState(false)
  const { t } = useTranslation(['navigation', 'common'])
  const navigate = useNavigate()
  const { setTheme } = useTheme()
  const { logout, hasPermission } = useAuth()

  useEffect(() => {
    const down = (e: KeyboardEvent) => {
      if (e.key === 'k' && (e.metaKey || e.ctrlKey)) {
        e.preventDefault()
        setOpen((open) => !open)
      }
    }

    document.addEventListener('keydown', down)
    return () => document.removeEventListener('keydown', down)
  }, [])

  const runCommand = useCallback((command: () => void) => {
    setOpen(false)
    command()
  }, [])

  const navigationItems = [
    {
      label: t('navigation:items.dashboard'),
      icon: LayoutDashboard,
      href: '/dashboard',
    },
    {
      label: t('navigation:items.users'),
      icon: Users,
      href: '/users',
      permission: 'system:users:read',
    },
    {
      label: t('navigation:items.permissions'),
      icon: Shield,
      href: '/permissions',
      permission: 'system:users:read',
    },
    {
      label: t('navigation:items.auditLogs'),
      icon: FileText,
      href: '/audit-logs',
      permission: 'system:audit:read',
    },
    {
      label: t('navigation:items.organizations'),
      icon: Building2,
      href: '/organizations',
      permission: 'system:organizations:read',
    },
    {
      label: t('navigation:items.projects'),
      icon: FolderKanban,
      href: '/projects',
      permission: 'system:projects:read',
    },
    {
      label: t('navigation:items.settings'),
      icon: Settings,
      href: '/settings',
      permission: 'system:settings:read',
    },
  ]

  const filteredNavItems = navigationItems.filter(
    (item) => !item.permission || hasPermission(item.permission)
  )

  return (
    <CommandDialog open={open} onOpenChange={setOpen}>
      <CommandInput placeholder={t('common:search.placeholder')} />
      <CommandList>
        <CommandEmpty>{t('common:search.noResults')}</CommandEmpty>

        <CommandGroup heading={t('navigation:sections.navigation')}>
          {filteredNavItems.map((item) => (
            <CommandItem
              key={item.href}
              onSelect={() => runCommand(() => navigate({ to: item.href }))}
            >
              <item.icon className="mr-2 h-4 w-4" />
              {item.label}
            </CommandItem>
          ))}
        </CommandGroup>

        <CommandSeparator />

        <CommandGroup heading={t('common:theme.label')}>
          <CommandItem onSelect={() => runCommand(() => setTheme('light'))}>
            <Sun className="mr-2 h-4 w-4" />
            {t('common:theme.light')}
          </CommandItem>
          <CommandItem onSelect={() => runCommand(() => setTheme('dark'))}>
            <Moon className="mr-2 h-4 w-4" />
            {t('common:theme.dark')}
          </CommandItem>
          <CommandItem onSelect={() => runCommand(() => setTheme('system'))}>
            <Monitor className="mr-2 h-4 w-4" />
            {t('common:theme.system')}
          </CommandItem>
        </CommandGroup>

        <CommandSeparator />

        <CommandGroup heading={t('common:actions.account')}>
          <CommandItem
            onSelect={() => runCommand(() => logout())}
            className="text-destructive"
          >
            <LogOut className="mr-2 h-4 w-4" />
            {t('common:actions.logout')}
          </CommandItem>
        </CommandGroup>
      </CommandList>
    </CommandDialog>
  )
}
