import { useState, useCallback, useMemo } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useTranslation } from 'react-i18next'
import { Loader2, CalendarIcon, LocateFixed } from 'lucide-react'
import { z } from 'zod'
import { format } from 'date-fns'
import { de, enUS } from 'date-fns/locale'

import { Button } from '@/components/ui/button'
import { Calendar } from '@/components/ui/calendar'
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
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Switch } from '@/components/ui/switch'
import { cn } from '@/lib/utils'
import { IpRestrictionType } from '../types'
import type { CreateIpRestrictionRequest } from '../types'

// IP address or CIDR regex
const IP_CIDR_REGEX = /^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)(?:\/(?:3[0-2]|[12]?[0-9]))?$/

function createSchema(t: (key: string) => string) {
  return z.object({
    ipAddress: z
      .string()
      .min(1, t('validation:required'))
      .regex(IP_CIDR_REGEX, t('ipRestrictions:create.invalidIp')),
    type: z.union([z.literal(IpRestrictionType.Whitelist), z.literal(IpRestrictionType.Blacklist)]),
    reason: z
      .string()
      .min(1, t('validation:required'))
      .max(500, t('validation:maxLength')),
    isPermanent: z.boolean(),
    expiresAt: z.date().optional().nullable(),
  })
}

type FormData = z.infer<ReturnType<typeof createSchema>>

interface CreateIpRestrictionModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSubmit: (data: CreateIpRestrictionRequest) => void
  isLoading?: boolean
}

export function CreateIpRestrictionModal({
  open,
  onOpenChange,
  onSubmit,
  isLoading,
}: CreateIpRestrictionModalProps) {
  const { t, i18n } = useTranslation()
  const schema = useMemo(() => createSchema(t), [t])
  const locale = i18n.language === 'de' ? de : enUS
  const [calendarOpen, setCalendarOpen] = useState(false)
  const [isFetchingIp, setIsFetchingIp] = useState(false)

  const form = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: {
      ipAddress: '',
      type: IpRestrictionType.Blacklist,
      reason: '',
      isPermanent: true,
      expiresAt: null,
    },
  })

  const isPermanent = form.watch('isPermanent')
  const expiresAt = form.watch('expiresAt')

  const fetchMyIp = useCallback(async () => {
    setIsFetchingIp(true)
    try {
      const response = await fetch('https://api.ipify.org?format=json')
      const data = await response.json()
      if (data.ip) {
        form.setValue('ipAddress', data.ip)
      }
    } catch {
      // Silently fail - user can still type manually
    } finally {
      setIsFetchingIp(false)
    }
  }, [form])

  const handleSubmit = form.handleSubmit((data) => {
    onSubmit({
      ipAddress: data.ipAddress.trim(),
      type: data.type,
      reason: data.reason.trim(),
      expiresAt: data.isPermanent ? null : data.expiresAt?.toISOString() ?? null,
    })
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
            <DialogTitle>{t('ipRestrictions:create.title')}</DialogTitle>
            <DialogDescription>
              {t('ipRestrictions:create.description')}
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-4">
            {/* IP Address */}
            <div className="space-y-2">
              <Label htmlFor="ip-address">
                {t('ipRestrictions:create.ipAddress')}
              </Label>
              <div className="flex gap-2">
                <Input
                  id="ip-address"
                  {...form.register('ipAddress')}
                  placeholder={t('ipRestrictions:create.ipAddressPlaceholder')}
                  autoFocus
                  className="flex-1"
                />
                <Button
                  type="button"
                  variant="outline"
                  size="icon"
                  onClick={fetchMyIp}
                  disabled={isFetchingIp}
                  title={t('ipRestrictions:create.getMyIp')}
                >
                  {isFetchingIp ? (
                    <Loader2 className="h-4 w-4 animate-spin" />
                  ) : (
                    <LocateFixed className="h-4 w-4" />
                  )}
                </Button>
              </div>
              <p className="text-xs text-muted-foreground">
                {t('ipRestrictions:create.ipAddressDescription')}
              </p>
              {form.formState.errors.ipAddress && (
                <p className="text-sm text-destructive">
                  {form.formState.errors.ipAddress.message}
                </p>
              )}
            </div>

            {/* Type */}
            <div className="space-y-2">
              <Label htmlFor="type">{t('ipRestrictions:create.type')}</Label>
              <Select
                value={String(form.watch('type'))}
                onValueChange={(value) => form.setValue('type', Number(value) as IpRestrictionType)}
              >
                <SelectTrigger id="type">
                  <SelectValue placeholder={t('ipRestrictions:create.typePlaceholder')} />
                </SelectTrigger>
                <SelectContent position="popper" className="z-[100]">
                  <SelectItem value={String(IpRestrictionType.Whitelist)}>
                    {t('ipRestrictions:type.whitelist')}
                  </SelectItem>
                  <SelectItem value={String(IpRestrictionType.Blacklist)}>
                    {t('ipRestrictions:type.blacklist')}
                  </SelectItem>
                </SelectContent>
              </Select>
            </div>

            {/* Reason */}
            <div className="space-y-2">
              <Label htmlFor="reason">{t('ipRestrictions:create.reason')}</Label>
              <Input
                id="reason"
                {...form.register('reason')}
                placeholder={t('ipRestrictions:create.reasonPlaceholder')}
              />
              {form.formState.errors.reason && (
                <p className="text-sm text-destructive">
                  {form.formState.errors.reason.message}
                </p>
              )}
            </div>

            {/* Permanent Toggle */}
            <div className="flex items-center justify-between">
              <div className="space-y-0.5">
                <Label htmlFor="permanent">
                  {t('ipRestrictions:create.permanent')}
                </Label>
                <p className="text-xs text-muted-foreground">
                  {t('ipRestrictions:create.expiresAtDescription')}
                </p>
              </div>
              <Switch
                id="permanent"
                checked={isPermanent}
                onCheckedChange={(checked) => {
                  form.setValue('isPermanent', checked)
                  if (checked) {
                    form.setValue('expiresAt', null)
                  }
                }}
              />
            </div>

            {/* Expiration Date (only if not permanent) */}
            {!isPermanent && (
              <div className="space-y-2">
                <Label>{t('ipRestrictions:create.expiresAt')}</Label>
                <Popover open={calendarOpen} onOpenChange={setCalendarOpen}>
                  <PopoverTrigger asChild>
                    <Button
                      variant="outline"
                      className={cn(
                        'w-full justify-start text-left font-normal',
                        !expiresAt && 'text-muted-foreground'
                      )}
                    >
                      <CalendarIcon className="mr-2 h-4 w-4" />
                      {expiresAt ? format(expiresAt, 'PPP', { locale }) : t('common:selectDate')}
                    </Button>
                  </PopoverTrigger>
                  <PopoverContent className="w-auto p-0 z-[100]" align="start">
                    <Calendar
                      mode="single"
                      selected={expiresAt ?? undefined}
                      onSelect={(date) => {
                        form.setValue('expiresAt', date ?? null)
                        setCalendarOpen(false)
                      }}
                      disabled={(date) => date < new Date()}
                      locale={locale}
                      initialFocus
                    />
                  </PopoverContent>
                </Popover>
              </div>
            )}
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
              {t('ipRestrictions:create.submit')}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
