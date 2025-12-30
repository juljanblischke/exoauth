/* eslint-disable react-refresh/only-export-components */
import { type ColumnDef, type Column } from '@tanstack/react-table'
import { useTranslation } from 'react-i18next'
import { ArrowUp, ArrowDown, ChevronsUpDown, Edit, Shield } from 'lucide-react'
import { UserAvatar } from '@/components/shared/user-avatar'
import { StatusBadge } from '@/components/shared/status-badge'
import { RelativeTime } from '@/components/shared/relative-time'
import { DataTableRowActions } from '@/components/shared/data-table/data-table-row-actions'
import { cn } from '@/lib/utils'
import type { SystemUserDto } from '../types'
import type { RowAction } from '@/types/table'
import { UserStatusBadges } from './user-status-badges'

// Sortable column header with multi-sort support
// - Click: single sort (replaces other sorts)
// - Shift+Click: add to multi-sort
// - Click sorted column: toggle asc/desc, click again to remove
function SortableHeader<T>({ column, label }: { column: Column<T>; label: string }) {
  const sorted = column.getIsSorted()
  const sortIndex = column.getSortIndex()
  const isMultiSort = sortIndex > 0

  const handleClick = (e: React.MouseEvent) => {
    // Ctrl+click for single sort (replaces others)
    if (e.ctrlKey || e.metaKey) {
      column.toggleSorting(sorted === 'asc', false)
    } else {
      // Regular click - always multi-sort
      if (sorted === 'desc') {
        // Third click - remove this column from sort
        column.clearSorting()
      } else {
        // Add to multi-sort or toggle direction
        column.toggleSorting(sorted === 'asc', true)
      }
    }
  }

  return (
    <button
      type="button"
      onClick={handleClick}
      className={cn(
        'group inline-flex items-center gap-1 rounded px-1 py-0.5 -ml-1 text-left font-medium transition-colors',
        'hover:bg-muted/50 hover:text-foreground',
        sorted && 'text-foreground'
      )}
    >
      {label}
      <span
        className={cn(
          'flex items-center transition-opacity',
          sorted ? 'opacity-100' : 'opacity-0 group-hover:opacity-50'
        )}
      >
        {sorted === 'asc' ? (
          <ArrowUp className="h-3.5 w-3.5" />
        ) : sorted === 'desc' ? (
          <ArrowDown className="h-3.5 w-3.5" />
        ) : (
          <ChevronsUpDown className="h-3.5 w-3.5" />
        )}
        {isMultiSort && (
          <span className="ml-0.5 text-[10px] tabular-nums text-muted-foreground">
            {sortIndex + 1}
          </span>
        )}
      </span>
    </button>
  )
}

interface UseUsersColumnsOptions {
  onEdit?: (user: SystemUserDto) => void
  onPermissions?: (user: SystemUserDto) => void
  currentUserId?: string
  rowActions?: RowAction<SystemUserDto>[]
}

export function useUsersColumns({
  onEdit,
  onPermissions,
  currentUserId,
  rowActions = [],
}: UseUsersColumnsOptions): ColumnDef<SystemUserDto>[] {
  const { t } = useTranslation()
  // Silence unused variable warning - currentUserId reserved for future use
  void currentUserId

  const nameLabel = t('users:fields.name')
  const statusLabel = t('users:fields.status')
  const securityLabel = t('users:fields.security')
  const emailVerifiedLabel = t('users:fields.emailVerified')
  const lastLoginLabel = t('users:fields.lastLogin')
  const createdAtLabel = t('users:fields.createdAt')

  const columns: ColumnDef<SystemUserDto>[] = [
    {
      id: 'name',
      accessorKey: 'fullName',
      header: ({ column }) => <SortableHeader column={column} label={nameLabel} />,
      meta: { label: nameLabel },
      enableSorting: true,
      cell: ({ row }) => {
        const user = row.original
        return (
          <div className="flex items-center gap-3">
            <UserAvatar
              name={user.fullName}
              email={user.email}
              size="sm"
            />
            <div className="flex flex-col">
              <span className="font-medium">{user.fullName}</span>
              <span className="text-xs text-muted-foreground">{user.email}</span>
            </div>
          </div>
        )
      },
    },
    {
      id: 'status',
      accessorKey: 'isActive',
      header: statusLabel,
      meta: { label: statusLabel },
      enableSorting: false,
      cell: ({ row }) => {
        const user = row.original
        return user.isActive ? (
          <StatusBadge
            status="success"
            label={t('users:status.active')}
          />
        ) : (
          <StatusBadge
            status="error"
            label={t('users:status.inactive')}
          />
        )
      },
    },
    {
      id: 'security',
      accessorFn: (row) => row.mfaEnabled, // Accessor needed for column toggle visibility
      header: securityLabel,
      meta: { label: securityLabel },
      enableSorting: false,
      cell: ({ row }) => {
        const user = row.original
        return (
          <UserStatusBadges
            user={user}
            showMfa
            showLocked
            showAnonymized
          />
        )
      },
    },
    {
      id: 'emailVerified',
      accessorKey: 'emailVerified',
      header: emailVerifiedLabel,
      meta: { label: emailVerifiedLabel },
      enableSorting: false,
      cell: ({ row }) => {
        const user = row.original
        return user.emailVerified ? (
          <StatusBadge
            status="success"
            label={t('users:emailVerified.verified')}
          />
        ) : (
          <StatusBadge
            status="warning"
            label={t('users:emailVerified.pending')}
          />
        )
      },
    },
    {
      id: 'lastLogin',
      accessorKey: 'lastLoginAt',
      header: ({ column }) => <SortableHeader column={column} label={lastLoginLabel} />,
      meta: { label: lastLoginLabel },
      enableSorting: true,
      cell: ({ row }) => {
        const lastLogin = row.original.lastLoginAt
        if (!lastLogin) {
          return <span className="text-muted-foreground">-</span>
        }
        return <RelativeTime date={lastLogin} />
      },
    },
    {
      id: 'createdAt',
      accessorKey: 'createdAt',
      header: ({ column }) => <SortableHeader column={column} label={createdAtLabel} />,
      meta: { label: createdAtLabel },
      enableSorting: true,
      cell: ({ row }) => <RelativeTime date={row.original.createdAt} />,
    },
  ]

  // Only add actions column if at least one action is available
  const hasAnyAction = onEdit || onPermissions || rowActions.length > 0
  if (hasAnyAction) {
    columns.push({
      id: 'actions',
      enableSorting: false,
      enableHiding: false,
      header: () => <span className="sr-only">Actions</span>,
      cell: ({ row }) => {
        const user = row.original

        // Build base actions from props
        const actions: RowAction<SystemUserDto>[] = []

        if (onEdit) {
          actions.push({
            label: t('common:actions.edit'),
            icon: <Edit className="h-4 w-4" />,
            onClick: onEdit,
            hidden: (u) => u.isAnonymized,
          })
        }

        if (onPermissions) {
          actions.push({
            label: t('users:actions.permissions'),
            icon: <Shield className="h-4 w-4" />,
            onClick: onPermissions,
            hidden: (u) => u.isAnonymized,
          })
        }

        // Add additional row actions (admin actions, etc.)
        actions.push(...rowActions)

        if (actions.length === 0) return null

        return (
          <div className="flex justify-end">
            <DataTableRowActions row={user} actions={actions} />
          </div>
        )
      },
    })
  }

  return columns
}
