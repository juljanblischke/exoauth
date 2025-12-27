import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { Mail, Calendar, Clock, Shield, Edit, Check } from 'lucide-react'
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
import { useSystemUser } from '../hooks'
import type { SystemUserDto, PermissionDto } from '../types'

interface UserDetailsSheetProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  user: SystemUserDto | null
  onEdit?: (user: SystemUserDto) => void
  onPermissions?: (user: SystemUserDto) => void
}

// Group permissions by category
function groupPermissions(permissions: PermissionDto[]) {
  const grouped: Record<string, PermissionDto[]> = {}
  permissions.forEach((p) => {
    const category = p.category || 'other'
    if (!grouped[category]) grouped[category] = []
    grouped[category].push(p)
  })
  return Object.entries(grouped).map(([category, perms]) => ({
    category,
    permissions: perms,
  }))
}

export function UserDetailsSheet({
  open,
  onOpenChange,
  user,
  onEdit,
  onPermissions,
}: UserDetailsSheetProps) {
  const { t } = useTranslation()
  const { data: userDetails, isLoading } = useSystemUser(open ? user?.id : undefined)

  const permissionGroups = useMemo(() => {
    if (!userDetails?.permissions) return []
    return groupPermissions(userDetails.permissions)
  }, [userDetails])

  if (!user) return null

  // Use API data when available, fall back to prop for immediate display
  const displayUser = userDetails ?? user

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="w-full sm:max-w-md flex flex-col p-0">
        <SheetHeader className="sr-only">
          <SheetTitle>{t('users:userDetails')}</SheetTitle>
          <SheetDescription>{t('users:subtitle')}</SheetDescription>
        </SheetHeader>

        {/* Fixed top section */}
        <div className="space-y-6 p-6 pb-0 shrink-0">
          {/* User header */}
          <div className="flex items-start gap-4">
            {isLoading ? (
              <Skeleton className="h-12 w-12 rounded-full" />
            ) : (
              <UserAvatar name={displayUser.fullName} email={displayUser.email} size="lg" />
            )}
            <div className="flex-1 space-y-1">
              {isLoading ? (
                <>
                  <Skeleton className="h-6 w-32" />
                  <Skeleton className="h-4 w-48" />
                </>
              ) : (
                <>
                  <h2 className="text-xl font-semibold">{displayUser.fullName}</h2>
                  <div className="flex items-center gap-2 text-sm text-muted-foreground">
                    <Mail className="h-3.5 w-3.5" />
                    {displayUser.email}
                  </div>
                </>
              )}
            </div>
          </div>

          {/* Status badges */}
          <div className="flex flex-wrap gap-2">
            {isLoading ? (
              <>
                <Skeleton className="h-5 w-16" />
                <Skeleton className="h-5 w-20" />
              </>
            ) : (
              <>
                <StatusBadge
                  status={displayUser.isActive ? 'success' : 'error'}
                  label={displayUser.isActive ? t('users:status.active') : t('users:status.inactive')}
                />
                <StatusBadge
                  status={displayUser.emailVerified ? 'success' : 'warning'}
                  label={displayUser.emailVerified ? t('users:emailVerified.verified') : t('users:emailVerified.pending')}
                />
              </>
            )}
          </div>

          {/* Details */}
          <div className="space-y-4 rounded-lg border p-4">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <Clock className="h-4 w-4" />
                {t('users:fields.lastLogin')}
              </div>
              <div className="text-sm font-medium">
                {isLoading ? (
                  <Skeleton className="h-4 w-20" />
                ) : displayUser.lastLoginAt ? (
                  <RelativeTime date={displayUser.lastLoginAt} />
                ) : (
                  <span className="text-muted-foreground">-</span>
                )}
              </div>
            </div>

            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <Calendar className="h-4 w-4" />
                {t('users:fields.createdAt')}
              </div>
              <div className="text-sm font-medium">
                {isLoading ? (
                  <Skeleton className="h-4 w-20" />
                ) : (
                  <RelativeTime date={displayUser.createdAt} />
                )}
              </div>
            </div>
          </div>
        </div>

        {/* Scrollable permissions section */}
        <div className="flex-1 overflow-y-auto px-6 py-4 min-h-0">
          <div className="space-y-3">
            <div className="flex items-center gap-2">
              <Shield className="h-4 w-4 text-muted-foreground" />
              <h3 className="text-sm font-medium">{t('users:actions.permissions')}</h3>
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
                          key={permission.id}
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
        <div className="shrink-0 border-t p-6 flex gap-2">
          {onEdit && (
            <Button
              variant="outline"
              className="flex-1"
              onClick={() => {
                onEdit(user)
                onOpenChange(false)
              }}
            >
              <Edit className="mr-2 h-4 w-4" />
              {t('common:actions.edit')}
            </Button>
          )}
          {onPermissions && (
            <Button
              variant="outline"
              className="flex-1"
              onClick={() => {
                onPermissions(user)
                onOpenChange(false)
              }}
            >
              <Shield className="mr-2 h-4 w-4" />
              {t('users:actions.permissions')}
            </Button>
          )}
        </div>
      </SheetContent>
    </Sheet>
  )
}
