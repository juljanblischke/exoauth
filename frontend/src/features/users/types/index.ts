import { z } from 'zod'
import type { TFunction } from 'i18next'

// System User DTOs
export interface SystemUserDto {
  id: string
  email: string
  firstName: string
  lastName: string
  fullName: string
  isActive: boolean
  emailVerified: boolean
  lastLoginAt: string | null
  createdAt: string
  updatedAt: string | null
}

export interface SystemUserDetailDto extends SystemUserDto {
  permissions: PermissionDto[]
}

export interface PermissionDto {
  id: string
  name: string
  description: string
  category: string
}

export interface SystemInviteDto {
  id: string
  email: string
  firstName: string
  lastName: string
  expiresAt: string
  createdAt: string
}

// Request types
export interface InviteUserRequest {
  email: string
  firstName: string
  lastName: string
  permissionIds: string[]
}

export interface UpdateUserRequest {
  firstName?: string
  lastName?: string
  isActive?: boolean
}

export interface UpdatePermissionsRequest {
  permissionIds: string[]
}

// Query params
export interface SystemUsersQueryParams {
  cursor?: string
  limit?: number
  sort?: string
  search?: string
}

// Form schemas
export const createInviteUserSchema = (t: TFunction) =>
  z.object({
    email: z.string().email(t('validation:email')),
    firstName: z.string().min(2, t('validation:minLength', { min: 2 })),
    lastName: z.string().min(2, t('validation:minLength', { min: 2 })),
    permissionIds: z.array(z.string()),
  })

export const createEditUserSchema = (t: TFunction) =>
  z.object({
    firstName: z.string().min(2, t('validation:minLength', { min: 2 })),
    lastName: z.string().min(2, t('validation:minLength', { min: 2 })),
    isActive: z.boolean(),
  })

// Form data types
export interface InviteUserFormData {
  email: string
  firstName: string
  lastName: string
  permissionIds: string[]
}

export interface EditUserFormData {
  firstName: string
  lastName: string
  isActive: boolean
}
