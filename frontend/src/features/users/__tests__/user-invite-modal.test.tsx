import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderWithProviders, screen, waitFor } from '@/test/test-utils'
import userEvent from '@testing-library/user-event'
import { UserInviteModal } from '../components/user-invite-modal'

// Mock permissions data
const mockPermissionGroups = [
  {
    category: 'system',
    permissions: [
      { id: 'perm-1', name: 'system:users:read', description: 'Read users', category: 'system' },
      { id: 'perm-2', name: 'system:users:write', description: 'Write users', category: 'system' },
    ],
  },
]

// Mock useInviteUser hook
const mockInviteUser = vi.fn()
vi.mock('../hooks', () => ({
  useInviteUser: () => ({
    mutate: mockInviteUser,
    isPending: false,
  }),
}))

// Mock useSystemPermissions hook
vi.mock('@/features/permissions', () => ({
  useSystemPermissions: () => ({
    data: mockPermissionGroups,
    isLoading: false,
  }),
}))

// Mock sonner toast
vi.mock('sonner', () => ({
  toast: {
    success: vi.fn(),
    error: vi.fn(),
  },
}))

describe('UserInviteModal', () => {
  const defaultProps = {
    open: true,
    onOpenChange: vi.fn(),
  }

  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders the modal when open', () => {
    renderWithProviders(<UserInviteModal {...defaultProps} />)

    expect(screen.getByText(/invite user/i)).toBeInTheDocument()
  })

  it('renders all form fields', () => {
    renderWithProviders(<UserInviteModal {...defaultProps} />)

    expect(screen.getByLabelText(/email/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/first name/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/last name/i)).toBeInTheDocument()
  })

  it('renders permissions section', async () => {
    renderWithProviders(<UserInviteModal {...defaultProps} />)

    await waitFor(() => {
      expect(screen.getByText(/permissions/i)).toBeInTheDocument()
      expect(screen.getByText('system:users:read')).toBeInTheDocument()
      expect(screen.getByText('system:users:write')).toBeInTheDocument()
    })
  })

  it('validates form and does not submit with invalid data', async () => {
    const user = userEvent.setup()
    renderWithProviders(<UserInviteModal {...defaultProps} />)

    const emailInput = screen.getByLabelText(/email/i)
    await user.type(emailInput, 'invalid-email')

    const submitButton = screen.getByRole('button', { name: /send invitation/i })
    await user.click(submitButton)

    // Verify inviteUser was NOT called with invalid data
    expect(mockInviteUser).not.toHaveBeenCalled()
  })

  it('validates required fields on submit', async () => {
    const user = userEvent.setup()
    renderWithProviders(<UserInviteModal {...defaultProps} />)

    const submitButton = screen.getByRole('button', { name: /send invitation/i })

    // Submit empty form to trigger all validations
    await user.click(submitButton)

    // Verify inviteUser was NOT called with empty form
    expect(mockInviteUser).not.toHaveBeenCalled()
  })

  it('allows selecting permissions', async () => {
    const user = userEvent.setup()
    renderWithProviders(<UserInviteModal {...defaultProps} />)

    await waitFor(() => {
      expect(screen.getByText('system:users:read')).toBeInTheDocument()
    })

    // Find the checkbox by its associated label
    const permissionCheckbox = screen.getByRole('checkbox', { name: /system:users:read/i })
    await user.click(permissionCheckbox)

    expect(permissionCheckbox).toBeChecked()
  })

  it('calls inviteUser with correct data when form is valid', async () => {
    const user = userEvent.setup()
    renderWithProviders(<UserInviteModal {...defaultProps} />)

    const emailInput = screen.getByLabelText(/email/i)
    const firstNameInput = screen.getByLabelText(/first name/i)
    const lastNameInput = screen.getByLabelText(/last name/i)

    await user.type(emailInput, 'newuser@example.com')
    await user.type(firstNameInput, 'New')
    await user.type(lastNameInput, 'User')

    // Select a permission
    await waitFor(() => {
      expect(screen.getByText('system:users:read')).toBeInTheDocument()
    })

    const permissionCheckbox = screen.getByRole('checkbox', { name: /system:users:read/i })
    await user.click(permissionCheckbox)

    const submitButton = screen.getByRole('button', { name: /send invitation/i })
    await user.click(submitButton)

    await waitFor(() => {
      expect(mockInviteUser).toHaveBeenCalledWith(
        {
          email: 'newuser@example.com',
          firstName: 'New',
          lastName: 'User',
          permissionIds: ['perm-1'],
        },
        expect.any(Object)
      )
    })
  })

  it('does not render when closed', () => {
    renderWithProviders(<UserInviteModal open={false} onOpenChange={vi.fn()} />)

    expect(screen.queryByText(/invite user/i)).not.toBeInTheDocument()
  })
})
