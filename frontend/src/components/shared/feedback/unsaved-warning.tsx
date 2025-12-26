import { useEffect, useCallback } from 'react'
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
  title?: string
  description?: string
}

export function UnsavedWarning({
  hasUnsavedChanges,
  open,
  onOpenChange,
  onDiscard,
  onSave,
  title = 'Unsaved changes',
  description = 'You have unsaved changes. Are you sure you want to leave? Your changes will be lost.',
}: UnsavedWarningProps) {
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
          <AlertDialogTitle>{title}</AlertDialogTitle>
          <AlertDialogDescription>{description}</AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter className="gap-2 sm:gap-0">
          <AlertDialogCancel>Continue editing</AlertDialogCancel>
          <AlertDialogAction
            onClick={onDiscard}
            className={buttonVariants({ variant: 'destructive' })}
          >
            Discard changes
          </AlertDialogAction>
          {onSave && (
            <AlertDialogAction onClick={onSave}>
              Save changes
            </AlertDialogAction>
          )}
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
