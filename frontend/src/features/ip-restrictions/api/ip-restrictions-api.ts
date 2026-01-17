import apiClient from '@/lib/axios'
import type { ApiResponse, CursorPaginationMeta } from '@/types'
import type {
  IpRestrictionDto,
  IpRestrictionsQueryParams,
  CreateIpRestrictionRequest,
  UpdateIpRestrictionRequest,
} from '../types'

export interface IpRestrictionsResponse {
  restrictions: IpRestrictionDto[]
  pagination: CursorPaginationMeta
}

export const ipRestrictionsApi = {
  // Get paginated list of IP restrictions
  getAll: async (params: IpRestrictionsQueryParams = {}): Promise<IpRestrictionsResponse> => {
    const { data } = await apiClient.get<ApiResponse<IpRestrictionDto[]>>(
      '/system/ip-restrictions',
      {
        params: {
          cursor: params.cursor,
          limit: params.limit || 20,
          type: params.type,
          source: params.source,
          includeExpired: params.includeExpired,
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
      restrictions: data.data,
      pagination,
    }
  },

  // Create a new IP restriction
  create: async (request: CreateIpRestrictionRequest): Promise<IpRestrictionDto> => {
    const { data } = await apiClient.post<ApiResponse<IpRestrictionDto>>(
      '/system/ip-restrictions',
      request
    )
    return data.data
  },

  // Update an IP restriction
  update: async (id: string, request: UpdateIpRestrictionRequest): Promise<IpRestrictionDto> => {
    const { data } = await apiClient.patch<ApiResponse<IpRestrictionDto>>(
      `/system/ip-restrictions/${id}`,
      request
    )
    return data.data
  },

  // Delete an IP restriction
  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/system/ip-restrictions/${id}`)
  },
}
