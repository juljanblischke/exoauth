// System Audit Log DTOs
export interface SystemAuditLogDto {
  id: string
  userId: string | null
  userEmail: string | null
  userFullName: string | null
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
  users: AuditLogUserFilterDto[]
  earliestDate: string | null
  latestDate: string | null
}

export interface AuditLogUserFilterDto {
  id: string
  email: string
  fullName: string
}

// Query params
export interface AuditLogsQueryParams {
  cursor?: string
  limit?: number
  sort?: string
  action?: string
  userId?: string
  from?: string
  to?: string
  entityType?: string
  entityId?: string
}
