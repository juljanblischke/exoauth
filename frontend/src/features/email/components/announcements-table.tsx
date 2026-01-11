import { useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import {
  flexRender,
  getCoreRowModel,
  useReactTable,
} from '@tanstack/react-table'
import { Megaphone, Edit, Send, Trash2 } from 'lucide-react'
import { useInView } from 'react-intersection-observer'

import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { Skeleton } from '@/components/ui/skeleton'
import { EmptyState } from '@/components/shared/feedback'
import { DataTableCard } from '@/components/shared/data-table'
import type { RowAction } from '@/types/table'
import { RelativeTime } from '@/components/shared/relative-time'
import { useIsMobile } from '@/hooks/use-media-query'
import { useAnnouncementsColumns } from './announcements-table-columns'
import { AnnouncementStatusBadge } from './announcement-status-badge'
import { EmailAnnouncementStatus, EmailAnnouncementTarget, type EmailAnnouncementDto } from '../types'

interface AnnouncementsTableProps {
  announcements: EmailAnnouncementDto[]
  isLoading: boolean
  hasMore: boolean
  onLoadMore: () => void
  isFetchingMore: boolean
  onEdit: (announcement: EmailAnnouncementDto) => void
  onSend: (announcement: EmailAnnouncementDto) => void
  onViewDetails: (announcement: EmailAnnouncementDto) => void
  onDelete: (announcement: EmailAnnouncementDto) => void
  canManage: boolean
}

export function AnnouncementsTable({
  announcements,
  isLoading,
  hasMore,
  onLoadMore,
  isFetchingMore,
  onEdit,
  onSend,
  onViewDetails,
  onDelete,
  canManage,
}: AnnouncementsTableProps) {
  const { t } = useTranslation()
  const isMobile = useIsMobile()

  const { ref: loadMoreRef } = useInView({
    onChange: (inView) => {
      if (inView && hasMore && !isFetchingMore) {
        onLoadMore()
      }
    },
  })

  const columns = useAnnouncementsColumns({
    onEdit,
    onSend,
    onViewDetails,
    onDelete,
    canManage,
  })

  const table = useReactTable({
    data: announcements,
    columns,
    getCoreRowModel: getCoreRowModel(),
  })

  // Mobile card render function
  const renderCard = useCallback(
    (announcement: EmailAnnouncementDto) => {
      const isDraft = announcement.status === EmailAnnouncementStatus.Draft

      const actions: RowAction<EmailAnnouncementDto>[] = []
      if (canManage && isDraft) {
        actions.push(
          {
            label: t('common:actions.edit'),
            icon: <Edit className="h-4 w-4" />,
            onClick: () => onEdit(announcement),
          },
          {
            label: t('email:announcements.send.button'),
            icon: <Send className="h-4 w-4" />,
            onClick: () => onSend(announcement),
          },
          {
            label: t('common:actions.delete'),
            icon: <Trash2 className="h-4 w-4" />,
            onClick: () => onDelete(announcement),
            variant: 'destructive' as const,
          }
        )
      }

      return (
        <DataTableCard
          key={announcement.id}
          data={announcement}
          primaryField="subject"
          secondaryField={(row) => {
            const target = row.targetType === EmailAnnouncementTarget.AllUsers 
              ? t('email:announcements.target.allUsers')
              : row.targetType === EmailAnnouncementTarget.ByPermission
                ? row.targetPermission || t('email:announcements.target.byPermission')
                : t('email:announcements.target.selectedUsers')
            return target
          }}
          icon={<Megaphone className="h-4 w-4" />}
          onClick={() => onViewDetails(announcement)}
          tertiaryFields={[
            {
              key: 'status',
              label: t('email:announcements.columns.status'),
              render: (value) => <AnnouncementStatusBadge status={value as EmailAnnouncementStatus} />,
            },
            {
              key: 'createdAt',
              label: t('email:announcements.columns.createdAt'),
              render: (value) => <RelativeTime date={value as string} />,
            },
            ...(announcement.status !== EmailAnnouncementStatus.Draft
              ? [
                  {
                    key: 'progress' as keyof EmailAnnouncementDto,
                    label: t('email:announcements.columns.progress'),
                    render: (_: unknown, row: EmailAnnouncementDto) => (
                      <span className="text-xs">
                        {row.sentCount}/{row.totalRecipients}
                      </span>
                    ),
                  },
                ]
              : []),
          ]}
          actions={actions.length > 0 ? actions : undefined}
        />
      )
    },
    [t, canManage, onEdit, onSend, onViewDetails, onDelete]
  )

  if (isLoading && announcements.length === 0) {
    return (
      <div className="space-y-4">
        {[1, 2, 3, 4, 5].map((i) => (
          <Skeleton key={i} className="h-16 w-full" />
        ))}
      </div>
    )
  }

  if (!isLoading && announcements.length === 0) {
    return (
      <EmptyState
        icon={Megaphone}
        title={t('email:announcements.empty.title')}
        description={t('email:announcements.empty.description')}
      />
    )
  }

  return isMobile ? (
    <div className="space-y-3">
      {announcements.map(renderCard)}
      {(hasMore || isFetchingMore) && (
        <div ref={loadMoreRef} className="py-4">
          {isFetchingMore && (
            <div className="space-y-3">
              <Skeleton className="h-32 w-full" />
              <Skeleton className="h-32 w-full" />
            </div>
          )}
        </div>
      )}
    </div>
  ) : (
    <div className="border rounded-md">
      <Table>
        <TableHeader>
          {table.getHeaderGroups().map((headerGroup) => (
            <TableRow key={headerGroup.id}>
              {headerGroup.headers.map((header) => (
                <TableHead key={header.id}>
                  {header.isPlaceholder
                    ? null
                    : flexRender(
                        header.column.columnDef.header,
                        header.getContext()
                      )}
                </TableHead>
              ))}
            </TableRow>
          ))}
        </TableHeader>
        <TableBody>
          {table.getRowModel().rows.map((row) => (
            <TableRow
              key={row.id}
              className="cursor-pointer hover:bg-muted/50"
              onClick={() => onViewDetails(row.original)}
            >
              {row.getVisibleCells().map((cell) => (
                <TableCell
                  key={cell.id}
                  onClick={(e) => {
                    // Prevent row click when clicking on actions
                    if (cell.column.id === 'actions') {
                      e.stopPropagation()
                    }
                  }}
                >
                  {flexRender(cell.column.columnDef.cell, cell.getContext())}
                </TableCell>
              ))}
            </TableRow>
          ))}
          {(hasMore || isFetchingMore) && (
            <TableRow ref={loadMoreRef}>
              <TableCell colSpan={columns.length}>
                {isFetchingMore && (
                  <div className="flex justify-center py-4">
                    <Skeleton className="h-8 w-32" />
                  </div>
                )}
              </TableCell>
            </TableRow>
          )}
        </TableBody>
      </Table>
    </div>
  )
}
