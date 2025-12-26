import { createRootRoute, createRoute, Outlet } from '@tanstack/react-router'
import { AppLayout } from '@/components/shared/layout'
import { NotFoundPage } from './not-found'
import { useAuth } from '@/contexts/auth-context'

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

// App layout - for authenticated pages (with sidebar)
const appLayoutRoute = createRoute({
  getParentRoute: () => rootRoute,
  id: 'app-layout',
  component: AppLayoutWrapper,
})

function AppLayoutWrapper() {
  return (
    <AppLayout>
      <Outlet />
    </AppLayout>
  )
}

// Dashboard route
const dashboardRoute = createRoute({
  getParentRoute: () => appLayoutRoute,
  path: '/dashboard',
  component: DashboardPage,
})

function DashboardPage() {
  const { user } = useAuth()

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Dashboard</h1>
        <p className="text-muted-foreground">
          Welcome back{user?.firstName ? `, ${user.firstName}` : ''}!
        </p>
      </div>
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <div className="rounded-lg border bg-card p-6">
          <h3 className="text-sm font-medium text-muted-foreground">Total Users</h3>
          <p className="text-2xl font-bold">--</p>
        </div>
        <div className="rounded-lg border bg-card p-6">
          <h3 className="text-sm font-medium text-muted-foreground">Active Sessions</h3>
          <p className="text-2xl font-bold">--</p>
        </div>
        <div className="rounded-lg border bg-card p-6">
          <h3 className="text-sm font-medium text-muted-foreground">Organizations</h3>
          <p className="text-2xl font-bold">--</p>
        </div>
        <div className="rounded-lg border bg-card p-6">
          <h3 className="text-sm font-medium text-muted-foreground">Projects</h3>
          <p className="text-2xl font-bold">--</p>
        </div>
      </div>
    </div>
  )
}

// Index route - redirect to dashboard
const indexRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/',
  component: () => {
    // Redirect to dashboard
    window.location.href = '/dashboard'
    return null
  },
})

// Users route (placeholder)
const usersRoute = createRoute({
  getParentRoute: () => appLayoutRoute,
  path: '/users',
  component: () => (
    <div>
      <h1 className="text-3xl font-bold tracking-tight">Users</h1>
      <p className="text-muted-foreground">Manage system users</p>
    </div>
  ),
})

// Permissions route (placeholder)
const permissionsRoute = createRoute({
  getParentRoute: () => appLayoutRoute,
  path: '/permissions',
  component: () => (
    <div>
      <h1 className="text-3xl font-bold tracking-tight">Permissions</h1>
      <p className="text-muted-foreground">Manage permissions</p>
    </div>
  ),
})

// Audit logs route (placeholder)
const auditLogsRoute = createRoute({
  getParentRoute: () => appLayoutRoute,
  path: '/audit-logs',
  component: () => (
    <div>
      <h1 className="text-3xl font-bold tracking-tight">Audit Logs</h1>
      <p className="text-muted-foreground">View audit logs</p>
    </div>
  ),
})

// Settings route (placeholder)
const settingsRoute = createRoute({
  getParentRoute: () => appLayoutRoute,
  path: '/settings',
  component: () => (
    <div>
      <h1 className="text-3xl font-bold tracking-tight">Settings</h1>
      <p className="text-muted-foreground">Application settings</p>
    </div>
  ),
})

// Build the route tree
export const routeTree = rootRoute.addChildren([
  indexRoute,
  authLayoutRoute,
  appLayoutRoute.addChildren([
    dashboardRoute,
    usersRoute,
    permissionsRoute,
    auditLogsRoute,
    settingsRoute,
  ]),
])
