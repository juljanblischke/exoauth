import { useState, useMemo, useCallback, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import {
  Edit,
  Shield,
  KeyRound,
  Unlock,
  UserX,
  UserCheck,
  UserMinus,
} from 'lucide-react'
import { type SortingState } from '@tanstack/react-table'
import { DataTable } from '@/components/shared/data-table'
import { SelectFilter, type SelectFilterOption } from '@/components/shared/form'
import { RelativeTime } from '@/components/shared/relative-time'
import { StatusBadge } from '@/components/shared/status-badge'
import { ConfirmDialog, TypeConfirmDialog } from '@/components/shared/feedback'
import { Checkbox } from '@/components/ui/checkbox'
import { Label } from '@/components/ui/label'
import { useDebounce } from '@/hooks'
import { useAuth, usePermissions } from '@/contexts/auth-context'
import { useSystemPermissions } from '@/features/permissions'
import {
  useSystemUsers,
  useResetUserMfa,
  useUnlockUser,
  useDeactivateUser,
  useActivateUser,
  useAnonymizeUser,
} from '../hooks'
import { useUsersColumns } from './users-table-columns'
import type { SystemUserDto } from '../types'
import type { RowAction } from '@/types/table'

// Map column IDs to backend field names
const sortFieldMap: Record<string, string> = {
  name: 'fullName',
  lastLogin: 'lastLoginAt',
  createdAt: 'createdAt',
}

interface UsersTableProps {
  onEdit?: (user: SystemUserDto) => void
  onPermissions?: (user: SystemUserDto) => void
  onRowClick?: (user: SystemUserDto) => void
}

export function UsersTable({ onEdit, onPermissions, onRowClick }: UsersTableProps) {
  const { t } = useTranslation()
  const { user: currentUser } = useAuth()
  const { hasPermission } = usePermissions()
  const [search, setSearch] = useState('')
  const [sorting, setSorting] = useState<SortingState>([])
  const [permissionFilters, setPermissionFilters] = useState<string[]>([])
  const debouncedSearch = useDebounce(search, 300)

  // User status filters
  const [statusFilter, setStatusFilter] = useState<string | undefined>(undefined)
  const [lockedFilter, setLockedFilter] = useState<string | undefined>(undefined)
  const [mfaFilter, setMfaFilter] = useState<string | undefined>(undefined)
  const [showAnonymized, setShowAnonymized] = useState(false)

  // Admin action dialog states
  const [selectedUser, setSelectedUser] = useState<SystemUserDto | null>(null)
  const [resetMfaDialogOpen, setResetMfaDialogOpen] = useState(false)
  const [unlockDialogOpen, setUnlockDialogOpen] = useState(false)
  const [deactivateDialogOpen, setDeactivateDialogOpen] = useState(false)
  const [activateDialogOpen, setActivateDialogOpen] = useState(false)
  const [anonymizeDialogOpen, setAnonymizeDialogOpen] = useState(false)

  // Admin action mutations
  const { mutate: resetMfa, isPending: isResettingMfa } = useResetUserMfa()
  const { mutate: unlockUser, isPending: isUnlocking } = useUnlockUser()
  const { mutate: deactivateUser, isPending: isDeactivating } = useDeactivateUser()
  const { mutate: activateUser, isPending: isActivating } = useActivateUser()
  const { mutate: anonymizeUser, isPending: isAnonymizing } = useAnonymizeUser()

  // Check if user can read permissions (to show the filter)
  const canReadPermissions = hasPermission('system:permissions:read')

  // Admin permission checks
  const canResetMfa = hasPermission('system:users:mfa:reset')
  const canUnlock = hasPermission('system:users:unlock')
  const canDeactivate = hasPermission('system:users:deactivate')
  const canActivate = hasPermission('system:users:activate')
  const canAnonymize = hasPermission('system:users:anonymize')

  // Fetch permissions for filter options (only if user has permission)
  const { data: permissionGroups } = useSystemPermissions()

  // Convert sorting state to backend format (e.g., "fullName:asc,createdAt:desc")
  const sortParam = useMemo(() => {
    if (sorting.length === 0) return undefined
    return sorting
      .map((s) => {
        const field = sortFieldMap[s.id] || s.id
        return `${field}:${s.desc ? 'desc' : 'asc'}`
      })
      .join(',')
  }, [sorting])

  // Convert filter strings to boolean values for API
  const isActiveParam = statusFilter === 'active' ? true : statusFilter === 'inactive' ? false : undefined
  const isLockedParam = lockedFilter === 'locked' ? true : lockedFilter === 'unlocked' ? false : undefined
  const mfaEnabledParam = mfaFilter === 'enabled' ? true : mfaFilter === 'disabled' ? false : undefined

  const {
    data,
    isLoading,
    isFetching,
    fetchNextPage,
    hasNextPage,
  } = useSystemUsers({
    search: debouncedSearch || undefined,
    sort: sortParam,
    permissionIds: permissionFilters.length > 0 ? permissionFilters : undefined,
    isActive: isActiveParam,
    isAnonymized: showAnonymized || undefined,
    isLocked: isLockedParam,
    mfaEnabled: mfaEnabledParam,
  })

  const users = useMemo(
    () => data?.pages.flatMap((page) => page.users) ?? [],
    [data]
  )

  // Admin action handlers
  const handleResetMfa = useCallback((user: SystemUserDto) => {
    setSelectedUser(user)
    setResetMfaDialogOpen(true)
  }, [])

  const handleUnlock = useCallback((user: SystemUserDto) => {
    setSelectedUser(user)
    setUnlockDialogOpen(true)
  }, [])

  const handleDeactivate = useCallback((user: SystemUserDto) => {
    setSelectedUser(user)
    setDeactivateDialogOpen(true)
  }, [])

  const handleActivate = useCallback((user: SystemUserDto) => {
    setSelectedUser(user)
    setActivateDialogOpen(true)
  }, [])

  const handleAnonymize = useCallback((user: SystemUserDto) => {
    setSelectedUser(user)
    setAnonymizeDialogOpen(true)
  }, [])

  const confirmResetMfa = useCallback(() => {
    if (!selectedUser) return
    resetMfa(
      { userId: selectedUser.id },
      {
        onSuccess: () => {
          toast.success(t('users:admin.mfa.resetSuccess'))
          setResetMfaDialogOpen(false)
          setSelectedUser(null)
        },
      }
    )
  }, [selectedUser, resetMfa, t])

  const confirmUnlock = useCallback(() => {
    if (!selectedUser) return
    unlockUser(
      { userId: selectedUser.id },
      {
        onSuccess: () => {
          toast.success(t('users:admin.unlock.success'))
          setUnlockDialogOpen(false)
          setSelectedUser(null)
        },
      }
    )
  }, [selectedUser, unlockUser, t])

  const confirmDeactivate = useCallback(() => {
    if (!selectedUser) return
    deactivateUser(selectedUser.id, {
      onSuccess: () => {
        toast.success(t('users:admin.deactivate.success'))
        setDeactivateDialogOpen(false)
        setSelectedUser(null)
      },
    })
  }, [selectedUser, deactivateUser, t])

  const confirmActivate = useCallback(() => {
    if (!selectedUser) return
    activateUser(selectedUser.id, {
      onSuccess: () => {
        toast.success(t('users:admin.activate.success'))
        setActivateDialogOpen(false)
        setSelectedUser(null)
      },
    })
  }, [selectedUser, activateUser, t])

  const confirmAnonymize = useCallback(() => {
    if (!selectedUser) return
    anonymizeUser(selectedUser.id, {
      onSuccess: () => {
        toast.success(t('users:admin.anonymize.success'))
        setAnonymizeDialogOpen(false)
        setSelectedUser(null)
      },
    })
  }, [selectedUser, anonymizeUser, t])

  // Admin actions (used in both table and mobile card)
  const adminActions: RowAction<SystemUserDto>[] = useMemo(() => {
    const actions: RowAction<SystemUserDto>[] = []
    const hasBasicActions = onEdit || onPermissions

    // Reset MFA action
    if (canResetMfa) {
      actions.push({
        label: t('users:admin.mfa.reset'),
        icon: <KeyRound className="h-4 w-4" />,
        onClick: handleResetMfa,
        hidden: (user) => !user.mfaEnabled || user.isAnonymized,
        separator: hasBasicActions,
      })
    }

    // Unlock user action
    if (canUnlock) {
      actions.push({
        label: t('users:admin.unlock.action'),
        icon: <Unlock className="h-4 w-4" />,
        onClick: handleUnlock,
        hidden: (user) => !user.isLocked || user.isAnonymized,
      })
    }

    // Deactivate user action
    if (canDeactivate) {
      actions.push({
        label: t('users:admin.deactivate.action'),
        icon: <UserX className="h-4 w-4" />,
        onClick: handleDeactivate,
        hidden: (user) => !user.isActive || user.isAnonymized || user.id === currentUser?.id,
        variant: 'destructive',
      })
    }

    // Activate user action
    if (canActivate) {
      actions.push({
        label: t('users:admin.activate.action'),
        icon: <UserCheck className="h-4 w-4" />,
        onClick: handleActivate,
        hidden: (user) => user.isActive || user.isAnonymized,
      })
    }

    // Anonymize user action (separator before destructive action)
    if (canAnonymize) {
      actions.push({
        label: t('users:admin.anonymize.action'),
        icon: <UserMinus className="h-4 w-4" />,
        onClick: handleAnonymize,
        hidden: (user) => user.isAnonymized || user.id === currentUser?.id,
        variant: 'destructive',
        separator: true,
      })
    }

    return actions
  }, [
    t,
    onEdit,
    onPermissions,
    currentUser?.id,
    canResetMfa,
    canUnlock,
    canDeactivate,
    canActivate,
    canAnonymize,
    handleResetMfa,
    handleUnlock,
    handleDeactivate,
    handleActivate,
    handleAnonymize,
  ])

  // Columns with admin actions for desktop table
  const columns = useUsersColumns({
    onEdit,
    onPermissions,
    currentUserId: currentUser?.id,
    rowActions: adminActions,
  })

  // Full row actions for mobile card (includes edit/permissions + admin)
  const rowActions: RowAction<SystemUserDto>[] = useMemo(() => {
    const actions: RowAction<SystemUserDto>[] = []

    if (onEdit) {
      actions.push({
        label: t('common:actions.edit'),
        icon: <Edit className="h-4 w-4" />,
        onClick: onEdit,
        hidden: (user) => user.isAnonymized,
      })
    }

    if (onPermissions) {
      actions.push({
        label: t('users:actions.permissions'),
        icon: <Shield className="h-4 w-4" />,
        onClick: onPermissions,
        hidden: (user) => user.isAnonymized,
      })
    }

    // Add admin actions
    actions.push(...adminActions)

    return actions
  }, [t, onEdit, onPermissions, adminActions])

  const handleLoadMore = useCallback(() => {
    if (hasNextPage && !isFetching) {
      fetchNextPage()
    }
  }, [hasNextPage, isFetching, fetchNextPage])

  // Build permission filter options from all permission groups
  const permissionOptions: SelectFilterOption[] = useMemo(() => {
    if (!permissionGroups) return []
    return permissionGroups.flatMap((group) =>
      group.permissions.map((permission) => ({
        label: `${permission.name}`,
        value: permission.id,
      }))
    )
  }, [permissionGroups])

  // Status filter options
  const statusOptions: SelectFilterOption[] = useMemo(
    () => [
      { label: t('users:status.active'), value: 'active' },
      { label: t('users:status.inactive'), value: 'inactive' },
    ],
    [t]
  )

  // Locked filter options
  const lockedOptions: SelectFilterOption[] = useMemo(
    () => [
      { label: t('users:filters.locked'), value: 'locked' },
      { label: t('users:filters.unlocked'), value: 'unlocked' },
    ],
    [t]
  )

  // MFA filter options
  const mfaOptions: SelectFilterOption[] = useMemo(
    () => [
      { label: t('users:filters.mfaEnabled'), value: 'enabled' },
      { label: t('users:filters.mfaDisabled'), value: 'disabled' },
    ],
    [t]
  )

  // Filter content with all filters
  const filterContent = (
    <div className="flex flex-wrap items-center gap-2">
      <SelectFilter
        label={t('users:filters.status')}
        options={statusOptions}
        value={statusFilter}
        onChange={setStatusFilter}
      />
      <SelectFilter
        label={t('users:filters.accountLock')}
        options={lockedOptions}
        value={lockedFilter}
        onChange={setLockedFilter}
      />
      <SelectFilter
        label={t('users:filters.mfa')}
        options={mfaOptions}
        value={mfaFilter}
        onChange={setMfaFilter}
      />
      {canReadPermissions && permissionOptions.length > 0 && (
        <SelectFilter
          label={t('users:actions.permissions')}
          options={permissionOptions}
          multiple
          values={permissionFilters}
          onValuesChange={setPermissionFilters}
        />
      )}
      <div className="flex items-center gap-2">
        <Checkbox
          id="show-anonymized"
          checked={showAnonymized}
          onCheckedChange={(checked) => setShowAnonymized(checked === true)}
        />
        <Label htmlFor="show-anonymized" className="text-sm font-normal cursor-pointer">
          {t('users:filters.showAnonymized')}
        </Label>
      </div>
    </div>
  )

  return (
    <>
      <DataTable
        columns={columns}
        data={users}
        isLoading={isLoading}
        isFetching={isFetching}
        hasMore={hasNextPage}
        onLoadMore={handleLoadMore}
        searchPlaceholder={t('users:search.placeholder', 'Search users...')}
        searchValue={search}
        onSearch={setSearch}
        initialSorting={sorting}
        onSortingChange={setSorting}
        toolbarContent={filterContent}
        emptyState={{
          title: t('users:empty.title'),
          description: t('users:empty.message'),
        }}
        rowActions={rowActions}
        onRowClick={onRowClick}
        mobileCard={{
          primaryField: 'fullName',
          secondaryField: 'email',
          avatar: (row) => ({
            name: row.fullName,
            email: row.email,
          }),
          tertiaryFields: [
            {
              key: 'isActive',
              render: (value): ReactNode => (
                <StatusBadge
                  status={value ? 'success' : 'error'}
                  label={value ? t('users:status.active') : t('users:status.inactive')}
                />
              ),
            },
            {
              key: 'lastLoginAt',
              label: t('users:fields.lastLogin'),
              render: (value): ReactNode =>
                value ? <RelativeTime date={value as string} /> : '-',
            },
          ],
        }}
      />

      {/* Reset MFA Confirmation Dialog */}
      <ConfirmDialog
        open={resetMfaDialogOpen}
        onOpenChange={setResetMfaDialogOpen}
        title={t('users:admin.mfa.confirmTitle')}
        description={t('users:admin.mfa.confirmDescription', { name: selectedUser?.fullName })}
        confirmLabel={t('users:admin.mfa.reset')}
        variant="destructive"
        onConfirm={confirmResetMfa}
        isLoading={isResettingMfa}
      />

      {/* Unlock User Confirmation Dialog */}
      <ConfirmDialog
        open={unlockDialogOpen}
        onOpenChange={setUnlockDialogOpen}
        title={t('users:admin.unlock.confirmTitle')}
        description={t('users:admin.unlock.confirmDescription', { name: selectedUser?.fullName })}
        confirmLabel={t('users:admin.unlock.action')}
        onConfirm={confirmUnlock}
        isLoading={isUnlocking}
      />

      {/* Deactivate User Confirmation Dialog */}
      <ConfirmDialog
        open={deactivateDialogOpen}
        onOpenChange={setDeactivateDialogOpen}
        title={t('users:admin.deactivate.confirmTitle')}
        description={t('users:admin.deactivate.confirmDescription', { name: selectedUser?.fullName })}
        confirmLabel={t('users:admin.deactivate.action')}
        variant="destructive"
        onConfirm={confirmDeactivate}
        isLoading={isDeactivating}
      />

      {/* Activate User Confirmation Dialog */}
      <ConfirmDialog
        open={activateDialogOpen}
        onOpenChange={setActivateDialogOpen}
        title={t('users:admin.activate.confirmTitle')}
        description={t('users:admin.activate.confirmDescription', { name: selectedUser?.fullName })}
        confirmLabel={t('users:admin.activate.action')}
        onConfirm={confirmActivate}
        isLoading={isActivating}
      />

      {/* Anonymize User Type Confirmation Dialog */}
      <TypeConfirmDialog
        open={anonymizeDialogOpen}
        onOpenChange={setAnonymizeDialogOpen}
        title={t('users:admin.anonymize.confirmTitle')}
        description={t('users:admin.anonymize.confirmDescription', { name: selectedUser?.fullName })}
        confirmLabel={t('users:admin.anonymize.action')}
        confirmText={selectedUser?.email ?? ''}
        placeholder={t('users:admin.anonymize.placeholder')}
        onConfirm={confirmAnonymize}
        isLoading={isAnonymizing}
      />
    </>
  )
}
