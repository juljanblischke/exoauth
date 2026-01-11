// Email Provider Types
export const EmailProviderType = {
  Smtp: 0,
  SendGrid: 1,
  Mailgun: 2,
  AmazonSes: 3,
  Resend: 4,
  Postmark: 5,
} as const

export type EmailProviderType = (typeof EmailProviderType)[keyof typeof EmailProviderType]

// Email Status
export const EmailStatus = {
  Queued: 0,
  Sending: 1,
  Sent: 2,
  Failed: 3,
  InDlq: 4,
} as const

export type EmailStatus = (typeof EmailStatus)[keyof typeof EmailStatus]

// Email Announcement Status
export const EmailAnnouncementStatus = {
  Draft: 0,
  Sending: 1,
  Sent: 2,
  PartiallyFailed: 3,
} as const

export type EmailAnnouncementStatus =
  (typeof EmailAnnouncementStatus)[keyof typeof EmailAnnouncementStatus]

// Email Announcement Target
export const EmailAnnouncementTarget = {
  AllUsers: 0,
  ByPermission: 1,
  SelectedUsers: 2,
} as const

export type EmailAnnouncementTarget =
  (typeof EmailAnnouncementTarget)[keyof typeof EmailAnnouncementTarget]

// ============================================================================
// Email Provider DTOs
// ============================================================================

export interface EmailProviderConfigDto {
  fromEmail: string
  fromName: string
  // SMTP specific
  host?: string
  port?: number
  username?: string
  password?: string
  useSsl?: boolean
  // API providers (SendGrid, Resend, Postmark)
  apiKey?: string
  // Mailgun specific
  domain?: string
  region?: string // "EU" or "US"
  // Amazon SES specific
  accessKey?: string
  secretKey?: string
  awsRegion?: string
  // Postmark specific
  serverToken?: string
}

export interface EmailProviderDto {
  id: string
  name: string
  type: EmailProviderType
  priority: number
  isEnabled: boolean
  failureCount: number
  lastFailureAt: string | null
  circuitBreakerOpenUntil: string | null
  isCircuitBreakerOpen: boolean
  totalSent: number
  totalFailed: number
  successRate: number
  lastSuccessAt: string | null
  createdAt: string
  updatedAt: string | null
}

export interface EmailProviderDetailDto {
  id: string
  name: string
  type: EmailProviderType
  priority: number
  isEnabled: boolean
  configuration: EmailProviderConfigDto
  failureCount: number
  lastFailureAt: string | null
  circuitBreakerOpenUntil: string | null
  isCircuitBreakerOpen: boolean
  totalSent: number
  totalFailed: number
  successRate: number
  lastSuccessAt: string | null
  createdAt: string
  updatedAt: string | null
}

export interface CreateEmailProviderRequest {
  name: string
  type: EmailProviderType
  priority: number
  isEnabled: boolean
  configuration: EmailProviderConfigDto
}

export interface UpdateEmailProviderRequest {
  name: string
  type: EmailProviderType
  priority: number
  isEnabled: boolean
  configuration: EmailProviderConfigDto
}

export interface ProviderPriorityItem {
  providerId: string
  priority: number
}

export interface ReorderProvidersRequest {
  providers: ProviderPriorityItem[]
}

// ============================================================================
// Email Configuration DTOs
// ============================================================================

export interface EmailConfigurationDto {
  id: string
  // Retry Settings
  maxRetriesPerProvider: number
  initialRetryDelayMs: number
  maxRetryDelayMs: number
  backoffMultiplier: number
  // Circuit Breaker Settings
  circuitBreakerFailureThreshold: number
  circuitBreakerWindowMinutes: number
  circuitBreakerOpenDurationMinutes: number
  // DLQ Settings
  autoRetryDlq: boolean
  dlqRetryIntervalHours: number
  // General Settings
  emailsEnabled: boolean
  testMode: boolean
  createdAt: string
  updatedAt: string | null
}

