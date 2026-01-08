import { useState, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import {
  flexRender,
  getCoreRowModel,
  useReactTable,
} from '@tanstack/react-table'
import { Mail } from 'lucide-react'
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
import { useIsMobile } from '@/hooks/use-media-query'
import { useEmailLogsColumnsWithActions } from './email-logs-table-columns'
import { EmailLogDetailsSheet } from './email-log-details-sheet'
import { EmailStatusBadge } from './email-status-badge'
import { RelativeTime } from '@/components/shared/relative-time'
import { emailApi } from '../api/email-api'
import type { EmailLogDto, EmailLogDetailDto, EmailStatus } from '../types'

interface EmailLogsTableProps {
  logs: EmailLogDto[]
  isLoading: boolean
  hasMore: boolean
  onLoadMore: () => void
  isFetchingMore: boolean
}

export function EmailLogsTable({
  logs,
  isLoading,
  hasMore,
  onLoadMore,
  isFetchingMore,
}: EmailLogsTableProps) {
  const { t } = useTranslation()
  const isMobile = useIsMobile()
  const [selectedLog, setSelectedLog] = useState<EmailLogDetailDto | null>(null)
  const [sheetOpen, setSheetOpen] = useState(false)

  const { ref: loadMoreRef } = useInView({
    onChange: (inView) => {
      if (inView && hasMore && !isFetchingMore) {
        onLoadMore()
      }
    },
  })

  const handleViewDetails = useCallback(async (log: EmailLogDto) => {
    setSheetOpen(true)
    try {
      const details = await emailApi.getLog(log.id)
      setSelectedLog(details)
    } catch {
      // Error handling done via toast in api layer
    }
  }, [])

  const columns = useEmailLogsColumnsWithActions({
    onViewDetails: handleViewDetails,
  })

  const table = useReactTable({
    data: logs,
    columns,
    getCoreRowModel: getCoreRowModel(),
  })

  // Mobile card render function
  const renderCard = useCallback(
    (log: EmailLogDto) => (
      <DataTableCard
        key={log.id}
        data={log}
        primaryField="recipientEmail"
        secondaryField="subject"
        icon={<Mail className="h-4 w-4" />}
        onClick={() => handleViewDetails(log)}
        tertiaryFields={[
          {
            key: 'templateName',
            label: t('email:logs.columns.template'),
          },
          {
            key: 'status',
            label: t('email:logs.columns.status'),
            render: (value) => <EmailStatusBadge status={value as EmailStatus} />,
          },
          {
            key: 'queuedAt',
            label: t('email:logs.columns.queuedAt'),
            render: (value) => <RelativeTime date={value as string} />,
          },
        ]}
      />
    ),
    [t, handleViewDetails]
  )

  if (isLoading && logs.length === 0) {
    return (
      <div className="space-y-4">
        {[1, 2, 3, 4, 5].map((i) => (
          <Skeleton key={i} className="h-16 w-full" />
        ))}
      </div>
    )
  }

  if (!isLoading && logs.length === 0) {
    return (
      <EmptyState
        icon={Mail}
        title={t('email:logs.empty.title')}
        description={t('email:logs.empty.description')}
      />
    )
  }

  return (
    <>
      {isMobile ? (
        <div className="space-y-3">
          {logs.map(renderCard)}
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
                <TableRow key={row.id}>
                  {row.getVisibleCells().map((cell) => (
                    <TableCell key={cell.id}>
                      {flexRender(
                        cell.column.columnDef.cell,
                        cell.getContext()
                      )}
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
      />
    </>
  )
}
