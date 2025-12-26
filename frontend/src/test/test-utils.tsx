import { type ReactNode } from 'react'
import { render, type RenderOptions, type RenderResult } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from 'i18next'
import type { User } from '@/types'

// Create a minimal i18n instance for tests
const testI18n = i18n.createInstance()
testI18n.init({
  lng: 'en',
  fallbackLng: 'en',
  ns: ['common', 'auth', 'users', 'errors', 'validation'],
  defaultNS: 'common',
  resources: {
    en: {
      common: {
        actions: {
          save: 'Save',
          cancel: 'Cancel',
          delete: 'Delete',
          edit: 'Edit',
          loading: 'Loading...',
        },
      },
      auth: {
        login: {
          title: 'Welcome back',
          subtitle: 'Sign in to your account',
          email: 'Email',
          password: 'Password',
          signIn: 'Sign in',
          signingIn: 'Signing in...',
          noAccount: "Don't have an account?",
          register: 'Register',
        },
        register: {
          title: 'Create account',
          subtitle: 'Register to get started',
          firstName: 'First name',
          lastName: 'Last name',
          email: 'Email',
          password: 'Password',
          createAccount: 'Create account',
          creating: 'Creating...',
          hasAccount: 'Already have an account?',
          signIn: 'Sign in',
        },
        password: {
          requirements: 'Password requirements',
          minLength: 'At least 12 characters',
          uppercase: 'One uppercase letter',
          lowercase: 'One lowercase letter',
          digit: 'One number',
          special: 'One special character',
        },
      },
      users: {
        title: 'System Users',
        subtitle: 'Manage system administrators',
        inviteUser: 'Invite User',
        search: {
          placeholder: 'Search users...',
        },
        fields: {
          email: 'Email',
          name: 'Name',
          status: 'Status',
          lastLogin: 'Last Login',
        },
        status: {
          active: 'Active',
          inactive: 'Inactive',
        },
        actions: {
          permissions: 'Permissions',
        },
        empty: {
          title: 'No users found',
          message: 'Invite your first team member',
        },
        invite: {
          description: 'Send an invitation to join the team',
          submit: 'Send Invitation',
        },
        messages: {
          inviteSuccess: 'Invitation sent successfully',
        },
        permissions: {
          title: 'Permissions',
        },
      },
      errors: {
        AUTH_INVALID_CREDENTIALS: 'Invalid email or password',
        AUTH_USER_INACTIVE: 'User account is inactive',
        UNKNOWN_ERROR: 'An unexpected error occurred',
      },
      validation: {
        required: 'This field is required',
        email: 'Please enter a valid email address',
        minLength: 'Must be at least {{min}} characters',
        password: {
          minLength: 'Password must be at least {{min}} characters',
          lowercase: 'Password must contain at least one lowercase letter',
          uppercase: 'Password must contain at least one uppercase letter',
          number: 'Password must contain at least one number',
          special: 'Password must contain at least one special character',
          mismatch: 'Passwords do not match',
        },
      },
    },
  },
  interpolation: {
    escapeValue: false,
  },
})

// Create a fresh QueryClient for each test
function createTestQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        gcTime: 0,
        staleTime: 0,
      },
      mutations: {
        retry: false,
      },
    },
  })
}

// Mock AuthContext value
export interface MockAuthContextValue {
  user: User | null
  isAuthenticated: boolean
  isLoading: boolean
  login: () => Promise<User>
  register: () => Promise<User>
  logout: () => Promise<void>
  refetch: () => Promise<void>
  hasPermission: (permission: string) => boolean
  hasAnyPermission: (permissions: string[]) => boolean
  hasAllPermissions: (permissions: string[]) => boolean
  tokenExpiresAt: string | null
}

export const mockUser: User = {
  id: 'user-1',
  email: 'test@example.com',
  firstName: 'Test',
  lastName: 'User',
  fullName: 'Test User',
  isActive: true,
  emailVerified: true,
  permissions: ['system:users:read', 'system:users:write'],
  lastLoginAt: '2025-01-01T00:00:00Z',
  createdAt: '2025-01-01T00:00:00Z',
}

export const createMockAuthContext = (
  overrides: Partial<MockAuthContextValue> = {}
): MockAuthContextValue => ({
  user: mockUser,
  isAuthenticated: true,
  isLoading: false,
  login: async () => mockUser,
  register: async () => mockUser,
  logout: async () => {},
  refetch: async () => {},
  hasPermission: (permission: string) =>
    mockUser.permissions.includes(permission),
  hasAnyPermission: (permissions: string[]) =>
    permissions.some((p) => mockUser.permissions.includes(p)),
  hasAllPermissions: (permissions: string[]) =>
    permissions.every((p) => mockUser.permissions.includes(p)),
  tokenExpiresAt: null,
  ...overrides,
})

// Simple wrapper for basic component tests (without routing)
interface TestWrapperOptions {
  queryClient?: QueryClient
}

export function createWrapper(options: TestWrapperOptions = {}) {
  const queryClient = options.queryClient ?? createTestQueryClient()

  return function TestWrapper({ children }: { children: ReactNode }) {
    return (
      <QueryClientProvider client={queryClient}>
        <I18nextProvider i18n={testI18n}>{children}</I18nextProvider>
      </QueryClientProvider>
    )
  }
}

// Custom render function that includes providers
interface CustomRenderOptions extends Omit<RenderOptions, 'wrapper'> {
  queryClient?: QueryClient
}

export function renderWithProviders(
  ui: React.ReactElement,
  options: CustomRenderOptions = {}
): RenderResult {
  const { queryClient, ...renderOptions } = options
  const Wrapper = createWrapper({ queryClient })

  return render(ui, { wrapper: Wrapper, ...renderOptions })
}

// Re-export commonly used testing utilities
export {
  screen,
  waitFor,
  fireEvent,
  within,
  cleanup,
} from '@testing-library/react'
export { default as userEvent } from '@testing-library/user-event'

// Export a function to create test query clients
export { createTestQueryClient }
