import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Navigate } from '@tanstack/react-router'
import { useAuth } from '@/contexts/auth-context'
import { LoginForm, MagicLinkForm, MagicLinkSent } from '@/features/auth'
import { LoadingSpinner } from '@/components/shared/feedback'

export function LoginPage() {
  const { t } = useTranslation('auth')
  const { isAuthenticated, isLoading } = useAuth()
  const [loginMode, setLoginMode] = useState<'password' | 'magic-link'>('password')
  const [magicLinkEmail, setMagicLinkEmail] = useState<string | null>(null)

  // Show loading while checking auth status
  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <LoadingSpinner size="lg" />
      </div>
    )
  }

  // Redirect to dashboard if already authenticated
  if (isAuthenticated) {
    return <Navigate to="/system/dashboard" />
  }

  // Handle magic link success
  const handleMagicLinkSuccess = (email: string) => {
    setMagicLinkEmail(email)
  }

  // If magic link was sent, show success message
  if (magicLinkEmail) {
    return <MagicLinkSent email={magicLinkEmail} onBack={() => setMagicLinkEmail(null)} />
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-background px-4">
      <div className="w-full max-w-md space-y-6">
        <div className="space-y-2 text-center">
          <h1 className="text-3xl font-bold tracking-tight">{t('login.title')}</h1>
          <p className="text-muted-foreground">{t('login.subtitle')}</p>
        </div>

        <div className="rounded-lg border bg-card p-6 shadow-sm">
          {loginMode === 'password' ? <LoginForm /> : <MagicLinkForm onSuccess={handleMagicLinkSuccess} />}

          <div className="mt-4 text-center">
            <button
              type="button"
              onClick={() => setLoginMode(loginMode === 'password' ? 'magic-link' : 'password')}
              className="text-sm text-primary hover:underline"
            >
              {loginMode === 'password'
                ? t('login.useMagicLink')
                : t('login.usePassword')}
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}
