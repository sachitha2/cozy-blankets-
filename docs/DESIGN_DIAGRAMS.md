# Cozy Comfort - Design Diagrams

This document contains all design diagrams for the Cozy Comfort Service-Oriented Architecture system.

## Table of Contents
1. [System Architecture Diagram](#system-architecture-diagram)
2. [Service Communication Flow](#service-communication-flow)
3. [Sequence Diagram - Order Processing](#sequence-diagram---order-processing)
4. [Class Diagram - ManufacturerService](#class-diagram---manufacturerservice)
5. [Class Diagram - DistributorService](#class-diagram---distributorservice)
6. [Class Diagram - SellerService](#class-diagram---sellerservice)
7. [Database ER Diagrams](#database-er-diagrams)
8. [Deployment Architecture](#deployment-architecture)

---

## System Architecture Diagram

```mermaid
graph TB
    subgraph "Client Layer"
        WebClient[Web Client Application<br/>Port 5006]
        ConsoleClient[Console Client Application]
    end

    subgraph "Service Layer"
        SellerService[SellerService<br/>Port 5003<br/>ASP.NET Core Web API]
        DistributorService[DistributorService<br/>Port 5002<br/>ASP.NET Core Web API]
        ManufacturerService[ManufacturerService<br/>Port 5001<br/>ASP.NET Core Web API]
    end

    subgraph "Data Layer"
        SellerDB[(SellerServiceDb<br/>SQLite)]
        DistributorDB[(DistributorServiceDb<br/>SQLite)]
        ManufacturerDB[(ManufacturerServiceDb<br/>SQLite)]
    end

    WebClient -->|HTTP REST API| SellerService
    ConsoleClient -->|HTTP REST API| SellerService
    SellerService -->|HTTP REST API| DistributorService
    DistributorService -->|HTTP REST API| ManufacturerService

    SellerService --> SellerDB
    DistributorService --> DistributorDB
    ManufacturerService --> ManufacturerDB

    style SellerService fill:#e1f5ff
    style DistributorService fill:#e1f5ff
    style ManufacturerService fill:#e1f5ff
    style SellerDB fill:#fff4e1
    style DistributorDB fill:#fff4e1
    style ManufacturerDB fill:#fff4e1
```

---

## Service Communication Flow

Per PDF: "Seller checks their own stock. If unavailable, they contact their assigned Distributor."

```mermaid
graph LR
    Customer[Customer] -->|1. Place Order| Seller[SellerService]
    Seller -->|2. Check Own Stock| Seller
    Seller -->|3. If Unavailable:<br/>Check Distributor| Distributor[DistributorService]
    Distributor -->|4. Check Inventory| Distributor
    Distributor -->|5. If Unavailable:<br/>Check Production| Manufacturer[ManufacturerService]
    Manufacturer -->|6. Production/Lead Time| Distributor
    Distributor -->|7. Availability Info| Seller
    Seller -->|8. Order Confirmation| Customer
```

---

## Sequence Diagram - Order Processing

PDF flow: Seller checks own stock first; if unavailable, contacts Distributor; Distributor checks stock, then Manufacturer if needed.

```mermaid
sequenceDiagram
    participant C as Customer
    participant S as SellerService
    participant D as DistributorService
    participant M as ManufacturerService

    C->>S: POST /api/customerorder
    S->>S: Check Seller's own stock first
    
    alt Seller has stock
        S->>S: Fulfill from own inventory
        S->>S: Create Customer Order
        S-->>C: Order Fulfilled (from seller stock)
    else Seller stock unavailable
        S->>D: GET /api/inventory (check availability)
        D-->>S: Inventory Available or Not
        
        alt Distributor has stock
            S->>D: POST /api/order
            D->>D: Update Inventory
            D-->>S: Order Fulfilled
            S->>S: Create Customer Order
            S-->>C: Order Confirmed
        else Distributor stock unavailable
            D->>M: GET /api/blankets/stock, POST /api/blankets/produce
            M-->>D: Production Capacity & Lead Time
            D-->>S: Availability with Lead Time
            S-->>C: Order Pending (with lead time)
        end
    end
```

---

## Class Diagram - ManufacturerService

```mermaid
classDiagram
    class BlanketsController {
        +GetBlankets()
        +GetStock(modelId)
        +ProcessProduction(request)
    }
    
    class IBlanketService {
        <<interface>>
        +GetAllBlanketsAsync()
        +GetStockByModelIdAsync()
        +ProcessProductionRequestAsync()
    }
    
    class BlanketService {
        -_blanketRepository
        -_stockRepository
        -_productionCapacityRepository
        +GetAllBlanketsAsync()
        +GetStockByModelIdAsync()
        +ProcessProductionRequestAsync()
    }
    
    class IBlanketRepository {
        <<interface>>
        +GetAllAsync()
        +GetByIdAsync()
        +AddAsync()
    }
    
    class BlanketRepository {
        -_context
        +GetAllAsync()
        +GetByIdAsync()
        +AddAsync()
    }
    
    class Blanket {
        +Id
        +ModelName
        +Material
        +UnitPrice
    }
    
    class Stock {
        +Id
        +BlanketId
        +Quantity
        +AvailableQuantity
    }
    
    BlanketsController --> IBlanketService
    IBlanketService <|.. BlanketService
    BlanketService --> IBlanketRepository
    IBlanketRepository <|.. BlanketRepository
    BlanketRepository --> Blanket
    BlanketRepository --> Stock
```

---

## Class Diagram - DistributorService

```mermaid
classDiagram
    class InventoryController {
        +GetInventory()
    }
    
    class OrderController {
        +ProcessOrder(request)
    }
    
    class IDistributorService {
        <<interface>>
        +GetAllInventoryAsync()
        +ProcessOrderAsync()
    }
    
    class DistributorService {
        -_inventoryRepository
        -_orderRepository
        -_manufacturerClient
        +GetAllInventoryAsync()
        +ProcessOrderAsync()
    }
    
    class IInventoryRepository {
        <<interface>>
        +GetAllAsync()
        +GetByBlanketIdAsync()
    }
    
    class InventoryRepository {
        -_context
        +GetAllAsync()
        +GetByBlanketIdAsync()
    }
    
    class IManufacturerServiceClient {
        <<interface>>
        +GetStockAsync()
        +CheckProductionCapacityAsync()
    }
    
    class Inventory {
        +Id
        +BlanketId
        +Quantity
        +AvailableQuantity
    }
    
    InventoryController --> IDistributorService
    OrderController --> IDistributorService
    IDistributorService <|.. DistributorService
    DistributorService --> IInventoryRepository
    DistributorService --> IManufacturerServiceClient
    IInventoryRepository <|.. InventoryRepository
    InventoryRepository --> Inventory
```

---

## Class Diagram - SellerService

```mermaid
classDiagram
    class CustomerOrderController {
        +ProcessCustomerOrder(request)
        +GetAllOrders()
        +GetOrderById(id)
        +CheckAvailability(modelId)
    }
    
    class ISellerService {
        <<interface>>
        +ProcessCustomerOrderAsync()
        +CheckAvailabilityAsync()
        +GetAllCustomerOrdersAsync()
    }
    
    class SellerService {
        -_orderRepository
        -_distributorClient
        +ProcessCustomerOrderAsync()
        +CheckAvailabilityAsync()
        +GetAllCustomerOrdersAsync()
    }
    
    class ICustomerOrderRepository {
        <<interface>>
        +GetAllAsync()
        +GetByIdAsync()
        +AddAsync()
    }
    
    class CustomerOrderRepository {
        -_context
        +GetAllAsync()
        +GetByIdAsync()
        +AddAsync()
    }
    
    class IDistributorServiceClient {
        <<interface>>
        +GetInventoryByBlanketIdAsync()
        +PlaceOrderAsync()
    }
    
    class CustomerOrder {
        +Id
        +CustomerName
        +Status
        +TotalAmount
        +OrderItems
    }
    
    CustomerOrderController --> ISellerService
    ISellerService <|.. SellerService
    SellerService --> ICustomerOrderRepository
    SellerService --> IDistributorServiceClient
    ICustomerOrderRepository <|.. CustomerOrderRepository
    CustomerOrderRepository --> CustomerOrder
```

---

## Database ER Diagrams

### ManufacturerService Database

```mermaid
erDiagram
    BLANKET ||--o{ STOCK : has
    BLANKET ||--o{ PRODUCTION_CAPACITY : has
    
    BLANKET {
        int Id PK
        string ModelName
        string Material
        string Description
        decimal UnitPrice
        datetime CreatedAt
    }
    
    STOCK {
        int Id PK
        int BlanketId FK
        int Quantity
        int ReservedQuantity
        int AvailableQuantity
        datetime LastUpdated
    }
    
    PRODUCTION_CAPACITY {
        int Id PK
        int BlanketId FK
        int DailyCapacity
        int CurrentProduction
        int LeadTimeDays
    }
```

### DistributorService Database

```mermaid
erDiagram
    INVENTORY ||--o{ ORDER : fulfills
    
    INVENTORY {
        int Id PK
        int BlanketId
        string ModelName
        int Quantity
        int ReservedQuantity
        int AvailableQuantity
        decimal UnitCost
        datetime LastUpdated
    }
    
    ORDER {
        int Id PK
        string SellerId
        int BlanketId
        int Quantity
        string Status
        datetime OrderDate
        datetime FulfilledDate
    }
```

### SellerService Database

```mermaid
erDiagram
    CUSTOMER_ORDER ||--o{ ORDER_ITEM : contains
    
    CUSTOMER_ORDER {
        int Id PK
        string CustomerName
        string CustomerEmail
        string CustomerPhone
        string ShippingAddress
        string Status
        decimal TotalAmount
        datetime OrderDate
        datetime FulfilledDate
    }
    
    ORDER_ITEM {
        int Id PK
        int CustomerOrderId FK
        int BlanketId
        string ModelName
        int Quantity
        decimal UnitPrice
        decimal SubTotal
        string Status
    }
```

---

## Deployment Architecture

```mermaid
graph TB
    subgraph "Load Balancer"
        LB[Load Balancer<br/>nginx/HAProxy]
    end
    
    subgraph "Web Tier"
        Web1[Web Client<br/>Instance 1]
        Web2[Web Client<br/>Instance 2]
    end
    
    subgraph "Service Tier"
        Seller1[SellerService<br/>Instance 1]
        Seller2[SellerService<br/>Instance 2]
        Distributor1[DistributorService<br/>Instance 1]
        Distributor2[DistributorService<br/>Instance 2]
        Manufacturer1[ManufacturerService<br/>Instance 1]
        Manufacturer2[ManufacturerService<br/>Instance 2]
    end
    
    subgraph "Data Tier"
        SellerDB[(SellerServiceDb<br/>SQLite/PostgreSQL)]
        DistributorDB[(DistributorServiceDb<br/>SQLite/PostgreSQL)]
        ManufacturerDB[(ManufacturerServiceDb<br/>SQLite/PostgreSQL)]
    end
    
    LB --> Web1
    LB --> Web2
    Web1 --> Seller1
    Web1 --> Seller2
    Web2 --> Seller1
    Web2 --> Seller2
    
    Seller1 --> Distributor1
    Seller1 --> Distributor2
    Seller2 --> Distributor1
    Seller2 --> Distributor2
    
    Distributor1 --> Manufacturer1
    Distributor1 --> Manufacturer2
    Distributor2 --> Manufacturer1
    Distributor2 --> Manufacturer2
    
    Seller1 --> SellerDB
    Seller2 --> SellerDB
    Distributor1 --> DistributorDB
    Distributor2 --> DistributorDB
    Manufacturer1 --> ManufacturerDB
    Manufacturer2 --> ManufacturerDB
```

---

## Component Diagram

```mermaid
graph TB
    subgraph "ManufacturerService Components"
        MC[Controllers]
        MS[Services]
        MR[Repositories]
        MM[Models]
        MD[Data]
    end
    
    subgraph "DistributorService Components"
        DC[Controllers]
        DS[Services]
        DR[Repositories]
        DM[Models]
        DD[Data]
    end
    
    subgraph "SellerService Components"
        SC[Controllers]
        SS[Services]
        SR[Repositories]
        SM[Models]
        SD[Data]
    end
    
    MC --> MS
    MS --> MR
    MR --> MD
    
    DC --> DS
    DS --> DR
    DR --> DD
    
    SC --> SS
    SS --> SR
    SR --> SD
    
    DS -.->|HTTP Client| MS
    SS -.->|HTTP Client| DS
```

---

## Notes

- All diagrams use standard UML/Mermaid notation
- Services communicate via RESTful HTTP APIs
- Each service maintains its own database (Database per Service pattern)
- Services are loosely coupled and can be deployed independently
- The architecture supports horizontal scaling of each service
