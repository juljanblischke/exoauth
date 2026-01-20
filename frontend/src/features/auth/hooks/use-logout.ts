import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from '@tanstack/react-router'
import { authApi } from '../api/auth-api'

const AUTH_SESSION_KEY = 'exoauth_has_session'

export function useLogout() {
  const queryClient = useQueryClient()
  const navigate = useNavigate()

  return useMutation({
    mutationFn: () => authApi.logout(),
    onSuccess: () => {
      // Clear session flag
      localStorage.removeItem(AUTH_SESSION_KEY)
      // Clear the auth cache
      queryClient.setQueryData(['auth', 'me'], null)
      // Clear all cached data
      queryClient.clear()
      // Navigate to login
      navigate({ to: '/login' })
    },
  })
}
