# DateTimePattern Domain Type

## Overview

`DateTimePattern` is a **single-case discriminated union** that wraps `System.DateTime` values to enforce type safety, format consistency, and centralized temporal logic throughout the Binnaculum investment tracking system.

```fsharp
type DateTimePattern =
    private | DateTimePattern of DateTime
```

**Location**: `src/Core/Keys/Pattern.fs`

## Why DateTimePattern?

### 1. Type Safety & Domain Consistency

By wrapping `DateTime`, `DateTimePattern` creates a distinct type that cannot be accidentally mixed with plain `DateTime` values. This enforces intentional usage throughout the codebase:

- **Cannot accidentally use**: `DateTime.Now` directly in domain models
- **Must use**: `DateTimePattern.FromDateTime(DateTime.Now)` explicitly
- **Prevents bugs**: Type system catches mixing of temporal types at compile-time

### 2. Standardized Format Control

`DateTimePattern` enforces a specific **ISO 8601 format** (`yyyy-MM-ddTHH:mm:ss`) for serialization, parsing, and database storage:

```fsharp
// Always serializes to this exact format
dateTimePattern.ToString()  // "2024-10-21T14:30:45"

// Strict parsing - fails if format doesn't match
DateTimePattern.Parse("2024-10-21T14:30:45")  // ✅ Success
DateTimePattern.Parse("10/21/2024")           // ❌ Fails with clear error
```

**Why this matters**:
- **Multi-broker support**: Tastytrade, IBKR, and other brokers have different datetime formats in their CSV statements
- **Consistent storage**: Database always receives standardized ISO 8601 format
- **Reliable imports**: AI agents and importers know exactly what format to expect

### 3. Centralized Temporal Logic

`DateTimePattern` provides convenience methods for common temporal operations:

```fsharp
// Create from raw DateTime
let pattern = DateTimePattern.FromDateTime(DateTime.Now)

// Strict parsing with validation
let pattern = DateTimePattern.Parse("2024-10-21T14:30:45")

// Safe parsing returning Option
let patternOpt = DateTimePattern.TryParse(someString)

// Date boundary helpers
let endOfDay = pattern.WithEndOfDay()  // Sets time to 23:59:59
let value = pattern.Value               // Extract underlying DateTime
```

### 4. Domain-Driven Design

In the investment tracking domain, temporal accuracy is critical:

- **Transaction timestamps** must be precise and consistent
- **Audit trails** (CreatedAt, UpdatedAt) must be reliable
- **Dividend dates**, **expiration dates**, **split dates** are business-critical data

By wrapping these in a domain type, the code clearly expresses: "This is a significant temporal marker in financial transactions, not just any DateTime."

## Usage in the Codebase

### In Database Models

```fsharp
type AuditableEntity =
    { CreatedAt: DateTimePattern option
      UpdatedAt: DateTimePattern option }

type BrokerMovement =
    { Id: int
      TimeStamp: DateTimePattern
      Amount: Money
      // ... other fields
    }

type OptionTrade =
    { Id: int
      TimeStamp: DateTimePattern
      ExpirationDate: DateTimePattern
      // ... other fields
    }
```

### In Data Transformation

```fsharp
// Converting from broker CSV transaction
let timeStamp = DateTimePattern.FromDateTime(transaction.Date)

// Creating audit records
let audit = AuditableEntity.FromDateTime(DateTime.UtcNow)

// Snapshot date operations
let normalizedDate = SnapshotManagerUtils.normalizeToStartOfDay(someDate)
```

### In Queries and Filters

```fsharp
// Filtering movements by date range
let movements = getByBrokerAccountIdFromDate(accountId, startDate: DateTimePattern)

// Handling date boundaries
let dateRange = generateDateRange(startDate, endDate)  // Both DateTimePattern
```

## API Reference

### Static Members

| Member | Signature | Purpose |
|--------|-----------|---------|
| `FromDateTime` | `DateTime -> DateTimePattern` | Create from raw DateTime |
| `Parse` | `string -> DateTimePattern` | Strict parsing (ISO 8601) |
| `TryParse` | `string -> DateTimePattern option` | Safe parsing with Option |

### Instance Members

| Member | Return Type | Purpose |
|--------|-------------|---------|
| `Value` | `DateTime` | Extract underlying DateTime |
| `WithEndOfDay()` | `DateTimePattern` | Set time to 23:59:59 |
| `ToString()` | `string` | Convert to ISO 8601 format |

## Comparison with Money Pattern

`DateTimePattern` follows the same domain-driven design pattern as `Money`:

```fsharp
// Both are wrapped discriminated unions
type DateTimePattern = private | DateTimePattern of DateTime
type Money = private | Money of decimal

// Both enforce format consistency
DateTimePattern.Parse("2024-10-21T14:30:45")  // ISO 8601
Money.FromAmount(100.50m)                      // Decimal precision

// Both prevent accidental type mixing
let amount = Money.FromAmount(50.0m)
let timestamp = DateTimePattern.FromDateTime(DateTime.Now)
// amount and timestamp are distinct types - cannot be accidentally swapped
```

## Best Practices

### ✅ DO

- Use `DateTimePattern.FromDateTime()` when converting from `System.DateTime`
- Use `DateTimePattern.Parse()` when loading from strings (validates format)
- Use `DateTimePattern.TryParse()` for user input (returns Option)
- Extract underlying DateTime via `.Value` only when necessary for interop
- Include `DateTimePattern` in all domain models representing temporal data

### ❌ DON'T

- Don't use raw `DateTime` in domain models (use `DateTimePattern` instead)
- Don't bypass parsing validation (use Parse/TryParse, not manual DateTime.Parse)
- Don't create `DateTimePattern` without going through the provided static members
- Don't mix `DateTimePattern` and `DateTime` in the same calculation

## For AI Agents and Developers

When working with temporal data in Binnaculum:

1. **Recognize `DateTimePattern`** as the canonical temporal type for the domain
2. **Always go through the API** (`FromDateTime`, `Parse`, `TryParse`) - never create directly
3. **Expect ISO 8601 format** in serialized form and when parsing strings
4. **Leverage the type system** - if the type doesn't match, it's probably a bug
5. **Use convenience methods** like `WithEndOfDay()` for common operations

This design ensures temporal data is handled consistently, safely, and with clear intent throughout the investment tracking system.
