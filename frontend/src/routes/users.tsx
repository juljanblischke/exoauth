import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Plus } from 'lucide-react'
import { PageHeader } from '@/components/shared/layout'
import { ConfirmDialog } from '@/components/shared/feedback'
import { Button } from '@/components/ui/button'
import { usePermissions } from '@/contexts/auth-context'
import {
  UsersTable,
  UserInviteModal,
  UserEditModal,
  UserDetailsSheet,
  UserPermissionsModal,
  useDeleteUser,
  type SystemUserDto,
} from '@/features/users'
import { getErrorMessage } from '@/lib/error-utils'

export function UsersPage() {
  const { t } = useTranslation()
  const { hasPermission } = usePermissions()
  const { mutate: deleteUser, isPending: isDeleting } = useDeleteUser()

  const canCreate = hasPermission('system:users:create')
  const canUpdate = hasPermission('system:users:update')
  const canDelete = hasPermission('system:users:delete')

  const [inviteOpen, setInviteOpen] = useState(false)
  const [editUser, setEditUser] = useState<SystemUserDto | null>(null)
  const [detailsUser, setDetailsUser] = useState<SystemUserDto | null>(null)
  const [permissionsUser, setPermissionsUser] = useState<SystemUserDto | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<SystemUserDto | null>(null)

  const handleRowClick = (user: SystemUserDto) => setDetailsUser(user)
  const handleEdit = (user: SystemUserDto) => setEditUser(user)
  const handlePermissions = (user: SystemUserDto) => setPermissionsUser(user)
  const handleDelete = (user: SystemUserDto) => setDeleteTarget(user)

  const confirmDelete = () => {
    if (!deleteTarget) return
    deleteUser(deleteTarget.id, {
      onSuccess: () => {
        toast.success(t('users:messages.deleteSuccess'))
        setDeleteTarget(null)
      },
      onError: (error) => {
        toast.error(getErrorMessage(error, t))
      },
    })
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title={t('users:title')}
        description={t('users:subtitle')}
        actions={
          canCreate ? (
            <Button onClick={() => setInviteOpen(true)}>
              <Plus className="mr-2 h-4 w-4" />
              {t('users:inviteUser')}
            </Button>
          ) : undefined
        }
      />

      <UsersTable
        onEdit={canUpdate ? handleEdit : undefined}
        onPermissions={canUpdate ? handlePermissions : undefined}
        onDelete={canDelete ? handleDelete : undefined}
        onRowClick={handleRowClick}
      />

      <UserInviteModal open={inviteOpen} onOpenChange={setInviteOpen} />

      <UserEditModal
        open={!!editUser}
        onOpenChange={(open) => !open && setEditUser(null)}
        user={editUser}
      />

      <UserDetailsSheet
        open={!!detailsUser}
        onOpenChange={(open) => !open && setDetailsUser(null)}
        user={detailsUser}
        onEdit={canUpdate ? handleEdit : undefined}
        onPermissions={canUpdate ? handlePermissions : undefined}
      />

      <UserPermissionsModal
        open={!!permissionsUser}
        onOpenChange={(open) => !open && setPermissionsUser(null)}
        user={permissionsUser}
      />

      <ConfirmDialog
        open={!!deleteTarget}
        onOpenChange={(open) => !open && setDeleteTarget(null)}
        title={t('users:confirmDelete.title')}
        description={t('users:confirmDelete.message', { name: deleteTarget?.fullName })}
        confirmLabel={t('common:actions.delete')}
        onConfirm={confirmDelete}
        isLoading={isDeleting}
        variant="destructive"
      />
    </div>
  )
}
