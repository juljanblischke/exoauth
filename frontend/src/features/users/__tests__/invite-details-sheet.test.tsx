import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderWithProviders, screen, waitFor } from '@/test/test-utils'
import userEvent from '@testing-library/user-event'
import { InviteDetailsSheet } from '../components/invite-details-sheet'
import type { SystemInviteListDto, SystemInviteDetailDto } from '../types'

// Mock invite data
const mockInvite: SystemInviteListDto = {
  id: 'invite-1',
  email: 'john@example.com',
  firstName: 'John',
  lastName: 'Doe',
  status: 'pending',
  expiresAt: '2025-02-01T00:00:00Z',
  createdAt: '2025-01-01T00:00:00Z',
  acceptedAt: null,
  revokedAt: null,
  resentAt: null,
  invitedBy: {
    id: 'user-1',
    email: 'admin@example.com',
    fullName: 'Admin User',
  },
}

const mockInviteDetails: SystemInviteDetailDto = {
  ...mockInvite,
  permissions: [
    { name: 'system:users:read', description: 'Read users' },
    { name: 'system:users:write', description: 'Write users' },
  ],
}

// Mutable mock data - can be changed per test
let mockHookData: SystemInviteDetailDto | null = mockInviteDetails

// Mock useSystemInvite hook
vi.mock('../hooks', () => ({
  useSystemInvite: () => ({
    data: mockHookData,
    isLoading: false,
  }),
}))

describe('InviteDetailsSheet', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockHookData = mockInviteDetails
  })

  it('renders the sheet with invite details', async () => {
    renderWithProviders(
      <InviteDetailsSheet
        open={true}
        onOpenChange={() => {}}
        invite={mockInvite}
      />
    )

    await waitFor(() => {
      expect(screen.getByText('John Doe')).toBeInTheDocument()
      expect(screen.getByText('john@example.com')).toBeInTheDocument()
    })
  })

  it('shows pending status badge', async () => {
    renderWithProviders(
      <InviteDetailsSheet
        open={true}
        onOpenChange={() => {}}
        invite={mockInvite}
      />
    )

    await waitFor(() => {
      expect(screen.getByText('Pending')).toBeInTheDocument()
    })
  })

  it('shows invitedBy information', async () => {
    renderWithProviders(
      <InviteDetailsSheet
        open={true}
        onOpenChange={() => {}}
        invite={mockInvite}
      />
    )

    await waitFor(() => {
      expect(screen.getByText('Admin User')).toBeInTheDocument()
    })
  })

  it('shows permissions when loaded', async () => {
    renderWithProviders(
      <InviteDetailsSheet
        open={true}
        onOpenChange={() => {}}
        invite={mockInvite}
      />
    )

    await waitFor(() => {
      expect(screen.getByText('system:users:read')).toBeInTheDocument()
      expect(screen.getByText('system:users:write')).toBeInTheDocument()
    })
  })

  it('renders resend button for pending invites', async () => {
    const onResend = vi.fn()

    renderWithProviders(
      <InviteDetailsSheet
        open={true}
        onOpenChange={() => {}}
        invite={mockInvite}
        onResend={onResend}
      />
    )

    await waitFor(() => {
      expect(screen.getByText('Resend')).toBeInTheDocument()
    })
  })

  it('renders revoke button for pending invites', async () => {
    const onRevoke = vi.fn()

    renderWithProviders(
      <InviteDetailsSheet
        open={true}
        onOpenChange={() => {}}
        invite={mockInvite}
        onRevoke={onRevoke}
      />
    )

    await waitFor(() => {
      expect(screen.getByText('Revoke')).toBeInTheDocument()
    })
  })

  it('calls onResend when resend button is clicked', async () => {
    const onResend = vi.fn()
    const onOpenChange = vi.fn()
    const user = userEvent.setup()

    renderWithProviders(
      <InviteDetailsSheet
        open={true}
        onOpenChange={onOpenChange}
        invite={mockInvite}
        onResend={onResend}
      />
    )

    await waitFor(() => {
      expect(screen.getByText('Resend')).toBeInTheDocument()
    })

    const resendButton = screen.getByText('Resend').closest('button')
    if (resendButton) {
      await user.click(resendButton)
      expect(onResend).toHaveBeenCalledWith(mockInvite)
      expect(onOpenChange).toHaveBeenCalledWith(false)
    }
  })

  it('calls onRevoke when revoke button is clicked', async () => {
    const onRevoke = vi.fn()
    const onOpenChange = vi.fn()
    const user = userEvent.setup()

    renderWithProviders(
      <InviteDetailsSheet
        open={true}
        onOpenChange={onOpenChange}
        invite={mockInvite}
        onRevoke={onRevoke}
      />
    )

    await waitFor(() => {
      expect(screen.getByText('Revoke')).toBeInTheDocument()
    })

    const revokeButton = screen.getByText('Revoke').closest('button')
    if (revokeButton) {
      await user.click(revokeButton)
      expect(onRevoke).toHaveBeenCalledWith(mockInvite)
      expect(onOpenChange).toHaveBeenCalledWith(false)
    }
  })

  it('returns null when invite is null', () => {
    const { container } = renderWithProviders(
      <InviteDetailsSheet
        open={true}
        onOpenChange={() => {}}
        invite={null}
      />
    )

    expect(container.firstChild).toBeNull()
  })
})

