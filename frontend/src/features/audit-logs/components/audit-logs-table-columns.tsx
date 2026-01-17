/* eslint-disable react-refresh/only-export-components */
import { type ColumnDef, type Column } from '@tanstack/react-table'
import { useTranslation } from 'react-i18next'
import { ArrowUp, ArrowDown, ChevronsUpDown } from 'lucide-react'
import { UserAvatar } from '@/components/shared/user-avatar'
import { RelativeTime } from '@/components/shared/relative-time'
import { Badge } from '@/components/ui/badge'
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from '@/components/ui/tooltip'
import { cn } from '@/lib/utils'
import type { SystemAuditLogDto } from '../types'

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

export function useAuditLogsColumns(): ColumnDef<SystemAuditLogDto>[] {
  const { t } = useTranslation()

  const timeLabel = t('auditLogs:fields.time')
  const userLabel = t('auditLogs:fields.user')
  const actionLabel = t('auditLogs:fields.action')
  const entityLabel = t('auditLogs:fields.entity')
  const ipAddressLabel = t('auditLogs:fields.ipAddress')

  const columns: ColumnDef<SystemAuditLogDto>[] = [
    {
      id: 'createdAt',
      accessorKey: 'createdAt',
      header: ({ column }) => <SortableHeader column={column} label={timeLabel} />,
      meta: { label: timeLabel },
      enableSorting: true,
      cell: ({ row }) => <RelativeTime date={row.original.createdAt} />,
    },
    {
      id: 'user',
      accessorKey: 'userFullName',
      header: userLabel,
      meta: { label: userLabel },
      enableSorting: false,
      cell: ({ row }) => {
        const log = row.original
        if (!log.userId) {
          return <span className="text-muted-foreground">{t('auditLogs:system')}</span>
        }
        return (
          <div className="flex items-center gap-3">
            <UserAvatar
              name={log.userFullName || ''}
              email={log.userEmail || ''}
              size="sm"
            />
            <div className="flex flex-col min-w-0">
              <Tooltip>
                <TooltipTrigger asChild>
                  <span className="font-medium truncate max-w-[150px]">{log.userFullName}</span>
                </TooltipTrigger>
                <TooltipContent>
                  <p>{log.userFullName}</p>
                </TooltipContent>
              </Tooltip>
              <Tooltip>
                <TooltipTrigger asChild>
                  <span className="text-xs text-muted-foreground truncate max-w-[150px]">{log.userEmail}</span>
                </TooltipTrigger>
                <TooltipContent>
                  <p>{log.userEmail}</p>
                </TooltipContent>
              </Tooltip>
            </div>
          </div>
        )
      },
    },
    {
      id: 'action',
      accessorKey: 'action',
      header: actionLabel,
      meta: { label: actionLabel },
      enableSorting: false,
      cell: ({ row }) => {
        const action = row.original.action
        return (
          <Badge variant="outline" className="font-mono text-xs">
            {action}
          </Badge>
        )
      },
    },
    {
      id: 'entity',
      accessorKey: 'entityType',
      header: entityLabel,
      meta: { label: entityLabel },
      enableSorting: false,
      cell: ({ row }) => {
        const log = row.original
        if (!log.entityType) {
          return <span className="text-muted-foreground">-</span>
        }
        return (
          <div className="flex flex-col min-w-0">
            <span className="font-medium">{log.entityType}</span>
            {log.entityId && (
              <Tooltip>
                <TooltipTrigger asChild>
                  <span className="text-xs text-muted-foreground font-mono truncate max-w-[120px]">
                    {log.entityId}
                  </span>
                </TooltipTrigger>
                <TooltipContent>
                  <p className="font-mono">{log.entityId}</p>
                </TooltipContent>
              </Tooltip>
            )}
          </div>
        )
      },
    },
    {
      id: 'ipAddress',
      accessorKey: 'ipAddress',
      header: ipAddressLabel,
      meta: { label: ipAddressLabel },
      enableSorting: false,
      cell: ({ row }) => {
        const ip = row.original.ipAddress
        if (!ip) {
          return <span className="text-muted-foreground">-</span>
        }
        return <span className="font-mono text-sm">{ip}</span>
      },
    },
  ]

  return columns
}
