import { useState } from 'react'

export type Language = 'en' | 'tr'

export const translations = {
  en: {
    // Navigation
    overview: 'Overview',
    customers: 'Customers',
    quotes: 'Quotes',
    workOrders: 'Work orders',
    inventory: 'Inventory',
    payments: 'Payments',
    auditLog: 'Audit log',
    admin: 'Admin',
    workspace: 'Workspace',
    system: 'System',
    singleCompany: 'Single company workspace',
    needAHand: 'Need a hand?',
    openGuide: 'Open the operations guide',
    clickToLogout: 'Click to logout',
    employee: 'Employee',

    // Top bar & Actions
    searchPlaceholder: 'Search work order, customer or quote (e.g. WO-1048)...',
    quickAction: 'Quick action',
    logout: 'Logout',
    signIn: 'Sign in',
    register: 'Register',
    resetPassword: 'Reset password',
    cancel: 'Cancel',
    save: 'Save',
    create: 'Create',
    close: 'Close',

    // Dashboard Overview
    dashboardTitle: 'Field Operations & Dispatch Command',
    dashboardSubtitle: 'Real-time overview of active work orders, quotes and technician statuses.',
    activeOrders: 'Active Work Orders',
    pendingQuotes: 'Pending Quotes',
    collectedToday: 'Collected Today',
    safetyCompliance: 'Safety Compliance',

    // Statuses
    inProgress: 'In progress',
    assigned: 'Assigned',
    open: 'Open',
    completed: 'Completed',
    onHold: 'On hold',
    cancelled: 'Cancelled',
    unassigned: 'Unassigned',
    invoiced: 'Invoiced',
    
    // Quote Statuses
    draft: 'Draft',
    issued: 'Issued',
    accepted: 'Accepted',
    rejected: 'Rejected',
    expired: 'Expired',

    // Language Toggle
    langEn: '🇺🇸 EN',
    langTr: '🇹🇷 TR'
  },
  tr: {
    // Navigation
    overview: 'Genel Bakış',
    customers: 'Müşteriler',
    quotes: 'Teklifler',
    workOrders: 'İş Emirleri',
    inventory: 'Envanter',
    payments: 'Ödemeler',
    auditLog: 'Denetim Kayıtları',
    admin: 'Yönetici',
    workspace: 'Çalışma Alanı',
    system: 'Sistem',
    singleCompany: 'Tekil Şirket Çalışma Alanı',
    needAHand: 'Yardım mı lazım?',
    openGuide: 'Operasyon rehberini aç',
    clickToLogout: 'Çıkış yapmak için tıklayın',
    employee: 'Çalışan',

    // Top bar & Actions
    searchPlaceholder: 'İş emri, müşteri veya teklif ara (örn. WO-1048)...',
    quickAction: 'Hızlı İşlem',
    logout: 'Çıkış Yap',
    signIn: 'Giriş Yap',
    register: 'Kayıt Ol',
    resetPassword: 'Şifre Sıfırla',
    cancel: 'Vazgeç',
    save: 'Kaydet',
    create: 'Oluştur',
    close: 'Kapat',

    // Dashboard Overview
    dashboardTitle: 'Saha Operasyonları ve Sevk Komuta',
    dashboardSubtitle: 'Aktif iş emirleri, teklifler ve teknisyen durumlarının gerçek zamanlı özeti.',
    activeOrders: 'Aktif İş Emirleri',
    pendingQuotes: 'Bekleyen Teklifler',
    collectedToday: 'Bugün Tahsil Edilen',
    safetyCompliance: 'İSG Uyum Oranı',

    // Statuses
    inProgress: 'Devam ediyor',
    assigned: 'Atandı',
    open: 'Açık',
    completed: 'Tamamlandı',
    onHold: 'Beklemede',
    cancelled: 'İptal edildi',
    unassigned: 'Atanmadı',
    invoiced: 'Faturalandı',

    // Quote Statuses
    draft: 'Taslak',
    issued: 'Gönderildi',
    accepted: 'Kabul Edildi',
    rejected: 'Reddedildi',
    expired: 'Süresi Doldu',

    // Language Toggle
    langEn: '🇺🇸 EN',
    langTr: '🇹🇷 TR'
  }
} as const

export function useI18n() {
  const [lang, setLangState] = useState<Language>(() => {
    return (localStorage.getItem('voltflow.lang') as Language) || 'en'
  })

  const setLang = (newLang: Language) => {
    localStorage.setItem('voltflow.lang', newLang)
    setLangState(newLang)
  }

  const t = translations[lang]

  return { lang, setLang, t }
}
