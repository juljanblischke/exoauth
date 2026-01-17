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
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import type { PasskeyDto } from '../types/passkey'

function createRenameSchema(t: (key: string) => string) {
  return z.object({
    name: z
      .string()
      .min(1, t('validation:required'))
      .max(100, t('validation:maxLength')),
  })
}

type RenameFormData = z.infer<ReturnType<typeof createRenameSchema>>

interface RenamePasskeyModalProps {
  passkey: PasskeyDto | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onConfirm: (name: string) => void
  isLoading?: boolean
}

export function RenamePasskeyModal({
  passkey,
  open,
  onOpenChange,
  onConfirm,
  isLoading,
}: RenamePasskeyModalProps) {
  const { t } = useTranslation()
  const renameSchema = useMemo(() => createRenameSchema(t), [t])

  const form = useForm<RenameFormData>({
    resolver: zodResolver(renameSchema),
    values: {
      name: passkey?.name || '',
    },
  })

  const handleSubmit = form.handleSubmit((data) => {
    onConfirm(data.name.trim())
  })

  if (!passkey) return null

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <form onSubmit={handleSubmit}>
          <DialogHeader>
            <DialogTitle>{t('auth:passkeys.rename.title')}</DialogTitle>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="passkey-name">
                {t('auth:passkeys.register.nameLabel')}
              </Label>
              <Input
                id="passkey-name"
                {...form.register('name')}
                placeholder={t('auth:passkeys.register.namePlaceholder')}
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
