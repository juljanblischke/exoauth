import { useTranslation } from 'react-i18next'
import type { ColumnDef } from '@tanstack/react-table'
import { UserAvatar } from '@/components/shared/user-avatar'
import { RelativeTime } from '@/components/shared/relative-time'
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
        return (
          <div className="flex items-center gap-3">
            <UserAvatar
              name={log.recipientUserFullName || ''}
              email={log.recipientEmail}
              size="sm"
            />
            <div className="flex flex-col min-w-0">
              <span className="font-medium truncate">
                {log.recipientUserFullName || log.recipientEmail}
              </span>
              {log.recipientUserFullName && (
                <span className="text-xs text-muted-foreground truncate">
                  {log.recipientEmail}
                </span>
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
        <div className="max-w-[200px] truncate" title={row.original.subject}>
          {row.original.subject}
        </div>
      ),
    },
    {
      accessorKey: 'templateName',
      header: t('email:logs.columns.template'),
      cell: ({ row }) => (
        <span className="text-sm text-muted-foreground font-mono">
          {row.original.templateName}
        </span>
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


