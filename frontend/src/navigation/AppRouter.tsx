import { Routes, Route, useNavigate } from 'react-router-dom'
import { findLabel, type NavGroup } from '../navigation/navModel'
import { ComingSoonView } from '../navigation/ComingSoonView'

import { DashboardView } from '../dashboard/DashboardView'
import { ScheduleView } from '../ScheduleView'
import { CustomersView } from '../customers/CustomersView'
import { CustomerDetailView } from '../customers/CustomerDetailView'
import { QuotesView } from '../quotes/QuotesView'
import { QuoteDetailPage } from '../quotes/QuoteDetailPage'
import { WorkOrdersView } from '../OperationsViews'
import { WorkOrderDetailPage } from '../WorkOrderDetailPage'
import { InvoicesView } from '../payments/InvoicesView'
import { InvoiceDetailView } from '../payments/InvoiceDetailView'
import { PaymentsView } from '../payments/PaymentsView'
import { QuickSaleView } from '../QuickSaleView'
import { CatalogView } from '../CatalogView'
import { StockView } from '../StockView'
import { ProjectsView } from '../projects/ProjectsView'
import { RemindersView } from '../reminders/RemindersView'
import { ProductIntakeView } from '../ProductIntakeView'
import { UsersView } from '../settings/UsersView'
import { TestDataGeneratorView } from '../generator/TestDataGeneratorView'
import { EndpointTriggerView } from '../generator/EndpointTriggerView'
import { SettingsView } from '../settings/SettingsView'

export function AppRouter({ navGroups, comingSoon }: { navGroups: NavGroup[]; comingSoon: Set<string> }) {
  const navigate = useNavigate()
  const navTo = (id: string) => navigate(id === 'dashboard' ? '/' : '/' + id)

  return (
    <Routes>
      <Route path="/" element={<DashboardView />} />
      <Route path="/schedule" element={<ScheduleView />} />
      <Route path="/customers" element={<CustomersView />} />
      <Route path="/customers/:id" element={<CustomerDetailView />} />
      <Route path="/quotes" element={<QuotesView />} />
      <Route path="/quotes/:id" element={<QuoteDetailPage />} />
      <Route path="/work-orders" element={<WorkOrdersView />} />
      <Route path="/work-orders/:id" element={<WorkOrderDetailPage />} />
      <Route path="/invoices" element={<InvoicesView />} />
      <Route path="/invoices/:id" element={<InvoiceDetailView />} />
      <Route path="/payments" element={<PaymentsView />} />
      <Route path="/quick-sale" element={<QuickSaleView section="sell" onNavigate={navTo} />} />
      <Route path="/sale-history" element={<QuickSaleView section="history" onNavigate={navTo} />} />
      <Route path="/cash-shift" element={<QuickSaleView section="shift" onNavigate={navTo} />} />
      <Route path="/catalog" element={<CatalogView />} />
      <Route path="/stock" element={<StockView />} />
      <Route path="/projects" element={<ProjectsView />} />
      <Route path="/reminders" element={<RemindersView />} />
      <Route path="/product-intake" element={<ProductIntakeView />} />
      <Route path="/users" element={<UsersView />} />
      <Route path="/test-data" element={<TestDataGeneratorView />} />
      <Route path="/endpoint-trigger" element={<EndpointTriggerView />} />
      <Route path="/settings" element={<SettingsView />} />
      {[...comingSoon].map(id => (
        <Route key={id} path={'/' + id} element={<ComingSoonView title={findLabel(navGroups, id) ?? ''} />} />
      ))}
    </Routes>
  )
}
