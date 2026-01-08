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
import enSettings from './locales/en/settings.json'
import enMfa from './locales/en/mfa.json'
import enSessions from './locales/en/sessions.json'
import enIpRestrictions from './locales/en/ipRestrictions.json'
import enEmail from './locales/en/email.json'

// DE imports
import deCommon from './locales/de/common.json'
import deAuth from './locales/de/auth.json'
import deNavigation from './locales/de/navigation.json'
import deUsers from './locales/de/users.json'
import deErrors from './locales/de/errors.json'
import deValidation from './locales/de/validation.json'
import deAuditLogs from './locales/de/auditLogs.json'
import deSettings from './locales/de/settings.json'
import deMfa from './locales/de/mfa.json'
import deSessions from './locales/de/sessions.json'
import deIpRestrictions from './locales/de/ipRestrictions.json'
import deEmail from './locales/de/email.json'

export const defaultNS = 'common'
export const resources = {
  'en-US': {
    common: enCommon,
    auth: enAuth,
    navigation: enNavigation,
    users: enUsers,
    errors: enErrors,
    validation: enValidation,
    auditLogs: enAuditLogs,
    settings: enSettings,
    mfa: enMfa,
    sessions: enSessions,
    ipRestrictions: enIpRestrictions,
    email: enEmail,
  },
  'de-DE': {
    common: deCommon,
    auth: deAuth,
    navigation: deNavigation,
    users: deUsers,
    errors: deErrors,
    validation: deValidation,
    auditLogs: deAuditLogs,
    settings: deSettings,
    mfa: deMfa,
    sessions: deSessions,
    ipRestrictions: deIpRestrictions,
    email: deEmail,
  },
} as const

i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources,
    fallbackLng: 'en-US',
    defaultNS,
    ns: ['common', 'auth', 'navigation', 'users', 'errors', 'validation', 'auditLogs', 'settings', 'mfa', 'sessions', 'ipRestrictions', 'email'],
    interpolation: {
      escapeValue: false,
    },
    detection: {
      order: ['localStorage', 'navigator'],
      caches: ['localStorage'],
      lookupLocalStorage: 'exoauth-language',
    },
    // Development: Show missing keys clearly
    saveMissing: import.meta.env.DEV,
    missingKeyHandler: (_lngs, ns, key, fallbackValue) => {
      console.warn(`[i18n] Missing translation: ${ns}:${key}`, { fallbackValue })
    },
    parseMissingKeyHandler: (key) => `⚠️ ${key}`,
  })

export default i18n
