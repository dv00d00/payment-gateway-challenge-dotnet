# Payment Gateway

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

## Improvements

- Add distributed **idempotency store**
- Add **logging**, **tracing**, and **metrics** (e.g., Prometheus/OpenTelemetry)
- Add **authentication** & merchant scoping
- Add **health checks** for internal + external (bank) dependencies
- Add **persistence** for failed requests beyond max retry
- Secure **encryption at rest** for card data (even last four digits)
- Circuit breaker, bulkhead, and retry strategies for **resilience**

## Notes

Throughout development, I felt a mix of dread and excitement—pretty typical when dealing with distributed systems, especially in the context of payments, where even small failures can have outsized consequences. The task called for a simple and maintainable solution that shouldn’t take more than 4 hours.

I’ll be honest: I didn’t quite manage to keep it simple. Maybe that’s the baggage of having seen too many real-world failures over the years. So I focused on the things I could control—like getting the domain model right. Even then, I couldn’t find peace of mind, especially after experimenting with idempotency on a separate branch. The tension between simplicity and robustness is very real here.

That said, I really enjoyed the challenge. It pushed me to think deeply about trade-offs, revisit some core design principles, and reflect on my instincts as a developer. Given more time, there’s plenty I’d love to refine—but even within the scope of this take-home, I’m happy with the direction I took and the decisions I made.