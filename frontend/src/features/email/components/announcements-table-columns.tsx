import { useTranslation } from 'react-i18next'
import type { ColumnDef } from '@tanstack/react-table'
import { MoreHorizontal, Edit, Send, Trash2 } from 'lucide-react'

import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { Progress } from '@/components/ui/progress'
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from '@/components/ui/tooltip'
import { RelativeTime } from '@/components/shared/relative-time'
import { AnnouncementStatusBadge } from './announcement-status-badge'
import { AnnouncementTargetBadge } from './announcement-target-badge'
import { EmailAnnouncementStatus, type EmailAnnouncementDto } from '../types'

interface UseAnnouncementsColumnsOptions {
  onEdit: (announcement: EmailAnnouncementDto) => void
  onSend: (announcement: EmailAnnouncementDto) => void
  onViewDetails: (announcement: EmailAnnouncementDto) => void
  onDelete: (announcement: EmailAnnouncementDto) => void
  canManage: boolean
}

export function useAnnouncementsColumns({
  onEdit,
  onSend,
  onDelete,
  canManage,
}: UseAnnouncementsColumnsOptions): ColumnDef<EmailAnnouncementDto>[] {
  const { t } = useTranslation()

  const columns: ColumnDef<EmailAnnouncementDto>[] = [
    {
      accessorKey: 'subject',
      header: t('email:announcements.columns.subject'),
      cell: ({ row }) => (
        <Tooltip>
          <TooltipTrigger asChild>
            <div className="max-w-[250px] truncate font-medium cursor-default">
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
      accessorKey: 'targetType',
      header: t('email:announcements.columns.target'),
      cell: ({ row }) => (
        <AnnouncementTargetBadge
          target={row.original.targetType}
          permission={row.original.targetPermission}
        />
      ),
    },
    {
      accessorKey: 'totalRecipients',
      header: t('email:announcements.columns.recipients'),
      cell: ({ row }) => {
        const announcement = row.original
        if (announcement.status === EmailAnnouncementStatus.Draft) {
          return <span className="text-muted-foreground">-</span>
        }
        return (
          <div className="space-y-1">
            <div className="text-sm">
              {announcement.sentCount} / {announcement.totalRecipients}
            </div>
            <Progress value={announcement.progress} className="h-1 w-16" />
          </div>
        )
      },
    },
    {
      accessorKey: 'status',
      header: t('email:announcements.columns.status'),
      cell: ({ row }) => <AnnouncementStatusBadge status={row.original.status} />,
    },
    {
      accessorKey: 'createdByUserFullName',
      header: t('email:announcements.columns.createdBy'),
      cell: ({ row }) => {
        const name = row.original.createdByUserFullName
        if (!name) {
          return <span className="text-muted-foreground">-</span>
        }
        return (
          <Tooltip>
            <TooltipTrigger asChild>
              <span className="text-sm truncate max-w-[120px] block">
                {name}
              </span>
            </TooltipTrigger>
            <TooltipContent>
              <p>{name}</p>
            </TooltipContent>
          </Tooltip>
        )
      },
    },
    {
      accessorKey: 'createdAt',
      header: t('email:announcements.columns.createdAt'),
      cell: ({ row }) => <RelativeTime date={row.original.createdAt} />,
    },
    {
      accessorKey: 'sentAt',
      header: t('email:announcements.columns.sentAt'),
      cell: ({ row }) =>
        row.original.sentAt ? (
          <RelativeTime date={row.original.sentAt} />
        ) : (
          <span className="text-muted-foreground">-</span>
        ),
    },
  ]

  // Actions column - only show for drafts that can be managed
  if (canManage) {
    columns.push({
      id: 'actions',
      cell: ({ row }) => {
        const announcement = row.original
        const isDraft = announcement.status === EmailAnnouncementStatus.Draft

        // Only show actions for drafts
        if (!isDraft) {
          return null
        }

        return (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon">
                <MoreHorizontal className="h-4 w-4" />
                <span className="sr-only">{t('common:actions.openMenu')}</span>
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem onClick={() => onEdit(announcement)}>
                <Edit className="mr-2 h-4 w-4" />
                {t('common:actions.edit')}
              </DropdownMenuItem>
              <DropdownMenuItem onClick={() => onSend(announcement)}>
                <Send className="mr-2 h-4 w-4" />
                {t('email:announcements.send.button')}
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem
                onClick={() => onDelete(announcement)}
                className="text-destructive focus:text-destructive"
              >
                <Trash2 className="mr-2 h-4 w-4" />
                {t('common:actions.delete')}
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        )
      },
    })
  }

  return columns
}
