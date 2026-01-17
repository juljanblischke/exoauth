import { useMemo, useEffect } from 'react'
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
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Switch } from '@/components/ui/switch'
import { Textarea } from '@/components/ui/textarea'
import { Separator } from '@/components/ui/separator'
import { EmailProviderType } from '../types'
import type { EmailProviderDto, EmailProviderConfigDto, CreateEmailProviderRequest, UpdateEmailProviderRequest } from '../types'

function createSchema(t: (key: string) => string) {
  return z.object({
    name: z
      .string()
      .min(1, t('validation:required'))
      .max(100, t('validation:maxLength')),
    description: z
      .string()
      .max(500, t('validation:maxLength'))
      .optional(),
    providerType: z.number(),
    isEnabled: z.boolean(),
    // SMTP fields
    smtpHost: z.string().optional(),
    smtpPort: z.coerce.number().min(1).max(65535).default(587),
    smtpUsername: z.string().optional(),
    smtpPassword: z.string().optional(),
    smtpUseSsl: z.boolean().optional(),
    // API-based provider fields
    apiKey: z.string().optional(),
    apiSecret: z.string().optional(),
    domain: z.string().optional(),
    region: z.string().optional(),
    // Common sender fields
    fromEmail: z.string().email(t('validation:invalidEmail')).optional().or(z.literal('')),
    fromName: z.string().optional(),
  })
}

type FormData = z.infer<ReturnType<typeof createSchema>>

interface ProviderFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  provider?: EmailProviderDto | null
  onSubmit: (data: CreateEmailProviderRequest | UpdateEmailProviderRequest) => void
  isLoading?: boolean
}

