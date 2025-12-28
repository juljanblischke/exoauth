import { useEffect, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { useSearch } from '@tanstack/react-router'
import { User, Shield, Clock, AlertCircle } from 'lucide-react'
import { LoadingSpinner } from '@/components/shared/feedback'
import { Badge } from '@/components/ui/badge'
import { useAuth } from '@/contexts/auth-context'
import { AcceptInviteForm, useValidateInvite } from '@/features/auth'

interface InviteSearch {
  token?: string
}

export function InvitePage() {
  const { t } = useTranslation('auth')
  const { token } = useSearch({ strict: false }) as InviteSearch
  const { isAuthenticated, isLoading: isAuthLoading, logout } = useAuth()
  const logoutInitiatedRef = useRef(false)

  // Fetch invite details
  const { data: inviteData, isLoading: isInviteLoading } = useValidateInvite(token)

  // Auto-logout if user is authenticated (so they can accept invite with new account)
  useEffect(() => {
    if (isAuthenticated && !logoutInitiatedRef.current) {
      logoutInitiatedRef.current = true
      logout()
    }
  }, [isAuthenticated, logout])

  // Show loading while checking auth status or during logout
  if (isAuthLoading || isAuthenticated) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <LoadingSpinner size="lg" />
      </div>
    )
  }

  // Show error if no token provided
  if (!token) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background px-4">
        <div className="w-full max-w-md space-y-6">
          <div className="rounded-lg border bg-card p-6 shadow-sm">
            <div className="text-center space-y-2">
              <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-destructive/10">
                <AlertCircle className="h-6 w-6 text-destructive" />
              </div>
              <h1 className="text-xl font-semibold text-destructive">{t('invite.invalid')}</h1>
            </div>
          </div>
        </div>
      </div>
    )
  }

  // Show loading while fetching invite
  if (isInviteLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background px-4">
        <div className="w-full max-w-md space-y-6">
          <div className="rounded-lg border bg-card p-6 shadow-sm">
            <div className="flex flex-col items-center gap-4">
              <LoadingSpinner size="lg" />
              <p className="text-sm text-muted-foreground">{t('invite.loading')}</p>
            </div>
          </div>
        </div>
      </div>
    )
  }

  // Show error if invite is invalid
  if (inviteData && !inviteData.valid) {
    const errorMessage =
      inviteData.errorCode === 'INVITE_ALREADY_ACCEPTED' ? t('invite.alreadyAccepted') :
      inviteData.errorCode === 'INVITE_REVOKED' ? t('invite.revoked') :
      inviteData.errorCode === 'AUTH_INVITE_EXPIRED' ? t('invite.expired') :
      t('invite.invalid')

    return (
      <div className="flex min-h-screen items-center justify-center bg-background px-4">
        <div className="w-full max-w-md space-y-6">
          <div className="rounded-lg border bg-card p-6 shadow-sm">
            <div className="text-center space-y-4">
              <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-destructive/10">
                <AlertCircle className="h-6 w-6 text-destructive" />
              </div>
              <div className="space-y-2">
                <h1 className="text-xl font-semibold text-destructive">{errorMessage}</h1>
                {inviteData.invitedBy && (
                  <p className="text-sm text-muted-foreground">
                    {t('invite.invitedBy', { name: inviteData.invitedBy.fullName })}
                  </p>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>
    )
  }

  // Valid invite - show details and form
  const fullName = inviteData?.firstName && inviteData?.lastName
    ? `${inviteData.firstName} ${inviteData.lastName}`
    : inviteData?.firstName || inviteData?.email || ''

  return (
    <div className="flex min-h-screen items-center justify-center bg-background px-4 py-8">
      <div className="w-full max-w-md space-y-6">
        {/* Header */}
        <div className="space-y-2 text-center">
          <h1 className="text-3xl font-bold tracking-tight">{t('invite.title')}</h1>
          {fullName && (
            <p className="text-lg text-muted-foreground">
              {t('invite.welcome', { name: fullName })}
            </p>
          )}
        </div>

        {/* Invite Details Card */}
        {inviteData && (
          <div className="rounded-lg border bg-card p-4 shadow-sm space-y-4">
            {/* Invited By */}
            {inviteData.invitedBy && (
              <div className="flex items-center gap-3">
                <div className="flex h-9 w-9 items-center justify-center rounded-full bg-primary/10">
                  <User className="h-4 w-4 text-primary" />
                </div>
                <div>
                  <p className="text-sm text-muted-foreground">
                    {t('invite.invitedBy', { name: inviteData.invitedBy.fullName })}
                  </p>
                </div>
              </div>
            )}

            {/* Permissions */}
            {inviteData.permissions && inviteData.permissions.length > 0 && (
              <div className="space-y-2">
                <div className="flex items-center gap-2 text-sm text-muted-foreground">
                  <Shield className="h-4 w-4" />
                  <span>{t('invite.permissions')}</span>
                </div>
                <div className="flex flex-wrap gap-1.5 pl-6">
                  {inviteData.permissions.map((permission) => (
                    <Badge
                      key={permission.name}
                      variant="secondary"
                      className="font-mono text-xs"
                      title={permission.description}
                    >
                      {permission.name}
                    </Badge>
                  ))}
                </div>
              </div>
            )}

            {/* Expires At */}
            {inviteData.expiresAt && (
              <div className="flex items-center gap-3 text-sm text-muted-foreground">
                <Clock className="h-4 w-4" />
                <span>
                  {t('invite.expiresAt', {
                    date: new Date(inviteData.expiresAt).toLocaleDateString()
                  })}
                </span>
              </div>
            )}
          </div>
        )}

        {/* Accept Form Card */}
        <div className="rounded-lg border bg-card p-6 shadow-sm">
          <div className="mb-4">
            <p className="text-sm text-muted-foreground">{t('invite.setPassword')}</p>
          </div>
          <AcceptInviteForm token={token} />
        </div>
      </div>
    </div>
  )
}
