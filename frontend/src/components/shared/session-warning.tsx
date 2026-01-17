import { useEffect, useState, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { useAuth } from '@/contexts/auth-context'

interface SessionWarningProps {
  warningTimeMs?: number
  onExtend?: () => Promise<void>
}

export function SessionWarning({
  warningTimeMs = 5 * 60 * 1000,
  onExtend,
}: SessionWarningProps) {
  const { t } = useTranslation('common')
  const { isAuthenticated, logout, tokenExpiresAt } = useAuth()
  const [showWarning, setShowWarning] = useState(false)
  const [timeLeft, setTimeLeft] = useState(0)

  const checkSession = useCallback(() => {
    if (!isAuthenticated || !tokenExpiresAt) {
      setShowWarning(false)
      return
    }

    const now = Date.now()
    const expiresAt = new Date(tokenExpiresAt).getTime()
    const remaining = expiresAt - now

    if (remaining <= 0) {
      logout()
      setShowWarning(false)
    } else if (remaining <= warningTimeMs) {
      setShowWarning(true)
      setTimeLeft(Math.ceil(remaining / 1000))
    } else {
      setShowWarning(false)
    }
  }, [isAuthenticated, tokenExpiresAt, warningTimeMs, logout])

  useEffect(() => {
    checkSession()
    const interval = setInterval(checkSession, 1000)
    return () => clearInterval(interval)
  }, [checkSession])

  useEffect(() => {
    if (showWarning && timeLeft > 0) {
      const timer = setInterval(() => {
        setTimeLeft((prev) => {
          if (prev <= 1) {
            logout()
            return 0
          }
          return prev - 1
        })
      }, 1000)
      return () => clearInterval(timer)
    }
  }, [showWarning, timeLeft, logout])

  const handleExtend = async () => {
    if (onExtend) {
      await onExtend()
    }
    setShowWarning(false)
  }

  const handleLogout = () => {
    logout()
    setShowWarning(false)
  }

  const formatTime = (seconds: number) => {
    const mins = Math.floor(seconds / 60)
    const secs = seconds % 60
    return `${mins}:${secs.toString().padStart(2, '0')}`
  }

  if (!showWarning) return null

  return (
    <AlertDialog open={showWarning} onOpenChange={setShowWarning}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>{t('session.warningTitle')}</AlertDialogTitle>
          <AlertDialogDescription>
            {t('session.warningDescription', { time: formatTime(timeLeft) })}
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel onClick={handleLogout}>
            {t('actions.logout')}
          </AlertDialogCancel>
          <AlertDialogAction onClick={handleExtend}>
            {t('session.extend')}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
