import {
  LayoutDashboard,
  Users,
  FileText,
  Settings,
  Building2,
  FolderKanban,
  Shield,
  Mail,
  type LucideIcon,
} from 'lucide-react'

export interface NavItem {
  label: string // i18n key
  href: string
  icon: LucideIcon
  permission?: string // Required permission (optional = always visible)
  badge?: number | string // Optional badge count
}

export interface NavSection {
  label: string // i18n key for section header
  items: NavItem[]
}

export const navigation: NavSection[] = [
  {
    label: 'navigation:sections.system',
    items: [
      {
        label: 'navigation:items.dashboard',
        href: '/dashboard',
        icon: LayoutDashboard,
        // No permission = always visible
      },
      {
        label: 'navigation:items.users',
        href: '/users',
        icon: Users,
        permission: 'system:users:read',
      },
      {
        label: 'navigation:items.auditLogs',
        href: '/audit-logs',
        icon: FileText,
        permission: 'system:audit:read',
      },
      {
        label: 'navigation:items.ipRestrictions',
        href: '/ip-restrictions',
        icon: Shield,
        permission: 'system:ip-restrictions:read',
      },
      {
        label: 'navigation:items.email',
        href: '/email',
        icon: Mail,
        permission: 'email:providers:read',
      },
    ],
  },
  {
    label: 'navigation:sections.management',
    items: [
      {
        label: 'navigation:items.organizations',
        href: '/organizations',
        icon: Building2,
        permission: 'system:organizations:read',
      },
      {
        label: 'navigation:items.projects',
        href: '/projects',
        icon: FolderKanban,
        permission: 'system:projects:read',
      },
    ],
  },
  {
    label: 'navigation:sections.settings',
    items: [
      {
        label: 'navigation:items.settings',
        href: '/settings',
        icon: Settings,
        permission: 'system:settings:read',
      },
    ],
  },
]
