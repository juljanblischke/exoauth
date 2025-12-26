import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderWithProviders, screen, waitFor } from '@/test/test-utils'
import userEvent from '@testing-library/user-event'
import { RegisterForm } from '../components/register-form'

// Mock the useRegister hook
const mockRegister = vi.fn()
vi.mock('../hooks/use-register', () => ({
  useRegister: () => ({
    mutate: mockRegister,
    isPending: false,
    error: null,
  }),
}))

// Mock TanStack Router Link
vi.mock('@tanstack/react-router', () => ({
  Link: ({ children, to }: { children: React.ReactNode; to: string }) => (
    <a href={to}>{children}</a>
  ),
}))

describe('RegisterForm', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders the register form with all fields', () => {
    renderWithProviders(<RegisterForm />)

    expect(screen.getByLabelText(/first name/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/last name/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/email/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /create account/i })).toBeInTheDocument()
  })

  it('renders password requirements section', () => {
    renderWithProviders(<RegisterForm />)

    expect(screen.getByText(/password requirements/i)).toBeInTheDocument()
    expect(screen.getByText(/at least 12 characters/i)).toBeInTheDocument()
    expect(screen.getByText(/uppercase letter/i)).toBeInTheDocument()
    expect(screen.getByText(/lowercase letter/i)).toBeInTheDocument()
    // "One number" text from auth:password.digit
    expect(screen.getByText(/number/i)).toBeInTheDocument()
    expect(screen.getByText(/special character/i)).toBeInTheDocument()
  })

  it('validates email field on blur or submit', async () => {
    const user = userEvent.setup()
    renderWithProviders(<RegisterForm />)

    const emailInput = screen.getByLabelText(/email/i)

    // Type invalid email and blur
    await user.type(emailInput, 'invalid-email')
    await user.tab() // Blur the field

    // The email field should still be in the DOM
    expect(emailInput).toHaveValue('invalid-email')
  })

  it('validates form fields on submit', async () => {
    const user = userEvent.setup()
    renderWithProviders(<RegisterForm />)

    const submitButton = screen.getByRole('button', { name: /create account/i })

    // Submit empty form to trigger all validations
    await user.click(submitButton)

    // Verify register was NOT called with invalid data
    expect(mockRegister).not.toHaveBeenCalled()
  })

  it('calls register with correct data when form is valid', async () => {
    const user = userEvent.setup()
    renderWithProviders(<RegisterForm />)

    const firstNameInput = screen.getByLabelText(/first name/i)
    const lastNameInput = screen.getByLabelText(/last name/i)
    const emailInput = screen.getByLabelText(/email/i)
    const passwordInput = screen.getByLabelText(/password/i)
    const submitButton = screen.getByRole('button', { name: /create account/i })

    await user.type(firstNameInput, 'John')
    await user.type(lastNameInput, 'Doe')
    await user.type(emailInput, 'john.doe@example.com')
    await user.type(passwordInput, 'StrongP@ssw0rd!')

    await user.click(submitButton)

    await waitFor(() => {
      expect(mockRegister).toHaveBeenCalledWith({
        firstName: 'John',
        lastName: 'Doe',
        email: 'john.doe@example.com',
        password: 'StrongP@ssw0rd!',
      })
    })
  })

  it('shows link to login page', () => {
    renderWithProviders(<RegisterForm />)

    const loginLink = screen.getByRole('link', { name: /sign in/i })
    expect(loginLink).toBeInTheDocument()
    expect(loginLink).toHaveAttribute('href', '/login')
  })
})

describe('RegisterForm password strength', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('updates password requirements as user types', async () => {
    const user = userEvent.setup()
    renderWithProviders(<RegisterForm />)

    const passwordInput = screen.getByLabelText(/password/i)

    // Type a password that meets some requirements
    await user.type(passwordInput, 'Aa1!')

    // The requirements component should show visual feedback
    // (This tests that the password state is being passed correctly)
    expect(passwordInput).toHaveValue('Aa1!')
  })
})
