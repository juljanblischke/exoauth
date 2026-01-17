import { useTranslation } from 'react-i18next'
import type { ColumnDef } from '@tanstack/react-table'
import { MoreHorizontal, RotateCcw, Trash2 } from 'lucide-react'

import { Button } from '@/components/ui/button'
import { UserAvatar } from '@/components/shared/user-avatar'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from '@/components/ui/tooltip'
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
        const displayName = email.recipientUserFullName || email.recipientEmail
        return (
          <div className="flex items-center gap-3">
            <UserAvatar
              name={email.recipientUserFullName || ''}
              email={email.recipientEmail}
              size="sm"
            />
            <div className="flex flex-col min-w-0">
              <Tooltip>
                <TooltipTrigger asChild>
                  <span className="font-medium truncate max-w-[150px]">
                    {displayName}
                  </span>
                </TooltipTrigger>
                <TooltipContent>
                  <p>{displayName}</p>
                </TooltipContent>
              </Tooltip>
              {email.recipientUserFullName && (
                <Tooltip>
                  <TooltipTrigger asChild>
                    <span className="text-xs text-muted-foreground truncate max-w-[150px]">
                      {email.recipientEmail}
                    </span>
                  </TooltipTrigger>
                  <TooltipContent>
                    <p>{email.recipientEmail}</p>
                  </TooltipContent>
                </Tooltip>
              )}
            </div>
          </div>
        )
      },
    },
    {
      accessorKey: 'subject',
      header: t('email:dlq.columns.subject'),
      cell: ({ row }) => (
        <Tooltip>
          <TooltipTrigger asChild>
            <div className="max-w-[200px] truncate cursor-default">
              {row.original.subject}
            </div>
          </TooltipTrigger>
          <TooltipContent className="max-w-[400px]">
            <p>{row.original.subject}</p>
          </TooltipContent>
        </Tooltip>
      ),
    },
    {
      accessorKey: 'templateName',
      header: t('email:dlq.columns.template'),
      cell: ({ row }) => (
        <Tooltip>
          <TooltipTrigger asChild>
            <span className="text-sm text-muted-foreground font-mono truncate max-w-[120px] block">
              {row.original.templateName}
            </span>
          </TooltipTrigger>
          <TooltipContent>
            <p className="font-mono">{row.original.templateName}</p>
          </TooltipContent>
        </Tooltip>
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
      cell: ({ row }) => {
        const lastError = row.original.lastError
        if (!lastError) {
          return <span className="text-muted-foreground">-</span>
        }
        return (
          <Tooltip>
            <TooltipTrigger asChild>
              <div className="max-w-[200px] truncate text-sm text-destructive cursor-default">
                {lastError}
              </div>
            </TooltipTrigger>
            <TooltipContent className="max-w-[400px]">
              <p className="text-destructive">{lastError}</p>
            </TooltipContent>
          </Tooltip>
        )
      },
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
