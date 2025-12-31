import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook, waitFor } from '@testing-library/react'
import { createWrapper } from '@/test/test-utils'
import type { SystemUserDto } from '../types'

// Mock users data
const mockUsers: SystemUserDto[] = [
  {
    id: 'user-1',
    email: 'john@example.com',
    firstName: 'John',
    lastName: 'Doe',
    fullName: 'John Doe',
    isActive: true,
    emailVerified: true,
    mfaEnabled: false,
    lastLoginAt: '2025-01-01T12:00:00Z',
    lockedUntil: null,
    isLocked: false,
    isAnonymized: false,
    failedLoginAttempts: 0,
    createdAt: '2024-12-01T00:00:00Z',
    updatedAt: '2025-01-01T12:00:00Z',
  },
  {
    id: 'user-2',
    email: 'jane@example.com',
    firstName: 'Jane',
    lastName: 'Smith',
    fullName: 'Jane Smith',
    isActive: false,
    emailVerified: true,
    mfaEnabled: false,
    lastLoginAt: null,
    lockedUntil: null,
    isLocked: false,
    isAnonymized: false,
    failedLoginAttempts: 0,
    createdAt: '2024-11-01T00:00:00Z',
    updatedAt: null,
  },
]

const mockUsersPage2: SystemUserDto[] = [
  {
    id: 'user-3',
    email: 'bob@example.com',
    firstName: 'Bob',
    lastName: 'Johnson',
    fullName: 'Bob Johnson',
    isActive: true,
    emailVerified: false,
    mfaEnabled: false,
    lastLoginAt: null,
    lockedUntil: null,
    isLocked: false,
    isAnonymized: false,
    failedLoginAttempts: 0,
    createdAt: '2024-10-01T00:00:00Z',
    updatedAt: null,
  },
]

// Mock the users API
const mockGetAllUsers = vi.fn()
vi.mock('../api/users-api', () => ({
  usersApi: {
    getAll: (params: Record<string, unknown>) => mockGetAllUsers(params),
  },
}))

// Import after mocking
import { useSystemUsers } from '../hooks/use-system-users'

describe('useSystemUsers', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('should fetch users on mount', async () => {
    mockGetAllUsers.mockResolvedValueOnce({
      users: mockUsers,
      pagination: { hasMore: false, nextCursor: null },
    })

    const { result } = renderHook(() => useSystemUsers(), {
      wrapper: createWrapper(),
    })

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    expect(mockGetAllUsers).toHaveBeenCalledWith({
      cursor: undefined,
      limit: 20,
      search: undefined,
      sort: undefined,
    })
  })

  it('should return users data', async () => {
    mockGetAllUsers.mockResolvedValueOnce({
      users: mockUsers,
      pagination: { hasMore: false, nextCursor: null },
    })

    const { result } = renderHook(() => useSystemUsers(), {
      wrapper: createWrapper(),
    })

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    const allUsers = result.current.data?.pages.flatMap((page) => page.users)
    expect(allUsers).toHaveLength(2)
    expect(allUsers?.[0].email).toBe('john@example.com')
    expect(allUsers?.[1].email).toBe('jane@example.com')
  })

  it('should pass search parameter', async () => {
    mockGetAllUsers.mockResolvedValueOnce({
      users: [mockUsers[0]],
      pagination: { hasMore: false, nextCursor: null },
    })

    const { result } = renderHook(() => useSystemUsers({ search: 'john' }), {
      wrapper: createWrapper(),
    })

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    expect(mockGetAllUsers).toHaveBeenCalledWith({
      cursor: undefined,
      limit: 20,
      search: 'john',
      sort: undefined,
    })
  })

  it('should pass sort parameter', async () => {
    mockGetAllUsers.mockResolvedValueOnce({
      users: mockUsers,
      pagination: { hasMore: false, nextCursor: null },
    })

    const { result } = renderHook(
      () => useSystemUsers({ sort: 'fullName:asc' }),
      { wrapper: createWrapper() }
    )

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    expect(mockGetAllUsers).toHaveBeenCalledWith({
      cursor: undefined,
      limit: 20,
      search: undefined,
      sort: 'fullName:asc',
    })
  })

  it('should indicate when more data is available', async () => {
    mockGetAllUsers.mockResolvedValueOnce({
      users: mockUsers,
      pagination: { hasMore: true, nextCursor: 'cursor-1' },
    })

    const { result } = renderHook(() => useSystemUsers(), {
      wrapper: createWrapper(),
    })

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    expect(result.current.hasNextPage).toBe(true)
  })

  it('should fetch next page with cursor', async () => {
    // First page
    mockGetAllUsers.mockResolvedValueOnce({
      users: mockUsers,
      pagination: { hasMore: true, nextCursor: 'cursor-1' },
    })

    const { result } = renderHook(() => useSystemUsers(), {
      wrapper: createWrapper(),
    })

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    // Second page
    mockGetAllUsers.mockResolvedValueOnce({
      users: mockUsersPage2,
      pagination: { hasMore: false, nextCursor: null },
    })

    result.current.fetchNextPage()

    await waitFor(() => {
      expect(mockGetAllUsers).toHaveBeenCalledTimes(2)
    })

    expect(mockGetAllUsers).toHaveBeenLastCalledWith({
      cursor: 'cursor-1',
      limit: 20,
      search: undefined,
      sort: undefined,
    })
  })

  it('should handle API error', async () => {
    const error = new Error('Network error')
    mockGetAllUsers.mockRejectedValueOnce(error)

    const { result } = renderHook(() => useSystemUsers(), {
      wrapper: createWrapper(),
    })

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })

    expect(result.current.error).toBe(error)
  })

  it('should be in loading state initially', () => {
    mockGetAllUsers.mockImplementationOnce(
      () => new Promise((resolve) => setTimeout(resolve, 100))
    )

    const { result } = renderHook(() => useSystemUsers(), {
      wrapper: createWrapper(),
    })

    expect(result.current.isLoading).toBe(true)
  })

  it('should use custom limit', async () => {
    mockGetAllUsers.mockResolvedValueOnce({
      users: mockUsers,
      pagination: { hasMore: false, nextCursor: null },
    })

    const { result } = renderHook(() => useSystemUsers({ limit: 50 }), {
      wrapper: createWrapper(),
    })

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    expect(mockGetAllUsers).toHaveBeenCalledWith({
      cursor: undefined,
      limit: 50,
      search: undefined,
      sort: undefined,
    })
  })
})
