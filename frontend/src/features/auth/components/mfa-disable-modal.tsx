import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { AlertTriangle, Loader2 } from 'lucide-react'

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
import { Alert, AlertDescription } from '@/components/ui/alert'
import { useMfaDisable } from '../hooks'
import { getErrorMessage } from '@/lib/error-utils'

interface MfaDisableModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSuccess?: () => void
}

export function MfaDisableModal({
  open,
  onOpenChange,
  onSuccess,
}: MfaDisableModalProps) {
  const { t } = useTranslation()
  const [code, setCode] = useState('')

  const mfaDisable = useMfaDisable()

  // Reset state when modal closes
  useEffect(() => {
    if (!open) {
      setCode('')
      mfaDisable.reset()
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open])

  const handleDisable = () => {
    if (code.length !== 6) return

    mfaDisable.mutate(
      { code },
      {
        onSuccess: () => {
          onOpenChange(false)
          onSuccess?.()
        },
      }
    )
  }

  const isDisabling = mfaDisable.isPending

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{t('mfa:disable.title')}</DialogTitle>
          <DialogDescription>{t('mfa:disable.description')}</DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          <Alert variant="destructive">
            <AlertTriangle className="h-4 w-4" />
            <AlertDescription>{t('mfa:disable.warning')}</AlertDescription>
          </Alert>

          <div className="space-y-2">
            <Label htmlFor="disable-mfa-code">{t('mfa:disable.enterCode')}</Label>
            <Input
              id="disable-mfa-code"
              type="text"
              inputMode="numeric"
              pattern="[0-9]*"
              maxLength={6}
              placeholder={t('mfa:disable.placeholder')}
              value={code}
              onChange={(e) => setCode(e.target.value.replace(/\D/g, ''))}
              className="text-center text-2xl tracking-widest font-mono"
              autoFocus
            />
            {mfaDisable.isError && (
              <p className="text-sm text-destructive">
                {getErrorMessage(mfaDisable.error, t)}
              </p>
            )}
          </div>

          <div className="flex gap-3">
            <Button
              variant="outline"
              className="flex-1"
              onClick={() => onOpenChange(false)}
              disabled={isDisabling}
            >
              {t('mfa:disable.cancel')}
            </Button>
            <Button
              variant="destructive"
              className="flex-1"
              onClick={handleDisable}
              disabled={code.length !== 6 || isDisabling}
            >
              {isDisabling ? (
                <>
                  <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                  {t('mfa:disable.disabling')}
                </>
              ) : (
                t('mfa:disable.button')
              )}
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
