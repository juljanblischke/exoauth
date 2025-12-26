import { useState, useMemo, useCallback, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Edit, Shield, Trash2 } from 'lucide-react'
import { type SortingState } from '@tanstack/react-table'
import { DataTable } from '@/components/shared/data-table'
import { RelativeTime } from '@/components/shared/relative-time'
import { StatusBadge } from '@/components/shared/status-badge'
import { useDebounce } from '@/hooks'
import { useAuth } from '@/contexts/auth-context'
import { useSystemUsers } from '../hooks'
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
  onDelete?: (user: SystemUserDto) => void
  onRowClick?: (user: SystemUserDto) => void
}

export function UsersTable({ onEdit, onPermissions, onDelete, onRowClick }: UsersTableProps) {
  const { t } = useTranslation()
  const { user: currentUser } = useAuth()
  const [search, setSearch] = useState('')
  const [sorting, setSorting] = useState<SortingState>([])
  const debouncedSearch = useDebounce(search, 300)

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

  const {
    data,
    isLoading,
    isFetching,
    fetchNextPage,
    hasNextPage,
  } = useSystemUsers({
    search: debouncedSearch || undefined,
    sort: sortParam,
  })

  const users = useMemo(
    () => data?.pages.flatMap((page) => page.users) ?? [],
    [data]
  )

  const columns = useUsersColumns({
    onEdit,
    onPermissions,
    onDelete,
    currentUserId: currentUser?.id,
  })

  const rowActions: RowAction<SystemUserDto>[] = useMemo(() => {
    const actions: RowAction<SystemUserDto>[] = []

    if (onEdit) {
      actions.push({
        label: t('common:actions.edit'),
        icon: <Edit className="h-4 w-4" />,
        onClick: onEdit,
      })
    }

    if (onPermissions) {
      actions.push({
        label: t('users:actions.permissions'),
        icon: <Shield className="h-4 w-4" />,
        onClick: onPermissions,
      })
    }

    if (onDelete) {
      actions.push({
        label: t('common:actions.delete'),
        icon: <Trash2 className="h-4 w-4" />,
        onClick: onDelete,
        variant: 'destructive',
        separator: actions.length > 0,
        disabled: (user) => user.id === currentUser?.id,
      })
    }

    return actions
  }, [t, onEdit, onPermissions, onDelete, currentUser?.id])

  const handleLoadMore = useCallback(() => {
    if (hasNextPage && !isFetching) {
      fetchNextPage()
    }
  }, [hasNextPage, isFetching, fetchNextPage])

  return (
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
  )
}
