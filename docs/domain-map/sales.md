# Bounded Context: Sales (Hızlı Satış / POS)

## Ubiquitous Language
- **Quick Sale (Hızlı Satış)**: A single-step transaction where items/services are sold and paid for immediately, without the need for a complex service quote/proposal lifecycle.
- **Cart (Sepet)**: Temporary holder of items being sold.
- **Walk-in Customer (Cari Olmayan Müşteri)**: A default or anonymous customer profile used when a specific customer is not selected.
- **Receipt (Fiş/Makbuz)**: The document generated immediately upon payment.

## Actors
- **Cashier / User (Kasiyer/Kullanıcı)**: Initiates and completes the quick sale.
- **Customer (Müşteri)**: The buyer (can be anonymous or registered).

## Commands (Imperative Verb)
- `StartQuickSaleCommand`: Initializes a new sale cart.
- `AddItemToCartCommand`: Adds a product or quick service to the cart.
- `CompleteSaleCommand`: Finalizes the sale, registers the payment, and completes the transaction.
- `CancelSaleCommand`: Discards the ongoing sale.

## Domain Events (Past Tense)
- `QuickSaleStartedEvent`
- `QuickSaleCompletedEvent`: Triggers inventory deduction (if physical items are sold) and finance entry creation.
- `QuickSaleCancelledEvent`

## Policies
- **When** `QuickSaleCompletedEvent` **Then** `CreatePaymentRecordCommand` (in Finance context).
- **When** `QuickSaleCompletedEvent` **Then** `DeductInventoryCommand` (in Inventory context, if applicable).

## Read Models
- `QuickSaleSummary`: Used for listing past daily sales.
- `DailySalesTotal`: For dashboard reporting.

## External Dependencies
- **Finance Context**: To register the incoming payment.
- **Inventory Context**: To deduct sold items from stock.

## Hotspots
- **H1**: Hızlı satışta satılan ürünler stoktan otomatik düşecek mi? (Şu an stok modülü var mı, yoksa basitçe ürün adı/fiyat girilerek mi satılıyor?)
- **H2**: Ödeme tipleri (Nakit, Kredi Kartı, Havale) sepet tamamlanırken mi seçilecek, yoksa hızlı satış sadece tek bir varsayılan (Nakit) ödeme tipiyle mi çalışacak?
- **H3**: Anonim müşteri (Walk-in) yapısı mı kuracağız yoksa her satışta mutlaka bir cari/müşteri seçilmesi zorunlu mu olacak?
- **H4**: Fiş/Fatura yazdırma işlemi satış tamamlandığı an (otomatik) mi sorulacak?
