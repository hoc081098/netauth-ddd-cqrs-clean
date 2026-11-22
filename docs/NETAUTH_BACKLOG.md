# NetAuth — Engineering Backlog & Learning Tracker

> **Mục đích:** Gom toàn bộ các task thực hành .NET Backend đã đề xuất từ 10/10 đến 21/11/2026 vào **một backlog duy nhất**, có thứ tự, dependency, checklist, Definition of Done (DoD) và bằng chứng hoàn thành. **Chưa xong backlog thì không tự thêm bài mới.**
>
> **Repository:** [hoc081098/netauth-ddd-cqrs-clean](https://github.com/hoc081098/netauth-ddd-cqrs-clean)  
> **Đường dẫn đề xuất trong repo:** `docs/NETAUTH_BACKLOG.md`  
> **Đối chiếu GitHub:** 23/11/2026; đọc source trên default branch, tests, CI workflow và CI job log.  
> **Commit mới nhất:** [`1665b26`](https://github.com/hoc081098/netauth-ddd-cqrs-clean/commit/1665b26) (22/11/2026, Merge PR #61).

## 0. Nguyên tắc tracking

NetAuth **đã có** nền tảng DDD/CQRS, JWT + Refresh Token Rotation, Role/Permission Authorization, HybridCache + Redis, Transactional Outbox/Quartz, OpenTelemetry tracing và Testcontainers. **Không làm lại từ đầu** các thành phần này. Backlog tập trung vào correctness, security, reliability, concurrency và verification ở production boundary.

**Trạng thái task** — cập nhật ở bảng tổng và từng task:

- `OPEN`: Chưa có bằng chứng đủ để đánh dấu hoàn thành trên baseline được kiểm tra; **không đồng nghĩa** chưa code ở local.
- `IN PROGRESS`: Đang triển khai; ghi link branch/PR.
- `BLOCKED`: Có dependency hoặc quyết định chưa được giải quyết; ghi rõ blocker.
- `DONE`: Tất cả DoD đã đạt, có link PR/commit/test/CI làm bằng chứng.
- `SKIPPED`: Chủ động bỏ qua kèm lý do; không coi là hoàn thành.

**Quy tắc:** Mỗi lần tập trung tối đa 1 task chính. Nếu task đã làm ở nhánh/local, đối chiếu checklist trước khi code lại. Không tick `[x]` chỉ vì đã đọc lý thuyết hoặc thảo luận với AI. Ưu tiên PR nhỏ, test deterministic, không đưa secret/token thật vào docs, code review hay logs.

### Progress board

| # | ID | Công việc | Priority | Effort | Dependency | Status | PR / Evidence |
|---:|---|---|---|---|---|---|---|
| 1 | [NA-01](#na-01--restore-ci--fix-logging-ca1873) | Restore CI & fix logging CA1873 | **P0** | 1–2h | — | DONE | [PR #61](https://github.com/hoc081098/netauth-ddd-cqrs-clean/pull/61) |
| 2 | [NA-02](#na-02--http-authentication--authorization-integration-tests) | HTTP AuthN/AuthZ tests + permission revocation | **P1** | 3–4h | NA-01 | OPEN | — |
| 3 | [NA-03](#na-03--concurrent-refresh-token-rotation) | Concurrent refresh token rotation | **P1** | 2–3h | NA-01 | OPEN | — |
| 4 | [NA-04](#na-04--outbox-retry-backoff--failure-semantics) | Outbox backoff, retry & failure handling | **P1** | 3–4h | NA-01 | OPEN | — |
| 5 | [NA-05](#na-05--cross-instance-permission-cache-invalidation) | Permission cache consistency đa instance | **P1** | 4–6h | NA-02, NA-04 | OPEN | — |
| 6 | [NA-06](#na-06--opentelemetry-custom-application-metrics) | OpenTelemetry custom Outbox metrics | **P2** | 2–3h | NA-01; NA-04 nếu tiện | OPEN | — |

**Tổng effort ước tính:** **15–22 giờ**, chưa tính debugging/review phát sinh. Đây là effort tham khảo, **không phải deadline**. Nếu chỉ có 2–4 giờ/tuần, cứ tiến từng task.

**Next action:** Làm **NA-01** trước. Khi CI xanh, chuyển **NA-02**; những task khác vẫn giữ backlog, không phát sinh task chỉ vì sang tuần mới.

---

## NA-01 — Restore CI & Fix Logging CA1873

**Priority:** P0 · **Status:** DONE · **Effort:** 1–2h · **Dependency:** Không có  
**Tham chiếu:** [build.yml](https://github.com/hoc081098/netauth-ddd-cqrs-clean/blob/main/.github/workflows/build.yml) · [Directory.Build.props](https://github.com/hoc081098/netauth-ddd-cqrs-clean/blob/main/Directory.Build.props) · [Failed CI run](https://github.com/hoc081098/netauth-ddd-cqrs-clean/actions/runs/38858961738) · [PR #61](https://github.com/hoc081098/netauth-ddd-cqrs-clean/pull/61)

### Mục tiêu

Khôi phục Release build + toàn bộ test pipeline, **không tắt quality gates**. Đồng thời tránh logging gây allocation/evaluation thừa hoặc vô tình lộ credentials.

### Hiện trạng đã xác minh

Workflow đang chạy **Restore → Release Build → Tests → Codecov**, dùng .NET 10. Lần chạy `38858961738` ngày 16/10/2026 **fail ở Build** nên Tests/Codecov bị skip. Log của lần chạy đó có **8 diagnostic `CA1873`**:

| File (dưới `src/NetAuth/`) | Số lỗi |
|---|---:|
| `Application/Users/SetUserRoles/UserRolesChangedDomainEventHandler.cs` | 2 |
| `Application/Users/Register/UserCreatedDomainEventHandler.cs` | 1 |
| `Application/Users/Login/RefreshTokenCreatedDomainEventHandler.cs` | 1 |
| `Application/Core/Behaviors/RequestLoggingPipelineBehavior.cs` | 4 |

`Directory.Build.props` đang bật `TreatWarningsAsErrors`, `AnalysisMode=All`, `EnforceCodeStyleInBuild`. Đây là bằng chứng từ **run được kiểm tra**, chưa khẳng định lỗi chắc chắn còn tái hiện ở local hoặc run mới hơn.

### Implementation checklist

- [x] Reproduce **diagnostic hiện tại** với .NET SDK/build configuration tương ứng CI; ghi rõ compiler/analyzer output.
- [x] Fix các callsite CA1873. Ưu tiên `[LoggerMessage]` source generation khi hợp lý (Outbox đang có ví dụ), hoặc `ILogger.IsEnabled(...)` để tránh tính argument tốn chi phí khi log level bị tắt. Không suppress hàng loạt.
- [x] Review `RequestLoggingPipelineBehavior`: không log password, refresh token, access token, Authorization header hoặc raw request chứa dữ liệu nhạy cảm; ưu tiên allowlist/redaction.
- [x] Chạy đúng các lệnh build/test trong pipeline và tạo PR sửa lỗi.
- [x] Verify GitHub Actions **Success** trên commit mới, ghi link vào evidence.

```bash
dotnet restore
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build --verbosity normal \
  /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### Definition of Done

- [x] Release build thành công, vẫn giữ analyzer/`TreatWarningsAsErrors`.
- [x] Tất cả unit, architecture, integration tests hiện có pass trong CI (bao gồm Testcontainers nếu test yêu cầu Docker).
- [x] Logging không làm lộ secret và không tạo expensive work không cần thiết khi tắt log level.
- [x] Có PR/commit và link Actions run **Success**.

**Evidence:** `PR: [#61](https://github.com/hoc081098/netauth-ddd-cqrs-clean/pull/61)` · `Commit: [1665b26](https://github.com/hoc081098/netauth-ddd-cqrs-clean/commit/1665b26)` · `CI: [Run 45063107010 (Success)](https://github.com/hoc081098/netauth-ddd-cqrs-clean/actions/runs/45063107010/job/137333201411)`  
**Học được:** MSBuild, Roslyn analyzers, logging performance, `[LoggerMessage]` source generation, Serilog destructuring với `{@Property}` syntax, `@` prefix trên parameters để an toàn với .NET 10 source generator, CI reliability.

---

## NA-02 — HTTP Authentication & Authorization Integration Tests

**Priority:** P1 · **Status:** OPEN · **Effort:** 3–4h · **Dependency:** NA-01  
**Tham chiếu:** [IntegrationTestWebAppFactory](https://github.com/hoc081098/netauth-ddd-cqrs-clean/blob/main/tests/IntegrationTests/Infrastructure/IntegrationTestWebAppFactory.cs) · [UserTests](https://github.com/hoc081098/netauth-ddd-cqrs-clean/blob/main/tests/IntegrationTests/Users/UserTests.cs) · [GetRolesEndpoint](https://github.com/hoc081098/netauth-ddd-cqrs-clean/blob/main/src/NetAuth/Web.Api/Endpoints/Users/GetRolesEndpoint.cs) · [PermissionClaimsTransformation](https://github.com/hoc081098/netauth-ddd-cqrs-clean/blob/main/src/NetAuth/Infrastructure/Authorization/PermissionClaimsTransformation.cs)

### Mục tiêu

Kiểm thử HTTP security boundary thật, không dừng ở `ISender.Send(...)`. Gộp **HTTP authentication/authorization tests** và **permission revocation regression test** (hai đề xuất cũ) thành **một task**, tránh lặp.

### Hiện trạng đã xác minh

Đã có `WebApplicationFactory<Program>`, PostgreSQL/Redis Testcontainers, các Application integration tests cho register/login/refresh/roles. Tuy nhiên, `IntegrationTestWebAppFactory` hiện **thay `IUserContext` bằng `TestUserContext`**; phù hợp Application test nhưng không đủ để kiểm chứng HTTP security production flow. Endpoint `GET /roles` yêu cầu `permission:roles:read`; cần xác nhận URL đầy đủ qua route-group prefix trước khi viết test (các đề xuất cũ dùng `/v1/roles`).

### Implementation checklist

- [ ] Tạo HTTP test factory/configuration giữ nguyên **JWT Bearer Authentication**, `IUserContext` thật, `IClaimsTransformation`, `IAuthorizationHandler` và policy provider. Reuse Testcontainers nhưng tránh state leakage giữa tests.
- [ ] Tạo user/login và cấp/thu hồi role qua use case thật. Dùng **JWT do chính app ký**, không mock `ClaimsPrincipal` hay inject trực tiếp permission claim.
- [ ] Dùng `HttpClient` gửi request tới protected roles endpoint; assert:

  | Scenario | Expected |
  |---|---|
  | Không có token | `401` |
  | JWT sai/hết hạn | `401` |
  | JWT hợp lệ, không có `roles:read` | `403` |
  | JWT hợp lệ, có `roles:read` | `200` |
  | Thu hồi role, dùng **lại JWT cũ** sau Outbox + cache invalidation | `403` |

- [ ] Chủ động kích hoạt/chờ **Outbox xử lý và invalidation hoàn tất**, không dùng `Task.Delay` mù hoặc chờ TTL tự hết.
- [ ] Đảm bảo test độc lập thứ tự, không reuse user/permission state bất cẩn.

### Definition of Done

- [ ] Phân biệt đúng `401` (unauthenticated) và `403` (authenticated, forbidden) bằng security pipeline thật.
- [ ] Có trường hợp được phép truy cập và bị chặn đúng permission.
- [ ] Cùng JWT trước/sau khi revoke role trả về lần lượt `200` và `403` **sau khi invalidation hoàn tất**.
- [ ] Test chạy ổn định với PostgreSQL/Redis Testcontainers và pass trong CI.

**Evidence:** `PR: TBD` · `Tests/CI: TBD`  
**Giới hạn:** Chỉ chứng minh **single-instance** revocation, chưa giải quyết L1 cache của instance khác (NA-05).  
**Học được:** ASP.NET Core pipeline, JWT validation, Claims Transformation, Authorization Policy, HTTP integration testing.

---

## NA-03 — Concurrent Refresh Token Rotation

**Priority:** P1 · **Status:** OPEN · **Effort:** 2–3h · **Dependency:** NA-01 (có thể tận dụng hạ tầng test NA-02)  
**Tham chiếu:** [RefreshTokenRepository](https://github.com/hoc081098/netauth-ddd-cqrs-clean/blob/main/src/NetAuth/Infrastructure/Repositories/RefreshTokenRepository.cs) · [UserTests](https://github.com/hoc081098/netauth-ddd-cqrs-clean/blob/main/tests/IntegrationTests/Users/UserTests.cs)

### Mục tiêu

Chứng minh **hai request đồng thời dùng cùng refresh token không thể cùng rotate thành công**, và xác định security policy đối với client retry/race hợp lệ so với token theft/reuse.

### Hiện trạng đã xác minh

`RefreshTokenRepository.GetByTokenHashAsync()` dùng PostgreSQL `SELECT ... FOR UPDATE`. Các integration tests đang kiểm tra **sequential** rotation và replay, nhưng bản test đã đọc chưa chứng minh concurrency thực sự. **Có row lock không đồng nghĩa đã chứng minh correctness khi race.**

### Implementation checklist

- [ ] Integration test với PostgreSQL thật; hai request/scopes/DbContexts/transactions **độc lập** cùng sử dụng token X.
- [ ] Đồng bộ để hai luồng thật sự overlap (barrier/coordination hợp lý); tránh test phụ thuộc hoàn toàn vào `Task.Delay`.
- [ ] Assert **tối đa 1 rotation thành công**; kiểm tra trạng thái token cũ, token mới, token chain/session cuối cùng trong DB.
- [ ] Kiểm tra reuse detection khi hai request gần như đồng thời. **Decision:** duplicate request hợp lệ có nên compromise token mới? Ảnh hưởng với mobile retry/multi-tab thế nào?
- [ ] Nếu kết quả không đúng policy đã chọn, chỉnh transaction/locking/domain logic tối thiểu và chạy test lặp lại.

### Definition of Done

- [ ] Race được tạo thực sự qua transaction độc lập; không có double-success.
- [ ] Final persisted state khớp policy đã viết rõ.
- [ ] Test chạy nhiều lần ổn định và pass trong CI.
- [ ] Có PR/test evidence và tài liệu hóa security trade-off.

**Evidence:** `PR: TBD` · `Tests: TBD` · `Policy: TBD`  
**Học được:** Row-level locking, transaction isolation, concurrency, refresh token reuse semantics.

---

## NA-04 — Outbox Retry, Backoff & Failure Semantics

**Priority:** P1 · **Status:** OPEN · **Effort:** 3–4h · **Dependency:** NA-01  
**Tham chiếu:** [OutboxProcessor](https://github.com/hoc081098/netauth-ddd-cqrs-clean/blob/main/src/NetAuth/Infrastructure/Outbox/OutboxProcessor.cs) · [OutboxMessage](https://github.com/hoc081098/netauth-ddd-cqrs-clean/blob/main/src/NetAuth/Infrastructure/Outbox/OutboxMessage.cs) · [OutboxSettings](https://github.com/hoc081098/netauth-ddd-cqrs-clean/blob/main/src/NetAuth/Infrastructure/Outbox/OutboxSettings.cs) · [UserRolesChangedDomainEventHandler](https://github.com/hoc081098/netauth-ddd-cqrs-clean/blob/main/src/NetAuth/Application/Users/SetUserRoles/UserRolesChangedDomainEventHandler.cs)

### Mục tiêu

Outbox xử lý lỗi transient theo retry schedule có giới hạn; **không đánh dấu thành công giả khi downstream thất bại**. Xác định rõ terminal failure/parking và delivery semantics.

### Hiện trạng đã xác minh

`OutboxProcessor` đã có `FOR UPDATE SKIP LOCKED`, batching, parallel processing, bulk update và `attempt_count < MaxAttempts`. SQL hiện **chưa lọc thời điểm `next_retry_at_utc`**, nên message lỗi có thể lại eligible ngay lần job kế tiếp. Deserialization failure đang được đánh dấu terminal (processed kèm error). `UserRolesChangedDomainEventHandler` bắt exception invalidation, log rồi **không rethrow**, khiến publisher có thể xem handler thành công dù cache chưa được invalidate.

### Implementation checklist

- [ ] Định nghĩa policy: transient/permanent errors, `MaxAttempts`, parked/terminal state, cách inspect/replay message lỗi.
- [ ] Bổ sung `NextRetryAtUtc` (hoặc cơ chế tương đương) vào model/database, EF migration; backoff **exponential có cap**, jitter tùy chọn; xác định hành vi khi hết attempts.
- [ ] Sửa SQL `SELECT ... FOR UPDATE SKIP LOCKED` chỉ chọn message **chưa processed, đến hạn, còn retry được**. Cập nhật bulk write để lưu `attempt_count`, retry time, error/terminal state phù hợp.
- [ ] Critical handler failure phải **propagate**; sửa path invalidation của role-change handler để không nuốt exception. Vẫn truyền `OperationCanceledException` đúng cách.
- [ ] Dùng `IClock` và fault-injecting handler/publisher để test deterministic: success, fail transient, chưa đến hạn, retry đúng hạn, success sau retry, vượt MaxAttempts, deserialize lỗi permanent.
- [ ] Tài liệu hóa **at-least-once**, không hứa exactly-once: publish thành công nhưng DB commit/ack lỗi vẫn có thể khiến side effect chạy lại. Consumer/handler có side effect cần idempotency phù hợp.

### Definition of Done

- [ ] Migration/query/update bảo đảm không retry trước hạn hoặc loop retry liên tục.
- [ ] Downstream failure không được mark processed-success; trạng thái được persist theo policy.
- [ ] Message terminal có thể truy vấn điều tra cùng attempts/error.
- [ ] Tests mô phỏng lỗi/recovery chạy với PostgreSQL thật, deterministic, pass CI.
- [ ] Có PR/test evidence và ghi rõ retry/idempotency policy.

**Evidence:** `PR: TBD` · `Migration: TBD` · `Tests: TBD`  
**Học được:** Retry policies, capped exponential backoff, `SKIP LOCKED`, failure classification, at-least-once/idempotency.

---

## NA-05 — Cross-Instance Permission Cache Invalidation

**Priority:** P1 · **Status:** OPEN · **Effort:** 4–6h · **Dependency:** NA-02, NA-04  
**Tham chiếu:** [PermissionService](https://github.com/hoc081098/netauth-ddd-cqrs-clean/blob/main/src/NetAuth/Infrastructure/Authorization/PermissionService.cs) · [UserRolesChangedDomainEventHandler](https://github.com/hoc081098/netauth-ddd-cqrs-clean/blob/main/src/NetAuth/Application/Users/SetUserRoles/UserRolesChangedDomainEventHandler.cs) · [InfrastructureDiModule](https://github.com/hoc081098/netauth-ddd-cqrs-clean/blob/main/src/NetAuth/Infrastructure/InfrastructureDiModule.cs)

### Mục tiêu

Định nghĩa và kiểm chứng **permission revocation consistency guarantee** khi chạy ≥2 API instances: mỗi instance có local L1 riêng, cùng dùng Redis L2 và PostgreSQL. Biết rõ stale-permission window và failure behavior.

### Hiện trạng đã xác minh

`PermissionService` dùng HybridCache với **L2 TTL 30 phút, L1 TTL 5 phút**, invalidation qua `RemoveAsync`. Trong role-change handler có comment nhận diện hạn chế khi chạy nhiều nodes. **Shared Redis L2 removal không tự chứng minh local L1 của mọi process được evict.** Redis Pub/Sub cũng **không durable** nếu subscriber offline.

### Implementation checklist

- [ ] Chốt **security policy trước khi chọn công nghệ**: strict/immediate revocation hay bounded/eventual consistency? Max stale window chấp nhận được? Redis/network outage thì xử lý thế nào?
- [ ] Viết two-instance test harness thật: **hai DI containers/service providers độc lập, hai L1 độc lập**, cùng PostgreSQL/Redis. Warm cache ở A và B, revoke role tại A rồi kiểm tra B.
- [ ] Chọn giải pháp đủ cho policy và trade-off: distributed invalidation notification + version/TTL reconciliation, authoritative version/epoch check, hoặc per-request check cho operation nhạy cảm. **Không mặc định Pub/Sub đơn lẻ bảo đảm eventual recovery sau missed notification.** Nếu dùng broker/fan-out, phải nghĩ tới queue/subscriber lifecycle.
- [ ] Tích hợp retry/failure handling với NA-04. Notification duplicate/out-of-order phải an toàn; tránh vòng lặp publish ↔ invalidate.
- [ ] Test normal propagation, subscriber restart/missed message, network/Redis failure; quan sát/đo stale window.

### Definition of Done

- [ ] Test tự động có **hai application instances thật sự độc lập** ở L1.
- [ ] Sau khi propagation được xác nhận, JWT cũ không còn dùng permission đã thu hồi trên cả A và B.
- [ ] Missed notification/reconnect/failure tuân theo **staleness policy đã viết và test**.
- [ ] Không tuyên bố immediate hoặc reliable revocation nếu thiết kế chỉ bảo đảm eventual/bounded consistency.
- [ ] Có design decision/ADR ngắn, PR và integration test evidence.

**Evidence:** `ADR: TBD` · `PR: TBD` · `Tests: TBD`  
**Học được:** HybridCache L1/L2, distributed cache consistency, event propagation, delivery guarantees, security SLA.

---

## NA-06 — OpenTelemetry Custom Application Metrics

**Priority:** P2 · **Status:** OPEN · **Effort:** 2–3h · **Dependency:** NA-01 (làm sau NA-04 cũng hợp lý)  
**Tham chiếu:** [InfrastructureDiModule](https://github.com/hoc081098/netauth-ddd-cqrs-clean/blob/main/src/NetAuth/Infrastructure/InfrastructureDiModule.cs) · [OutboxProcessor](https://github.com/hoc081098/netauth-ddd-cqrs-clean/blob/main/src/NetAuth/Infrastructure/Outbox/OutboxProcessor.cs)

### Mục tiêu

Bổ sung metrics theo dõi Outbox throughput, failure và processing duration, bên cạnh tracing/logs hiện có. Giữ metric tags **low-cardinality**.

### Hiện trạng đã xác minh

DI đang cấu hình `.WithTracing(...)` với ASP.NET Core, HttpClient, EF Core, Redis, Npgsql và OTLP exporter. Outbox đã đo duration bằng `Stopwatch` và log timing nhưng source được kiểm tra **chưa có `.WithMetrics(...)` hoặc custom `Meter` cho Outbox**.

### Implementation checklist

- [ ] Tạo `Meter` rõ tên (ví dụ `NetAuth.Outbox`) qua `System.Diagnostics.Metrics`.
- [ ] Tạo `Counter<long>` cho outcome (success/failure/attempt) và `Histogram<double>` cho processing duration (định nghĩa đơn vị rõ ràng). Tránh nhầm attempts với successfully processed messages.
- [ ] Cấu hình OpenTelemetry `.WithMetrics(...)`, đăng ký meter và metrics exporter/collector/backend phù hợp. **Jaeger cho tracing không mặc nhiên là metrics dashboard.**
- [ ] Test `MeterListener`: counter increments và histogram recording cho cả success/failure; kiểm chứng dữ liệu đến receiver thật.
- [ ] Viết hướng dẫn ngắn vào README; **không** sử dụng `user_id`, `message_id`, access token hay cardinality cao trong metric tags.

### Definition of Done

- [ ] Counter + histogram phản ánh đúng success/failure và duration.
- [ ] Test `MeterListener` pass; dữ liệu metrics xem được tại backend/collector cấu hình.
- [ ] Đã review tag cardinality/unit; có PR và hướng dẫn verify.

**Evidence:** `PR: TBD` · `Metrics/Tests: TBD`  
**Học được:** Metrics vs traces vs logs, Counter, Histogram, MeterListener, high-cardinality risk.

---

## 1. Những quyết định phải chốt (không phải task mới)

| Quyết định | Trước task | Tại sao |
|---|---|---|
| Concurrent refresh: duplicate request hợp lệ hay token theft? | NA-03 | Reuse detection quá gắt có thể revoke cả session vừa rotate. |
| Outbox retry/parking/permanent failure policy | NA-04 | Tránh retry storm, message mắc kẹt, đánh dấu success giả. |
| At-least-once và idempotency contract | NA-04/NA-05 | Handler có thể bị gọi lại dù một lần trước đã tạo side effect. |
| Permission revocation SLA: immediate, bounded hay eventual? | NA-05 | Chọn Redis Pub/Sub, version check hay authoritative lookup theo guarantee cần đạt. |
| Metrics naming, receiver và cardinality | NA-06 | Tránh monitoring overhead và dùng nhầm tracing backend cho metrics. |

## 2. Links & Verification Notes

**Code đã đọc từ GitHub (default branch):**

- [CI workflow](https://github.com/hoc081098/netauth-ddd-cqrs-clean/blob/main/.github/workflows/build.yml), [Directory.Build.props](https://github.com/hoc081098/netauth-ddd-cqrs-clean/blob/main/Directory.Build.props)
- [OutboxProcessor](https://github.com/hoc081098/netauth-ddd-cqrs-clean/blob/main/src/NetAuth/Infrastructure/Outbox/OutboxProcessor.cs), [OutboxMessage](https://github.com/hoc081098/netauth-ddd-cqrs-clean/blob/main/src/NetAuth/Infrastructure/Outbox/OutboxMessage.cs), [OutboxSettings](https://github.com/hoc081098/netauth-ddd-cqrs-clean/blob/main/src/NetAuth/Infrastructure/Outbox/OutboxSettings.cs)
- [PermissionService](https://github.com/hoc081098/netauth-ddd-cqrs-clean/blob/main/src/NetAuth/Infrastructure/Authorization/PermissionService.cs), [PermissionClaimsTransformation](https://github.com/hoc081098/netauth-ddd-cqrs-clean/blob/main/src/NetAuth/Infrastructure/Authorization/PermissionClaimsTransformation.cs), [UserRolesChangedDomainEventHandler](https://github.com/hoc081098/netauth-ddd-cqrs-clean/blob/main/src/NetAuth/Application/Users/SetUserRoles/UserRolesChangedDomainEventHandler.cs)
- [RefreshTokenRepository](https://github.com/hoc081098/netauth-ddd-cqrs-clean/blob/main/src/NetAuth/Infrastructure/Repositories/RefreshTokenRepository.cs), [IntegrationTestWebAppFactory](https://github.com/hoc081098/netauth-ddd-cqrs-clean/blob/main/tests/IntegrationTests/Infrastructure/IntegrationTestWebAppFactory.cs), [UserTests](https://github.com/hoc081098/netauth-ddd-cqrs-clean/blob/main/tests/IntegrationTests/Users/UserTests.cs)
- [InfrastructureDiModule](https://github.com/hoc081098/netauth-ddd-cqrs-clean/blob/main/src/NetAuth/Infrastructure/InfrastructureDiModule.cs), [GetRolesEndpoint](https://github.com/hoc081098/netauth-ddd-cqrs-clean/blob/main/src/NetAuth/Web.Api/Endpoints/Users/GetRolesEndpoint.cs)

**CI evidence:** [Failed run 16/10/2026](https://github.com/hoc081098/netauth-ddd-cqrs-clean/actions/runs/38858961738): Restore Success, Build Failure với 8 `CA1873`, Tests/Codecov Skipped.  
**CI evidence (mới):** PR #61 đã merge (commit `1665b26`); CI run thành công trên main: [Run 45063107010](https://github.com/hoc081098/netauth-ddd-cqrs-clean/actions/runs/45063107010/job/137333201411).

**Điều quan trọng khi cập nhật tracker:**

1. `Implemented` nghĩa là code thực sự tồn tại/được kiểm chứng; `Discussed` chỉ là đã học, không tự động là done.
2. `OPEN` không chứng minh rằng mày chưa làm trên local/nhánh chưa merge. Bắt đầu task cần kiểm tra lại current branch, source và test.
3. Đọc source/logs **không đồng nghĩa** đã chạy test hoặc đã tái hiện lỗi runtime.
4. Không đưa token thật, credentials, connection strings hoặc secrets vào tài liệu, PR và test artifacts.

## 3. Weekly Progress Log

Chỉ ghi **khi có tiến triển thực tế**. Không tạo thêm task mỗi thứ Bảy nếu backlog vẫn còn.

| Date | Task | Work done / Finding | Evidence | Next |
|---|---|---|---|---|
| 2026-11-22 | Backlog setup | Gom 6 task và đối chiếu source/CI; chưa đánh dấu task nào DONE | Tài liệu này | NA-01 |
| 2026-11-22 | NA-01 | Fix CA1873 logging warnings: migrate 4 files to `[LoggerMessage]` source generation. Bump SonarQube to 10.19.0. Learn about `@` prefix for Serilog destructuring safety with .NET 10 source generator. Merged PR #61. CI passed. | [PR #61](https://github.com/hoc081098/netauth-ddd-cqrs-clean/pull/61), [commit 1665b26](https://github.com/hoc081098/netauth-ddd-cqrs-clean/commit/1665b26), [CI Run 45063107010](https://github.com/hoc081098/netauth-ddd-cqrs-clean/actions/runs/45063107010/job/137333201411) | NA-02 |

**Mẫu cập nhật khi hoàn thành một task:**

```text
Task: NA-__
Status: DONE / IN PROGRESS / BLOCKED
Implemented:
Tests executed + actual results:
PR/Commit/CI links:
Security/Reliability decision:
Remaining caveat (if any):
```

---

> **Nguyên tắc xuyên suốt:** Verify hệ thống đã có dưới concurrency, failure và security pressure **trước khi** thêm công nghệ hoặc kiến trúc mới.
