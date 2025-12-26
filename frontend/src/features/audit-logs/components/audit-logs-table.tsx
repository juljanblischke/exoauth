import { useState, useMemo, useCallback, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { type DateRange } from 'react-day-picker'
import { type SortingState } from '@tanstack/react-table'
import { DataTable } from '@/components/shared/data-table'
import { DateRangePicker, SelectFilter } from '@/components/shared/form'
import { RelativeTime } from '@/components/shared/relative-time'
import { Badge } from '@/components/ui/badge'
import { UserDetailsSheet } from '@/features/users/components/user-details-sheet'
import type { SystemUserDto } from '@/features/users/types'
import { useAuditLogs, useAuditLogFilters } from '../hooks'
import { useAuditLogsColumns } from './audit-logs-table-columns'
import { AuditLogDetailsSheet } from './audit-log-details-sheet'
import type { SelectFilterOption } from '@/components/shared/form'
import type { SystemAuditLogDto } from '../types'

const sortFieldMap: Record<string, string> = {
  createdAt: 'createdAt',
}

export function AuditLogsTable() {
  const { t } = useTranslation()
  const [sorting, setSorting] = useState<SortingState>([{ id: 'createdAt', desc: true }])
  const [dateRange, setDateRange] = useState<DateRange | undefined>()
  const [actionFilter, setActionFilter] = useState<string | undefined>()
  const [userFilter, setUserFilter] = useState<string | undefined>()

  // Audit log details sheet
  const [selectedLog, setSelectedLog] = useState<SystemAuditLogDto | null>(null)
  const [logSheetOpen, setLogSheetOpen] = useState(false)

  // User details sheet (opened from audit log details)
  const [selectedUser, setSelectedUser] = useState<SystemUserDto | null>(null)
  const [userSheetOpen, setUserSheetOpen] = useState(false)

  const { data: filtersData } = useAuditLogFilters()

  const fromDate = dateRange?.from?.toISOString()
  const toDate = dateRange?.to?.toISOString()

  // Convert sorting state to backend format
  const sortParam = useMemo(() => {
    if (sorting.length === 0) return undefined
    return sorting
      .map((s) => {
        const field = sortFieldMap[s.id] || s.id
        return `${field}:${s.desc ? 'desc' : 'asc'}`
      })
      .join(',')
  }, [sorting])

  const {
    data,
    isLoading,
    isFetching,
    fetchNextPage,
    hasNextPage,
  } = useAuditLogs({
    sort: sortParam,
    action: actionFilter,
    userId: userFilter,
    from: fromDate,
    to: toDate,
  })

  const logs = useMemo(
    () => data?.pages.flatMap((page) => page.logs) ?? [],
    [data]
  )

  const columns = useAuditLogsColumns()

  // Build filter options from API data
  const actionOptions: SelectFilterOption[] = useMemo(() => {
    if (!filtersData?.actions) return []
    return filtersData.actions.map((action) => ({
      label: action,
      value: action,
    }))
  }, [filtersData])

  const userOptions: SelectFilterOption[] = useMemo(() => {
    if (!filtersData?.users) return []
    return filtersData.users.map((user) => ({
      label: user.fullName || user.email,
      value: user.id,
    }))
  }, [filtersData])

  const handleLoadMore = useCallback(() => {
    if (hasNextPage && !isFetching) {
      fetchNextPage()
    }
  }, [hasNextPage, isFetching, fetchNextPage])

  const handleRowClick = useCallback((log: SystemAuditLogDto) => {
    setSelectedLog(log)
    setLogSheetOpen(true)
  }, [])

  const handleUserClick = useCallback((userId: string) => {
    // Find user info from the selected log or filters data
    const userInfo = filtersData?.users.find(u => u.id === userId)
    const user: SystemUserDto = {
      id: userId,
      email: userInfo?.email || selectedLog?.userEmail || '',
      firstName: '',
      lastName: '',
      fullName: userInfo?.fullName || selectedLog?.userFullName || '',
      isActive: true,
      emailVerified: true,
      lastLoginAt: null,
      createdAt: selectedLog?.createdAt || new Date().toISOString(),
      updatedAt: null,
    }
    setSelectedUser(user)
    setUserSheetOpen(true)
  }, [filtersData?.users, selectedLog])

  const filterContent = (
    <>
      {actionOptions.length > 0 && (
        <SelectFilter
          label={t('auditLogs:filters.action')}
          options={actionOptions}
          value={actionFilter}
          onChange={setActionFilter}
        />
      )}
      {userOptions.length > 0 && (
        <SelectFilter
          label={t('auditLogs:filters.user')}
          options={userOptions}
          value={userFilter}
          onChange={setUserFilter}
        />
      )}
      <DateRangePicker
        value={dateRange}
        onChange={setDateRange}
        placeholder={t('auditLogs:filters.dateRange')}
      />
    </>
  )

  return (
    <>
      <DataTable
        columns={columns}
        data={logs}
        isLoading={isLoading}
        isFetching={isFetching}
        hasMore={hasNextPage}
        onLoadMore={handleLoadMore}
        initialSorting={sorting}
        onSortingChange={setSorting}
        toolbarContent={filterContent}
        onRowClick={handleRowClick}
        emptyState={{
          title: t('auditLogs:empty.title'),
          description: t('auditLogs:empty.description'),
        }}
        mobileCard={{
          primaryField: 'action',
          secondaryField: 'userFullName',
          avatar: (row) => ({
            name: row.userFullName || undefined,
            email: row.userEmail || undefined,
          }),
          tertiaryFields: [
            {
              key: 'action',
              render: (value): ReactNode => (
                <Badge variant="outline" className="font-mono text-xs">
                  {value as string}
                </Badge>
              ),
            },
            {
              key: 'createdAt',
              label: t('auditLogs:fields.time'),
              render: (value): ReactNode => <RelativeTime date={value as string} />,
            },
            {
              key: 'ipAddress',
              label: t('auditLogs:fields.ipAddress'),
              render: (value): ReactNode =>
                value ? (
                  <span className="font-mono text-xs">{value as string}</span>
                ) : (
                  '-'
                ),
            },
          ],
        }}
      />

      <AuditLogDetailsSheet
        open={logSheetOpen}
        onOpenChange={setLogSheetOpen}
        log={selectedLog}
        onUserClick={handleUserClick}
      />

      <UserDetailsSheet
        open={userSheetOpen}
        onOpenChange={setUserSheetOpen}
        user={selectedUser}
      />
    </>
  )
}
