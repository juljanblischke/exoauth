import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook, waitFor } from '@testing-library/react'
import { createWrapper, mockUser } from '@/test/test-utils'

// Mock the auth API
const mockLoginApi = vi.fn()
vi.mock('../api/auth-api', () => ({
  authApi: {
    login: (data: { email: string; password: string }) => mockLoginApi(data),
  },
}))

// Mock TanStack Router
const mockNavigate = vi.fn()
vi.mock('@tanstack/react-router', () => ({
  useNavigate: () => mockNavigate,
}))

// Import after mocking
import { useLogin } from '../hooks/use-login'

describe('useLogin', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    localStorage.clear()
  })

  it('should call the login API with correct data', async () => {
    const mockResponse = {
      user: mockUser,
      accessToken: 'test-token',
      refreshToken: 'test-refresh',
    }
    mockLoginApi.mockResolvedValueOnce(mockResponse)

    const { result } = renderHook(() => useLogin(), {
      wrapper: createWrapper(),
    })

    result.current.mutate({ email: 'test@example.com', password: 'password123' })

    await waitFor(() => {
      expect(mockLoginApi).toHaveBeenCalledWith({
        email: 'test@example.com',
        password: 'password123',
      })
    })
  })

  it('should navigate to dashboard on success', async () => {
    const mockResponse = {
      user: mockUser,
      accessToken: 'test-token',
      refreshToken: 'test-refresh',
    }
    mockLoginApi.mockResolvedValueOnce(mockResponse)

    const { result } = renderHook(() => useLogin(), {
      wrapper: createWrapper(),
    })

    result.current.mutate({ email: 'test@example.com', password: 'password123' })

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    expect(mockNavigate).toHaveBeenCalledWith({ to: '/dashboard' })
  })

  it('should set session flag on success', async () => {
    const mockResponse = {
      user: mockUser,
      accessToken: 'test-token',
      refreshToken: 'test-refresh',
    }
    mockLoginApi.mockResolvedValueOnce(mockResponse)

    const { result } = renderHook(() => useLogin(), {
      wrapper: createWrapper(),
    })

    result.current.mutate({ email: 'test@example.com', password: 'password123' })

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    expect(localStorage.getItem('exoauth_has_session')).toBe('true')
  })

  it('should handle login error', async () => {
    const error = new Error('Invalid credentials')
    mockLoginApi.mockRejectedValueOnce(error)

    const { result } = renderHook(() => useLogin(), {
      wrapper: createWrapper(),
    })

    result.current.mutate({ email: 'test@example.com', password: 'wrong' })

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })

    expect(result.current.error).toBe(error)
  })

  it('should be in pending state while logging in', async () => {
    // Make the API call take some time
    let resolvePromise: (value: unknown) => void
    const pendingPromise = new Promise((resolve) => {
      resolvePromise = resolve
    })
    mockLoginApi.mockImplementationOnce(() => pendingPromise)

    const { result } = renderHook(() => useLogin(), {
      wrapper: createWrapper(),
    })

    result.current.mutate({ email: 'test@example.com', password: 'password123' })

    // Wait for the mutation to start
    await waitFor(() => {
      expect(result.current.isPending).toBe(true)
    })

    // Clean up - resolve the promise
    resolvePromise!({
      user: mockUser,
      accessToken: 'test-token',
      refreshToken: 'test-refresh',
    })

    await waitFor(() => {
      expect(result.current.isPending).toBe(false)
    })
  })
})
