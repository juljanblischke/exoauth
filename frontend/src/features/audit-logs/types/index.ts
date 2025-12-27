// System Audit Log DTOs
export interface SystemAuditLogDto {
  id: string
  userId: string | null
  userEmail: string | null
  userFullName: string | null
  targetUserId: string | null
  targetUserEmail: string | null
  targetUserFullName: string | null
  action: string
  entityType: string | null
  entityId: string | null
  ipAddress: string | null
  userAgent: string | null
  details: Record<string, unknown> | null
  createdAt: string
}

// Filter DTOs
export interface AuditLogFiltersDto {
  actions: string[]
  earliestDate: string | null
  latestDate: string | null
}

// Query params
export interface AuditLogsQueryParams {
  cursor?: string
  limit?: number
  sort?: string
  search?: string
  actions?: string[]
  involvedUserIds?: string[]
  from?: string
  to?: string
  entityType?: string
  entityId?: string
}
