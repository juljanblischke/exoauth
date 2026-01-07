// IP Restriction types - backend uses numeric values
export const IpRestrictionType = {
  Whitelist: 0,
  Blacklist: 1,
} as const

export type IpRestrictionType = (typeof IpRestrictionType)[keyof typeof IpRestrictionType]

export const IpRestrictionSource = {
  Manual: 0,
  Auto: 1,
} as const

export type IpRestrictionSource = (typeof IpRestrictionSource)[keyof typeof IpRestrictionSource]

// IP Restriction DTO from backend
export interface IpRestrictionDto {
  id: string
  ipAddress: string
  type: IpRestrictionType
  reason: string
  source: IpRestrictionSource
  expiresAt: string | null
  createdAt: string
  createdByUserId: string | null
  createdByUserEmail: string | null
  createdByUserFullName: string | null
}

// Create IP Restriction request
export interface CreateIpRestrictionRequest {
  ipAddress: string
  type: IpRestrictionType
  reason: string
  expiresAt?: string | null
}

// Update IP Restriction request
export interface UpdateIpRestrictionRequest {
  type: IpRestrictionType
  reason: string
  expiresAt?: string | null
}

// Query params for listing IP restrictions
export interface IpRestrictionsQueryParams {
  cursor?: string
  limit?: number
  type?: IpRestrictionType
  source?: IpRestrictionSource
  includeExpired?: boolean
  search?: string
  sort?: string
}
