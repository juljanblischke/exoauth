import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Plus } from 'lucide-react'
import { PageHeader } from '@/components/shared/layout'
import { ConfirmDialog } from '@/components/shared/feedback'
import { Button } from '@/components/ui/button'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { usePermissions } from '@/contexts/auth-context'
import {
  UsersTable,
  UserInviteModal,
  UserEditModal,
  UserDetailsSheet,
  UserPermissionsModal,
  InvitationsTable,
  InviteDetailsSheet,
  useDeleteUser,
  useRevokeInvite,
  useResendInvite,
  type SystemUserDto,
  type SystemInviteListDto,
} from '@/features/users'
import { getErrorMessage } from '@/lib/error-utils'

export function UsersPage() {
  const { t } = useTranslation()
  const { hasPermission } = usePermissions()
  const { mutate: deleteUser, isPending: isDeleting } = useDeleteUser()
  const { mutate: revokeInvite, isPending: isRevoking } = useRevokeInvite()
  const { mutate: resendInvite } = useResendInvite()

  const canCreate = hasPermission('system:users:create')
  const canUpdate = hasPermission('system:users:update')
  const canDelete = hasPermission('system:users:delete')

  // User modals/sheets state
  const [inviteOpen, setInviteOpen] = useState(false)
  const [editUser, setEditUser] = useState<SystemUserDto | null>(null)
  const [detailsUser, setDetailsUser] = useState<SystemUserDto | null>(null)
  const [permissionsUser, setPermissionsUser] = useState<SystemUserDto | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<SystemUserDto | null>(null)

  // Invite sheets/dialogs state
  const [detailsInvite, setDetailsInvite] = useState<SystemInviteListDto | null>(null)
  const [revokeTarget, setRevokeTarget] = useState<SystemInviteListDto | null>(null)

  // User handlers
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

  // Invite handlers
  const handleInviteRowClick = (invite: SystemInviteListDto) => setDetailsInvite(invite)
  const handleViewInviteDetails = (invite: SystemInviteListDto) => setDetailsInvite(invite)

  const handleResendInvite = (invite: SystemInviteListDto) => {
    resendInvite(invite.id, {
      onSuccess: () => {
        toast.success(t('users:invites.resend.success'))
      },
      onError: (error) => {
        const errorMessage = getErrorMessage(error, t)
        // Check if it's a cooldown error (429 rate limit)
        if (errorMessage.includes('429') || errorMessage.toLowerCase().includes('wait')) {
          toast.error(t('users:invites.resend.cooldown'))
        } else {
          toast.error(errorMessage)
        }
      },
    })
  }

  const handleRevokeInvite = (invite: SystemInviteListDto) => setRevokeTarget(invite)

  const confirmRevoke = () => {
    if (!revokeTarget) return
    revokeInvite(revokeTarget.id, {
      onSuccess: () => {
        toast.success(t('users:invites.revoke.success'))
        setRevokeTarget(null)
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

      <Tabs defaultValue="users" className="space-y-4">
        <TabsList>
          <TabsTrigger value="users">{t('users:tabs.users')}</TabsTrigger>
          <TabsTrigger value="invitations">{t('users:tabs.invitations')}</TabsTrigger>
        </TabsList>

        <TabsContent value="users" className="space-y-4">
          <UsersTable
            onEdit={canUpdate ? handleEdit : undefined}
            onPermissions={canUpdate ? handlePermissions : undefined}
            onDelete={canDelete ? handleDelete : undefined}
            onRowClick={handleRowClick}
          />
        </TabsContent>

        <TabsContent value="invitations" className="space-y-4">
          <InvitationsTable
            onViewDetails={handleViewInviteDetails}
            onResend={canUpdate ? handleResendInvite : undefined}
            onRevoke={canUpdate ? handleRevokeInvite : undefined}
            onRowClick={handleInviteRowClick}
          />
        </TabsContent>
      </Tabs>

      {/* User modals/sheets */}
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

      {/* Invite sheets/dialogs */}
      <InviteDetailsSheet
        open={!!detailsInvite}
        onOpenChange={(open) => !open && setDetailsInvite(null)}
        invite={detailsInvite}
        onResend={canUpdate ? handleResendInvite : undefined}
        onRevoke={canUpdate ? handleRevokeInvite : undefined}
      />

      <ConfirmDialog
        open={!!revokeTarget}
        onOpenChange={(open) => !open && setRevokeTarget(null)}
        title={t('users:invites.revoke.confirm.title')}
        description={t('users:invites.revoke.confirm.description')}
        confirmLabel={t('users:invites.actions.revoke')}
        onConfirm={confirmRevoke}
        isLoading={isRevoking}
        variant="destructive"
      />
    </div>
  )
}
