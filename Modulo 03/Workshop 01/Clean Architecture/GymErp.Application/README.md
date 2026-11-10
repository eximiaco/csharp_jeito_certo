# GymErp.Application · Use Cases (Clean Architecture)

Este projeto é a camada **Application** na Clean Architecture. Contém os **casos de uso** (handlers) e as **abstrações** (interfaces) que a Application precisa para persistência, transação e publicação de eventos. Nenhuma tecnologia externa (EF, Kafka, HTTP) é referenciada aqui, nem o container de DI: o registro dos casos de uso fica na composition root da Api.

## Estrutura

```
GymErp.Application/
├── Abstractions/              ← Interfaces implementadas pela Infrastructure
│   ├── IEnrollmentRepository.cs
│   ├── IUnitOfWork.cs
│   └── IEventPublisher.cs
├── UseCases/                  ← Um use case por pasta (interface + handler + DTOs)
│   ├── AddNewEnrollment/
│   │   ├── IAddNewEnrollmentUseCase.cs
│   │   ├── AddNewEnrollmentHandler.cs
│   │   └── AddNewEnrollmentRequest.cs
│   ├── CancelEnrollment/
│   │   ├── ICancelEnrollmentUseCase.cs
│   │   ├── CancelEnrollmentHandler.cs
│   │   ├── CancelEnrollmentRequest.cs
│   │   └── CancelEnrollmentResponse.cs
│   └── SuspendEnrollment/
│       ├── ISuspendEnrollmentUseCase.cs
│       ├── SuspendEnrollmentHandler.cs
│       └── SuspendEnrollmentCommand.cs
└── UseCases/EnrollmentError.cs   ← erro tipado (NotFound, RuleViolated)
```

- **Abstractions/**: interfaces que a Application depende para persistência, UoW e publicação de eventos. A Infrastructure implementa essas interfaces.
- **UseCases/**: cada feature contém a interface do use case, o handler (implementação) e os DTOs de entrada/saída. Os handlers dependem apenas das abstrações em `Abstractions/` e do Domain.
