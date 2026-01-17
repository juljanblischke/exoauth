import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { Loader2, AlertTriangle } from 'lucide-react'

import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useMfaVerify } from '../hooks'
import { getDeviceInfo } from '@/lib/device'
import { getErrorMessage } from '@/lib/error-utils'
import { CaptchaWidget } from './captcha-widget'
import type { DeviceApprovalRequiredResponse } from '../types'

interface MfaVerifyModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  mfaToken: string
  rememberMe: boolean
  onDeviceApprovalRequired?: (response: DeviceApprovalRequiredResponse) => void
}

export function MfaVerifyModal({
  open,
  onOpenChange,
  mfaToken,
  rememberMe,
  onDeviceApprovalRequired,
}: MfaVerifyModalProps) {
  const { t } = useTranslation()
  const [code, setCode] = useState('')
  const [useBackupCode, setUseBackupCode] = useState(false)
  const [isTokenExpired, setIsTokenExpired] = useState(false)
  const [showCaptcha, setShowCaptcha] = useState(false)
  const [captchaToken, setCaptchaToken] = useState<string | null>(null)
  const [captchaKey, setCaptchaKey] = useState(0)

  const mfaVerify = useMfaVerify({
    onDeviceApprovalRequired: (response) => {
      onOpenChange(false)
      onDeviceApprovalRequired?.(response)
    },
    onCaptchaRequired: () => {
      setShowCaptcha(true)
      setCaptchaToken(null)
    },
    onCaptchaExpired: () => {
      setCaptchaToken(null)
      setCaptchaKey((k) => k + 1)
    },
    onError: (error) => {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      const apiError = error as any
      const errorCode = apiError?.code || apiError?.response?.data?.errors?.[0]?.code
      if (errorCode === 'MFA_TOKEN_INVALID' || errorCode === 'MFA_TOKEN_EXPIRED') {
        setIsTokenExpired(true)
      }
    },
  })

  // Reset state when modal closes
  useEffect(() => {
    if (!open) {
      setCode('')
      setUseBackupCode(false)
      setIsTokenExpired(false)
      setShowCaptcha(false)
      setCaptchaToken(null)
      setCaptchaKey(0)
      mfaVerify.reset()
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open])

  const handleVerify = () => {
    const expectedLength = useBackupCode ? 8 : 6
    if (code.length !== expectedLength) return
    if (showCaptcha && !captchaToken) return

    const deviceInfo = getDeviceInfo()
    mfaVerify.mutate({
      mfaToken,
      code,
      rememberMe,
      captchaToken: captchaToken || undefined,
      ...deviceInfo,
    })
  }

  const handleCaptchaVerify = (token: string) => {
    setCaptchaToken(token)
  }

  const handleCaptchaExpire = () => {
    setCaptchaToken(null)
  }

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      handleVerify()
    }
  }

  const isVerifying = mfaVerify.isPending
  const expectedLength = useBackupCode ? 8 : 6

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className="sm:max-w-md"
        onPointerDownOutside={(e) => e.preventDefault()}
        onEscapeKeyDown={(e) => e.preventDefault()}
      >
        <DialogHeader>
          <DialogTitle>
            {useBackupCode ? t('mfa:verify.backupCodeTitle') : t('mfa:verify.title')}
          </DialogTitle>
          <DialogDescription>
            {useBackupCode
              ? t('mfa:verify.backupCodeDescription')
              : t('mfa:verify.description')}
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          {isTokenExpired ? (
            /* Expired State */
            <div className="flex flex-col items-center gap-4 py-6">
              <div className="rounded-full bg-destructive/10 p-3">
                <AlertTriangle className="h-8 w-8 text-destructive" />
              </div>
              <p className="text-center font-medium text-destructive">
                {t('mfa:errors.tokenExpired')}
              </p>
              <Button onClick={() => onOpenChange(false)} className="w-full">
                {t('mfa:verify.startNewLogin')}
              </Button>
            </div>
          ) : (
            /* Input State */
            <>
              <div className="space-y-2">
                <Label htmlFor="verify-mfa-code">
                  {useBackupCode
                    ? t('mfa:verify.backupCodeDescription')
                    : t('mfa:verify.description')}
                </Label>
                <Input
                  id="verify-mfa-code"
                  type="text"
                  inputMode={useBackupCode ? 'text' : 'numeric'}
                  pattern={useBackupCode ? undefined : '[0-9]*'}
                  maxLength={expectedLength}
                  placeholder={
                    useBackupCode
                      ? t('mfa:verify.backupCodePlaceholder')
                      : t('mfa:verify.placeholder')
                  }
                  value={code}
                  onChange={(e) =>
                    setCode(
                      useBackupCode
                        ? e.target.value.toUpperCase()
                        : e.target.value.replace(/\D/g, '')
                    )
                  }
                  onKeyDown={handleKeyDown}
                  className="text-center text-2xl tracking-widest font-mono"
                  autoFocus
                />
                {mfaVerify.isError && !isTokenExpired && (
                  <p className="text-sm text-destructive">
                    {getErrorMessage(mfaVerify.error, t)}
                  </p>
                )}
              </div>

              {showCaptcha && (
                <CaptchaWidget
                  key={captchaKey}
                  onVerify={handleCaptchaVerify}
                  onExpire={handleCaptchaExpire}
                  action="mfa_verify"
                />
              )}

              <Button
                className="w-full"
                onClick={handleVerify}
                disabled={
                  code.length !== expectedLength ||
                  isVerifying ||
                  (showCaptcha && !captchaToken)
                }
              >
                {isVerifying ? (
                  <>
                    <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                    {t('mfa:verify.verifying')}
                  </>
                ) : (
                  t('mfa:verify.button')
                )}
              </Button>

              <button
                type="button"
                className="w-full text-sm text-muted-foreground hover:text-foreground"
                onClick={() => {
                  setCode('')
                  setUseBackupCode(!useBackupCode)
                }}
                disabled={isVerifying}
              >
                {useBackupCode ? t('mfa:verify.backToCode') : t('mfa:verify.useBackupCode')}
              </button>
            </>
          )}
        </div>
      </DialogContent>
    </Dialog>
  )
}
