import { useState, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { Loader2, ShieldCheck, ShieldAlert, AlertTriangle } from 'lucide-react'

import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'

import { DeviceApprovalCodeInput } from './device-approval-code-input'
import { useApproveDeviceByCode } from '../hooks'
import type { DeviceApprovalModalState, DeviceApprovalModalProps } from '../types'

export function DeviceApprovalModal({
  open,
  onOpenChange,
  approvalToken,
  riskFactors,
  onSuccess,
}: Omit<DeviceApprovalModalProps, 'onDeny'>) {
  const { t } = useTranslation()

  const [code, setCode] = useState('')
  const [modalState, setModalState] = useState<DeviceApprovalModalState>('input')
  const [remainingAttempts, setRemainingAttempts] = useState<number | null>(null)

  const approveByCode = useApproveDeviceByCode()

  const resetModal = useCallback(() => {
    setCode('')
    setModalState('input')
    setRemainingAttempts(null)
  }, [])

  const handleSubmit = () => {
    // Remove dash for API call
    const cleanCode = code.replace('-', '')
    if (cleanCode.length !== 8) return

    setModalState('loading')

    approveByCode.mutate(
      { approvalToken, code: cleanCode },
      {
        onSuccess: () => {
          setModalState('success')
        },
        onError: (error) => {
          // eslint-disable-next-line @typescript-eslint/no-explicit-any
          const apiError = error as any
          const errorCode = apiError?.code || apiError?.response?.data?.errors?.[0]?.code

          if (errorCode === 'APPROVAL_TOKEN_EXPIRED') {
            setModalState('expired')
          } else if (errorCode === 'APPROVAL_MAX_ATTEMPTS') {
            setModalState('maxAttempts')
          } else {
            // Wrong code - show remaining attempts
            const attempts = apiError?.response?.data?.data?.remainingAttempts
            if (typeof attempts === 'number') {
              setRemainingAttempts(attempts)
            }
            setModalState('error')
          }
        },
      }
    )
  }

  const handleRetryLogin = () => {
    resetModal()
    onOpenChange(false)
    onSuccess()
  }

  const handleStartNewLogin = () => {
    resetModal()
    onOpenChange(false)
  }

  const handleOpenChange = (newOpen: boolean) => {
    if (!newOpen) {
      resetModal()
    }
    onOpenChange(newOpen)
  }

  // Check if code is complete
  const isCodeComplete = code.replace('-', '').length === 8
  const isLoading = modalState === 'loading'

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <ShieldAlert className="h-5 w-5 text-amber-500" />
            {t('auth:deviceApproval.title')}
          </DialogTitle>
          <DialogDescription>
            {t('auth:deviceApproval.description')}
          </DialogDescription>
        </DialogHeader>

        {/* Risk factors display */}
        {riskFactors.length > 0 && modalState === 'input' && (
          <div className="flex flex-wrap gap-2 text-xs text-muted-foreground">
            {riskFactors.map((factor) => (
              <span
                key={factor}
                className="rounded-full bg-amber-100 px-2 py-1 text-amber-800 dark:bg-amber-900/20 dark:text-amber-400"
              >
                {t(`auth:deviceApproval.riskFactors.${factor}`, { defaultValue: factor.replace('_', ' ') })}
              </span>
            ))}
          </div>
        )}

        <div className="space-y-4 py-4">
          {/* Input State */}
          {(modalState === 'input' || modalState === 'error') && (
            <>
              <div className="space-y-2">
                <label className="text-sm font-medium">
                  {t('auth:deviceApproval.codeLabel')}
                </label>
                <DeviceApprovalCodeInput
                  value={code}
                  onChange={setCode}
                  disabled={isLoading}
                  error={modalState === 'error'}
                />
                <p className="text-center text-sm text-muted-foreground">
                  {t('auth:deviceApproval.codeHint')}
                </p>
              </div>

              {/* Error message with remaining attempts */}
              {modalState === 'error' && (
                <div className="rounded-md bg-destructive/10 p-3 text-center text-sm text-destructive">
                  {t('errors:codes.APPROVAL_CODE_INVALID')}
                  {remainingAttempts !== null && (
                    <span className="block mt-1 font-medium">
                      {t('auth:deviceApproval.attemptsRemaining', { count: remainingAttempts })}
                    </span>
                  )}
                </div>
              )}

              <Button
                onClick={handleSubmit}
                disabled={!isCodeComplete || isLoading}
                className="w-full"
              >
                {isLoading ? (
                  <>
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                    {t('common:loading')}
                  </>
                ) : (
                  t('auth:deviceApproval.submitButton')
                )}
              </Button>
            </>
          )}

          {/* Loading State */}
          {modalState === 'loading' && (
            <div className="flex flex-col items-center gap-4 py-6">
              <Loader2 className="h-8 w-8 animate-spin text-primary" />
              <p className="text-sm text-muted-foreground">
                {t('auth:deviceApproval.linkApproval.loading')}
              </p>
            </div>
          )}

          {/* Success State */}
          {modalState === 'success' && (
            <div className="flex flex-col items-center gap-4 py-6">
              <div className="rounded-full bg-green-100 p-3 dark:bg-green-900/20">
                <ShieldCheck className="h-8 w-8 text-green-600 dark:text-green-400" />
              </div>
              <p className="text-center font-medium">
                {t('auth:deviceApproval.success')}
              </p>
              <Button onClick={handleRetryLogin} className="w-full">
                {t('auth:deviceApproval.retryButton')}
              </Button>
            </div>
          )}

          {/* Expired State */}
          {modalState === 'expired' && (
            <div className="flex flex-col items-center gap-4 py-6">
              <div className="rounded-full bg-destructive/10 p-3">
                <AlertTriangle className="h-8 w-8 text-destructive" />
              </div>
              <p className="text-center font-medium text-destructive">
                {t('auth:deviceApproval.expired')}
              </p>
              <Button onClick={handleStartNewLogin} className="w-full">
                {t('auth:deviceApproval.startNewLogin')}
              </Button>
            </div>
          )}

          {/* Max Attempts State */}
          {modalState === 'maxAttempts' && (
            <div className="flex flex-col items-center gap-4 py-6">
              <div className="rounded-full bg-destructive/10 p-3">
                <AlertTriangle className="h-8 w-8 text-destructive" />
              </div>
              <p className="text-center font-medium text-destructive">
                {t('auth:deviceApproval.maxAttempts')}
              </p>
              <Button onClick={handleStartNewLogin} className="w-full">
                {t('auth:deviceApproval.startNewLogin')}
              </Button>
            </div>
          )}
        </div>
      </DialogContent>
    </Dialog>
  )
}
