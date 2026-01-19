import { Fragment } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useLocation } from '@tanstack/react-router'
import { ChevronRight, Home } from 'lucide-react'
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from '@/components/ui/breadcrumb'

// Map routes to i18n keys
const routeLabels: Record<string, string> = {
  dashboard: 'navigation:breadcrumb.dashboard',
  users: 'navigation:breadcrumb.users',
  permissions: 'navigation:breadcrumb.permissions',
  'audit-logs': 'navigation:breadcrumb.auditLogs',
  'ip-restrictions': 'navigation:breadcrumb.ipRestrictions',
  organizations: 'navigation:breadcrumb.organizations',
  projects: 'navigation:breadcrumb.projects',
  settings: 'navigation:breadcrumb.settings',
  email: 'navigation:breadcrumb.email',
  create: 'navigation:breadcrumb.createUser',
  edit: 'navigation:breadcrumb.editUser',
}

export function Breadcrumbs() {
  const { t } = useTranslation()
  const location = useLocation()

  // Parse pathname into segments, filtering out 'system' prefix
  const segments = location.pathname
    .split('/')
    .filter((segment) => segment !== '' && segment !== 'system')

  if (segments.length === 0) {
    return null
  }

  return (
    <Breadcrumb>
      <BreadcrumbList>
        {/* Home link */}
        <BreadcrumbItem>
          <BreadcrumbLink asChild>
            <Link to="/system/dashboard" className="flex items-center gap-1">
              <Home className="h-4 w-4" />
              <span className="sr-only">{t('navigation:breadcrumb.home')}</span>
            </Link>
          </BreadcrumbLink>
        </BreadcrumbItem>

        {segments.map((segment, index) => {
          const isLast = index === segments.length - 1
          // Prepend /system since we filtered it out from segments
          const href = '/system/' + segments.slice(0, index + 1).join('/')
          const label = routeLabels[segment] || segment

          return (
            <Fragment key={href}>
              <BreadcrumbSeparator>
                <ChevronRight className="h-4 w-4" />
              </BreadcrumbSeparator>
              <BreadcrumbItem>
                {isLast ? (
                  <BreadcrumbPage>{t(label)}</BreadcrumbPage>
                ) : (
                  <BreadcrumbLink asChild>
                    <Link to={href}>{t(label)}</Link>
                  </BreadcrumbLink>
                )}
              </BreadcrumbItem>
            </Fragment>
          )
        })}
      </BreadcrumbList>
    </Breadcrumb>
  )
}
