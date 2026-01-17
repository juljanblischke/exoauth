import { useState, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import {
  flexRender,
  getCoreRowModel,
  useReactTable,
} from '@tanstack/react-table'
import { MailX, RotateCcw, Trash2 } from 'lucide-react'
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
import { RelativeTime } from '@/components/shared/relative-time'
import { useIsMobile } from '@/hooks/use-media-query'
import { useDlqColumns } from './email-dlq-table-columns'
import { EmailLogDetailsSheet } from './email-log-details-sheet'
import { UserDetailsSheet } from '@/features/users/components/user-details-sheet'
import { emailApi } from '../api/email-api'
import type { EmailLogDto, EmailLogDetailDto } from '../types'
import type { SystemUserDto } from '@/features/users/types'

interface EmailDlqTableProps {
  emails: EmailLogDto[]
  isLoading: boolean
  hasMore: boolean
  onLoadMore: () => void
  isFetchingMore: boolean
  onRetry: (email: EmailLogDto) => void
  onDelete: (email: EmailLogDto) => void
  canManage: boolean
}

export function EmailDlqTable({
  emails,
  isLoading,
  hasMore,
  onLoadMore,
  isFetchingMore,
  onRetry,
  onDelete,
  canManage,
}: EmailDlqTableProps) {
  const { t } = useTranslation()
  const isMobile = useIsMobile()
  const [selectedLog, setSelectedLog] = useState<EmailLogDetailDto | null>(null)
  const [sheetOpen, setSheetOpen] = useState(false)
  const [selectedUser, setSelectedUser] = useState<SystemUserDto | null>(null)
  const [userSheetOpen, setUserSheetOpen] = useState(false)

  const { ref: loadMoreRef } = useInView({
    onChange: (inView) => {
      if (inView && hasMore && !isFetchingMore) {
        onLoadMore()
      }
    },
  })

  const handleViewDetails = useCallback(async (email: EmailLogDto) => {
    setSheetOpen(true)
    try {
      const details = await emailApi.getLog(email.id)
      setSelectedLog(details)
    } catch {
      // Error handling done via toast in api layer
    }
  }, [])

  const handleUserClick = useCallback((userId: string) => {
    // Find user info from the selected log
    if (selectedLog?.recipientUserId === userId) {
      setSelectedUser({
        id: userId,
        fullName: selectedLog.recipientUserFullName || null,
        email: selectedLog.recipientEmail,
      } as SystemUserDto)
      setUserSheetOpen(true)
    }
  }, [selectedLog])

  const columns = useDlqColumns({
    onRetry,
    onDelete,
    canManage,
  })

  const table = useReactTable({
    data: emails,
    columns,
    getCoreRowModel: getCoreRowModel(),
  })

  // Mobile card render function
  const renderCard = useCallback(
    (email: EmailLogDto) => (
      <DataTableCard
        key={email.id}
        data={email}
        primaryField="recipientEmail"
        secondaryField={(row) => row.recipientUserFullName || row.subject}
        avatar={{
          name: email.recipientUserFullName || undefined,
          email: email.recipientEmail,
        }}
        onClick={() => handleViewDetails(email)}
        tertiaryFields={[
          {
            key: 'subject',
            label: t('email:dlq.columns.subject'),
          },
          {
            key: 'templateName',
            label: t('email:dlq.columns.template'),
          },
          {
            key: 'retryCount',
            label: t('email:dlq.columns.retryCount'),
            render: (value) => String(value),
          },
          {
            key: 'movedToDlqAt',
            label: t('email:dlq.columns.movedAt'),
            render: (value) => value ? <RelativeTime date={value as string} /> : '-',
          },
        ]}
        actions={
          canManage
            ? [
                {
                  label: t('email:dlq.actions.retry'),
                  icon: <RotateCcw className="h-4 w-4" />,
                  onClick: () => onRetry(email),
                },
                {
                  label: t('email:dlq.actions.delete'),
                  icon: <Trash2 className="h-4 w-4" />,
                  onClick: () => onDelete(email),
                  variant: 'destructive' as const,
                },
              ]
            : undefined
        }
      />
    ),
    [t, canManage, onRetry, onDelete, handleViewDetails]
  )

  if (isLoading && emails.length === 0) {
    return (
      <div className="space-y-4">
        {[1, 2, 3, 4, 5].map((i) => (
          <Skeleton key={i} className="h-16 w-full" />
        ))}
      </div>
    )
  }

  if (!isLoading && emails.length === 0) {
    return (
      <EmptyState
        icon={MailX}
        title={t('email:dlq.empty.title')}
        description={t('email:dlq.empty.description')}
      />
    )
  }

  return (
    <>
      {isMobile ? (
        <div className="space-y-3">
          {emails.map(renderCard)}
          {(hasMore || isFetchingMore) && (
            <div ref={loadMoreRef} className="py-4">
              {isFetchingMore && (
                <div className="space-y-3">
                  <Skeleton className="h-24 w-full" />
                  <Skeleton className="h-24 w-full" />
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
                  onClick={() => handleViewDetails(row.original)}
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
      )}

      <EmailLogDetailsSheet
        log={selectedLog}
        open={sheetOpen}
        onOpenChange={(open) => {
          setSheetOpen(open)
          if (!open) {
            setSelectedLog(null)
          }
        }}
        onUserClick={handleUserClick}
      />

      <UserDetailsSheet
        user={selectedUser}
        open={userSheetOpen}
        onOpenChange={(open) => {
          setUserSheetOpen(open)
          if (!open) {
            setSelectedUser(null)
          }
        }}
      />
    </>
  )
}
