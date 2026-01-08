import { useState, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { AlertCircle, Search } from 'lucide-react'

import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Input } from '@/components/ui/input'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { DateRangePicker } from '@/components/shared/form'
import { useDebounce } from '@/hooks/use-debounce'
import { useEmailLogs } from '../hooks/use-email-logs'
import { EmailLogsTable } from './email-logs-table'
import { EmailStatus } from '../types'
import type { DateRange } from 'react-day-picker'

const STATUS_OPTIONS = [
  { value: 'all', labelKey: 'email:logs.filters.statusPlaceholder' },
  { value: String(EmailStatus.Queued), labelKey: 'email:logs.status.queued' },
  { value: String(EmailStatus.Sending), labelKey: 'email:logs.status.sending' },
  { value: String(EmailStatus.Sent), labelKey: 'email:logs.status.sent' },
  { value: String(EmailStatus.Failed), labelKey: 'email:logs.status.failed' },
  { value: String(EmailStatus.InDlq), labelKey: 'email:logs.status.inDlq' },
  { value: String(EmailStatus.RetriedFromDlq), labelKey: 'email:logs.status.retriedFromDlq' },
]

export function EmailLogsTab() {
  const { t } = useTranslation()

  // Filter state
  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState<string>('all')
  const [dateRange, setDateRange] = useState<DateRange | undefined>(undefined)

  const debouncedSearch = useDebounce(search, 300)

  // Build query params
  const queryParams = {
    search: debouncedSearch || undefined,
    status: statusFilter !== 'all' ? (Number(statusFilter) as EmailStatus) : undefined,
    fromDate: dateRange?.from?.toISOString(),
    toDate: dateRange?.to?.toISOString(),
  }

  const {
    data,
    isLoading,
    error,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage,
  } = useEmailLogs(queryParams)

  const logs = data?.pages.flatMap((page) => page.logs) ?? []

  const handleLoadMore = useCallback(() => {
    if (hasNextPage && !isFetchingNextPage) {
      fetchNextPage()
    }
  }, [fetchNextPage, hasNextPage, isFetchingNextPage])

  if (error) {
    return (
      <Alert variant="destructive">
        <AlertCircle className="h-4 w-4" />
        <AlertTitle>{t('common:error')}</AlertTitle>
        <AlertDescription>{t('email:errors.loadLogs')}</AlertDescription>
      </Alert>
    )
  }

  return (
    <div className="space-y-4">
      {/* Filters */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center">
        {/* Search */}
        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder={t('email:logs.filters.search')}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="pl-9"
          />
        </div>

        {/* Status Filter */}
        <Select value={statusFilter} onValueChange={setStatusFilter}>
          <SelectTrigger className="w-[180px]">
            <SelectValue placeholder={t('email:logs.filters.status')} />
          </SelectTrigger>
          <SelectContent>
            {STATUS_OPTIONS.map((option) => (
              <SelectItem key={option.value} value={option.value}>
                {t(option.labelKey)}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        {/* Date Range */}
        <DateRangePicker
          value={dateRange}
          onChange={setDateRange}
          placeholder={t('email:logs.filters.dateRange')}
        />
      </div>

      {/* Table */}
      <EmailLogsTable
        logs={logs}
        isLoading={isLoading}
        hasMore={hasNextPage ?? false}
        onLoadMore={handleLoadMore}
        isFetchingMore={isFetchingNextPage}
      />
    </div>
  )
}
