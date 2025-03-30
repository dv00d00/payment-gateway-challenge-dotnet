# Payment Gateway – Take-Home Assignment

## Summary

This project implements a simplified **Payment Gateway** API that supports:

- Initiating a payment using card details
- Returning one of the following results: `Authorized`, `Declined`, `Rejected`
- Retrieving payment details by ID

The API validates payment input, simulates interaction with an acquiring bank, and ensures safe handling of sensitive information.

---

## Design Principles

### Invalid State Is Unrepresentable

Core domain concepts (`CardNumber`, `ExpiryDate`, `Money`, etc.) are modeled as **value objects** with guarded constructors and validation. This prevents illegal values from propagating through the system.

### Bank Communication Abstraction

`IBankClient` abstracts the interaction with the acquiring bank. Responses are modeled using a discriminated union-like `BankResponse`, allowing type-safe handling of edge cases like:

- `Rejected` (input error)
- `Declined` (bank refused)
- `Authorized`
- `CommunicationError` with `Justification` enum to distinguish between timeouts, exceptions, and unexpected responses


## Testing

I used a mix of unit and integration tests:

- Integration tests assume bank emulator is running, hardcoded urls and ports
- I've used a combination of example and generated test data
- Random data is generated using FsCheck which is a property-based testing library

---

##  Idempotency: Initial Thoughts

Although out of scope, I started considering **idempotency** and **retry strategies**.

### Observations:

- The **bank API does not expose an idempotency key**, which complicates payment gateway deduplication
- With a likely **multi-node deployment** behind a load balancer, idempotency will require a combination of:
    - Distributed locking
    - Linearizable storage (e.g., Redis with RedLock or etcd)
    - Or extracting bank communication into a **dedicated backend service**

> Best-effort idempotency (e.g., via deduplication on request fingerprint + TTL cache) could be explored further

---

## Out of Scope 

These areas were intentionally left out or only partially explored due to time constraints:

- **Idempotency** support
- Retry logic for failed HTTP calls
    - Including exponential backoff, jitter, circuit breaker, bulkhead
- State persistence for:
    - Final-failed requests
    - Communication failures for monitoring/reporting
- Encryption-at-rest for sensitive data
- Authentication/authorization of merchants
- Secure logging (handling PII)
- Health checks and structured metrics/tracing

---

## 🛠️ Improvements

- Add distributed **idempotency store**
- Add **logging**, **tracing**, and **metrics** (e.g., Prometheus/OpenTelemetry)
- Add **authentication** & merchant scoping
- Add **health checks** for internal + external (bank) dependencies
- Add **persistence** for failed requests beyond max retry
- Secure **encryption at rest** for card data (even last four digits)
- Circuit breaker, bulkhead, and retry strategies for **resilience**