// System Permission DTOs
export interface SystemPermissionDto {
  id: string
  name: string
  description: string
  category: string
  createdAt: string
}

export interface SystemPermissionGroupDto {
  category: string
  permissions: SystemPermissionDto[]
}
