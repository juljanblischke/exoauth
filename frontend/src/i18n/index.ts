import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import LanguageDetector from 'i18next-browser-languagedetector'

// EN imports
import enCommon from './locales/en/common.json'
import enAuth from './locales/en/auth.json'
import enNavigation from './locales/en/navigation.json'
import enUsers from './locales/en/users.json'
import enErrors from './locales/en/errors.json'
import enValidation from './locales/en/validation.json'
import enAuditLogs from './locales/en/auditLogs.json'

// DE imports
import deCommon from './locales/de/common.json'
import deAuth from './locales/de/auth.json'
import deNavigation from './locales/de/navigation.json'
import deUsers from './locales/de/users.json'
import deErrors from './locales/de/errors.json'
import deValidation from './locales/de/validation.json'
import deAuditLogs from './locales/de/auditLogs.json'

export const defaultNS = 'common'
export const resources = {
  en: {
    common: enCommon,
    auth: enAuth,
    navigation: enNavigation,
    users: enUsers,
    errors: enErrors,
    validation: enValidation,
    auditLogs: enAuditLogs,
  },
  de: {
    common: deCommon,
    auth: deAuth,
    navigation: deNavigation,
    users: deUsers,
    errors: deErrors,
    validation: deValidation,
    auditLogs: deAuditLogs,
  },
} as const

i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources,
    fallbackLng: 'en',
    defaultNS,
    ns: ['common', 'auth', 'navigation', 'users', 'errors', 'validation', 'auditLogs'],
    interpolation: {
      escapeValue: false,
    },
    detection: {
      order: ['localStorage', 'navigator'],
      caches: ['localStorage'],
      lookupLocalStorage: 'exoauth-language',
    },
  })

export default i18n
