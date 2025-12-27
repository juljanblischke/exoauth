import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { Mail, Calendar, Clock, Shield, Send, XCircle, User, Check } from 'lucide-react'
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { UserAvatar } from '@/components/shared/user-avatar'
import { StatusBadge } from '@/components/shared/status-badge'
import { RelativeTime } from '@/components/shared/relative-time'
import { useSystemInvite } from '../hooks'
import type { SystemInviteListDto, InviteStatus, InvitePermissionDto } from '../types'

// Map invite status to StatusBadge status
const statusMap: Record<InviteStatus, 'success' | 'warning' | 'error' | 'neutral'> = {
  pending: 'warning',
  accepted: 'success',
  expired: 'neutral',
  revoked: 'error',
}

interface InviteDetailsSheetProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  invite: SystemInviteListDto | null
  onResend?: (invite: SystemInviteListDto) => void
  onRevoke?: (invite: SystemInviteListDto) => void
}

// Group permissions by extracting category from permission name (e.g., "system:users:read" -> "system")
function groupPermissions(permissions: InvitePermissionDto[]) {
  const grouped: Record<string, InvitePermissionDto[]> = {}
  permissions.forEach((p) => {
    const category = p.name.split(':')[0] || 'other'
    if (!grouped[category]) grouped[category] = []
    grouped[category].push(p)
  })
  return Object.entries(grouped).map(([category, perms]) => ({
    category,
    permissions: perms,
  }))
}

