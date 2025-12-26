import { useState, useCallback, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Loader2 } from 'lucide-react'

import { Button } from '@/components/ui/button'
import { Checkbox } from '@/components/ui/checkbox'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { LoadingSpinner } from '@/components/shared/feedback'
import { getErrorMessage } from '@/lib/error-utils'

import { useSystemUser, useUpdatePermissions } from '../hooks'
import { useSystemPermissions } from '@/features/permissions'
import type { SystemUserDto } from '../types'

interface UserPermissionsModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  user: SystemUserDto | null
}

export function UserPermissionsModal({
  open,
  onOpenChange,
  user,
}: UserPermissionsModalProps) {
  const { t } = useTranslation()
  // Track user modifications separately from initial data
  const [permissionOverrides, setPermissionOverrides] = useState<Record<string, boolean>>({})

  const { data: userDetail, isLoading: userLoading } = useSystemUser(
    open ? user?.id : undefined
  )
  const { data: permissionGroups, isLoading: permissionsLoading } =
    useSystemPermissions()
  const { mutate: updatePermissions, isPending } = useUpdatePermissions()

  // Derive selected permissions from server data + local overrides
  const selectedPermissions = useMemo(() => {
    const serverPermissions = new Set(userDetail?.permissions.map((p) => p.id) ?? [])
    // Apply local overrides
    for (const [permId, isSelected] of Object.entries(permissionOverrides)) {
      if (isSelected) {
        serverPermissions.add(permId)
      } else {
        serverPermissions.delete(permId)
      }
    }
    return Array.from(serverPermissions)
  }, [userDetail?.permissions, permissionOverrides])

  // Handle open change with reset
  const handleOpenChange = useCallback(
    (newOpen: boolean) => {
      if (!newOpen) {
        setPermissionOverrides({})
      }
      onOpenChange(newOpen)
    },
    [onOpenChange]
  )

  const handlePermissionToggle = (permissionId: string) => {
    const isCurrentlySelected = selectedPermissions.includes(permissionId)
    setPermissionOverrides((prev) => ({
      ...prev,
      [permissionId]: !isCurrentlySelected,
    }))
  }

  const handleSave = () => {
    if (!user) return

    updatePermissions(
      {
        id: user.id,
        data: { permissionIds: selectedPermissions },
      },
      {
        onSuccess: () => {
          toast.success(t('users:permissions.success', 'Permissions updated successfully'))
          onOpenChange(false)
        },
        onError: (error) => {
          toast.error(getErrorMessage(error, t))
        },
      }
    )
  }

  const isLoading = userLoading || permissionsLoading

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="max-w-lg">
        <DialogHeader>
          <DialogTitle>{t('users:permissions.title', 'Manage Permissions')}</DialogTitle>
          <DialogDescription>
            {user?.fullName} ({user?.email})
          </DialogDescription>
        </DialogHeader>

        {isLoading ? (
          <div className="flex justify-center py-8">
            <LoadingSpinner size="lg" />
          </div>
        ) : (
          <div className="max-h-96 overflow-y-auto space-y-4 py-4">
            {permissionGroups?.map((group) => (
              <div key={group.category} className="space-y-2">
                <h4 className="text-sm font-medium capitalize border-b pb-1">
                  {group.category}
                </h4>
                <div className="space-y-2 pl-2">
                  {group.permissions.map((permission) => (
                    <div
                      key={permission.id}
                      className="flex items-start gap-2"
                    >
                      <Checkbox
                        id={`perm-${permission.id}`}
                        checked={selectedPermissions.includes(permission.id)}
                        onCheckedChange={() =>
                          handlePermissionToggle(permission.id)
                        }
                      />
                      <div className="flex flex-col">
                        <label
                          htmlFor={`perm-${permission.id}`}
                          className="text-sm font-medium cursor-pointer"
                        >
                          {permission.name}
                        </label>
                        <span className="text-xs text-muted-foreground">
                          {permission.description}
                        </span>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            ))}
          </div>
        )}

        <DialogFooter>
          <Button
            type="button"
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={isPending}
          >
            {t('common:actions.cancel')}
          </Button>
          <Button onClick={handleSave} disabled={isPending || isLoading}>
            {isPending ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                {t('common:actions.loading')}
              </>
            ) : (
              t('common:actions.save')
            )}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
