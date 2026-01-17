import { useMemo } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useTranslation } from 'react-i18next'
import { Loader2 } from 'lucide-react'
import { z } from 'zod'

import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import type { DeviceDto } from '../types/device'

function createRenameSchema(t: (key: string) => string) {
  return z.object({
    name: z
      .string()
      .min(1, t('validation:required'))
      .max(100, t('validation:maxLength')),
  })
}

type RenameFormData = z.infer<ReturnType<typeof createRenameSchema>>

interface RenameDeviceModalProps {
  device: DeviceDto | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onConfirm: (name: string) => void
  isLoading?: boolean
}

export function RenameDeviceModal({
  device,
  open,
  onOpenChange,
  onConfirm,
  isLoading,
}: RenameDeviceModalProps) {
  const { t } = useTranslation()
  const renameSchema = useMemo(() => createRenameSchema(t), [t])

  const form = useForm<RenameFormData>({
    resolver: zodResolver(renameSchema),
    values: {
      name: device?.name || '',
    },
  })

  const handleSubmit = form.handleSubmit((data) => {
    onConfirm(data.name.trim())
  })

  if (!device) return null

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <form onSubmit={handleSubmit}>
          <DialogHeader>
            <DialogTitle>{t('auth:devices.rename.title')}</DialogTitle>
            <DialogDescription>
              {t('auth:devices.rename.description')}
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="device-name">
                {t('auth:devices.rename.label')}
              </Label>
              <Input
                id="device-name"
                {...form.register('name')}
                placeholder={t('auth:devices.rename.placeholder')}
                autoFocus
              />
              {form.formState.errors.name && (
                <p className="text-sm text-destructive">
                  {form.formState.errors.name.message}
                </p>
              )}
            </div>
          </div>
          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={() => onOpenChange(false)}
            >
              {t('common:actions.cancel')}
            </Button>
            <Button type="submit" disabled={!form.formState.isValid || isLoading}>
              {isLoading && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}
              {t('common:actions.save')}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
