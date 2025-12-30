/* eslint-disable react-refresh/only-export-components */
import { type ColumnDef, type Column } from '@tanstack/react-table'
import { useTranslation } from 'react-i18next'
import { ArrowUp, ArrowDown, ChevronsUpDown, Eye, Pencil, Send, XCircle } from 'lucide-react'
import { UserAvatar } from '@/components/shared/user-avatar'
import { StatusBadge } from '@/components/shared/status-badge'
import { RelativeTime } from '@/components/shared/relative-time'
import { DataTableRowActions } from '@/components/shared/data-table/data-table-row-actions'
import { cn } from '@/lib/utils'
import type { SystemInviteListDto, InviteStatus } from '../types'
import type { RowAction } from '@/types/table'

// Map invite status to StatusBadge status
const statusMap: Record<InviteStatus, 'success' | 'warning' | 'error' | 'neutral'> = {
  pending: 'warning',
  accepted: 'success',
  expired: 'neutral',
  revoked: 'error',
}

// Sortable column header
function SortableHeader<T>({ column, label }: { column: Column<T>; label: string }) {
  const sorted = column.getIsSorted()
  const sortIndex = column.getSortIndex()
  const isMultiSort = sortIndex > 0

  const handleClick = (e: React.MouseEvent) => {
    if (e.ctrlKey || e.metaKey) {
      column.toggleSorting(sorted === 'asc', false)
    } else {
      if (sorted === 'desc') {
        column.clearSorting()
      } else {
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

interface UseInvitationsColumnsOptions {
  onViewDetails?: (invite: SystemInviteListDto) => void
  onEdit?: (invite: SystemInviteListDto) => void
  onResend?: (invite: SystemInviteListDto) => void
  onRevoke?: (invite: SystemInviteListDto) => void
}

export function useInvitationsColumns({
  onViewDetails,
  onEdit,
  onResend,
  onRevoke,
}: UseInvitationsColumnsOptions): ColumnDef<SystemInviteListDto>[] {
  const { t } = useTranslation()

  const emailLabel = t('users:invites.fields.email')
  const statusLabel = t('users:invites.fields.status')
  const expiresLabel = t('users:invites.fields.expiresAt')
  const createdLabel = t('users:invites.fields.createdAt')

  const columns: ColumnDef<SystemInviteListDto>[] = [
    {
      id: 'email',
      accessorKey: 'email',
      header: ({ column }) => <SortableHeader column={column} label={emailLabel} />,
      meta: { label: emailLabel },
      enableSorting: true,
      cell: ({ row }) => {
        const invite = row.original
        const fullName = `${invite.firstName} ${invite.lastName}`.trim()
        return (
          <div className="flex items-center gap-3">
            <UserAvatar name={fullName} email={invite.email} size="sm" />
            <div className="flex flex-col">
              <span className="font-medium">{fullName}</span>
              <span className="text-xs text-muted-foreground">{invite.email}</span>
            </div>
          </div>
        )
      },
    },
    {
      id: 'status',
      accessorKey: 'status',
      header: statusLabel,
      meta: { label: statusLabel },
      enableSorting: false,
      cell: ({ row }) => {
        const status = row.original.status
        return (
          <StatusBadge
            status={statusMap[status]}
            label={t(`users:invites.status.${status}`)}
          />
        )
      },
    },
    {
      id: 'expiresAt',
      accessorKey: 'expiresAt',
      header: ({ column }) => <SortableHeader column={column} label={expiresLabel} />,
      meta: { label: expiresLabel },
      enableSorting: true,
      cell: ({ row }) => {
        const invite = row.original
        // Don't show expiry for accepted/revoked invites
        if (invite.status === 'accepted' || invite.status === 'revoked') {
          return <span className="text-muted-foreground">-</span>
        }
        return <RelativeTime date={invite.expiresAt} />
      },
    },
    {
      id: 'createdAt',
      accessorKey: 'createdAt',
      header: ({ column }) => <SortableHeader column={column} label={createdLabel} />,
      meta: { label: createdLabel },
      enableSorting: true,
      cell: ({ row }) => <RelativeTime date={row.original.createdAt} />,
    },
  ]

  // Add actions column if any action is available
  const hasAnyAction = onViewDetails || onEdit || onResend || onRevoke
  if (hasAnyAction) {
    columns.push({
      id: 'actions',
      enableSorting: false,
      enableHiding: false,
      header: () => <span className="sr-only">Actions</span>,
      cell: ({ row }) => {
        const invite = row.original
        const actions: RowAction<SystemInviteListDto>[] = []

        if (onViewDetails) {
          actions.push({
            label: t('users:invites.actions.viewDetails'),
            icon: <Eye className="h-4 w-4" />,
            onClick: onViewDetails,
          })
        }

        if (onEdit) {
          // Can only edit pending invites
          const canEdit = invite.status === 'pending'
          actions.push({
            label: t('common:actions.edit'),
            icon: <Pencil className="h-4 w-4" />,
            onClick: onEdit,
            disabled: !canEdit,
          })
        }

        if (onResend) {
          // Can only resend pending or expired invites
          const canResend = invite.status === 'pending' || invite.status === 'expired'
          actions.push({
            label: t('users:invites.actions.resend'),
            icon: <Send className="h-4 w-4" />,
            onClick: onResend,
            disabled: !canResend,
          })
        }

        if (onRevoke) {
          // Can only revoke pending invites
          const canRevoke = invite.status === 'pending'
          actions.push({
            label: t('users:invites.actions.revoke'),
            icon: <XCircle className="h-4 w-4" />,
            onClick: onRevoke,
            variant: 'destructive',
            separator: actions.length > 0,
            disabled: !canRevoke,
          })
        }

        if (actions.length === 0) return null

        return (
          <div className="flex justify-end">
            <DataTableRowActions row={invite} actions={actions} />
          </div>
        )
      },
    })
  }

  return columns
}
