# GymErp.Application — Core (Hexagonal)

Este projeto é o **core da aplicação** na arquitetura hexagonal. Contém apenas **portas** (contratos) e **implementações dos use cases** (handlers). Nenhuma tecnologia externa (EF, Kafka, HTTP) é referenciada aqui.

## Estrutura

```
GymErp.Application/
├── Ports/
│   ├── Inbound/          ← Como o mundo chama o sistema (drivers)
│   │   ├── IAddNewEnrollmentUseCase.cs
│   │   ├── ICancelEnrollmentUseCase.cs
│   │   └── ISuspendEnrollmentUseCase.cs
│   └── Outbound/         ← O que o core precisa do mundo (driven)
│       ├── IEnrollmentRepository.cs
│       ├── IUnitOfWork.cs
│       └── IEventPublisher.cs
├── UseCases/             ← Implementações dos use cases (handlers + DTOs)
│   ├── AddNewEnrollment/
│   ├── CancelEnrollment/
│   └── SuspendEnrollment/
└── DependencyInjection.cs
```

- **Ports.Inbound**: interfaces dos use cases (portas de entrada). Os adapters (ex.: API, consumers) chamam essas interfaces.
- **Ports.Outbound**: interfaces que o core usa para persistência, transação e publicação de eventos. Os adapters (ex.: EF, Kafka) implementam essas interfaces.
- **UseCases/**: cada feature contém o handler (implementação do use case), request/response e depende apenas das portas outbound.
