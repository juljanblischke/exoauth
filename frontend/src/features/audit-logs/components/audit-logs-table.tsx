import { useState, useMemo, useCallback, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { type DateRange } from 'react-day-picker'
import { type SortingState } from '@tanstack/react-table'
import { DataTable } from '@/components/shared/data-table'
import { DateRangePicker, SelectFilter } from '@/components/shared/form'
import { RelativeTime } from '@/components/shared/relative-time'
import { Badge } from '@/components/ui/badge'
import { usePermissions } from '@/contexts/auth-context'
import { useDebounce } from '@/hooks'
import { UserDetailsSheet } from '@/features/users/components/user-details-sheet'
import { useSystemUsers } from '@/features/users/hooks'
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
  const { hasPermission } = usePermissions()
  const [sorting, setSorting] = useState<SortingState>([{ id: 'createdAt', desc: true }])
  const [dateRange, setDateRange] = useState<DateRange | undefined>()
  const [actionFilters, setActionFilters] = useState<string[]>([])
  const [userFilters, setUserFilters] = useState<string[]>([])
  const [searchValue, setSearchValue] = useState('')
  const debouncedSearch = useDebounce(searchValue, 300)

  // Audit log details sheet
  const [selectedLog, setSelectedLog] = useState<SystemAuditLogDto | null>(null)
  const [logSheetOpen, setLogSheetOpen] = useState(false)

  // User details sheet (opened from audit log details)
  const [selectedUser, setSelectedUser] = useState<SystemUserDto | null>(null)
  const [userSheetOpen, setUserSheetOpen] = useState(false)

  // Check if user can read users (to show the user filter)
  const canReadUsers = hasPermission('system:users:read')

  const { data: filtersData } = useAuditLogFilters()

  // Fetch users for filter (only if user has permission) - uses first page
  const { data: usersData } = useSystemUsers({ limit: 100 })

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
    search: debouncedSearch || undefined,
    actions: actionFilters.length > 0 ? actionFilters : undefined,
    involvedUserIds: userFilters.length > 0 ? userFilters : undefined,
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

  // Build user filter options from users API (paginated, first page)
  const userOptions: SelectFilterOption[] = useMemo(() => {
    if (!usersData?.pages) return []
    const users = usersData.pages.flatMap((page) => page.users)
    return users.map((user) => ({
      label: user.fullName || user.email,
      value: user.id,
    }))
  }, [usersData])

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
    // Find user info from the users data or selected log
    const users = usersData?.pages?.flatMap((page) => page.users) ?? []
    const userInfo = users.find(u => u.id === userId)
    const user: SystemUserDto = {
      id: userId,
      email: userInfo?.email || selectedLog?.userEmail || '',
      firstName: userInfo?.firstName || '',
      lastName: userInfo?.lastName || '',
      fullName: userInfo?.fullName || selectedLog?.userFullName || '',
      isActive: userInfo?.isActive ?? true,
      emailVerified: userInfo?.emailVerified ?? true,
      mfaEnabled: userInfo?.mfaEnabled ?? false,
      lastLoginAt: userInfo?.lastLoginAt || null,
      lockedUntil: userInfo?.lockedUntil || null,
      isLocked: userInfo?.isLocked ?? false,
      isAnonymized: userInfo?.isAnonymized ?? false,
      failedLoginAttempts: userInfo?.failedLoginAttempts ?? 0,
      createdAt: userInfo?.createdAt || selectedLog?.createdAt || new Date().toISOString(),
      updatedAt: userInfo?.updatedAt || null,
    }
    setSelectedUser(user)
    setUserSheetOpen(true)
  }, [usersData, selectedLog])

  const filterContent = (
    <>
      {actionOptions.length > 0 && (
        <SelectFilter
          label={t('auditLogs:filters.action')}
          options={actionOptions}
          multiple
          values={actionFilters}
          onValuesChange={setActionFilters}
        />
      )}
      {canReadUsers && userOptions.length > 0 && (
        <SelectFilter
          label={t('auditLogs:filters.user')}
          options={userOptions}
          multiple
          values={userFilters}
          onValuesChange={setUserFilters}
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
        searchValue={searchValue}
        onSearch={setSearchValue}
        searchPlaceholder={t('auditLogs:searchPlaceholder')}
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
