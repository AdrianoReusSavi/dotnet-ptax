# dotnet-ptax

<p>
  <img alt="CI" src="https://github.com/AdrianoReusSavi/dotnet-ptax/actions/workflows/ci.yml/badge.svg" />
  <img alt="License" src="https://img.shields.io/badge/license-MIT-green" />
  <img alt=".NET" src="https://img.shields.io/badge/.NET-10.0-purple" />
  <img alt="PostgreSQL" src="https://img.shields.io/badge/PostgreSQL-16-blue" />
  <img alt="Redis" src="https://img.shields.io/badge/Redis-7-red" />
</p>

Exchange-rate service built on the PTAX rates published by Banco Central do Brasil — .NET 10, PostgreSQL, Redis, Docker.

A background worker keeps the official daily rates in PostgreSQL and raises alerts when a rate moves more than a threshold from one business day to the next. An HTTP API serves the rates and the alerts, with a Redis cache in front of the most common lookup. The whole stack starts with one `docker compose up`.

## How it works

- **Source:** the [Olinda OData API](https://olinda.bcb.gov.br/olinda/servico/PTAX/versao/v1/aplicacao) of Banco Central. A business day has five bulletins (opening, three intermediate, closing); only the **closing** bulletin is stored, because it is the official PTAX rate of the day.
- **Sync job:** every hour (configurable), the worker asks the API for each configured currency over a **sliding window** of the last 7 days, ending today in UTC. Re-reading recent days instead of fetching only "new" ones means a bulletin published late, or corrected, is picked up on the next cycle without any bookkeeping.
- **Idempotent writes:** rates are saved with `INSERT ... ON CONFLICT (currency, date) DO UPDATE`, inside one transaction per batch. Running the same window again updates the rows in place and never creates duplicates.
- **Resilient HTTP:** the Banco Central client uses the standard resilience handler (retries with backoff, timeouts, circuit breaker). A server error is retried; a bad request is not.
- **Failure isolation:** each currency is synced in its own scope and `try`. One currency failing is logged and does not stop the others, nor the worker. A shutdown is not treated as a failure.
- **Variation alerts:** after each sync, the worker compares each day's sell rate with the **previous available day** (so weekends and holidays are skipped naturally). A change at or above the threshold (1% by default) becomes an alert. Alerts are stored with `ON CONFLICT DO NOTHING`, and only the new ones are logged as warnings, so the hourly re-syncs do not repeat them.
- **Latest rate lookup:** asking for a date with no bulletin (a weekend, a holiday, or today before the closing is out) returns the latest rate **on or before** that date, with its real date in the response, so the client always knows which day it got.
- **Cache:** that lookup goes through Redis (cache-aside). The expiration depends on the answer: a rate for the exact date asked never changes, so it is kept for 1 day; a fallback to an earlier date can change as soon as the new bulletin arrives, so it is kept for 5 minutes. "Not found" is not cached.
- **Redis is optional at runtime:** if Redis fails, the API logs a warning and answers from PostgreSQL. A circuit breaker opens after repeated failures, so while Redis is down the requests skip it entirely instead of waiting for a timeout each time.

## API

| Endpoint | Query | Responses |
| - | - | - |
| `GET /rates/{currency}` | `date` (optional, default today UTC) | `200` latest rate on or before `date` · `404` no rate · `400` invalid input |
| `GET /rates/{currency}/history` | `from`, `to` (required) | `200` rates in the period, oldest first (`[]` if none) · `400` invalid input |
| `GET /alerts` | `currency`, `from`, `to` (all optional) | `200` alerts in the period, newest first (`[]` if none) · `400` invalid input |

- Dates are `yyyy-MM-dd`. Currencies are 3-letter codes in any case (`usd` works).
- Periods are inclusive and at most 366 days.
- `/alerts` without `to` ends today (UTC); without `from` starts 30 days before `to`; without `currency` returns every currency.

`GET /rates/usd?date=2026-11-22` (a Sunday):

```json
{
  "currency": "USD",
  "date": "2026-11-19",
  "buyRate": 5.88830,
  "sellRate": 5.88890,
  "buyParity": 1.00000,
  "sellParity": 1.00000
}
```

`GET /alerts?from=2026-11-01`:

```json
[
  {
    "currency": "USD",
    "previousDate": "2026-11-18",
    "date": "2026-11-19",
    "previousSellRate": 5.81020,
    "sellRate": 5.88890,
    "changePercent": 1.35
  }
]
```

Errors are returned as `application/problem+json` ([RFC 9457](https://www.rfc-editor.org/rfc/rfc9457)). Validation errors are collected in one response, keyed by parameter:

```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "currency": ["Currency must be a 3-letter code."],
    "from": ["'from' must be on or before 'to'."]
  }
}
```

A malformed date (`?from=abc`) or a missing required parameter is also a `400`, not a `500`.

## Design decisions

- **Only the closing bulletin.** The intermediate bulletins are useful intraday, but the product here is the official daily rate. One row per currency per day keeps the key simple: `(currency, date)`.
- **Alerts computed from stored data.** The detector reads the rates from the database over the sync window, not from the API response, so an alert is always backed by rows that exist. It is a pure function (`VariationDetector`), tested without a database.
- **Cache with `IDistributedCache`, not `HybridCache`.** The expiration depends on the result (exact date or fallback) and "not found" must not be cached. A decorator over `IDailyRateReader` keeps this out of the endpoint and out of the repository.
- **Migrations as a one-shot container.** In Compose, an EF Core migrations bundle runs before the API and the worker start, and both wait for it to finish successfully. Neither service migrates on startup, so they never race to apply the same migration, and neither starts against a database without tables.
- **Configuration is validated at startup.** An empty currency list, a code that is not 3 uppercase letters, or a non-positive threshold stops the worker before its first cycle, with a message naming the setting.

## Project structure

```
src/
  Ptax.Bcb/        Client for the Banco Central PTAX API (no dependency on the rest)
  Ptax.Data/       EF Core context, entities, migrations and repositories
  Ptax.Worker/     Sync job, variation detector and alert service
  Ptax.Api/        Minimal API, cache decorator and Redis circuit breaker
tests/
  Ptax.Bcb.Tests/     HTTP client tested with a fake handler, no network
  Ptax.Data.Tests/    Repositories against a real PostgreSQL (Testcontainers)
  Ptax.Worker.Tests/  Worker and services with hand-written fakes
  Ptax.Api.Tests/     Endpoints end to end (WebApplicationFactory + PostgreSQL + Redis containers)
```

## Running with Docker Compose

```
docker compose up -d --build
```

This starts PostgreSQL, Redis, the migrations, the worker and the API at <http://127.0.0.1:8080>. The first sync runs right away, so `GET /rates/usd` answers a few seconds after startup.

```
docker compose logs worker     # one "Synced" line per currency per cycle, warnings for new alerts
docker compose down            # data stays in the postgres_data volume
docker compose down -v         # removes the data too
```

### Podman on Windows

The stack runs on Podman through its Docker API. Learned the hard way:

- Use the default builder: `docker buildx use default`. With the `docker-container` builder, each image is sent back to Podman as a tarball, and that transfer can hang (stuck in `sending tarball`).
- If `docker` commands hang, or `podman machine start` warns that "API forwarding for Docker API clients is not available", run `podman machine stop`, then `wsl --shutdown`, then `podman machine start`.
- Prefer `127.0.0.1` over `localhost`: `localhost` tries IPv6 first and adds about 2 seconds to every new connection.

## Running locally

Requirements: .NET 10 SDK and a container runtime.

```
docker run -d --name ptax-db -e POSTGRES_PASSWORD=dev -e POSTGRES_DB=ptax -p 5432:5432 postgres:16-alpine
docker run -d --name ptax-redis -p 6379:6379 redis:7-alpine

dotnet ef database update --project src/Ptax.Data --connection "Host=127.0.0.1;Database=ptax;Username=postgres;Password=dev"
```

Connection strings are read from user secrets (never committed):

```
dotnet user-secrets set "ConnectionStrings:Ptax" "Host=127.0.0.1;Database=ptax;Username=postgres;Password=dev" --project src/Ptax.Worker
dotnet user-secrets set "ConnectionStrings:Ptax" "Host=127.0.0.1;Database=ptax;Username=postgres;Password=dev" --project src/Ptax.Api
dotnet user-secrets set "ConnectionStrings:Redis" "127.0.0.1:6379" --project src/Ptax.Api

dotnet run --project src/Ptax.Worker
dotnet run --project src/Ptax.Api
```

### Configuration

Worker settings, under `PtaxSync` (environment variables use `__`, for example `PtaxSync__AlertThresholdPercent=0.5`):

| Setting | Default | Notes |
| - | - | - |
| `Currencies` | `["USD", "EUR"]` | 3-letter uppercase codes. An unknown code syncs nothing: the Banco Central API answers it with an empty list, not an error. |
| `Interval` | `01:00:00` | Time between sync cycles. |
| `WindowDays` | `7` | How many days back each cycle re-reads. |
| `AlertThresholdPercent` | `1` | Minimum change, in percent, between two business days to raise an alert. Must be greater than zero. |

## Running the tests

```
dotnet test
```

The Data and Api tests start PostgreSQL and Redis with [Testcontainers](https://testcontainers.com/), so a container runtime must be running. No test calls the real Banco Central API. CI runs the tests and builds the Docker images on every push.

## License

[MIT](LICENSE)
