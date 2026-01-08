import apiClient from '@/lib/axios'
import type { ApiResponse, CursorPaginationMeta } from '@/types'
import type {
  EmailProviderDto,
  EmailProviderDetailDto,
  CreateEmailProviderRequest,
  UpdateEmailProviderRequest,
  ReorderProvidersRequest,
  EmailConfigurationDto,
  UpdateEmailConfigurationRequest,
  EmailLogDto,
  EmailLogDetailDto,
  EmailLogFiltersDto,
  EmailLogsQueryParams,
  DlqQueryParams,
  RetryAllDlqEmailsResult,
  SendTestEmailRequest,
  TestEmailResultDto,
  EmailAnnouncementDto,
  EmailAnnouncementDetailDto,
  CreateAnnouncementRequest,
  UpdateAnnouncementRequest,
  PreviewAnnouncementRequest,
  AnnouncementPreviewDto,
  AnnouncementsQueryParams,
} from '../types'

// Response types with pagination
export interface EmailLogsResponse {
  logs: EmailLogDto[]
  pagination: CursorPaginationMeta
}

export interface DlqEmailsResponse {
  emails: EmailLogDto[]
  pagination: CursorPaginationMeta
}

export interface AnnouncementsResponse {
  announcements: EmailAnnouncementDto[]
  pagination: CursorPaginationMeta
}