export interface UpdateEmailConfigurationRequest {
  maxRetriesPerProvider: number
  initialRetryDelayMs: number
  maxRetryDelayMs: number
  backoffMultiplier: number
  circuitBreakerFailureThreshold: number
  circuitBreakerWindowMinutes: number
  circuitBreakerOpenDurationMinutes: number
  autoRetryDlq: boolean
  dlqRetryIntervalHours: number
  emailsEnabled: boolean
  testMode: boolean
}

// ============================================================================
// Email Log DTOs
// ============================================================================

export interface EmailLogDto {
  id: string
  recipientUserId: string | null
  recipientEmail: string
  recipientUserFullName: string | null
  subject: string
  templateName: string
  language: string
  status: EmailStatus
  retryCount: number
  lastError: string | null
  sentViaProviderId: string | null
  sentViaProviderName: string | null
  queuedAt: string
  sentAt: string | null
  failedAt: string | null
  movedToDlqAt: string | null
  announcementId: string | null
  createdAt: string
}

export interface EmailLogDetailDto extends EmailLogDto {
  templateVariables: string | null
}

export interface EmailStatusFilterOption {
  status: EmailStatus
  label: string
}

export interface EmailLogFiltersDto {
  templates: string[]
  statuses: EmailStatusFilterOption[]
}

export interface EmailLogsQueryParams {
  cursor?: string
  limit?: number
  status?: EmailStatus
  templateName?: string
  search?: string
  recipientUserId?: string
  announcementId?: string
  fromDate?: string
  toDate?: string
  sort?: string
}

// ============================================================================
// DLQ DTOs
// ============================================================================

export interface DlqQueryParams {
  cursor?: string
  limit?: number
  search?: string
  sort?: string
}

export interface RetryAllDlqEmailsResult {
  count: number
}

// ============================================================================
// Test Email DTOs
// ============================================================================

export interface SendTestEmailRequest {
  recipientEmail: string
  providerId?: string
}

export interface TestEmailResultDto {
  success: boolean
  error: string | null
  providerUsedId: string | null
  providerUsedName: string | null
  attemptCount: number
  totalProvidersAttempted: number
}

// ============================================================================
// Announcement DTOs
// ============================================================================

export interface EmailAnnouncementDto {
  id: string
  subject: string
  targetType: EmailAnnouncementTarget
  targetPermission: string | null
  totalRecipients: number
  sentCount: number
  failedCount: number
  progress: number
  status: EmailAnnouncementStatus
  createdByUserId: string
  createdByUserFullName: string | null
  sentAt: string | null
  createdAt: string
}

export interface EmailAnnouncementDetailDto {
  id: string
  subject: string
  htmlBody: string
  plainTextBody: string | null
  targetType: EmailAnnouncementTarget
  targetPermission: string | null
  targetUserIds: string[] | null
  totalRecipients: number
  sentCount: number
  failedCount: number
  progress: number
  status: EmailAnnouncementStatus
  createdByUserId: string
  createdByUserFullName: string | null
  sentAt: string | null
  createdAt: string
  updatedAt: string | null
}

export interface CreateAnnouncementRequest {
  subject: string
  htmlBody: string
  plainTextBody?: string
  targetType: EmailAnnouncementTarget
  targetPermission?: string
  targetUserIds?: string[]
}

export interface UpdateAnnouncementRequest {
  subject: string
  htmlBody: string
  plainTextBody?: string
  targetType: EmailAnnouncementTarget
  targetPermission?: string
  targetUserIds?: string[]
}

export interface PreviewAnnouncementRequest {
  subject: string
  htmlBody: string
  plainTextBody?: string
}

export interface AnnouncementPreviewDto {
  subject: string
  htmlBody: string
  plainTextBody: string | null
  estimatedRecipients: number
}

export interface AnnouncementsQueryParams {
  cursor?: string
  limit?: number
  status?: EmailAnnouncementStatus
  search?: string
  sort?: string
}
