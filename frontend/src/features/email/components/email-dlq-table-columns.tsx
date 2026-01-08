import { useTranslation } from 'react-i18next'
import type { ColumnDef } from '@tanstack/react-table'
import { MoreHorizontal, RotateCcw, Trash2 } from 'lucide-react'

import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { RelativeTime } from '@/components/shared/relative-time'
import type { EmailLogDto } from '../types'

interface UseDlqColumnsOptions {
  onRetry: (email: EmailLogDto) => void
  onDelete: (email: EmailLogDto) => void
  canManage: boolean
}

export function useDlqColumns({
  onRetry,
  onDelete,
  canManage,
}: UseDlqColumnsOptions): ColumnDef<EmailLogDto>[] {
  const { t } = useTranslation()

  const columns: ColumnDef<EmailLogDto>[] = [
    {
      accessorKey: 'recipientEmail',
      header: t('email:dlq.columns.recipient'),
      cell: ({ row }) => {
        const email = row.original
        return (
          <div className="min-w-0">
            <div className="font-medium truncate">{email.recipientEmail}</div>
            {email.recipientUserFullName && (
              <div className="text-sm text-muted-foreground truncate">
                {email.recipientUserFullName}
              </div>
            )}
          </div>
        )
      },
    },
    {
      accessorKey: 'subject',
      header: t('email:dlq.columns.subject'),
      cell: ({ row }) => (
        <div className="max-w-[200px] truncate" title={row.original.subject}>
          {row.original.subject}
        </div>
      ),
    },
    {
      accessorKey: 'templateName',
      header: t('email:dlq.columns.template'),
      cell: ({ row }) => (
        <span className="text-sm text-muted-foreground font-mono">
          {row.original.templateName}
        </span>
      ),
    },
    {
      accessorKey: 'retryCount',
      header: t('email:dlq.columns.retryCount'),
      cell: ({ row }) => (
        <span className="text-sm">{row.original.retryCount}</span>
      ),
    },
    {
      accessorKey: 'lastError',
      header: t('email:dlq.columns.lastError'),
      cell: ({ row }) => (
        <div
          className="max-w-[200px] truncate text-sm text-destructive"
          title={row.original.lastError ?? undefined}
        >
          {row.original.lastError ?? '-'}
        </div>
      ),
    },
    {
      accessorKey: 'movedToDlqAt',
      header: t('email:dlq.columns.movedAt'),
      cell: ({ row }) =>
        row.original.movedToDlqAt ? (
          <RelativeTime date={row.original.movedToDlqAt} />
        ) : (
          <span className="text-muted-foreground">-</span>
        ),
    },
  ]

  if (canManage) {
    columns.push({
      id: 'actions',
      cell: ({ row }) => {
        const email = row.original
        return (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon">
                <MoreHorizontal className="h-4 w-4" />
                <span className="sr-only">{t('common:actions.openMenu')}</span>
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem onClick={() => onRetry(email)}>
                <RotateCcw className="mr-2 h-4 w-4" />
                {t('email:dlq.actions.retry')}
              </DropdownMenuItem>
              <DropdownMenuItem
                onClick={() => onDelete(email)}
                className="text-destructive focus:text-destructive"
              >
                <Trash2 className="mr-2 h-4 w-4" />
                {t('email:dlq.actions.delete')}
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        )
      },
    })
  }

  return columns
}
