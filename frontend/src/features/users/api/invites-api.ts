import apiClient from '@/lib/axios'
import type { ApiResponse, CursorPaginationMeta } from '@/types'
import type {
  SystemInviteListDto,
  SystemInviteDetailDto,
  SystemInvitesQueryParams,
  UpdateInviteRequest,
} from '../types'

export interface SystemInvitesResponse {
  invites: SystemInviteListDto[]
  pagination: CursorPaginationMeta
}

export const invitesApi = {
  // Get paginated list of system invites
  getAll: async (params: SystemInvitesQueryParams = {}): Promise<SystemInvitesResponse> => {
    // Convert statuses array to comma-separated string if needed
    const statusesParam = params.statuses?.length ? params.statuses.join(',') : undefined

    const { data } = await apiClient.get<ApiResponse<SystemInviteListDto[]>>(
      '/system/invites',
      {
        params: {
          cursor: params.cursor,
          limit: params.limit || 20,
          search: params.search,
          statuses: statusesParam,
          sort: params.sort,
          includeExpired: params.includeExpired,
          includeRevoked: params.includeRevoked,
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
      invites: data.data,
      pagination,
    }
  },

  // Get single invite by ID
  getById: async (id: string): Promise<SystemInviteDetailDto> => {
    const { data } = await apiClient.get<ApiResponse<SystemInviteDetailDto>>(
      `/system/invites/${id}`
    )
    return data.data
  },

  // Revoke an invite
  revoke: async (id: string): Promise<SystemInviteListDto> => {
    const { data } = await apiClient.post<ApiResponse<SystemInviteListDto>>(
      `/system/invites/${id}/revoke`
    )
    return data.data
  },

  // Resend an invite email
  resend: async (id: string): Promise<SystemInviteListDto> => {
    const { data } = await apiClient.post<ApiResponse<SystemInviteListDto>>(
      `/system/invites/${id}/resend`
    )
    return data.data
  },

  // Update an invite (only for pending invites)
  update: async (id: string, request: UpdateInviteRequest): Promise<SystemInviteDetailDto> => {
    const { data } = await apiClient.patch<ApiResponse<SystemInviteDetailDto>>(
      `/system/invites/${id}`,
      request
    )
    return data.data
  },
}
