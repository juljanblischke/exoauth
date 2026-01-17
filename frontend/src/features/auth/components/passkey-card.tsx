import { useTranslation } from 'react-i18next'
import { Fingerprint, MoreVertical, Pencil, Trash2, Loader2 } from 'lucide-react'

import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from '@/components/ui/tooltip'
import { RelativeTime } from '@/components/shared'
import type { PasskeyDto } from '../types/passkey'

interface PasskeyCardProps {
  passkey: PasskeyDto
  onRename: (passkey: PasskeyDto) => void
  onDelete: (passkey: PasskeyDto) => void
  isDeleting?: boolean
}

export function PasskeyCard({
  passkey,
  onRename,
  onDelete,
  isDeleting,
}: PasskeyCardProps) {
  const { t } = useTranslation()

  return (
    <div className="flex items-start gap-4 p-4 rounded-lg border bg-card">
      <div className="flex-shrink-0 p-2 rounded-full bg-muted">
        <Fingerprint className="h-5 w-5 text-muted-foreground" />
      </div>

      <div className="flex-1 min-w-0">
        <Tooltip>
          <TooltipTrigger asChild>
            <h4 className="font-medium truncate cursor-default">{passkey.name}</h4>
          </TooltipTrigger>
          <TooltipContent>
            <p>{passkey.name}</p>
          </TooltipContent>
        </Tooltip>

        <div className="mt-1 text-sm text-muted-foreground space-y-0.5">
          <div className="flex items-center gap-1.5">
            <span>{t('auth:passkeys.card.created')}:</span>
            <RelativeTime date={passkey.createdAt} />
          </div>
          <div className="flex items-center gap-1.5">
            <span>{t('auth:passkeys.card.lastUsed')}:</span>
            {passkey.lastUsedAt ? (
              <RelativeTime date={passkey.lastUsedAt} />
            ) : (
              <span>{t('auth:passkeys.card.neverUsed')}</span>
            )}
          </div>
        </div>
      </div>

      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button variant="ghost" size="icon" disabled={isDeleting}>
            {isDeleting ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : (
              <MoreVertical className="h-4 w-4" />
            )}
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end">
          <DropdownMenuItem onClick={() => onRename(passkey)}>
            <Pencil className="h-4 w-4 mr-2" />
            {t('auth:passkeys.card.rename')}
          </DropdownMenuItem>
          <DropdownMenuItem
            onClick={() => onDelete(passkey)}
            className="text-destructive focus:text-destructive"
          >
            <Trash2 className="h-4 w-4 mr-2" />
            {t('auth:passkeys.card.delete')}
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
    </div>
  )
}
