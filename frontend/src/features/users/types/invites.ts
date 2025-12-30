// Invite status - matches backend Status property
export type InviteStatus = 'pending' | 'accepted' | 'expired' | 'revoked'

// InvitedBy user reference - matches InvitedByDto
export interface InvitedByDto {
  id: string
  email: string
  fullName: string
}

// Permission info for invite - matches InvitePermissionDto
export interface InvitePermissionDto {
  name: string
  description: string
}

// System invite list item - matches SystemInviteListDto
export interface SystemInviteListDto {
  id: string
  email: string
  firstName: string
  lastName: string
  status: InviteStatus
  expiresAt: string
  createdAt: string
  acceptedAt: string | null
  revokedAt: string | null
  resentAt: string | null
  invitedBy: InvitedByDto
}

// System invite detail - matches SystemInviteDetailDto
export interface SystemInviteDetailDto extends SystemInviteListDto {
  permissions: InvitePermissionDto[]
}

// Query params for invites list
export interface SystemInvitesQueryParams {
  cursor?: string
  limit?: number
  search?: string
  statuses?: InviteStatus[]
  sort?: string
  includeExpired?: boolean
  includeRevoked?: boolean
}

// Request to update an invite (PATCH)
export interface UpdateInviteRequest {
  firstName?: string
  lastName?: string
  permissionIds?: string[]
}
