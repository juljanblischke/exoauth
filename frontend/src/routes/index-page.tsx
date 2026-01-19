import { Navigate } from '@tanstack/react-router'
import { LoadingSpinner } from '@/components/shared/feedback'
import { useAuth } from '@/contexts/auth-context'

export function IndexRedirect() {
  const { isAuthenticated, isLoading } = useAuth()

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <LoadingSpinner size="lg" />
      </div>
    )
  }

  return <Navigate to={isAuthenticated ? '/system/dashboard' : '/system/login'} />
}
