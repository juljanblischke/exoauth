import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Download, Copy, Check } from 'lucide-react'
import { toast } from 'sonner'

import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

interface BackupCodesDisplayProps {
  codes: string[]
  className?: string
}

export function BackupCodesDisplay({ codes, className }: BackupCodesDisplayProps) {
  const { t } = useTranslation()
  const [copied, setCopied] = useState(false)
  const [downloaded, setDownloaded] = useState(false)

  const handleCopy = async () => {
    const codesText = codes.join('\n')
    await navigator.clipboard.writeText(codesText)
    setCopied(true)
    toast.success(t('mfa:confirm.copied'))
    setTimeout(() => setCopied(false), 2000)
  }

  const handleDownload = () => {
    const codesText = [
      'ExoAuth Backup Codes',
      '====================',
      '',
      'Keep these codes in a safe place.',
      'Each code can only be used once.',
      '',
      ...codes,
      '',
      `Generated: ${new Date().toISOString()}`,
    ].join('\n')

    const blob = new Blob([codesText], { type: 'text/plain' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = 'exoauth-backup-codes.txt'
    document.body.appendChild(a)
    a.click()
    document.body.removeChild(a)
    URL.revokeObjectURL(url)

    setDownloaded(true)
    toast.success(t('mfa:confirm.downloaded'))
  }

  return (
    <div className={cn('space-y-4', className)}>
      <div className="grid grid-cols-2 gap-2 p-4 bg-muted rounded-lg font-mono text-sm">
        {codes.map((code, index) => (
          <div
            key={index}
            className="px-3 py-2 bg-background rounded border text-center"
          >
            {code}
          </div>
        ))}
      </div>

      <div className="flex gap-3">
        <Button
          variant="outline"
          className="flex-1"
          onClick={handleDownload}
        >
          {downloaded ? (
            <Check className="h-4 w-4 mr-2" />
          ) : (
            <Download className="h-4 w-4 mr-2" />
          )}
          {t('mfa:confirm.downloadButton')}
        </Button>
        <Button
          variant="outline"
          className="flex-1"
          onClick={handleCopy}
        >
          {copied ? (
            <Check className="h-4 w-4 mr-2" />
          ) : (
            <Copy className="h-4 w-4 mr-2" />
          )}
          {t('mfa:confirm.copyButton')}
        </Button>
      </div>
    </div>
  )
}
