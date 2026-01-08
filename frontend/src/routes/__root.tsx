/* eslint-disable react-refresh/only-export-components */
import { createRootRoute, createRoute, Outlet, Navigate } from '@tanstack/react-router'
import { AppLayout } from '@/components/shared/layout'
import { LoadingSpinner } from '@/components/shared/feedback'
import { useAuth, usePermissions } from '@/contexts/auth-context'
import { NotFoundPage } from './not-found'
import { ForbiddenPage } from './forbidden'
import { LoginPage } from './login'
import { RegisterPage } from './register'
import { IndexRedirect } from './index-page'
import { DashboardPage } from './dashboard'
import { UsersPage } from './users'
import { InvitePage } from './invite'
import { AuditLogsPage } from './audit-logs'
import { IpRestrictionsPage } from './ip-restrictions'
import { SettingsPage } from './settings'
import { ImprintPage, PrivacyPage, TermsPage } from './legal'
import { ResetPasswordPage } from './reset-password'
import { ApproveDevicePage } from './approve-device'
import { EmailPage } from './email'

// Permission-protected page wrapper
function withPermission(Component: React.ComponentType, permission: string) {
  return function ProtectedPage() {
    const { hasPermission } = usePermissions()
    if (!hasPermission(permission)) {
      return <Navigate to="/forbidden" />
    }
    return <Component />
  }
}

// Root route - wraps everything
const rootRoute = createRootRoute({
  component: RootComponent,
  notFoundComponent: NotFoundPage,
})

function RootComponent() {
  return <Outlet />
}

// Auth layout - for login, register, etc. (no sidebar)
const authLayoutRoute = createRoute({
  getParentRoute: () => rootRoute,
  id: 'auth-layout',
  component: () => <Outlet />,
})

// Login route
const loginRoute = createRoute({
  getParentRoute: () => authLayoutRoute,
  path: '/login',
  component: LoginPage,
})

// Register route
const registerRoute = createRoute({
  getParentRoute: () => authLayoutRoute,
  path: '/register',
  component: RegisterPage,
})

// Forbidden route
const forbiddenRoute = createRoute({
  getParentRoute: () => authLayoutRoute,
  path: '/forbidden',
  component: ForbiddenPage,
})

// Invite route - for accepting invitations
const inviteRoute = createRoute({
  getParentRoute: () => authLayoutRoute,
  path: '/invite',
  component: InvitePage,
  validateSearch: (search: Record<string, unknown>) => ({
    token: (search.token as string) || '',
  }),
})

// Legal pages - public, no auth required
const imprintRoute = createRoute({
  getParentRoute: () => authLayoutRoute,
  path: '/imprint',
  component: ImprintPage,
})

const privacyRoute = createRoute({
  getParentRoute: () => authLayoutRoute,
  path: '/privacy',
  component: PrivacyPage,
})

const termsRoute = createRoute({
  getParentRoute: () => authLayoutRoute,
  path: '/terms',
  component: TermsPage,
})

// Reset Password route
const resetPasswordRoute = createRoute({
  getParentRoute: () => authLayoutRoute,
  path: '/reset-password',
  component: ResetPasswordPage,
  validateSearch: (search: Record<string, unknown>) => ({
    token: (search.token as string) || '',
  }),
})

// Approve Device route (email link approval)
const approveDeviceRoute = createRoute({
  getParentRoute: () => authLayoutRoute,
  path: '/approve-device/$token',
  component: ApproveDevicePage,
})

// App layout - for authenticated pages (with sidebar)
const appLayoutRoute = createRoute({
  getParentRoute: () => rootRoute,
  id: 'app-layout',
  component: AppLayoutWrapper,
})

function AppLayoutWrapper() {
  const { isAuthenticated, isLoading } = useAuth()

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <LoadingSpinner size="lg" />
      </div>
    )
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" />
  }

  return (
    <AppLayout>
      <Outlet />
    </AppLayout>
  )
}

// Index route - redirect based on auth status
const indexRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/',
  component: IndexRedirect,
})

// Dashboard route
const dashboardRoute = createRoute({
  getParentRoute: () => appLayoutRoute,
  path: '/dashboard',
  component: DashboardPage,
})

// Users route (requires system:users:read)
const usersRoute = createRoute({
  getParentRoute: () => appLayoutRoute,
  path: '/users',
  component: withPermission(UsersPage, 'system:users:read'),
})

// Audit Logs route (requires system:audit:read)
const auditLogsRoute = createRoute({
  getParentRoute: () => appLayoutRoute,
  path: '/audit-logs',
  component: withPermission(AuditLogsPage, 'system:audit:read'),
})

// IP Restrictions route (requires system:ip-restrictions:read)
const ipRestrictionsRoute = createRoute({
  getParentRoute: () => appLayoutRoute,
  path: '/ip-restrictions',
  component: withPermission(IpRestrictionsPage, 'system:ip-restrictions:read'),
})

// Email route (requires any email permission - tabs check individual permissions)
const emailRoute = createRoute({
  getParentRoute: () => appLayoutRoute,
  path: '/email',
  component: EmailPage,
})

// Settings route
const settingsRoute = createRoute({
  getParentRoute: () => appLayoutRoute,
  path: '/settings',
  component: SettingsPage,
})

// Build the route tree
export const routeTree = rootRoute.addChildren([
  indexRoute,
  authLayoutRoute.addChildren([
    loginRoute,
    registerRoute,
    forbiddenRoute,
    inviteRoute,
    imprintRoute,
    privacyRoute,
    termsRoute,
    resetPasswordRoute,
    approveDeviceRoute,
  ]),
  appLayoutRoute.addChildren([
    dashboardRoute,
    usersRoute,
    auditLogsRoute,
    ipRestrictionsRoute,
    emailRoute,
    settingsRoute,
  ]),
])
