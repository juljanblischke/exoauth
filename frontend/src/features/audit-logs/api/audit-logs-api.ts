import apiClient from '@/lib/axios'
import type { ApiResponse, CursorPaginationMeta } from '@/types'
import type {
  SystemAuditLogDto,
  AuditLogFiltersDto,
  AuditLogsQueryParams,
} from '../types'

export interface AuditLogsResponse {
  logs: SystemAuditLogDto[]
  pagination: CursorPaginationMeta
}

export const auditLogsApi = {
  // Get paginated list of audit logs
  getAll: async (params: AuditLogsQueryParams = {}): Promise<AuditLogsResponse> => {
    const { data } = await apiClient.get<ApiResponse<SystemAuditLogDto[]>>(
      '/system/audit-logs',
      {
        params: {
          cursor: params.cursor,
          limit: params.limit || 20,
          sort: params.sort,
          action: params.action,
          userId: params.userId,
          from: params.from,
          to: params.to,
          entityType: params.entityType,
          entityId: params.entityId,
        },
      }
    )
    const pagination: CursorPaginationMeta = (data.meta
      ?.pagination as unknown as CursorPaginationMeta) ?? {
      cursor: null,
      nextCursor: null,
      hasMore: false,
    }
    return {
      logs: data.data,
      pagination,
    }
  },

  // Get available filter options
  getFilters: async (): Promise<AuditLogFiltersDto> => {
    const { data } = await apiClient.get<ApiResponse<AuditLogFiltersDto>>(
      '/system/audit-logs/filters'
    )
    return data.data
  },
}
