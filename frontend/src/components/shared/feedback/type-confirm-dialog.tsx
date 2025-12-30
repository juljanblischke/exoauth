import { useState } from 'react'
import { Trans, useTranslation } from 'react-i18next'
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
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { buttonVariants } from '@/components/ui/button'
import { cn } from '@/lib/utils'

interface TypeConfirmDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  title: string
  description: string
  confirmText: string
  confirmLabel?: string
  cancelLabel?: string
  placeholder?: string
  loadingLabel?: string
  onConfirm: () => void
  onCancel?: () => void
  isLoading?: boolean
}

export function TypeConfirmDialog({
  open,
  onOpenChange,
  title,
  description,
  confirmText,
  confirmLabel,
  cancelLabel,
  loadingLabel,
  onConfirm,
  onCancel,
  isLoading = false,
}: TypeConfirmDialogProps) {
  const { t } = useTranslation()
  const [inputValue, setInputValue] = useState('')

  // Use translations for defaults
  const resolvedConfirmLabel = confirmLabel ?? t('common:actions.delete')
  const resolvedCancelLabel = cancelLabel ?? t('common:actions.cancel')
  const resolvedLoadingLabel = loadingLabel ?? t('common:states.deleting')

  const isConfirmEnabled = inputValue === confirmText

  const handleCancel = () => {
    setInputValue('')
    onCancel?.()
    onOpenChange(false)
  }

  const handleConfirm = () => {
    if (isConfirmEnabled) {
      onConfirm()
      setInputValue('')
    }
  }

  const handleOpenChange = (isOpen: boolean) => {
    if (!isOpen) {
      setInputValue('')
    }
    onOpenChange(isOpen)
  }

  return (
    <AlertDialog open={open} onOpenChange={handleOpenChange}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>{title}</AlertDialogTitle>
          <AlertDialogDescription>{description}</AlertDialogDescription>
        </AlertDialogHeader>
        <div className="py-4">
          <Label htmlFor="confirm-input" className="text-sm text-muted-foreground">
            <Trans
              i18nKey="common:confirm.typeToConfirm"
              values={{ text: confirmText }}
              components={{ highlight: <span className="font-mono font-semibold text-foreground" /> }}
            />
          </Label>
          <Input
            id="confirm-input"
            value={inputValue}
            onChange={(e) => setInputValue(e.target.value)}
            placeholder={confirmText}
            className="mt-2"
            autoComplete="off"
            disabled={isLoading}
          />
        </div>
        <AlertDialogFooter>
          <AlertDialogCancel onClick={handleCancel} disabled={isLoading}>
            {resolvedCancelLabel}
          </AlertDialogCancel>
          <AlertDialogAction
            onClick={handleConfirm}
            disabled={!isConfirmEnabled || isLoading}
            className={cn(buttonVariants({ variant: 'destructive' }))}
          >
            {isLoading ? resolvedLoadingLabel : resolvedConfirmLabel}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
