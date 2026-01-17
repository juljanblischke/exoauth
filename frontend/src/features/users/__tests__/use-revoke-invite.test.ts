import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook, waitFor } from '@testing-library/react'
import { createWrapper } from '@/test/test-utils'
import { useRevokeInvite } from '../hooks/use-revoke-invite'
import { invitesApi } from '../api/invites-api'
import type { SystemInviteListDto } from '../types'

// Mock the invites API
vi.mock('../api/invites-api', () => ({
  invitesApi: {
    revoke: vi.fn(),
  },
}))

const mockRevokedInvite: SystemInviteListDto = {
  id: 'invite-1',
  email: 'john@example.com',
  firstName: 'John',
  lastName: 'Doe',
  status: 'revoked',
  expiresAt: '2025-02-01T00:00:00Z',
  createdAt: '2025-01-01T00:00:00Z',
  acceptedAt: null,
  revokedAt: '2025-01-15T00:00:00Z',
  resentAt: null,
  invitedBy: {
    id: 'user-1',
    email: 'admin@example.com',
    fullName: 'Admin User',
  },
}

describe('useRevokeInvite', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('should revoke an invite successfully', async () => {
    vi.mocked(invitesApi.revoke).mockResolvedValueOnce(mockRevokedInvite)

    const { result } = renderHook(() => useRevokeInvite(), {
      wrapper: createWrapper(),
    })

    result.current.mutate('invite-1')

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    expect(invitesApi.revoke).toHaveBeenCalledWith('invite-1')
    expect(result.current.data).toEqual(mockRevokedInvite)
  })

  it('should handle revoke error', async () => {
    const error = new Error('Invite already revoked')
    vi.mocked(invitesApi.revoke).mockRejectedValueOnce(error)

    const { result } = renderHook(() => useRevokeInvite(), {
      wrapper: createWrapper(),
    })

    result.current.mutate('invite-1')

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })

    expect(result.current.error).toBeDefined()
  })

  it('should be in idle state initially', () => {
    const { result } = renderHook(() => useRevokeInvite(), {
      wrapper: createWrapper(),
    })

    expect(result.current.isIdle).toBe(true)
    expect(result.current.isPending).toBe(false)
  })

  it('should set isPending while revoking', async () => {
    // Create a promise that we control
    let resolvePromise: (value: SystemInviteListDto) => void
    const pendingPromise = new Promise<SystemInviteListDto>((resolve) => {
      resolvePromise = resolve
    })

    vi.mocked(invitesApi.revoke).mockReturnValueOnce(pendingPromise)

    const { result } = renderHook(() => useRevokeInvite(), {
      wrapper: createWrapper(),
    })

    result.current.mutate('invite-1')

    await waitFor(() => {
      expect(result.current.isPending).toBe(true)
    })

    // Resolve the promise
    resolvePromise!(mockRevokedInvite)

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })
  })
})

describe('useRevokeInvite error handling', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('should handle network errors', async () => {
    const networkError = new Error('Network error')
    vi.mocked(invitesApi.revoke).mockRejectedValueOnce(networkError)

    const { result } = renderHook(() => useRevokeInvite(), {
      wrapper: createWrapper(),
    })

    result.current.mutate('invite-1')

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })
  })

  it('should handle 403 forbidden errors', async () => {
    const forbiddenError = { response: { status: 403, data: { message: 'Forbidden' } } }
    vi.mocked(invitesApi.revoke).mockRejectedValueOnce(forbiddenError)

    const { result } = renderHook(() => useRevokeInvite(), {
      wrapper: createWrapper(),
    })

    result.current.mutate('invite-1')

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })
  })

  it('should handle 404 not found errors', async () => {
    const notFoundError = { response: { status: 404, data: { message: 'Not found' } } }
    vi.mocked(invitesApi.revoke).mockRejectedValueOnce(notFoundError)

    const { result } = renderHook(() => useRevokeInvite(), {
      wrapper: createWrapper(),
    })

    result.current.mutate('invite-1')

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })
  })
})
