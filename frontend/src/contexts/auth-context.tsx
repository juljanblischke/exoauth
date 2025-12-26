import {
  createContext,
  useContext,
  useCallback,
  useMemo,
  type ReactNode,
} from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import apiClient, { extractData } from '@/lib/axios'
import type {
  User,
  AuthState,
  LoginRequest,
  RegisterRequest,
  ApiResponse,
  AuthResponse,
  LogoutResponse,
} from '@/types'

interface AuthContextValue extends AuthState {
  login: (data: LoginRequest) => Promise<User>
  register: (data: RegisterRequest) => Promise<User>
  logout: () => Promise<void>
  refetch: () => Promise<void>
  hasPermission: (permission: string) => boolean
  hasAnyPermission: (permissions: string[]) => boolean
  hasAllPermissions: (permissions: string[]) => boolean
  tokenExpiresAt: string | null
}

const AuthContext = createContext<AuthContextValue | null>(null)

const AUTH_QUERY_KEY = ['auth', 'me'] as const

interface AuthProviderProps {
  children: ReactNode
}

export function AuthProvider({ children }: AuthProviderProps) {
  const queryClient = useQueryClient()

  // Fetch current user
  const {
    data: user,
    isLoading,
    refetch: refetchUser,
  } = useQuery({
    queryKey: AUTH_QUERY_KEY,
    queryFn: async () => {
      const response = await apiClient.get<ApiResponse<User>>('/auth/me')
      return extractData(response)
    },
    retry: false,
    staleTime: 5 * 60 * 1000, // 5 minutes
  })

  // Login mutation
  const loginMutation = useMutation({
    mutationFn: async (data: LoginRequest) => {
      const response = await apiClient.post<ApiResponse<AuthResponse>>(
        '/auth/login',
        data
      )
      return extractData(response)
    },
    onSuccess: (data) => {
      queryClient.setQueryData(AUTH_QUERY_KEY, data.user)
    },
  })

  // Register mutation
  const registerMutation = useMutation({
    mutationFn: async (data: RegisterRequest) => {
      const response = await apiClient.post<ApiResponse<AuthResponse>>(
        '/auth/register',
        data
      )
      return extractData(response)
    },
    onSuccess: (data) => {
      queryClient.setQueryData(AUTH_QUERY_KEY, data.user)
    },
  })

  // Logout mutation
  const logoutMutation = useMutation({
    mutationFn: async () => {
      const response =
        await apiClient.post<ApiResponse<LogoutResponse>>('/auth/logout')
      return extractData(response)
    },
    onSuccess: () => {
      queryClient.setQueryData(AUTH_QUERY_KEY, null)
      queryClient.clear()
    },
  })

  const login = useCallback(
    async (data: LoginRequest): Promise<User> => {
      const result = await loginMutation.mutateAsync(data)
      return result.user
    },
    [loginMutation]
  )

  const register = useCallback(
    async (data: RegisterRequest): Promise<User> => {
      const result = await registerMutation.mutateAsync(data)
      return result.user
    },
    [registerMutation]
  )

  const logout = useCallback(async (): Promise<void> => {
    await logoutMutation.mutateAsync()
  }, [logoutMutation])

  const refetch = useCallback(async (): Promise<void> => {
    await refetchUser()
  }, [refetchUser])

  // Permission helpers
  const hasPermission = useCallback(
    (permission: string): boolean => {
      if (!user?.permissions) return false
      return user.permissions.includes(permission)
    },
    [user?.permissions]
  )

  const hasAnyPermission = useCallback(
    (permissions: string[]): boolean => {
      if (!user?.permissions) return false
      return permissions.some((p) => user.permissions.includes(p))
    },
    [user?.permissions]
  )

  const hasAllPermissions = useCallback(
    (permissions: string[]): boolean => {
      if (!user?.permissions) return false
      return permissions.every((p) => user.permissions.includes(p))
    },
    [user?.permissions]
  )

  const value = useMemo<AuthContextValue>(
    () => ({
      user: user ?? null,
      isAuthenticated: !!user,
      isLoading,
      tokenExpiresAt: null, // TODO: Get from session/cookie when backend provides it
      login,
      register,
      logout,
      refetch,
      hasPermission,
      hasAnyPermission,
      hasAllPermissions,
    }),
    [
      user,
      isLoading,
      login,
      register,
      logout,
      refetch,
      hasPermission,
      hasAnyPermission,
      hasAllPermissions,
    ]
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}

// Hook for checking permissions
export function usePermissions() {
  const { hasPermission, hasAnyPermission, hasAllPermissions, user } = useAuth()

  return {
    permissions: user?.permissions ?? [],
    hasPermission,
    hasAnyPermission,
    hasAllPermissions,
  }
}
