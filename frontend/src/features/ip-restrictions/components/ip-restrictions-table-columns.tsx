/* eslint-disable react-refresh/only-export-components */
import { type ColumnDef, type Column } from '@tanstack/react-table'
import { useTranslation } from 'react-i18next'
import { ArrowUp, ArrowDown, ChevronsUpDown, Pencil, Trash2 } from 'lucide-react'
import { RelativeTime } from '@/components/shared/relative-time'
import { UserAvatar } from '@/components/shared/user-avatar'
import { DataTableRowActions } from '@/components/shared/data-table'
import { Badge } from '@/components/ui/badge'
import { cn } from '@/lib/utils'
import { IpRestrictionTypeBadge } from './ip-restriction-type-badge'
import { IpRestrictionSourceBadge } from './ip-restriction-source-badge'
import type { IpRestrictionDto } from '../types'
import type { RowAction } from '@/types/table'

interface UseIpRestrictionsColumnsOptions {
  onEdit?: (restriction: IpRestrictionDto) => void
  onDelete?: (restriction: IpRestrictionDto) => void
}

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

export function useIpRestrictionsColumns(
  options: UseIpRestrictionsColumnsOptions = {}
): ColumnDef<IpRestrictionDto>[] {
  const { t } = useTranslation()
  const { onEdit, onDelete } = options

  const ipAddressLabel = t('ipRestrictions:table.ipAddress')
  const typeLabel = t('ipRestrictions:table.type')
  const reasonLabel = t('ipRestrictions:table.reason')
  const sourceLabel = t('ipRestrictions:table.source')
  const expiresAtLabel = t('ipRestrictions:table.expiresAt')
  const createdAtLabel = t('ipRestrictions:table.createdAt')
  const createdByLabel = t('ipRestrictions:table.createdBy')

  const columns: ColumnDef<IpRestrictionDto>[] = [
    {
      id: 'ipAddress',
      accessorKey: 'ipAddress',
      header: ipAddressLabel,
      meta: { label: ipAddressLabel },
      enableSorting: false,
      cell: ({ row }) => (
        <span className="font-mono text-sm">{row.original.ipAddress}</span>
      ),
    },
    {
      id: 'type',
      accessorKey: 'type',
      header: typeLabel,
      meta: { label: typeLabel },
      enableSorting: false,
      cell: ({ row }) => <IpRestrictionTypeBadge type={row.original.type} />,
    },
    {
      id: 'reason',
      accessorKey: 'reason',
      header: reasonLabel,
      meta: { label: reasonLabel },
      enableSorting: false,
      cell: ({ row }) => (
        <span className="max-w-[200px] truncate" title={row.original.reason}>
          {row.original.reason}
        </span>
      ),
    },
    {
      id: 'source',
      accessorKey: 'source',
      header: sourceLabel,
      meta: { label: sourceLabel },
      enableSorting: false,
      cell: ({ row }) => <IpRestrictionSourceBadge source={row.original.source} />,
    },
    {
      id: 'expiresAt',
      accessorKey: 'expiresAt',
      header: expiresAtLabel,
      meta: { label: expiresAtLabel },
      enableSorting: false,
      cell: ({ row }) => {
        const expiresAt = row.original.expiresAt
        if (!expiresAt) {
          return <span className="text-muted-foreground">{t('ipRestrictions:never')}</span>
        }
        const isExpired = new Date(expiresAt) < new Date()
        if (isExpired) {
          return (
            <Badge variant="outline" className="text-muted-foreground">
              {t('ipRestrictions:expired')}
            </Badge>
          )
        }
        return <RelativeTime date={expiresAt} />
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
    {
      id: 'createdBy',
      accessorKey: 'createdByUserEmail',
      header: createdByLabel,
      meta: { label: createdByLabel },
      enableSorting: false,
      cell: ({ row }) => {
        const restriction = row.original
        if (!restriction.createdByUserId) {
          return <span className="text-muted-foreground">{t('ipRestrictions:details.system')}</span>
        }
        return (
          <div className="flex items-center gap-3">
            <UserAvatar
              name={restriction.createdByUserFullName || ''}
              email={restriction.createdByUserEmail || ''}
              size="sm"
            />
            <div className="flex flex-col">
              <span className="font-medium">{restriction.createdByUserFullName}</span>
              <span className="text-xs text-muted-foreground">{restriction.createdByUserEmail}</span>
            </div>
          </div>
        )
      },
    },
  ]

  // Add actions column if any action is available
  const hasAnyAction = onEdit || onDelete
  if (hasAnyAction) {
    columns.push({
      id: 'actions',
      enableSorting: false,
      enableHiding: false,
      header: () => <span className="sr-only">Actions</span>,
      cell: ({ row }) => {
        const restriction = row.original
        const actions: RowAction<IpRestrictionDto>[] = []

        if (onEdit) {
          actions.push({
            label: t('common:actions.edit'),
            icon: <Pencil className="h-4 w-4" />,
            onClick: onEdit,
          })
        }

        if (onDelete) {
          actions.push({
            label: t('common:actions.delete'),
            icon: <Trash2 className="h-4 w-4" />,
            onClick: onDelete,
            variant: 'destructive',
            separator: actions.length > 0,
          })
        }

        if (actions.length === 0) return null

        return (
          <div className="flex justify-end">
            <DataTableRowActions row={restriction} actions={actions} />
          </div>
        )
      },
    })
  }

  return columns
}
