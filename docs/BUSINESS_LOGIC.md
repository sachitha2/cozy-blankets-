# Business Logic vs PDF Requirements

This document maps the implementation to the **Current Process** described in the assessment PDF.

## PDF: Current Process (to automate)

1. **A Seller receives an order from a customer for a specific blanket.**
2. **The Seller checks their own stock.** If unavailable, they contact their assigned Distributor (via phone or email) to check Distributor stock.
3. **The Distributor checks their stock.** If unavailable, they contact the Manufacturer (again, via phone or email) to check production capacity and lead times.
4. **The Manufacturer provides information.**
5. **This information flows back through the Distributor to the Seller.**
6. **The Seller updates the customer.** If the blanket is available, the order is placed, and the process reverses for fulfillment.

---

## Implementation Mapping

### 1. Seller receives order
- **API**: `POST /api/customerorder`
- **Implementation**: `SellerService.ProcessCustomerOrderAsync()` receives customer and line items.

### 2. Seller checks their own stock first
- **Requirement**: "The Seller checks their own stock. If unavailable, they contact their assigned Distributor."
- **Implementation**:
  - **Seller’s own stock**: `SellerInventory` entity and `ISellerInventoryRepository` (seller-held inventory).
  - For each order line, the seller first checks `_sellerInventoryRepository.GetByBlanketIdAsync(blanketId)`.
  - If `AvailableQuantity >= requested quantity` → order line is **fulfilled from seller stock** (inventory decremented), no call to Distributor for that line.
  - If seller has no stock or not enough → proceed to step 3 (contact Distributor).

### 3. Distributor checks their stock
- **Requirement**: "The Distributor checks their stock. If unavailable, they contact the Manufacturer."
- **Implementation**:
  - Seller calls Distributor: `GET /api/inventory` (availability) and `POST /api/order` (place order).
  - `DistributorService.ProcessOrderAsync()` first checks `_inventoryRepository.GetByBlanketIdAsync(blanketId)`.
  - If distributor has enough → order fulfilled from distributor stock and inventory updated.
  - If not enough or no stock → step 4 (contact Manufacturer).

### 4. Manufacturer provides information
- **Requirement**: "Check production capacity and lead times"; "The Manufacturer provides information."
- **Implementation**:
  - Distributor calls Manufacturer: `GET /api/blankets/stock/{modelId}` and `POST /api/blankets/produce` (production request).
  - `ManufacturerService` returns: `CanProduce`, `LeadTimeDays`, `AvailableStock`, `Message`.
  - Manufacturer does **not** change stock in this flow; it only reports capacity and lead time.

### 5. Information flows back
- **Requirement**: "This information flows back through the Distributor to the Seller."
- **Implementation**:
  - Distributor returns to Seller: `OrderResponseDto` with `Status` (Fulfilled / PendingManufacturer / Cancelled), `Message`, `EstimatedDeliveryDays` when applicable.
  - Seller uses this to set each line and overall order status and to build the response for the client.

### 6. Seller updates customer; order placed and fulfillment
- **Requirement**: "The Seller updates the customer. If the blanket is available, the order is placed, and the process reverses for fulfillment."
- **Implementation**:
  - Seller persists `CustomerOrder` and returns `CustomerOrderResponseDto` with per-item status (Fulfilled / Processing / Unavailable) and overall order status.
  - **Fulfillment from seller stock**: quantity deducted from `SellerInventory` when fulfilling from own stock.
  - **Fulfillment from distributor**: Distributor already deducted inventory in `ProcessOrderAsync` when status is Fulfilled.
  - **Pending manufacturer**: order is saved with status Processing and optional `EstimatedDeliveryDays`; no automatic reverse fulfillment in this version (could be extended later).

---

## PendingManufacturer: current behavior

When distributor stock is insufficient, the Distributor calls the Manufacturer for **production capacity and lead time only**. The system does **not**:

- Create a committed production order at the Manufacturer
- Trigger reverse fulfillment (Manufacturer → Distributor → Seller) when production completes

The customer receives an order status of **Processing** with an estimated delivery (lead time in days). Actual backorder fulfillment would require a separate flow (e.g. production order commit, notification when stock is ready, then fulfillment).

**Future: backorder fulfillment** — A later enhancement could add: (1) a Production Order API at the Manufacturer that commits/reserves production, and (2) a way for the Distributor to be notified when stock is ready (callback or polling) so pending orders can be fulfilled automatically.

---

## Availability check (GET /api/availability/{modelId})

- **Seller**: First checks **seller’s own stock** via `_sellerInventoryRepository.GetByBlanketIdAsync(modelId)`. If available, returns that quantity and a message like "available in seller stock".
- **If not in seller stock**: Seller then calls Distributor (inventory/availability); Distributor does not call Manufacturer for a simple availability check—only when **placing an order** does Distributor call Manufacturer for production/lead time.

---

## Data added for PDF alignment

- **SellerInventory**: Represents the seller’s own stock (per blanket). Seeded for BlanketId 1 and 2 so that “check own stock first” and “fulfill from seller stock” are exercised.
- **Order processing**: For each line, logic order is: (1) Seller stock → (2) Distributor stock → (3) Manufacturer production/lead time, matching the PDF flow.

---

## Summary

| PDF step | Implementation |
|----------|----------------|
| Seller receives order | `POST /api/customerorder` |
| Seller checks own stock | `SellerInventory` + check first in `ProcessCustomerOrderAsync` and `CheckAvailabilityAsync` |
| If unavailable, contact Distributor | Call DistributorService (inventory + order) |
| Distributor checks stock | `DistributorService.ProcessOrderAsync` checks inventory first |
| If unavailable, contact Manufacturer | `ManufacturerServiceClient.CheckProductionAsync()` → `/api/blankets/produce` |
| Manufacturer provides information | Stock + production capacity + lead time returned |
| Info flows back | DTOs from Manufacturer → Distributor → Seller |
| Seller updates customer; order placed | `CustomerOrderResponseDto` with status and messages |
| Fulfillment | Seller stock decremented when fulfilled from seller; distributor stock decremented when fulfilled from distributor |
