# GymErp.Core · o hexágono

Tudo o que é de dentro: domínio, portas e a implementação da porta de condução. Nenhuma referência a EF Core, ASP.NET, broker ou `Microsoft.Extensions`.

```
GymErp.Core/
├── Domain/                         ← idêntico ao Domain da solução Clean Architecture
│   ├── Common/                     ← Aggregate, IDomainEvent
│   └── Enrollments/                ← Enrollment, Client, states, eventos
├── Ports/
│   ├── Driving/
│   │   └── IForManagingEnrollments.cs   ← porta de condução, requests, responses e EnrollmentError
│   └── Driven/
│       ├── IForStoringEnrollments.cs
│       └── IForNotifyingEnrollmentEvents.cs
└── EnrollmentManagement.cs         ← implementa IForManagingEnrollments usando só portas conduzidas
```
