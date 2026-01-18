/* eslint-disable react-refresh/only-export-components */
import {
  createContext,
  useContext,
  useCallback,
  useMemo,
  useEffect,
  useState,
  type ReactNode,
} from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
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
  login: (data: LoginRequest) => Promise<AuthResponse>
  register: (data: RegisterRequest) => Promise<AuthResponse>
  logout: () => Promise<void>
  refetch: () => Promise<void>
  hasPermission: (permission: string) => boolean
  hasAnyPermission: (permissions: string[]) => boolean
  hasAllPermissions: (permissions: string[]) => boolean
  tokenExpiresAt: string | null
  sessionId: string | null
  deviceId: string | null
}

const AuthContext = createContext<AuthContextValue | null>(null)

const AUTH_QUERY_KEY = ['auth', 'me'] as const
const AUTH_SESSION_KEY = 'exoauth_has_session'

// Check if user might have a session (logged in before)
const hasSession = () => localStorage.getItem(AUTH_SESSION_KEY) === 'true'
const setSession = (value: boolean) => {
  if (value) {
    localStorage.setItem(AUTH_SESSION_KEY, 'true')
  } else {
    localStorage.removeItem(AUTH_SESSION_KEY)
  }
}

interface AuthProviderProps {
  children: ReactNode
}

export function AuthProvider({ children }: AuthProviderProps) {
  const queryClient = useQueryClient()
  const { t, i18n } = useTranslation()
  const [sessionId, setSessionId] = useState<string | null>(null)
  const [deviceId, setDeviceId] = useState<string | null>(null)

  // Listen for session expired events from axios interceptor
  useEffect(() => {
    const handleSessionExpired = () => {
      // Redirect immediately to avoid re-render race conditions
      window.location.href = '/login'
    }

    const handleForceReauth = () => {
      queryClient.setQueryData(AUTH_QUERY_KEY, null)
      queryClient.clear()
      toast.warning(t('forceReauth.title'), {
        description: t('forceReauth.description'),
        duration: 5000,
      })
      // Delay redirect so user can see the toast
      setTimeout(() => {
        window.location.href = '/login'
      }, 1500)
    }

    window.addEventListener('auth:session-expired', handleSessionExpired)
    window.addEventListener('auth:force-reauth', handleForceReauth)
    return () => {
      window.removeEventListener('auth:session-expired', handleSessionExpired)
      window.removeEventListener('auth:force-reauth', handleForceReauth)
    }
  }, [queryClient, t])

  // Multi-tab logout sync: Listen for localStorage changes from other tabs
  useEffect(() => {
    const handleStorageChange = (e: StorageEvent) => {
      if (e.key === AUTH_SESSION_KEY && !e.newValue) {
        // Logged out in another tab - sync this tab
        queryClient.setQueryData(AUTH_QUERY_KEY, null)
        queryClient.clear()
      }
    }
    window.addEventListener('storage', handleStorageChange)
    return () => window.removeEventListener('storage', handleStorageChange)
  }, [queryClient])

  // Fetch current user - only if we might have a session
  const {
    data: user,
    isLoading,
    refetch: refetchUser,
  } = useQuery({
    queryKey: AUTH_QUERY_KEY,
    queryFn: async (): Promise<User | null> => {
      // Skip API call if user never logged in
      if (!hasSession()) {
        return null
      }
      try {
        const response = await apiClient.get<ApiResponse<User>>('/system/auth/me')
        const user = extractData(response)
        return user
      } catch {
        // Session expired or invalid - clear the flag
        setSession(false)
        return null
      }
    },
    retry: false,
    staleTime: 5 * 60 * 1000, // 5 minutes
  })

  // Sync preferredLanguage with i18n when user data changes
  useEffect(() => {
    if (user?.preferredLanguage && user.preferredLanguage !== i18n.language) {
      i18n.changeLanguage(user.preferredLanguage)
    }
  }, [user?.preferredLanguage, i18n])

  // Login mutation
  const loginMutation = useMutation({
    mutationFn: async (data: LoginRequest) => {
      const response = await apiClient.post<ApiResponse<AuthResponse>>(
        '/system/auth/login',
        data
      )
      return extractData(response)
    },
    onSuccess: (data) => {
      setSession(true)
      setSessionId(data.sessionId)
      setDeviceId(data.deviceId)
      queryClient.setQueryData(AUTH_QUERY_KEY, data.user)
    },
  })

  // Register mutation
  const registerMutation = useMutation({
    mutationFn: async (data: RegisterRequest) => {
      const response = await apiClient.post<ApiResponse<AuthResponse>>(
        '/system/auth/register',
        data
      )
      return extractData(response)
    },
    onSuccess: (data) => {
      setSession(true)
      setSessionId(data.sessionId)
      setDeviceId(data.deviceId)
      queryClient.setQueryData(AUTH_QUERY_KEY, data.user)
    },
  })

  // Logout mutation
  const logoutMutation = useMutation({
    mutationFn: async () => {
      const response =
        await apiClient.post<ApiResponse<LogoutResponse>>('/system/auth/logout')
      return extractData(response)
    },
    onSuccess: () => {
      setSession(false)
      setSessionId(null)
      setDeviceId(null)
      queryClient.setQueryData(AUTH_QUERY_KEY, null)
      queryClient.clear()
    },
  })

  const login = useCallback(
    async (data: LoginRequest): Promise<AuthResponse> => {
      const result = await loginMutation.mutateAsync(data)
      return result
    },
    [loginMutation]
  )

  const register = useCallback(
    async (data: RegisterRequest): Promise<AuthResponse> => {
      const result = await registerMutation.mutateAsync(data)
      return result
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
    [user]
  )

  const hasAnyPermission = useCallback(
    (permissions: string[]): boolean => {
      if (!user?.permissions) return false
      return permissions.some((p) => user.permissions.includes(p))
    },
    [user]
  )

  const hasAllPermissions = useCallback(
    (permissions: string[]): boolean => {
      if (!user?.permissions) return false
      return permissions.every((p) => user.permissions.includes(p))
    },
    [user]
  )

  const value = useMemo<AuthContextValue>(
    () => ({
      user: user ?? null,
      isAuthenticated: !!user,
      isLoading,
      tokenExpiresAt: null, // TODO: Get from session/cookie when backend provides it
      sessionId,
      deviceId,
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
      sessionId,
      deviceId,
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
