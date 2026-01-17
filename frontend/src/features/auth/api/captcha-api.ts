import apiClient, { extractData } from '@/lib/axios'
import type { ApiResponse } from '@/types/api'
import type { CaptchaConfig } from '../types/captcha'

export const captchaApi = {
  /**
   * Get CAPTCHA configuration
   * Returns provider type, site key, and enabled status
   */
  getConfig: async (): Promise<CaptchaConfig> => {
    const response = await apiClient.get<ApiResponse<CaptchaConfig>>(
      '/captcha/config'
    )
    return extractData(response)
  },
}
