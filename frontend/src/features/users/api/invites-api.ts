import apiClient from '@/lib/axios'
import type { ApiResponse, CursorPaginationMeta } from '@/types'
import type {
  SystemInviteListDto,
  SystemInviteDetailDto,
  SystemInvitesQueryParams,
} from '../types'

export interface SystemInvitesResponse {
  invites: SystemInviteListDto[]
  pagination: CursorPaginationMeta
}

export const invitesApi = {
  // Get paginated list of system invites
  getAll: async (params: SystemInvitesQueryParams = {}): Promise<SystemInvitesResponse> => {
    // Convert status array to comma-separated string if needed
    const statusParam = Array.isArray(params.status)
      ? params.status.join(',')
      : params.status

    const { data } = await apiClient.get<ApiResponse<SystemInviteListDto[]>>(
      '/system/invites',
      {
        params: {
          cursor: params.cursor,
          limit: params.limit || 20,
          search: params.search,
          status: statusParam,
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
}
