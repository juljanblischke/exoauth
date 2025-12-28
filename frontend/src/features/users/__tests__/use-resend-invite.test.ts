import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook, waitFor } from '@testing-library/react'
import { createWrapper } from '@/test/test-utils'
import { useResendInvite } from '../hooks/use-resend-invite'
import { invitesApi } from '../api/invites-api'
import type { SystemInviteListDto } from '../types'

// Mock the invites API
vi.mock('../api/invites-api', () => ({
  invitesApi: {
    resend: vi.fn(),
  },
}))

const mockResentInvite: SystemInviteListDto = {
  id: 'invite-1',
  email: 'john@example.com',
  firstName: 'John',
  lastName: 'Doe',
  status: 'pending',
  expiresAt: '2025-02-15T00:00:00Z', // Extended expiry
  createdAt: '2025-01-01T00:00:00Z',
  acceptedAt: null,
  revokedAt: null,
  resentAt: '2025-01-15T00:00:00Z',
  invitedBy: {
    id: 'user-1',
    email: 'admin@example.com',
    fullName: 'Admin User',
  },
}

describe('useResendInvite', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('should resend an invite successfully', async () => {
    vi.mocked(invitesApi.resend).mockResolvedValueOnce(mockResentInvite)

    const { result } = renderHook(() => useResendInvite(), {
      wrapper: createWrapper(),
    })

    result.current.mutate('invite-1')

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    expect(invitesApi.resend).toHaveBeenCalledWith('invite-1')
    expect(result.current.data).toEqual(mockResentInvite)
  })

  it('should handle resend error', async () => {
    const error = new Error('Resend failed')
    vi.mocked(invitesApi.resend).mockRejectedValueOnce(error)

    const { result } = renderHook(() => useResendInvite(), {
      wrapper: createWrapper(),
    })

    result.current.mutate('invite-1')

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })

    expect(result.current.error).toBeDefined()
  })

  it('should be in idle state initially', () => {
    const { result } = renderHook(() => useResendInvite(), {
      wrapper: createWrapper(),
    })

    expect(result.current.isIdle).toBe(true)
    expect(result.current.isPending).toBe(false)
  })

  it('should set isPending while resending', async () => {
    // Create a promise that we control
    let resolvePromise: (value: SystemInviteListDto) => void
    const pendingPromise = new Promise<SystemInviteListDto>((resolve) => {
      resolvePromise = resolve
    })

    vi.mocked(invitesApi.resend).mockReturnValueOnce(pendingPromise)

    const { result } = renderHook(() => useResendInvite(), {
      wrapper: createWrapper(),
    })

    result.current.mutate('invite-1')

    await waitFor(() => {
      expect(result.current.isPending).toBe(true)
    })

    // Resolve the promise
    resolvePromise!(mockResentInvite)

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })
  })
})

describe('useResendInvite cooldown error handling', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('should handle cooldown errors', async () => {
    const cooldownError = {
      response: {
        status: 429,
        data: {
          errorCode: 'INVITE_RESEND_COOLDOWN',
          message: 'Please wait 5 minutes before resending',
        },
      },
    }
    vi.mocked(invitesApi.resend).mockRejectedValueOnce(cooldownError)

    const { result } = renderHook(() => useResendInvite(), {
      wrapper: createWrapper(),
    })

    result.current.mutate('invite-1')

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })
  })

  it('should handle network errors', async () => {
    const networkError = new Error('Network error')
    vi.mocked(invitesApi.resend).mockRejectedValueOnce(networkError)

    const { result } = renderHook(() => useResendInvite(), {
      wrapper: createWrapper(),
    })

    result.current.mutate('invite-1')

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })
  })

  it('should handle 403 forbidden errors', async () => {
    const forbiddenError = { response: { status: 403, data: { message: 'Forbidden' } } }
    vi.mocked(invitesApi.resend).mockRejectedValueOnce(forbiddenError)

    const { result } = renderHook(() => useResendInvite(), {
      wrapper: createWrapper(),
    })

    result.current.mutate('invite-1')

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })
  })

  it('should handle already accepted invite errors', async () => {
    const acceptedError = {
      response: {
        status: 400,
        data: {
          errorCode: 'INVITE_ALREADY_ACCEPTED',
          message: 'Invite has already been accepted',
        },
      },
    }
    vi.mocked(invitesApi.resend).mockRejectedValueOnce(acceptedError)

    const { result } = renderHook(() => useResendInvite(), {
      wrapper: createWrapper(),
    })

    result.current.mutate('invite-1')

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })
  })
})
