import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { QRCodeSVG } from 'qrcode.react'
import { Eye, EyeOff, Loader2 } from 'lucide-react'

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
import { useMfaSetup, useMfaConfirm } from '../hooks'
import type { MfaSetupResponse, MfaConfirmResponse } from '../types'
import { getDeviceInfo } from '@/lib/device'

interface MfaSetupModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSuccess: (response: MfaConfirmResponse) => void
  /** If true, user cannot close the modal (for required MFA setup) */
  required?: boolean
  /** Setup token for forced MFA flow (login/register) */
  setupToken?: string
}

export function MfaSetupModal({
  open,
  onOpenChange,
  onSuccess,
  required = false,
  setupToken,
}: MfaSetupModalProps) {
  const { t } = useTranslation()
  const [step, setStep] = useState<'qr' | 'verify'>('qr')
  const [showManualKey, setShowManualKey] = useState(false)
  const [code, setCode] = useState('')
  const [setupData, setSetupData] = useState<MfaSetupResponse | null>(null)

  const mfaSetup = useMfaSetup()
  // Skip cache when using setupToken - form handles auth after backup codes shown
  const mfaConfirm = useMfaConfirm({ skipCache: !!setupToken })

  // Start setup when modal opens
  useEffect(() => {
    if (open && !setupData && !mfaSetup.isPending) {
      mfaSetup.mutate(setupToken, {
        onSuccess: (data) => {
          setSetupData(data)
        },
      })
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, setupData, setupToken])

  // Reset state when modal closes
  useEffect(() => {
    if (!open) {
      setStep('qr')
      setShowManualKey(false)
      setCode('')
      setSetupData(null)
      mfaSetup.reset()
      mfaConfirm.reset()
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open])

  const handleVerify = () => {
    if (code.length !== 6) return

    // Include device info when using setupToken (for session creation)
    const deviceInfo = setupToken ? getDeviceInfo() : {}

    mfaConfirm.mutate(
      { code, setupToken, ...deviceInfo },
      {
        onSuccess: (data: MfaConfirmResponse) => {
          onSuccess(data)
          onOpenChange(false)
        },
      }
    )
  }

  const handleClose = () => {
    if (!required) {
      onOpenChange(false)
    }
  }

  const isLoading = mfaSetup.isPending
  const isVerifying = mfaConfirm.isPending

  return (
    <Dialog open={open} onOpenChange={required ? undefined : onOpenChange}>
      <DialogContent
        className="sm:max-w-md"
        onPointerDownOutside={required ? (e) => e.preventDefault() : undefined}
        onEscapeKeyDown={required ? (e) => e.preventDefault() : undefined}
      >
        <DialogHeader>
          <DialogTitle>{t('mfa:setup.title')}</DialogTitle>
          <DialogDescription>{t('mfa:setup.description')}</DialogDescription>
        </DialogHeader>

        {isLoading ? (
          <div className="flex items-center justify-center py-8">
            <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
          </div>
        ) : setupData ? (
          <div className="space-y-6">
            {step === 'qr' && (
              <>
                {/* Step 2: QR Code */}
                <div className="space-y-2">
                  <h4 className="font-medium">{t('mfa:setup.step2.title')}</h4>
                  <p className="text-sm text-muted-foreground">
                    {t('mfa:setup.step2.description')}
                  </p>
                </div>

                <div className="flex justify-center p-4 bg-white rounded-lg">
                  <QRCodeSVG
                    value={setupData.qrCodeUri}
                    size={200}
                    level="M"
                  />
                </div>

                {/* Manual Entry */}
                <div className="space-y-2">
                  <button
                    type="button"
                    className="text-sm text-primary hover:underline flex items-center gap-2"
                    onClick={() => setShowManualKey(!showManualKey)}
                  >
                    {showManualKey ? (
                      <EyeOff className="h-4 w-4" />
                    ) : (
                      <Eye className="h-4 w-4" />
                    )}
                    {t('mfa:setup.manualEntry.button')}
                  </button>

                  {showManualKey && (
                    <div className="p-3 bg-muted rounded-lg">
                      <p className="text-xs text-muted-foreground mb-1">
                        {t('mfa:setup.manualEntry.description')}
                      </p>
                      <code className="text-sm font-mono break-all">
                        {setupData.manualEntryKey}
                      </code>
                    </div>
                  )}
                </div>

                <Button className="w-full" onClick={() => setStep('verify')}>
                  {t('common:actions.next')}
                </Button>
              </>
            )}

            {step === 'verify' && (
              <>
                {/* Step 3: Verify */}
                <div className="space-y-2">
                  <h4 className="font-medium">{t('mfa:setup.step3.title')}</h4>
                  <p className="text-sm text-muted-foreground">
                    {t('mfa:setup.step3.description')}
                  </p>
                </div>

                <div className="space-y-2">
                  <Label htmlFor="mfa-code">{t('mfa:verify.description')}</Label>
                  <Input
                    id="mfa-code"
                    type="text"
                    inputMode="numeric"
                    pattern="[0-9]*"
                    maxLength={6}
                    placeholder={t('mfa:setup.step3.placeholder')}
                    value={code}
                    onChange={(e) => setCode(e.target.value.replace(/\D/g, ''))}
                    className="text-center text-2xl tracking-widest font-mono"
                    autoFocus
                  />
                  {mfaConfirm.isError && (
                    <p className="text-sm text-destructive">
                      {t('mfa:errors.codeInvalid')}
                    </p>
                  )}
                </div>

                <div className="flex gap-3">
                  <Button
                    variant="outline"
                    onClick={() => setStep('qr')}
                    disabled={isVerifying}
                  >
                    {t('common:actions.back')}
                  </Button>
                  <Button
                    className="flex-1"
                    onClick={handleVerify}
                    disabled={code.length !== 6 || isVerifying}
                  >
                    {isVerifying ? (
                      <>
                        <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                        {t('mfa:setup.step3.verifying')}
                      </>
                    ) : (
                      t('mfa:setup.step3.button')
                    )}
                  </Button>
                </div>
              </>
            )}

            {!required && (
              <Button
                variant="ghost"
                className="w-full"
                onClick={handleClose}
                disabled={isVerifying}
              >
                {t('mfa:setup.cancel')}
              </Button>
            )}
          </div>
        ) : mfaSetup.isError ? (
          <div className="py-4 text-center">
            <p className="text-destructive">{t('mfa:errors.setupFailed')}</p>
            <Button
              variant="outline"
              className="mt-4"
              onClick={() => mfaSetup.mutate(setupToken)}
            >
              {t('common:actions.retry')}
            </Button>
          </div>
        ) : null}
      </DialogContent>
    </Dialog>
  )
}