export function InviteDetailsSheet({
  open,
  onOpenChange,
  invite,
  onResend,
  onRevoke,
}: InviteDetailsSheetProps) {
  const { t } = useTranslation()
  const { data: inviteDetails, isLoading } = useSystemInvite(open ? invite?.id ?? null : null)

  const permissionGroups = useMemo(() => {
    if (!inviteDetails?.permissions) return []
    return groupPermissions(inviteDetails.permissions)
  }, [inviteDetails])

  if (!invite) return null

  // Use API data when available, fall back to prop for immediate display
  const displayInvite = inviteDetails ?? invite
  const fullName = `${displayInvite.firstName} ${displayInvite.lastName}`.trim()
  const canResend = displayInvite.status === 'pending' || displayInvite.status === 'expired'
  const canRevoke = displayInvite.status === 'pending'

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="w-full sm:max-w-md flex flex-col p-0">
        <SheetHeader className="sr-only">
          <SheetTitle>{t('users:invites.details.title')}</SheetTitle>
          <SheetDescription>{t('users:invites.title')}</SheetDescription>
        </SheetHeader>

        {/* Fixed top section */}
        <div className="space-y-6 p-6 pb-0 shrink-0">
          {/* Invite header */}
          <div className="flex items-start gap-4">
            {isLoading ? (
              <Skeleton className="h-12 w-12 rounded-full" />
            ) : (
              <UserAvatar name={fullName} email={displayInvite.email} size="lg" />
            )}
            <div className="flex-1 space-y-1">
              {isLoading ? (
                <>
                  <Skeleton className="h-6 w-32" />
                  <Skeleton className="h-4 w-48" />
                </>
              ) : (
                <>
                  <h2 className="text-xl font-semibold">{fullName}</h2>
                  <div className="flex items-center gap-2 text-sm text-muted-foreground">
                    <Mail className="h-3.5 w-3.5" />
                    {displayInvite.email}
                  </div>
                </>
              )}
            </div>
          </div>

          {/* Status badge */}
          <div className="flex flex-wrap gap-2">
            {isLoading ? (
              <Skeleton className="h-5 w-20" />
            ) : (
              <StatusBadge
                status={statusMap[displayInvite.status]}
                label={t(`users:invites.status.${displayInvite.status}`)}
              />
            )}
          </div>

          {/* Details */}
          <div className="space-y-4 rounded-lg border p-4">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <User className="h-4 w-4" />
                {t('users:invites.fields.invitedBy')}
              </div>
              <div className="text-sm font-medium">
                {isLoading ? (
                  <Skeleton className="h-4 w-24" />
                ) : (
                  displayInvite.invitedBy.fullName
                )}
              </div>
            </div>

            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <Calendar className="h-4 w-4" />
                {t('users:invites.fields.createdAt')}
              </div>
              <div className="text-sm font-medium">
                {isLoading ? (
                  <Skeleton className="h-4 w-20" />
                ) : (
                  <RelativeTime date={displayInvite.createdAt} />
                )}
              </div>
            </div>

            {displayInvite.status === 'pending' && (
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2 text-sm text-muted-foreground">
                  <Clock className="h-4 w-4" />
                  {t('users:invites.fields.expiresAt')}
                </div>
                <div className="text-sm font-medium">
                  {isLoading ? (
                    <Skeleton className="h-4 w-20" />
                  ) : (
                    <RelativeTime date={displayInvite.expiresAt} />
                  )}
                </div>
              </div>
            )}

            {displayInvite.acceptedAt && (
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2 text-sm text-muted-foreground">
                  <Check className="h-4 w-4" />
                  {t('users:invites.fields.acceptedAt')}
                </div>
                <div className="text-sm font-medium">
                  <RelativeTime date={displayInvite.acceptedAt} />
                </div>
              </div>
            )}

            {displayInvite.revokedAt && (
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2 text-sm text-muted-foreground">
                  <XCircle className="h-4 w-4" />
                  {t('users:invites.fields.revokedAt')}
                </div>
                <div className="text-sm font-medium">
                  <RelativeTime date={displayInvite.revokedAt} />
                </div>
              </div>
            )}

            {displayInvite.resentAt && (
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2 text-sm text-muted-foreground">
                  <Send className="h-4 w-4" />
                  {t('users:invites.fields.resentAt')}
                </div>
                <div className="text-sm font-medium">
                  <RelativeTime date={displayInvite.resentAt} />
                </div>
              </div>
            )}
          </div>
        </div>

        {/* Scrollable permissions section */}
        <div className="flex-1 overflow-y-auto px-6 py-4 min-h-0">
          <div className="space-y-3">
            <div className="flex items-center gap-2">
              <Shield className="h-4 w-4 text-muted-foreground" />
              <h3 className="text-sm font-medium">{t('users:invites.details.permissions')}</h3>
            </div>

            {isLoading ? (
              <div className="space-y-2">
                <Skeleton className="h-4 w-full" />
                <Skeleton className="h-4 w-3/4" />
                <Skeleton className="h-4 w-1/2" />
              </div>
            ) : permissionGroups.length === 0 ? (
              <p className="text-sm text-muted-foreground">
                {t('users:permissions.none')}
              </p>
            ) : (
              <div className="space-y-4">
                {permissionGroups.map((group) => (
                  <div key={group.category} className="space-y-2">
                    <h4 className="text-xs font-medium uppercase text-muted-foreground">
                      {group.category}
                    </h4>
                    <div className="grid grid-cols-1 gap-1">
                      {group.permissions.map((permission) => (
                        <div
                          key={permission.name}
                          className="flex items-center gap-2 text-sm py-1"
                        >
                          <Check className="h-3.5 w-3.5 shrink-0 text-emerald-500" />
                          <span>{permission.name}</span>
                        </div>
                      ))}
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>

        {/* Fixed bottom actions */}
        {(onResend || onRevoke) && (
          <div className="shrink-0 border-t p-6 flex gap-2">
            {onResend && (
              <Button
                variant="outline"
                className="flex-1"
                disabled={!canResend}
                onClick={() => {
                  onResend(invite)
                  onOpenChange(false)
                }}
              >
                <Send className="mr-2 h-4 w-4" />
                {t('users:invites.actions.resend')}
              </Button>
            )}
            {onRevoke && (
              <Button
                variant="destructive"
                className="flex-1"
                disabled={!canRevoke}
                onClick={() => {
                  onRevoke(invite)
                  onOpenChange(false)
                }}
              >
                <XCircle className="mr-2 h-4 w-4" />
                {t('users:invites.actions.revoke')}
              </Button>
            )}
          </div>
        )}
      </SheetContent>
    </Sheet>
  )
}
