// CAPTCHA Provider types from backend (Task 021)
// Backend returns lowercase, but we accept any case for flexibility
export type CaptchaProvider = string

export interface CaptchaConfig {
  provider: CaptchaProvider
  siteKey: string
  enabled: boolean
}

export interface CaptchaWidgetProps {
  onVerify: (token: string) => void
  onError?: (error: string) => void
  onExpire?: () => void
  onLoad?: () => void
  action?: string // For reCAPTCHA v3 action tracking
  className?: string
}
