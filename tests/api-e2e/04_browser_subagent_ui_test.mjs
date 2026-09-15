// tests/api-e2e/04_browser_subagent_ui_test.mjs
// Browser Subagent UI Testing Akışının Kaydedilmiş Script Versiyonu

export const e2eFlow = {
  name: "Voltflow Full Manual Checklist E2E UI Test",
  steps: [
    {
      step: 1,
      title: "Giriş ve Oturum Testi",
      action: "navigate",
      url: "http://localhost:5173/login",
      credentials: { email: "admin@voltflow.com", password: "Admin123!" },
      verify: "Dashboard yüklendi, sol alttan çıkış yapıldı ve oturum iptal edildi."
    },
    {
      step: 2,
      title: "OTP (000000) ile Yeni Kullanıcı Kaydı",
      action: "register",
      tab: "Create account",
      payload: {
        fullName: "Mustafa Demir",
        email: "mustafa.test@voltflow.com",
        password: "Password123!",
        otp: "000000"
      },
      verify: "Kullanıcı anında onaylandı ve oturum otomatik açıldı."
    },
    {
      step: 3,
      title: "Müşteri ve Teklif Ekleme (Modal UX)",
      action: "modal_create",
      customer: {
        fullName: "Atlas Enerji Sanayi A.Ş.",
        email: "iletisim@atlasenerji.com",
        phone: "+90 532 888 77 66"
      },
      quote: {
        customer: "Atlas Enerji Sanayi A.Ş.",
        title: "Trafo Bakımı ve Danışmanlık Hizmeti"
      },
      verify: "Modal scrim/autofocus doğrulandı, müşteri ve Draft teklif oluşturuldu."
    },
    {
      step: 4,
      title: "İş Emirleri & Side Drawer UX",
      action: "work_order_and_drawer",
      workOrder: {
        customer: "Atlas Enerji Sanayi A.Ş.",
        title: "Saha Pano Değişimi ve Testi"
      },
      drawerAction: "Mark Completed",
      verify: "Sayfa yenilenmeden Side Drawer açıldı ve iş emri tamamlandı statüsüne alındı."
    }
  ]
};

console.log("Voltflow E2E UI Test Planı başarıyla kaydedildi.");
