import { useEffect, useCallback } from 'react'
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
import { buttonVariants } from '@/components/ui/button'

interface UnsavedWarningProps {
  hasUnsavedChanges: boolean
  open: boolean
  onOpenChange: (open: boolean) => void
  onDiscard: () => void
  onSave?: () => void
}

export function UnsavedWarning({
  hasUnsavedChanges,
  open,
  onOpenChange,
  onDiscard,
  onSave,
}: UnsavedWarningProps) {
  const { t } = useTranslation('common')

  const handleBeforeUnload = useCallback(
    (e: BeforeUnloadEvent) => {
      if (hasUnsavedChanges) {
        e.preventDefault()
        e.returnValue = ''
      }
    },
    [hasUnsavedChanges]
  )

  useEffect(() => {
    window.addEventListener('beforeunload', handleBeforeUnload)
    return () => {
      window.removeEventListener('beforeunload', handleBeforeUnload)
    }
  }, [handleBeforeUnload])

  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>{t('unsavedChanges.title')}</AlertDialogTitle>
          <AlertDialogDescription>{t('unsavedChanges.description')}</AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter className="gap-2">
          <AlertDialogCancel>{t('unsavedChanges.continueEditing')}</AlertDialogCancel>
          <AlertDialogAction
            onClick={onDiscard}
            className={buttonVariants({ variant: 'destructive' })}
          >
            {t('unsavedChanges.discard')}
          </AlertDialogAction>
          {onSave && (
            <AlertDialogAction onClick={onSave}>
              {t('unsavedChanges.save')}
            </AlertDialogAction>
          )}
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
