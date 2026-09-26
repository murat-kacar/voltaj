import { Routes, Route } from 'react-router-dom'
import { findLabel, type NavGroup } from '../navigation/navModel'
import { ComingSoonView } from '../navigation/ComingSoonView'

import { AgendaView } from '../features/agenda/AgendaView'
import { ServicesView } from '../features/services/ServicesView'
import { CustomersView } from '../features/customers/CustomersView'
import { CustomerDetailView } from '../features/customers/CustomerDetailView'
import { CatalogView } from '../features/catalog/CatalogView'
import { StockView } from '../features/inventory/StockView'
import { RemindersView } from '../features/agenda/reminders/RemindersView'
import { UsersView } from '../features/settings/UsersView'
import { TestDataGeneratorView } from '../features/generator/TestDataGeneratorView'
import { EndpointTriggerView } from '../features/generator/EndpointTriggerView'
import { SettingsView } from '../features/settings/SettingsView'
import { QuickSaleView } from '../features/pos/QuickSaleView'
import { GoodsReceiptView } from '../features/inventory/GoodsReceiptView'

export function AppRouter({ navGroups, comingSoon }: { navGroups: NavGroup[]; comingSoon: Set<string> }) {
  return (
    <Routes>
      <Route path="/" element={<AgendaView />} />
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
