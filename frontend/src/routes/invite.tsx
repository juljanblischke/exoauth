import { useTranslation } from 'react-i18next'
import { Navigate, useSearch } from '@tanstack/react-router'
import { LoadingSpinner } from '@/components/shared/feedback'
import { useAuth } from '@/contexts/auth-context'
import { AcceptInviteForm } from '@/features/auth'

interface InviteSearch {
  token?: string
}

export function InvitePage() {
  const { t } = useTranslation('auth')
  const { token } = useSearch({ strict: false }) as InviteSearch
  const { isAuthenticated, isLoading } = useAuth()

  // Show loading while checking auth status
  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <LoadingSpinner size="lg" />
      </div>
    )
  }

  // Redirect to dashboard if already authenticated
  if (isAuthenticated) {
    return <Navigate to="/dashboard" />
  }

  // Show error if no token provided
  if (!token) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background px-4">
        <div className="w-full max-w-md space-y-6">
          <div className="rounded-lg border bg-card p-6 shadow-sm">
            <div className="text-center space-y-2">
              <h1 className="text-xl font-semibold text-destructive">{t('invite.invalid')}</h1>
              <p className="text-sm text-muted-foreground">
                {t('invite.expired')}
              </p>
            </div>
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-background px-4">
      <div className="w-full max-w-md space-y-6">
        <div className="space-y-2 text-center">
          <h1 className="text-3xl font-bold tracking-tight">{t('invite.title')}</h1>
          <p className="text-muted-foreground">{t('invite.setPassword')}</p>
        </div>

        <div className="rounded-lg border bg-card p-6 shadow-sm">
          <AcceptInviteForm token={token} />
        </div>
      </div>
    </div>
  )
}
