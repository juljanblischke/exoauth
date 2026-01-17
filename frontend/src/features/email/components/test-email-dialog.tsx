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
import type { EmailProviderDto } from '../types'

function createSchema(t: (key: string) => string) {
  return z.object({
    toEmail: z
      .string()
      .min(1, t('validation:required'))
      .email(t('validation:invalidEmail')),
  })
}

type FormData = z.infer<ReturnType<typeof createSchema>>

interface TestEmailDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  provider: EmailProviderDto | null
  onSubmit: (providerId: string, toEmail: string) => void
  isLoading?: boolean
}

export function TestEmailDialog({
  open,
  onOpenChange,
  provider,
  onSubmit,
  isLoading,
}: TestEmailDialogProps) {
  const { t } = useTranslation()
  const schema = useMemo(() => createSchema(t), [t])

  const form = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: {
      toEmail: '',
    },
  })

  const handleSubmit = form.handleSubmit((data) => {
    if (provider) {
      onSubmit(provider.id, data.toEmail.trim())
    }
  })

  const handleClose = () => {
    form.reset()
    onOpenChange(false)
  }

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent className="sm:max-w-md">
        <form onSubmit={handleSubmit}>
          <DialogHeader>
            <DialogTitle>{t('email:providers.test.title')}</DialogTitle>
            <DialogDescription>
              {t('email:providers.test.description', { name: provider?.name ?? '' })}
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="toEmail">{t('email:providers.test.toEmail')}</Label>
              <Input
                id="toEmail"
                type="email"
                {...form.register('toEmail')}
                placeholder={t('email:providers.test.toEmailPlaceholder')}
                autoFocus
              />
              {form.formState.errors.toEmail && (
                <p className="text-sm text-destructive">
                  {form.formState.errors.toEmail.message}
                </p>
              )}
            </div>
          </div>

          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={handleClose}
              disabled={isLoading}
            >
              {t('common:actions.cancel')}
            </Button>
            <Button type="submit" disabled={isLoading}>
              {isLoading && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}
              {t('email:providers.test.send')}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
