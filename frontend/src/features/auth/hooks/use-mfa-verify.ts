import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from '@tanstack/react-router'
import { mfaApi } from '../api/mfa-api'

const AUTH_QUERY_KEY = ['auth', 'me'] as const
const AUTH_SESSION_KEY = 'exoauth_has_session'

export function useMfaVerify() {
  const queryClient = useQueryClient()
  const navigate = useNavigate()

  return useMutation({
    mutationFn: mfaApi.verify,
    onSuccess: (response) => {
      // Set session flag for page refresh detection
      localStorage.setItem(AUTH_SESSION_KEY, 'true')
      // Update the auth cache with the user data
      queryClient.setQueryData(AUTH_QUERY_KEY, response.user)
      // Navigate to dashboard
      navigate({ to: '/dashboard' })
    },
  })
}