export const emailApi = {
  // ============================================================================
  // Providers
  // ============================================================================

  getProviders: async (): Promise<EmailProviderDto[]> => {
    const { data } = await apiClient.get<ApiResponse<EmailProviderDto[]>>(
      '/system/email/providers'
    )
    return data.data
  },

  getProvider: async (id: string): Promise<EmailProviderDetailDto> => {
    const { data } = await apiClient.get<ApiResponse<EmailProviderDetailDto>>(
      `/system/email/providers/${id}`
    )
    return data.data
  },

  createProvider: async (request: CreateEmailProviderRequest): Promise<EmailProviderDto> => {
    const { data } = await apiClient.post<ApiResponse<EmailProviderDto>>(
      '/system/email/providers',
      request
    )
    return data.data
  },

  updateProvider: async (
    id: string,
    request: UpdateEmailProviderRequest
  ): Promise<EmailProviderDto> => {
    const { data } = await apiClient.put<ApiResponse<EmailProviderDto>>(
      `/system/email/providers/${id}`,
      request
    )
    return data.data
  },

  deleteProvider: async (id: string): Promise<void> => {
    await apiClient.delete(`/system/email/providers/${id}`)
  },

  testProvider: async (request: SendTestEmailRequest): Promise<TestEmailResultDto> => {
    const { data } = await apiClient.post<ApiResponse<TestEmailResultDto>>(
      '/system/email/test',
      request
    )
    return data.data
  },

  resetCircuitBreaker: async (id: string): Promise<EmailProviderDto> => {
    const { data } = await apiClient.post<ApiResponse<EmailProviderDto>>(
      `/system/email/providers/${id}/reset-circuit-breaker`
    )
    return data.data
  },

  reorderProviders: async (request: ReorderProvidersRequest): Promise<EmailProviderDto[]> => {
    const { data } = await apiClient.post<ApiResponse<EmailProviderDto[]>>(
      '/system/email/providers/reorder',
      request
    )
    return data.data
  },

  // ============================================================================
  // Configuration
  // ============================================================================

  getConfiguration: async (): Promise<EmailConfigurationDto> => {
    const { data } = await apiClient.get<ApiResponse<EmailConfigurationDto>>(
      '/system/email/configuration'
    )
    return data.data
  },

  updateConfiguration: async (
    request: UpdateEmailConfigurationRequest
  ): Promise<EmailConfigurationDto> => {
    const { data } = await apiClient.put<ApiResponse<EmailConfigurationDto>>(
      '/system/email/configuration',
      request
    )
    return data.data
  },

  // ============================================================================
  // Logs
  // ============================================================================

  getLogs: async (params: EmailLogsQueryParams = {}): Promise<EmailLogsResponse> => {
    const { data } = await apiClient.get<ApiResponse<EmailLogDto[]>>('/system/email/logs', {
      params: {
        cursor: params.cursor,
        limit: params.limit || 20,
        status: params.status,
        templateName: params.templateName,
        search: params.search,
        recipientUserId: params.recipientUserId,
        announcementId: params.announcementId,
        fromDate: params.fromDate,
        toDate: params.toDate,
        sort: params.sort,
      },
    })
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

  getLog: async (id: string): Promise<EmailLogDetailDto> => {
    const { data } = await apiClient.get<ApiResponse<EmailLogDetailDto>>(
      `/system/email/logs/${id}`
    )
    return data.data
  },

  getLogFilters: async (): Promise<EmailLogFiltersDto> => {
    const { data } = await apiClient.get<ApiResponse<EmailLogFiltersDto>>(
      '/system/email/logs/filters'
    )
    return data.data
  },

  // ============================================================================
  // DLQ
  // ============================================================================

  getDlqEmails: async (params: DlqQueryParams = {}): Promise<DlqEmailsResponse> => {
    const { data } = await apiClient.get<ApiResponse<EmailLogDto[]>>('/system/email/dlq', {
      params: {
        cursor: params.cursor,
        limit: params.limit || 20,
        search: params.search,
        sort: params.sort,
      },
    })
    const pagination: CursorPaginationMeta = (data.meta
      ?.pagination as unknown as CursorPaginationMeta) ?? {
      cursor: null,
      nextCursor: null,
      hasMore: false,
    }
    return {
      emails: data.data,
      pagination,
    }
  },

  retryDlqEmail: async (id: string): Promise<EmailLogDto> => {
    const { data } = await apiClient.post<ApiResponse<EmailLogDto>>(
      `/system/email/dlq/${id}/retry`
    )
    return data.data
  },

  retryAllDlqEmails: async (): Promise<RetryAllDlqEmailsResult> => {
    const { data } = await apiClient.post<ApiResponse<RetryAllDlqEmailsResult>>(
      '/system/email/dlq/retry-all'
    )
    return data.data
  },

  deleteDlqEmail: async (id: string): Promise<void> => {
    await apiClient.delete(`/system/email/dlq/${id}`)
  },

  // ============================================================================
  // Announcements
  // ============================================================================

  getAnnouncements: async (params: AnnouncementsQueryParams = {}): Promise<AnnouncementsResponse> => {
    const { data } = await apiClient.get<ApiResponse<EmailAnnouncementDto[]>>(
      '/system/email/announcements',
      {
        params: {
          cursor: params.cursor,
          limit: params.limit || 20,
          status: params.status,
          search: params.search,
          sort: params.sort,
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
      announcements: data.data,
      pagination,
    }
  },

  getAnnouncement: async (id: string): Promise<EmailAnnouncementDetailDto> => {
    const { data } = await apiClient.get<ApiResponse<EmailAnnouncementDetailDto>>(
      `/system/email/announcements/${id}`
    )
    return data.data
  },

  createAnnouncement: async (
    request: CreateAnnouncementRequest
  ): Promise<EmailAnnouncementDetailDto> => {
    const { data } = await apiClient.post<ApiResponse<EmailAnnouncementDetailDto>>(
      '/system/email/announcements',
      request
    )
    return data.data
  },

  updateAnnouncement: async (
    id: string,
    request: UpdateAnnouncementRequest
  ): Promise<EmailAnnouncementDetailDto> => {
    const { data } = await apiClient.put<ApiResponse<EmailAnnouncementDetailDto>>(
      `/system/email/announcements/${id}`,
      request
    )
    return data.data
  },

  deleteAnnouncement: async (id: string): Promise<void> => {
    await apiClient.delete(`/system/email/announcements/${id}`)
  },

  sendAnnouncement: async (id: string): Promise<EmailAnnouncementDto> => {
    const { data } = await apiClient.post<ApiResponse<EmailAnnouncementDto>>(
      `/system/email/announcements/${id}/send`
    )
    return data.data
  },

  previewAnnouncement: async (
    request: PreviewAnnouncementRequest
  ): Promise<AnnouncementPreviewDto> => {
    const { data } = await apiClient.post<ApiResponse<AnnouncementPreviewDto>>(
      '/system/email/announcements/preview',
      request
    )
    return data.data
  },
}
