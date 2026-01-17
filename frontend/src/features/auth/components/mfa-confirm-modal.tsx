import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { AlertTriangle } from 'lucide-react'

import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { BackupCodesDisplay } from './backup-codes-display'

interface MfaConfirmModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  backupCodes: string[]
  /** Called when user clicks continue (after saving backup codes) */
  onContinue?: () => void
}

const CONTINUE_DELAY_MS = 3000

function MfaConfirmModalContent({
  backupCodes,
  onContinue,
  onOpenChange,
}: Pick<MfaConfirmModalProps, 'backupCodes' | 'onContinue' | 'onOpenChange'>) {
  const { t } = useTranslation()
  const [canContinue, setCanContinue] = useState(false)

  // Delay before user can continue (to encourage saving codes)
  useEffect(() => {
    const timer = setTimeout(() => {
      setCanContinue(true)
    }, CONTINUE_DELAY_MS)
    return () => clearTimeout(timer)
  }, [])

  return (
    <>
      <DialogHeader>
        <DialogTitle>{t('mfa:confirm.title')}</DialogTitle>
        <DialogDescription>{t('mfa:confirm.description')}</DialogDescription>
      </DialogHeader>

      <div className="space-y-4">
        <Alert variant="destructive">
          <AlertTriangle className="h-4 w-4" />
          <AlertDescription>{t('mfa:confirm.warning')}</AlertDescription>
        </Alert>

        <BackupCodesDisplay codes={backupCodes} />

        <div className="pt-2">
          <Button
            className="w-full"
            onClick={() => {
              if (onContinue) {
                onContinue()
              } else {
                onOpenChange(false)
              }
            }}
            disabled={!canContinue}
          >
            {t('mfa:confirm.continueButton')}
          </Button>
          {!canContinue && (
            <p className="text-xs text-muted-foreground text-center mt-2">
              {t('mfa:confirm.continueWarning')}
            </p>
          )}
        </div>
      </div>
    </>
  )
}

export function MfaConfirmModal({
  open,
  onOpenChange,
  backupCodes,
  onContinue,
}: MfaConfirmModalProps) {

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className="sm:max-w-md"
        onPointerDownOutside={(e) => e.preventDefault()}
        onEscapeKeyDown={(e) => e.preventDefault()}
      >
        {open && (
          <MfaConfirmModalContent
            backupCodes={backupCodes}
            onContinue={onContinue}
            onOpenChange={onOpenChange}
          />
        )}
      </DialogContent>
    </Dialog>
  )
}
