import { useState, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetFooter,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet'
import { Button } from '@/components/ui/button'
import { UnsavedWarning } from '../feedback/unsaved-warning'
import { cn } from '@/lib/utils'

interface FormSheetProps {
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
  side?: 'top' | 'right' | 'bottom' | 'left'
  size?: 'sm' | 'md' | 'lg' | 'xl'
  showFooter?: boolean
  className?: string
}

const sizeClasses = {
  sm: 'sm:max-w-sm',
  md: 'sm:max-w-md',
  lg: 'sm:max-w-lg',
  xl: 'sm:max-w-xl',
}

export function FormSheet({
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
  side = 'right',
  size = 'md',
  showFooter = true,
  className,
}: FormSheetProps) {
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
      <Sheet open={open} onOpenChange={handleOpenChange}>
        <SheetContent
          side={side}
          className={cn(sizeClasses[size], 'flex flex-col', className)}
        >
          <form onSubmit={handleSubmit} className="flex flex-1 flex-col">
            <SheetHeader>
              <SheetTitle>{title}</SheetTitle>
              {description && (
                <SheetDescription>{description}</SheetDescription>
              )}
            </SheetHeader>

            <div className="flex-1 overflow-y-auto py-4">{children}</div>

            {showFooter && (
              <SheetFooter className="flex-shrink-0 gap-2 sm:gap-0">
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
              </SheetFooter>
            )}
          </form>
        </SheetContent>
      </Sheet>

      <UnsavedWarning
        hasUnsavedChanges={isDirty}
        open={showUnsavedWarning}
        onOpenChange={setShowUnsavedWarning}
        onDiscard={handleDiscard}
      />
    </>
  )
}
