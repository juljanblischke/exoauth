import { useQuery } from '@tanstack/react-query'
import { auditLogsApi } from '../api/audit-logs-api'

export const AUDIT_LOG_FILTERS_KEY = ['system', 'audit-logs', 'filters'] as const

export function useAuditLogFilters() {
  return useQuery({
    queryKey: AUDIT_LOG_FILTERS_KEY,
    queryFn: auditLogsApi.getFilters,
    staleTime: 5 * 60 * 1000, // 5 minutes
  })
}
