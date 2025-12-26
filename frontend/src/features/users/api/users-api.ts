import apiClient from '@/lib/axios'
import type { ApiResponse, CursorPaginationMeta } from '@/types'
import type {
  SystemUserDto,
  SystemUserDetailDto,
  SystemInviteDto,
  InviteUserRequest,
  UpdateUserRequest,
  UpdatePermissionsRequest,
  SystemUsersQueryParams,
} from '../types'

export interface SystemUsersResponse {
  users: SystemUserDto[]
  pagination: CursorPaginationMeta
}

export const usersApi = {
  // Get paginated list of system users
  getAll: async (params: SystemUsersQueryParams = {}): Promise<SystemUsersResponse> => {
    const { data } = await apiClient.get<ApiResponse<SystemUserDto[]>>('/system/users', {
      params: {
        cursor: params.cursor,
        limit: params.limit || 20,
        sort: params.sort,
        search: params.search,
      },
    })
    const pagination: CursorPaginationMeta = (data.meta?.pagination as unknown as CursorPaginationMeta) ?? {
      cursor: null,
      nextCursor: null,
      hasMore: false,
    }
    return {
      users: data.data,
      pagination,
    }
  },

  // Get single user by ID
  getById: async (id: string): Promise<SystemUserDetailDto> => {
    const { data } = await apiClient.get<ApiResponse<SystemUserDetailDto>>(
      `/system/users/${id}`
    )
    return data.data
  },

  // Invite a new user
  invite: async (request: InviteUserRequest): Promise<SystemInviteDto> => {
    const { data } = await apiClient.post<ApiResponse<SystemInviteDto>>(
      '/system/users/invite',
      request
    )
    return data.data
  },

  // Update user profile
  update: async (id: string, request: UpdateUserRequest): Promise<SystemUserDto> => {
    const { data } = await apiClient.put<ApiResponse<SystemUserDto>>(
      `/system/users/${id}`,
      request
    )
    return data.data
  },

  // Update user permissions
  updatePermissions: async (
    id: string,
    request: UpdatePermissionsRequest
  ): Promise<SystemUserDetailDto> => {
    const { data } = await apiClient.put<ApiResponse<SystemUserDetailDto>>(
      `/system/users/${id}/permissions`,
      request
    )
    return data.data
  },

  // Delete user
  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/system/users/${id}`)
  },
}