export function ProviderFormDialog({
  open,
  onOpenChange,
  provider,
  onSubmit,
  isLoading,
}: ProviderFormDialogProps) {
  const { t } = useTranslation()
  const schema = useMemo(() => createSchema(t), [t])
  const isEdit = !!provider

  const form = useForm<FormData>({
    resolver: zodResolver(schema) as never,
    defaultValues: {
      name: '',
      description: '',
      providerType: EmailProviderType.Smtp,
      isEnabled: true,
      smtpHost: '',
      smtpPort: 587,
      smtpUsername: '',
      smtpPassword: '',
      smtpUseSsl: true,
      apiKey: '',
      apiSecret: '',
      domain: '',
      region: '',
      fromEmail: '',
      fromName: '',
    },
  })

  const providerType = form.watch('providerType')

  useEffect(() => {
    if (open && provider) {
      form.reset({
        name: provider.name,
        description: '',
        providerType: provider.type,
        isEnabled: provider.isEnabled,
        smtpHost: '',
        smtpPort: 587,
        smtpUsername: '',
        smtpPassword: '',
        smtpUseSsl: true,
        apiKey: '',
        apiSecret: '',
        domain: '',
        region: '',
        fromEmail: '',
        fromName: '',
      })
    } else if (open && !provider) {
      form.reset({
        name: '',
        description: '',
        providerType: EmailProviderType.Smtp,
        isEnabled: true,
        smtpHost: '',
        smtpPort: 587,
        smtpUsername: '',
        smtpPassword: '',
        smtpUseSsl: true,
        apiKey: '',
        apiSecret: '',
        domain: '',
        region: '',
        fromEmail: '',
        fromName: '',
      })
    }
  }, [open, provider, form])

  const handleSubmit = form.handleSubmit((data) => {
    const config: Record<string, unknown> = {}

    // Add provider-specific config
    if (data.providerType === EmailProviderType.Smtp) {
      if (data.smtpHost) config.host = data.smtpHost
      if (data.smtpPort) config.port = data.smtpPort
      if (data.smtpUsername) config.username = data.smtpUsername
      if (data.smtpPassword) config.password = data.smtpPassword
      config.useSsl = data.smtpUseSsl
    } else {
      if (data.apiKey) config.apiKey = data.apiKey
      if (data.apiSecret) config.apiSecret = data.apiSecret
      if (data.domain) config.domain = data.domain
      if (data.region) config.region = data.region
    }

    // Add common required fields
    config.fromEmail = data.fromEmail || ''
    config.fromName = data.fromName || ''

    onSubmit({
      name: data.name.trim(),
      type: data.providerType as EmailProviderType,
      priority: 0, // Will be set by backend
      isEnabled: data.isEnabled,
      configuration: config as unknown as EmailProviderConfigDto,
    })
  })

  const handleClose = () => {
    form.reset()
    onOpenChange(false)
  }

  const isSmtp = providerType === EmailProviderType.Smtp
  const needsDomain = providerType === EmailProviderType.Mailgun
  const needsRegion = providerType === EmailProviderType.AmazonSes

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent className="sm:max-w-lg max-h-[85vh] overflow-y-auto">
        <form onSubmit={handleSubmit}>
          <DialogHeader>
            <DialogTitle>
              {isEdit
                ? t('email:providers.edit.title')
                : t('email:providers.create.title')}
            </DialogTitle>
            <DialogDescription>
              {isEdit
                ? t('email:providers.edit.description')
                : t('email:providers.create.description')}
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-4">
            {/* Basic Info */}
            <div className="space-y-2">
              <Label htmlFor="name">{t('email:providers.form.name')}</Label>
              <Input
                id="name"
                {...form.register('name')}
                placeholder={t('email:providers.form.namePlaceholder')}
                autoFocus
              />
              {form.formState.errors.name && (
                <p className="text-sm text-destructive">
                  {form.formState.errors.name.message}
                </p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="description">{t('email:providers.form.description')}</Label>
              <Textarea
                id="description"
                {...form.register('description')}
                placeholder={t('email:providers.form.descriptionPlaceholder')}
                rows={2}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="providerType">{t('email:providers.form.type')}</Label>
              <Select
                value={String(providerType)}
                onValueChange={(value) => form.setValue('providerType', Number(value))}
                disabled={isEdit}
              >
                <SelectTrigger id="providerType">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={String(EmailProviderType.Smtp)}>
                    {t('email:providers.types.smtp')}
                  </SelectItem>
                  <SelectItem value={String(EmailProviderType.SendGrid)}>
                    {t('email:providers.types.sendGrid')}
                  </SelectItem>
                  <SelectItem value={String(EmailProviderType.Mailgun)}>
                    {t('email:providers.types.mailgun')}
                  </SelectItem>
                  <SelectItem value={String(EmailProviderType.AmazonSes)}>
                    {t('email:providers.types.amazonSes')}
                  </SelectItem>
                  <SelectItem value={String(EmailProviderType.Resend)}>
                    {t('email:providers.types.resend')}
                  </SelectItem>
                  <SelectItem value={String(EmailProviderType.Postmark)}>
                    {t('email:providers.types.postmark')}
                  </SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="flex items-center justify-between">
              <div className="space-y-0.5">
                <Label htmlFor="isEnabled">{t('email:providers.form.enabled')}</Label>
                <p className="text-xs text-muted-foreground">
                  {t('email:providers.form.enabledDescription')}
                </p>
              </div>
              <Switch
                id="isEnabled"
                checked={form.watch('isEnabled')}
                onCheckedChange={(checked) => form.setValue('isEnabled', checked)}
              />
            </div>

            <Separator />

            {/* Provider-specific configuration */}
            <div className="space-y-4">
              <h4 className="text-sm font-medium">
                {t('email:providers.form.configuration')}
              </h4>

              {isSmtp ? (
                <>
                  <div className="grid grid-cols-2 gap-4">
                    <div className="space-y-2">
                      <Label htmlFor="smtpHost">{t('email:providers.form.smtpHost')}</Label>
                      <Input
                        id="smtpHost"
                        {...form.register('smtpHost')}
                        placeholder="smtp.example.com"
                      />
                    </div>
                    <div className="space-y-2">
                      <Label htmlFor="smtpPort">{t('email:providers.form.smtpPort')}</Label>
                      <Input
                        id="smtpPort"
                        type="number"
                        {...form.register('smtpPort')}
                        placeholder="587"
                      />
                    </div>
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="smtpUsername">{t('email:providers.form.smtpUsername')}</Label>
                    <Input
                      id="smtpUsername"
                      {...form.register('smtpUsername')}
                      placeholder={t('email:providers.form.smtpUsernamePlaceholder')}
                    />
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="smtpPassword">{t('email:providers.form.smtpPassword')}</Label>
                    <Input
                      id="smtpPassword"
                      type="password"
                      {...form.register('smtpPassword')}
                      placeholder={isEdit ? '••••••••' : ''}
                    />
                    {isEdit && (
                      <p className="text-xs text-muted-foreground">
                        {t('email:providers.form.passwordHint')}
                      </p>
                    )}
                  </div>

                  <div className="flex items-center justify-between">
                    <Label htmlFor="smtpUseSsl">{t('email:providers.form.smtpUseSsl')}</Label>
                    <Switch
                      id="smtpUseSsl"
                      checked={form.watch('smtpUseSsl')}
                      onCheckedChange={(checked) => form.setValue('smtpUseSsl', checked)}
                    />
                  </div>
                </>
              ) : (
                <>
                  <div className="space-y-2">
                    <Label htmlFor="apiKey">{t('email:providers.form.apiKey')}</Label>
                    <Input
                      id="apiKey"
                      type="password"
                      {...form.register('apiKey')}
                      placeholder={isEdit ? '••••••••' : ''}
                    />
                    {isEdit && (
                      <p className="text-xs text-muted-foreground">
                        {t('email:providers.form.apiKeyHint')}
                      </p>
                    )}
                  </div>

                  {needsDomain && (
                    <div className="space-y-2">
                      <Label htmlFor="domain">{t('email:providers.form.domain')}</Label>
                      <Input
                        id="domain"
                        {...form.register('domain')}
                        placeholder="mg.example.com"
                      />
                    </div>
                  )}

                  {needsRegion && (
                    <div className="space-y-2">
                      <Label htmlFor="region">{t('email:providers.form.region')}</Label>
                      <Input
                        id="region"
                        {...form.register('region')}
                        placeholder="us-east-1"
                      />
                    </div>
                  )}
                </>
              )}
            </div>

            <Separator />

            {/* Sender settings */}
            <div className="space-y-4">
              <h4 className="text-sm font-medium">
                {t('email:providers.form.senderSettings')}
              </h4>

              <div className="space-y-2">
                <Label htmlFor="fromEmail">{t('email:providers.form.fromEmail')}</Label>
                <Input
                  id="fromEmail"
                  type="email"
                  {...form.register('fromEmail')}
                  placeholder="noreply@example.com"
                />
                {form.formState.errors.fromEmail && (
                  <p className="text-sm text-destructive">
                    {form.formState.errors.fromEmail.message}
                  </p>
                )}
              </div>

              <div className="space-y-2">
                <Label htmlFor="fromName">{t('email:providers.form.fromName')}</Label>
                <Input
                  id="fromName"
                  {...form.register('fromName')}
                  placeholder="My Application"
                />
              </div>
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
              {isEdit ? t('common:actions.save') : t('common:actions.create')}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
