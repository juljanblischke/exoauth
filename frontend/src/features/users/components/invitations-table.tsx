import { useState, useMemo, useCallback, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Eye, Send, XCircle } from 'lucide-react'
import { type SortingState } from '@tanstack/react-table'
import { DataTable } from '@/components/shared/data-table'
import { SelectFilter, type SelectFilterOption } from '@/components/shared/form'
import { RelativeTime } from '@/components/shared/relative-time'
import { StatusBadge } from '@/components/shared/status-badge'
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

// All possible invite statuses
const INVITE_STATUSES: InviteStatus[] = ['pending', 'accepted', 'expired', 'revoked']

interface InvitationsTableProps {
  onViewDetails?: (invite: SystemInviteListDto) => void
  onResend?: (invite: SystemInviteListDto) => void
  onRevoke?: (invite: SystemInviteListDto) => void
  onRowClick?: (invite: SystemInviteListDto) => void
}

export function InvitationsTable({
  onViewDetails,
  onResend,
  onRevoke,
  onRowClick,
}: InvitationsTableProps) {
  const { t } = useTranslation()
  const [search, setSearch] = useState('')
  const [sorting, setSorting] = useState<SortingState>([])
  const [statusFilter, setStatusFilter] = useState<string | undefined>()
  const debouncedSearch = useDebounce(search, 300)

  const {
    data,
    isLoading,
    isFetching,
    fetchNextPage,
    hasNextPage,
  } = useSystemInvites({
    search: debouncedSearch || undefined,
    status: statusFilter ? [statusFilter as InviteStatus] : undefined,
  })

  const invites = useMemo(
    () => data?.pages.flatMap((page) => page.invites) ?? [],
    [data]
  )

  const columns = useInvitationsColumns({
    onViewDetails,
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
  }, [t, onViewDetails, onResend, onRevoke])

  const handleLoadMore = useCallback(() => {
    if (hasNextPage && !isFetching) {
      fetchNextPage()
    }
  }, [hasNextPage, isFetching, fetchNextPage])

  // Build status filter options
  const statusOptions: SelectFilterOption[] = useMemo(() => {
    return INVITE_STATUSES.map((status) => ({
      label: t(`users:invites.status.${status}`),
      value: status,
    }))
  }, [t])

  const filterContent = (
    <SelectFilter
      label={t('users:invites.fields.status')}
      options={statusOptions}
      value={statusFilter}
      onChange={setStatusFilter}
    />
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
