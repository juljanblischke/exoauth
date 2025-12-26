import apiClient from '@/lib/axios'
import type { ApiResponse } from '@/types'
import type { SystemPermissionDto, SystemPermissionGroupDto } from '../types'

export const permissionsApi = {
  // Get all permissions and group by category on client side
  getAll: async (): Promise<SystemPermissionGroupDto[]> => {
    const { data } = await apiClient.get<ApiResponse<SystemPermissionDto[]>>(
      '/system/permissions'
    )

    // Group permissions by category
    const grouped = data.data.reduce<Record<string, SystemPermissionDto[]>>(
      (acc, permission) => {
        const category = permission.category
        if (!acc[category]) {
          acc[category] = []
        }
        acc[category].push(permission)
        return acc
      },
      {}
    )

    // Convert to array of groups
    return Object.entries(grouped).map(([category, permissions]) => ({
      category,
      permissions,
    }))
  },
}
