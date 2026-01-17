import { useState, useMemo, useCallback, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Eye, Send, XCircle, Pencil } from 'lucide-react'
import { type SortingState } from '@tanstack/react-table'
import { DataTable } from '@/components/shared/data-table'
import { SelectFilter, type SelectFilterOption } from '@/components/shared/form'
import { RelativeTime } from '@/components/shared/relative-time'
import { StatusBadge } from '@/components/shared/status-badge'
import { Checkbox } from '@/components/ui/checkbox'
import { Label } from '@/components/ui/label'
import { useDebounce } from '@/hooks'
import { useSystemInvites } from '../hooks'
import { useInvitationsColumns } from './invitations-table-columns'
import type { SystemInviteListDto, InviteStatus } from '../types'
import type { RowAction } from '@/types/table'

// Map invite status to StatusBadge status
const statusMap: Record<InviteStatus, 'success' | 'warning' | 'error' | 'neutral'> = {
  pending: 'warning',
  accepted: 'success',
  expired: 'neutral',
  revoked: 'error',
}

// All possible invite statuses (for filter when showing expired/revoked)
const INVITE_STATUSES: InviteStatus[] = ['pending', 'accepted', 'expired', 'revoked']
// Default statuses shown when not including expired/revoked
const DEFAULT_STATUSES: InviteStatus[] = ['pending', 'accepted']

interface InvitationsTableProps {
  onViewDetails?: (invite: SystemInviteListDto) => void
  onEdit?: (invite: SystemInviteListDto) => void
  onResend?: (invite: SystemInviteListDto) => void
  onRevoke?: (invite: SystemInviteListDto) => void
  onRowClick?: (invite: SystemInviteListDto) => void
}

// Map column IDs to backend field names
const sortFieldMap: Record<string, string> = {
  email: 'email',
  name: 'firstName',
  expiresAt: 'expiresAt',
  createdAt: 'createdAt',
}

