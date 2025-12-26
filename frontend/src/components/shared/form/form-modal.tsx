import { useState, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { UnsavedWarning } from '../feedback/unsaved-warning'
import { cn } from '@/lib/utils'

interface FormModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  title: string
  description?: string
  children: React.ReactNode
  onSubmit?: () => void | Promise<void>
  onCancel?: () => void
  submitLabel?: string
  cancelLabel?: string
  isSubmitting?: boolean
  isDirty?: boolean
  size?: 'sm' | 'md' | 'lg' | 'xl' | 'full'
  showFooter?: boolean
  className?: string
}

const sizeClasses = {
  sm: 'sm:max-w-sm',
  md: 'sm:max-w-md',
  lg: 'sm:max-w-lg',
  xl: 'sm:max-w-xl',
  full: 'sm:max-w-4xl',
}

export function FormModal({
  open,
  onOpenChange,
  title,
  description,
  children,
  onSubmit,
  onCancel,
  submitLabel,
  cancelLabel,
  isSubmitting = false,
  isDirty = false,
  size = 'md',
  showFooter = true,
  className,
}: FormModalProps) {
  const { t } = useTranslation('common')
  const [showUnsavedWarning, setShowUnsavedWarning] = useState(false)

  const handleOpenChange = useCallback(
    (isOpen: boolean) => {
      if (!isOpen && isDirty) {
        setShowUnsavedWarning(true)
      } else {
        onOpenChange(isOpen)
      }
    },
    [isDirty, onOpenChange]
  )

  const handleDiscard = useCallback(() => {
    setShowUnsavedWarning(false)
    onOpenChange(false)
    onCancel?.()
  }, [onOpenChange, onCancel])

  const handleCancel = useCallback(() => {
    if (isDirty) {
      setShowUnsavedWarning(true)
    } else {
      onOpenChange(false)
      onCancel?.()
    }
  }, [isDirty, onOpenChange, onCancel])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    await onSubmit?.()
  }

  return (
    <>
      <Dialog open={open} onOpenChange={handleOpenChange}>
        <DialogContent className={cn(sizeClasses[size], className)}>
          <form onSubmit={handleSubmit}>
            <DialogHeader>
              <DialogTitle>{title}</DialogTitle>
              {description && (
                <DialogDescription>{description}</DialogDescription>
              )}
            </DialogHeader>

            <div className="py-4">{children}</div>

            {showFooter && (
              <DialogFooter className="gap-2 sm:gap-0">
                <Button
                  type="button"
                  variant="outline"
                  onClick={handleCancel}
                  disabled={isSubmitting}
                >
                  {cancelLabel || t('actions.cancel')}
                </Button>
                <Button type="submit" disabled={isSubmitting}>
                  {isSubmitting
                    ? t('actions.loading')
                    : submitLabel || t('actions.save')}
                </Button>
              </DialogFooter>
            )}
          </form>
        </DialogContent>
      </Dialog>

      <UnsavedWarning
        hasUnsavedChanges={isDirty}
        open={showUnsavedWarning}
        onOpenChange={setShowUnsavedWarning}
        onDiscard={handleDiscard}
      />
    </>
  )
}