describe('InviteDetailsSheet with accepted invite', () => {
  const acceptedInvite: SystemInviteListDto = {
    ...mockInvite,
    status: 'accepted',
    acceptedAt: '2025-01-10T00:00:00Z',
  }

  beforeEach(() => {
    vi.clearAllMocks()
    // Set mock to return accepted invite data
    mockHookData = { ...acceptedInvite, permissions: [] }
  })

  it('disables resend button for accepted invites', async () => {
    renderWithProviders(
      <InviteDetailsSheet
        open={true}
        onOpenChange={() => {}}
        invite={acceptedInvite}
        onResend={() => {}}
      />
    )

    await waitFor(() => {
      const resendButton = screen.getByText('Resend').closest('button')
      expect(resendButton).toBeDisabled()
    })
  })

  it('disables revoke button for accepted invites', async () => {
    renderWithProviders(
      <InviteDetailsSheet
        open={true}
        onOpenChange={() => {}}
        invite={acceptedInvite}
        onRevoke={() => {}}
      />
    )

    await waitFor(() => {
      const revokeButton = screen.getByText('Revoke').closest('button')
      expect(revokeButton).toBeDisabled()
    })
  })
})

describe('InviteDetailsSheet with expired invite', () => {
  const expiredInvite: SystemInviteListDto = {
    ...mockInvite,
    status: 'expired',
    expiresAt: '2024-12-01T00:00:00Z',
  }

  beforeEach(() => {
    vi.clearAllMocks()
    // Set mock to return expired invite data
    mockHookData = { ...expiredInvite, permissions: [] }
  })

  it('enables resend button for expired invites', async () => {
    renderWithProviders(
      <InviteDetailsSheet
        open={true}
        onOpenChange={() => {}}
        invite={expiredInvite}
        onResend={() => {}}
      />
    )

    await waitFor(() => {
      const resendButton = screen.getByText('Resend').closest('button')
      expect(resendButton).not.toBeDisabled()
    })
  })

  it('disables revoke button for expired invites', async () => {
    renderWithProviders(
      <InviteDetailsSheet
        open={true}
        onOpenChange={() => {}}
        invite={expiredInvite}
        onRevoke={() => {}}
      />
    )

    await waitFor(() => {
      const revokeButton = screen.getByText('Revoke').closest('button')
      expect(revokeButton).toBeDisabled()
    })
  })
})
