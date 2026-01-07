import { useState, useMemo, useCallback, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { type SortingState } from '@tanstack/react-table'
import { Plus, Pencil, Trash2, Shield } from 'lucide-react'
import { DataTable } from '@/components/shared/data-table'
import type { RowAction } from '@/types/table'
import { SelectFilter } from '@/components/shared/form'
import { ConfirmDialog } from '@/components/shared/feedback'
import { RelativeTime } from '@/components/shared/relative-time'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { useAuth } from '@/contexts'
import { useDebounce } from '@/hooks'
import { UserDetailsSheet } from '@/features/users/components/user-details-sheet'
import { useSystemUsers } from '@/features/users/hooks'
import type { SystemUserDto } from '@/features/users/types'
import { useIpRestrictions, useCreateIpRestriction, useUpdateIpRestriction, useDeleteIpRestriction } from '../hooks'
import { useIpRestrictionsColumns } from './ip-restrictions-table-columns'
import { IpRestrictionDetailsSheet } from './ip-restriction-details-sheet'
import { CreateIpRestrictionModal } from './create-ip-restriction-modal'
import { EditIpRestrictionModal } from './edit-ip-restriction-modal'
import { IpRestrictionTypeBadge } from './ip-restriction-type-badge'
import { IpRestrictionSourceBadge } from './ip-restriction-source-badge'
import type { SelectFilterOption } from '@/components/shared/form'
import { IpRestrictionType, IpRestrictionSource } from '../types'
import type { IpRestrictionDto, CreateIpRestrictionRequest, UpdateIpRestrictionRequest } from '../types'

const sortFieldMap: Record<string, string> = {
  createdAt: 'createdAt',
}

export function IpRestrictionsTable() {
  const { t } = useTranslation()
  const [sorting, setSorting] = useState<SortingState>([{ id: 'createdAt', desc: true }])
  const [typeFilter, setTypeFilter] = useState<string[]>([])
  const [sourceFilter, setSourceFilter] = useState<string[]>([])
  const [searchValue, setSearchValue] = useState('')
  const debouncedSearch = useDebounce(searchValue, 300)

  // Modal states
  const [createModalOpen, setCreateModalOpen] = useState(false)
  const [editModalOpen, setEditModalOpen] = useState(false)
  const [detailsSheetOpen, setDetailsSheetOpen] = useState(false)
  const [selectedRestriction, setSelectedRestriction] = useState<IpRestrictionDto | null>(null)
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false)
  const [restrictionToDelete, setRestrictionToDelete] = useState<IpRestrictionDto | null>(null)
  const [userSheetOpen, setUserSheetOpen] = useState(false)
  const [selectedUser, setSelectedUser] = useState<SystemUserDto | null>(null)

  // Mutations
  const createMutation = useCreateIpRestriction()
  const updateMutation = useUpdateIpRestriction()
  const deleteMutation = useDeleteIpRestriction()

  // Permissions
  const { hasPermission } = useAuth()
  const canManage = hasPermission('system:ip-restrictions:manage')

  // Fetch users for user details (to get full user info when clicking on creator)
  const { data: usersData } = useSystemUsers({ limit: 100 })

  // Convert sorting state to backend format
  const sortParam = useMemo(() => {
    if (sorting.length === 0) return undefined
    return sorting
      .map((s) => {
        const field = sortFieldMap[s.id] || s.id
        return `${field}:${s.desc ? 'desc' : 'asc'}`
      })
      .join(',')
  }, [sorting])

  const {
    data,
    isLoading,
    isFetching,
    fetchNextPage,
    hasNextPage,
  } = useIpRestrictions({
    sort: sortParam,
    search: debouncedSearch || undefined,
    type: typeFilter.length === 1 ? Number(typeFilter[0]) as IpRestrictionType : undefined,
    source: sourceFilter.length === 1 ? Number(sourceFilter[0]) as IpRestrictionSource : undefined,
  })

  const restrictions = useMemo(
    () => data?.pages.flatMap((page) => page.restrictions) ?? [],
    [data]
  )

  // Filter options
  const typeOptions: SelectFilterOption[] = [
    { label: t('ipRestrictions:type.whitelist'), value: String(IpRestrictionType.Whitelist) },
    { label: t('ipRestrictions:type.blacklist'), value: String(IpRestrictionType.Blacklist) },
  ]

  const sourceOptions: SelectFilterOption[] = [
    { label: t('ipRestrictions:source.manual'), value: String(IpRestrictionSource.Manual) },
    { label: t('ipRestrictions:source.auto'), value: String(IpRestrictionSource.Auto) },
  ]

  const handleLoadMore = useCallback(() => {
    if (hasNextPage && !isFetching) {
      fetchNextPage()
    }
  }, [hasNextPage, isFetching, fetchNextPage])

  const handleRowClick = useCallback((restriction: IpRestrictionDto) => {
    setSelectedRestriction(restriction)
    setDetailsSheetOpen(true)
  }, [])

  const handleCreate = useCallback((data: CreateIpRestrictionRequest) => {
    createMutation.mutate(data, {
      onSuccess: () => {
        setCreateModalOpen(false)
      },
    })
  }, [createMutation])

  const handleEditClick = useCallback((restriction: IpRestrictionDto) => {
    setSelectedRestriction(restriction)
    setEditModalOpen(true)
  }, [])

  const handleUpdate = useCallback((id: string, data: UpdateIpRestrictionRequest) => {
    updateMutation.mutate({ id, request: data }, {
      onSuccess: () => {
        setEditModalOpen(false)
        setDetailsSheetOpen(false)
      },
    })
  }, [updateMutation])

  const handleDeleteClick = useCallback((id: string) => {
    const restriction = restrictions.find((r) => r.id === id)
    if (restriction) {
      setRestrictionToDelete(restriction)
      setDeleteConfirmOpen(true)
    }
  }, [restrictions])

  const handleDeleteConfirm = useCallback(() => {
    if (restrictionToDelete) {
      deleteMutation.mutate(restrictionToDelete.id, {
        onSuccess: () => {
          setDeleteConfirmOpen(false)
          setRestrictionToDelete(null)
          setDetailsSheetOpen(false)
          setSelectedRestriction(null)
        },
      })
    }
  }, [restrictionToDelete, deleteMutation])

  // Handler for delete from row action (receives restriction object)
  const handleDeleteFromAction = useCallback((restriction: IpRestrictionDto) => {
    setRestrictionToDelete(restriction)
    setDeleteConfirmOpen(true)
  }, [])

  // Handler for clicking on creator user
  const handleUserClick = useCallback((userId: string) => {
    // Find user info from the users data or selected restriction
    const users = usersData?.pages?.flatMap((page) => page.users) ?? []
    const userInfo = users.find(u => u.id === userId)
    const restriction = selectedRestriction || restrictions.find(r => r.createdByUserId === userId)
    const user: SystemUserDto = {
      id: userId,
      email: userInfo?.email || restriction?.createdByUserEmail || '',
      firstName: userInfo?.firstName || '',
      lastName: userInfo?.lastName || '',
      fullName: userInfo?.fullName || restriction?.createdByUserFullName || '',
      isActive: userInfo?.isActive ?? true,
      emailVerified: userInfo?.emailVerified ?? true,
      mfaEnabled: userInfo?.mfaEnabled ?? false,
      lastLoginAt: userInfo?.lastLoginAt || null,
      lockedUntil: userInfo?.lockedUntil || null,
      isLocked: userInfo?.isLocked ?? false,
      isAnonymized: userInfo?.isAnonymized ?? false,
      failedLoginAttempts: userInfo?.failedLoginAttempts ?? 0,
      createdAt: userInfo?.createdAt || restriction?.createdAt || new Date().toISOString(),
      updatedAt: userInfo?.updatedAt || null,
    }
    setSelectedUser(user)
    setUserSheetOpen(true)
  }, [usersData, selectedRestriction, restrictions])

  // Columns with action callbacks for desktop table
  const columns = useIpRestrictionsColumns({
    onEdit: canManage ? handleEditClick : undefined,
    onDelete: canManage ? handleDeleteFromAction : undefined,
  })

  // Row actions for mobile card
  const rowActions: RowAction<IpRestrictionDto>[] = useMemo(() => {
    if (!canManage) return []

    return [
      {
        label: t('common:actions.edit'),
        icon: <Pencil className="h-4 w-4" />,
        onClick: handleEditClick,
      },
      {
        label: t('common:actions.delete'),
        icon: <Trash2 className="h-4 w-4" />,
        onClick: handleDeleteFromAction,
        variant: 'destructive',
        separator: true,
      },
    ]
  }, [canManage, t, handleEditClick, handleDeleteFromAction])

  const filterContent = (
    <>
      <SelectFilter
        label={t('ipRestrictions:filters.type')}
        options={typeOptions}
        values={typeFilter}
        onValuesChange={setTypeFilter}
      />
      <SelectFilter
        label={t('ipRestrictions:filters.source')}
        options={sourceOptions}
        values={sourceFilter}
        onValuesChange={setSourceFilter}
      />
      {canManage && (
        <Button onClick={() => setCreateModalOpen(true)}>
          <Plus className="h-4 w-4 mr-2" />
          {t('common:actions.add')}
        </Button>
      )}
    </>
  )

  return (
    <>
      <DataTable
        columns={columns}
        data={restrictions}
        isLoading={isLoading}
        isFetching={isFetching}
        hasMore={hasNextPage}
        onLoadMore={handleLoadMore}
        searchValue={searchValue}
        onSearch={setSearchValue}
        searchPlaceholder={t('ipRestrictions:filters.search')}
        initialSorting={sorting}
        onSortingChange={setSorting}
        toolbarContent={filterContent}
        onRowClick={handleRowClick}
        emptyState={{
          title: t('ipRestrictions:table.noResults'),
          description: t('ipRestrictions:table.noResultsDescription'),
        }}
        mobileCard={{
          primaryField: 'ipAddress',
          secondaryField: 'reason',
          icon: () => (
            <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10">
              <Shield className="h-5 w-5 text-primary" />
            </div>
          ),
          tertiaryFields: [
            {
              key: 'type',
              render: (value): ReactNode => (
                <IpRestrictionTypeBadge type={value as IpRestrictionType} />
              ),
            },
            {
              key: 'source',
              render: (value): ReactNode => (
                <IpRestrictionSourceBadge source={value as IpRestrictionSource} />
              ),
            },
            {
              key: 'createdByUserFullName',
              label: t('ipRestrictions:table.createdBy'),
              render: (value, row): ReactNode => (
                <span>{(value as string) || row.createdByUserEmail || t('ipRestrictions:details.system')}</span>
              ),
            },
            {
              key: 'createdAt',
              label: t('ipRestrictions:table.createdAt'),
              render: (value): ReactNode => <RelativeTime date={value as string} />,
            },
          ],
        }}
        rowActions={rowActions}
      />

      <IpRestrictionDetailsSheet
        open={detailsSheetOpen}
        onOpenChange={setDetailsSheetOpen}
        restriction={selectedRestriction}
        onEdit={canManage ? handleEditClick : undefined}
        onDelete={canManage ? handleDeleteClick : undefined}
        onUserClick={handleUserClick}
        isDeleting={deleteMutation.isPending}
      />

      <CreateIpRestrictionModal
        open={createModalOpen}
        onOpenChange={setCreateModalOpen}
        onSubmit={handleCreate}
        isLoading={createMutation.isPending}
      />

      <EditIpRestrictionModal
        open={editModalOpen}
        onOpenChange={setEditModalOpen}
        restriction={selectedRestriction}
        onSubmit={handleUpdate}
        isLoading={updateMutation.isPending}
      />

      <ConfirmDialog
        open={deleteConfirmOpen}
        onOpenChange={setDeleteConfirmOpen}
        title={t('ipRestrictions:delete.title')}
        description={t('ipRestrictions:delete.description')}
        confirmLabel={t('ipRestrictions:delete.confirm')}
        onConfirm={handleDeleteConfirm}
        variant="destructive"
        isLoading={deleteMutation.isPending}
      />

      <UserDetailsSheet
        open={userSheetOpen}
        onOpenChange={setUserSheetOpen}
        user={selectedUser}
      />
    </>
  )
}
