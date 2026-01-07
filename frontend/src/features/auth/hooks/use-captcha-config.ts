import { useQuery } from '@tanstack/react-query'
import { captchaApi } from '../api/captcha-api'

export const CAPTCHA_CONFIG_KEY = ['captcha', 'config'] as const

export function useCaptchaConfig() {
  return useQuery({
    queryKey: CAPTCHA_CONFIG_KEY,
    queryFn: captchaApi.getConfig,
    staleTime: 5 * 60 * 1000, // 5 minutes - config rarely changes
    gcTime: 30 * 60 * 1000, // 30 minutes
    retry: 1, // Graceful degradation - don't retry too much
  })
}
