import { useTranslation } from 'react-i18next'
import type { ColumnDef } from '@tanstack/react-table'
import { UserAvatar } from '@/components/shared/user-avatar'
import { RelativeTime } from '@/components/shared/relative-time'
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from '@/components/ui/tooltip'
import { EmailStatusBadge } from './email-status-badge'
import type { EmailLogDto } from '../types'

export function useEmailLogsColumns(): ColumnDef<EmailLogDto>[] {
  const { t } = useTranslation()

  return [
    {
      accessorKey: 'recipientEmail',
      header: t('email:logs.columns.recipient'),
      cell: ({ row }) => {
        const log = row.original
        const displayName = log.recipientUserFullName || log.recipientEmail
        return (
          <div className="flex items-center gap-3">
            <UserAvatar
              name={log.recipientUserFullName || ''}
              email={log.recipientEmail}
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
              {log.recipientUserFullName && (
                <Tooltip>
                  <TooltipTrigger asChild>
                    <span className="text-xs text-muted-foreground truncate max-w-[150px]">
                      {log.recipientEmail}
                    </span>
                  </TooltipTrigger>
                  <TooltipContent>
                    <p>{log.recipientEmail}</p>
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
      header: t('email:logs.columns.subject'),
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
      header: t('email:logs.columns.template'),
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
      accessorKey: 'status',
      header: t('email:logs.columns.status'),
      cell: ({ row }) => <EmailStatusBadge status={row.original.status} />,
    },
    {
      accessorKey: 'sentViaProviderName',
      header: t('email:logs.columns.provider'),
      cell: ({ row }) => (
        <span className="text-sm">
          {row.original.sentViaProviderName ?? '-'}
        </span>
      ),
    },
    {
      accessorKey: 'queuedAt',
      header: t('email:logs.columns.queuedAt'),
      cell: ({ row }) => <RelativeTime date={row.original.queuedAt} />,
    },
    {
      accessorKey: 'sentAt',
      header: t('email:logs.columns.sentAt'),
      cell: ({ row }) =>
        row.original.sentAt ? (
          <RelativeTime date={row.original.sentAt} />
        ) : (
          <span className="text-muted-foreground">-</span>
        ),
    },
  ]
}


