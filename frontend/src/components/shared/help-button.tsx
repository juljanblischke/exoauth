import { useState } from 'react'
import { HelpCircle, X, ExternalLink, MessageCircle, Book } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover'
import { cn } from '@/lib/utils'

interface HelpButtonProps {
  className?: string
}

export function HelpButton({ className }: HelpButtonProps) {
  const { t } = useTranslation('common')
  const [open, setOpen] = useState(false)

  const helpLinks = [
    {
      icon: Book,
      label: t('help.documentation'),
      href: '/docs',
      external: false,
    },
    {
      icon: MessageCircle,
      label: t('help.support'),
      href: 'mailto:support@exoauth.io',
      external: true,
    },
    {
      icon: ExternalLink,
      label: t('help.feedback'),
      href: 'https://github.com/exoauth/feedback',
      external: true,
    },
  ]

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          size="icon"
          variant="outline"
          className={cn(
            'fixed bottom-4 right-4 z-40 h-12 w-12 rounded-full shadow-lg',
            className
          )}
        >
          {open ? (
            <X className="h-5 w-5" />
          ) : (
            <HelpCircle className="h-5 w-5" />
          )}
          <span className="sr-only">{t('help.toggle')}</span>
        </Button>
      </PopoverTrigger>
      <PopoverContent
        align="end"
        side="top"
        className="w-56 p-2"
        sideOffset={8}
      >
        <div className="space-y-1">
          <p className="px-2 py-1.5 text-sm font-medium text-muted-foreground">
            {t('help.title')}
          </p>
          {helpLinks.map((link) => (
            <a
              key={link.href}
              href={link.href}
              target={link.external ? '_blank' : undefined}
              rel={link.external ? 'noopener noreferrer' : undefined}
              className="flex items-center gap-2 rounded-md px-2 py-1.5 text-sm hover:bg-accent"
              onClick={() => setOpen(false)}
            >
              <link.icon className="h-4 w-4" />
              {link.label}
              {link.external && <ExternalLink className="ml-auto h-3 w-3" />}
            </a>
          ))}
        </div>
      </PopoverContent>
    </Popover>
  )
}
