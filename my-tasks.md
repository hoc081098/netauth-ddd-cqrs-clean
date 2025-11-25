# Những phần còn thiếu/hở

### 1. RBAC/Permission chưa có
- Không thấy aggregate/bảng **Role/Permission/UserRole**.
- JWT không nhúng claim quyền/role (NetAuth/Infrastructure/Authentication/JwtProvider.cs).
- DI chỉ `AddAuthorization()` mặc định (NetAuth/Infrastructure/InfrastructureDiModule.cs).
- Endpoint không gắn policy/requirement.

### 2. CQRS mới có một nửa (mới có Command)
- Có Register/Login Command Handler.
- **Chưa có Query Handler**, chưa có read model riêng.
- Chưa tách đọc/ghi hoặc tối ưu truy vấn.

### 3. Transaction / Unit-of-Work còn rải rác
- Mỗi handler tự gọi `unitOfWork.SaveChangesAsync` (RegisterCommandHandler.cs, LoginCommandHandler.cs).
- **Chưa có pipeline behavior** đảm bảo mọi command chạy trong transaction + outbox **atomic** cùng nhau.

### 4. Outbox còn thiếu nhiều phần
- Hiện **mới re-publish domain events** vào MediatR **trong cùng process** (NetAuth/Infrastructure/AppDbContext.cs, NetAuth/Infrastructure/Outbox/OutboxProcessor.cs).
- Chưa có mapping Integration Event.
- Chưa đẩy ra message bus.
- Chưa có retry/backoff/dead-letter/cleanup.
- Chưa đạt chuẩn **full transactional outbox** cho giao tiếp liên service.

### 5. Auth còn cơ bản
- Chưa có refresh token + rotation.
- Chưa có revoke/blacklist token.
- Chưa có lockout khi nhập sai nhiều lần.
- Chưa có reset mật khẩu / confirm email.
- Chưa có audit đăng nhập.
- JWT chưa chứa roles/permissions để phục vụ authorization.

---

# Gợi ý tiếp theo

### 1. Thiết kế đầy đủ hệ thống RBAC
- Aggregate Role/Permission.
- Bảng Role/Permission/UserRole.
- Seeding dữ liệu.
- Policy + attribute để enforce.

### 2. Bổ sung query side chuẩn CQRS
- IQuery/IQueryHandler.
- DTO đọc tách biệt.
- Có thể dùng DB riêng cho đọc khi scale.

### 3. Transactional pipeline
- Viết MediatR pipeline đảm bảo mọi Command chạy trong transaction.
- Transaction + Outbox atomic.
- Có thể kết hợp EF Execution Strategy.

### 4. Outbox hoàn chỉnh
- Mapping domain event → integration event.
- Publisher ra Message Bus (Kafka/RabbitMQ/Redis Stream/etc.).
- Retry/backoff.
- Dead-letter queue.
- Cleanup job.

### 5. Hoàn thiện authentication
- Refresh token + rotation.
- Token revoke/blacklist.
- Lockout sau n lần sai.
- Reset password / confirm email.
- Audit log đăng nhập.
- Nhúng roles/permissions vào JWT để phục vụ authorization.

---

Nếu muốn tao viết luôn **bản checklist chi tiết cho roadmap**, hoặc vẽ **kiến trúc DDD + Outbox** cho NetAuth, tao làm tiếp cho.