export function InvitationsTable({
  onViewDetails,
  onEdit,
  onResend,
  onRevoke,
  onRowClick,
}: InvitationsTableProps) {
  const { t } = useTranslation()
  const [search, setSearch] = useState('')
  const [sorting, setSorting] = useState<SortingState>([{ id: 'createdAt', desc: true }])
  const [statusFilters, setStatusFilters] = useState<string[]>([])
  const [showExpired, setShowExpired] = useState(false)
  const [showRevoked, setShowRevoked] = useState(false)
  const debouncedSearch = useDebounce(search, 300)

  // Convert sorting state to backend format
  const sortParam = useMemo(() => {
    if (sorting.length === 0) return undefined
    const s = sorting[0]
    const field = sortFieldMap[s.id] || s.id
    return `${field}:${s.desc ? 'desc' : 'asc'}`
  }, [sorting])

  // Determine which statuses to filter by
  const activeStatuses = useMemo(() => {
    if (statusFilters.length > 0) {
      return statusFilters as InviteStatus[]
    }
    return undefined
  }, [statusFilters])

  const {
    data,
    isLoading,
    isFetching,
    fetchNextPage,
    hasNextPage,
    refetch,
    isRefetching,
  } = useSystemInvites({
    search: debouncedSearch || undefined,
    statuses: activeStatuses,
    sort: sortParam,
    includeExpired: showExpired,
    includeRevoked: showRevoked,
  })

  const invites = useMemo(
    () => data?.pages.flatMap((page) => page.invites) ?? [],
    [data]
  )

  const columns = useInvitationsColumns({
    onViewDetails,
    onEdit,
    onResend,
    onRevoke,
  })

  const rowActions: RowAction<SystemInviteListDto>[] = useMemo(() => {
    const actions: RowAction<SystemInviteListDto>[] = []

    if (onViewDetails) {
      actions.push({
        label: t('users:invites.actions.viewDetails'),
        icon: <Eye className="h-4 w-4" />,
        onClick: onViewDetails,
      })
    }

    if (onEdit) {
      actions.push({
        label: t('common:actions.edit'),
        icon: <Pencil className="h-4 w-4" />,
        onClick: onEdit,
        disabled: (invite) => invite.status !== 'pending',
      })
    }

    if (onResend) {
      actions.push({
        label: t('users:invites.actions.resend'),
        icon: <Send className="h-4 w-4" />,
        onClick: onResend,
        disabled: (invite) =>
          invite.status !== 'pending' && invite.status !== 'expired',
      })
    }

    if (onRevoke) {
      actions.push({
        label: t('users:invites.actions.revoke'),
        icon: <XCircle className="h-4 w-4" />,
        onClick: onRevoke,
        variant: 'destructive',
        separator: actions.length > 0,
        disabled: (invite) => invite.status !== 'pending',
      })
    }

    return actions
  }, [t, onViewDetails, onEdit, onResend, onRevoke])

  const handleLoadMore = useCallback(() => {
    if (hasNextPage && !isFetching) {
      fetchNextPage()
    }
  }, [hasNextPage, isFetching, fetchNextPage])

  const handleRefresh = useCallback(async () => {
    await refetch()
    toast.success(t('users:invites.refreshed'))
  }, [refetch, t])

  // Build status filter options based on what's currently shown
  const statusOptions: SelectFilterOption[] = useMemo(() => {
    const availableStatuses = showExpired && showRevoked
      ? INVITE_STATUSES
      : showExpired
        ? ['pending', 'accepted', 'expired'] as InviteStatus[]
        : showRevoked
          ? ['pending', 'accepted', 'revoked'] as InviteStatus[]
          : DEFAULT_STATUSES

    return availableStatuses.map((status) => ({
      label: t(`users:invites.status.${status}`),
      value: status,
    }))
  }, [t, showExpired, showRevoked])

  const filterContent = (
    <div className="flex flex-wrap items-center gap-2">
      <SelectFilter
        label={t('users:invites.fields.status')}
        options={statusOptions}
        multiple
        values={statusFilters}
        onValuesChange={setStatusFilters}
      />
      <div className="flex items-center gap-2">
        <Checkbox
          id="show-expired"
          checked={showExpired}
          onCheckedChange={(checked) => setShowExpired(checked === true)}
        />
        <Label htmlFor="show-expired" className="text-sm font-normal cursor-pointer">
          {t('users:invites.filters.showExpired')}
        </Label>
      </div>
      <div className="flex items-center gap-2">
        <Checkbox
          id="show-revoked"
          checked={showRevoked}
          onCheckedChange={(checked) => setShowRevoked(checked === true)}
        />
        <Label htmlFor="show-revoked" className="text-sm font-normal cursor-pointer">
          {t('users:invites.filters.showRevoked')}
        </Label>
      </div>
    </div>
  )

  return (
    <DataTable
      columns={columns}
      data={invites}
      isLoading={isLoading}
      isFetching={isFetching}
      hasMore={hasNextPage}
      onLoadMore={handleLoadMore}
      searchPlaceholder={t('users:invites.search', 'Search invitations...')}
      searchValue={search}
      onSearch={setSearch}
      initialSorting={sorting}
      onSortingChange={setSorting}
      toolbarContent={filterContent}
      onRefresh={handleRefresh}
      isRefreshing={isRefetching}
      emptyState={{
        title: t('users:invites.empty.title'),
        description: t('users:invites.empty.description'),
      }}
      rowActions={rowActions}
      onRowClick={onRowClick}
      mobileCard={{
        primaryField: 'email',
        secondaryField: (row) => `${row.firstName} ${row.lastName}`,
        avatar: (row) => ({
          name: `${row.firstName} ${row.lastName}`,
          email: row.email,
        }),
        tertiaryFields: [
          {
            key: 'status',
            render: (value): ReactNode => (
              <StatusBadge
                status={statusMap[value as InviteStatus]}
                label={t(`users:invites.status.${value}`)}
              />
            ),
          },
          {
            key: 'expiresAt',
            label: t('users:invites.fields.expiresAt'),
            render: (value, row): ReactNode => {
              const invite = row as SystemInviteListDto
              if (invite.status === 'accepted' || invite.status === 'revoked') {
                return '-'
              }
              return <RelativeTime date={value as string} />
            },
          },
        ],
      }}
    />
  )
}
