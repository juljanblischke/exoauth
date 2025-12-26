import { useInfiniteQuery } from '@tanstack/react-query'
import { auditLogsApi } from '../api/audit-logs-api'
import type { AuditLogsQueryParams } from '../types'

export const AUDIT_LOGS_KEY = ['system', 'audit-logs'] as const

interface UseAuditLogsOptions {
  sort?: string
  action?: string
  userId?: string
  from?: string
  to?: string
  entityType?: string
  entityId?: string
  limit?: number
}

export function useAuditLogs(options: UseAuditLogsOptions = {}) {
  const { sort, action, userId, from, to, entityType, entityId, limit = 20 } = options

  return useInfiniteQuery({
    queryKey: [...AUDIT_LOGS_KEY, { sort, action, userId, from, to, entityType, entityId }],
    queryFn: async ({ pageParam }) => {
      const params: AuditLogsQueryParams = {
        cursor: pageParam as string | undefined,
        limit,
        sort,
        action,
        userId,
        from,
        to,
        entityType,
        entityId,
      }
      return auditLogsApi.getAll(params)
    },
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage) =>
      lastPage.pagination.hasMore ? lastPage.pagination.nextCursor : undefined,
  })
}
