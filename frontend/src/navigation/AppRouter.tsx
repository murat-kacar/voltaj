import { Routes, Route } from 'react-router-dom'
import { findLabel, type NavGroup } from '../navigation/navModel'
import { ComingSoonView } from '../navigation/ComingSoonView'

import { DashboardView } from '../dashboard/DashboardView'
import { ServicesView } from '../services/ServicesView'
import { CustomersView } from '../customers/CustomersView'
import { CustomerDetailView } from '../customers/CustomerDetailView'
import { CatalogView } from '../CatalogView'
import { StockView } from '../StockView'
import { RemindersView } from '../reminders/RemindersView'
import { UsersView } from '../settings/UsersView'
import { TestDataGeneratorView } from '../generator/TestDataGeneratorView'
import { EndpointTriggerView } from '../generator/EndpointTriggerView'
import { SettingsView } from '../settings/SettingsView'
import { QuickSaleView } from '../sales/QuickSaleView'
import { GoodsReceiptView } from '../inventory/GoodsReceiptView'

export function AppRouter({ navGroups, comingSoon }: { navGroups: NavGroup[]; comingSoon: Set<string> }) {
  return (
    <Routes>
      <Route path="/" element={<DashboardView />} />
      <Route path="/services" element={<ServicesView />} />
      <Route path="/customers" element={<CustomersView />} />
      <Route path="/customers/:id" element={<CustomerDetailView />} />
      <Route path="/catalog" element={<CatalogView />} />
      <Route path="/stock" element={<StockView />} />
      <Route path="/reminders" element={<RemindersView />} />
      <Route path="/users" element={<UsersView />} />
      <Route path="/test-data" element={<TestDataGeneratorView />} />
      <Route path="/endpoint-trigger" element={<EndpointTriggerView />} />
      <Route path="/settings" element={<SettingsView />} />
      <Route path="/pos" element={<QuickSaleView />} />
      <Route path="/goods-receipt" element={<GoodsReceiptView />} />
      {[...comingSoon].map(id => (
        <Route key={id} path={'/' + id} element={<ComingSoonView title={findLabel(navGroups, id) ?? ''} />} />
      ))}
    </Routes>
  )
}
