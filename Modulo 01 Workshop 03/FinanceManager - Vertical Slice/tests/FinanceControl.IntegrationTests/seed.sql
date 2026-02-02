INSERT INTO master.dbo.Users
(Id, Email, Name, CreatedAt, UpdatedAt)
VALUES('bb4e4025-4909-4591-825a-7ec0b0436754', 'user@tests.com', 'UserForTests', '2024-08-21 16:44:23.436', '2024-08-21 16:44:23.436');

INSERT INTO master.dbo.TransactionCategories
(Id, Name, Description, [Type], CreatedAt, UpdatedAt)
VALUES('2e484230-cdcc-4374-bf16-272075696744', 'forTests', 'forTests', 1, '2024-08-21 16:44:23.861', '2024-08-21 16:44:23.861');

INSERT INTO master.dbo.BankAccounts
(Id, Name, Balance_Amount, Balance_Currency, UserId, CreatedAt, UpdatedAt)
VALUES('ee544d49-9c9c-4930-b2c7-d500f093e909', 'forTests', 0, 'USD', 'bb4e4025-4909-4591-825a-7ec0b0436754', '2024-08-21 16:44:23.861', '');

INSERT INTO master.dbo.BudgetAlerts
(Id, MaxAllowedValue_Amount, MaxAllowedValue_Currency, CategoryId, AccountId, BankAccountId, CreatedAt, UpdatedAt)
VALUES('b56548e2-d09a-4472-9dab-ae6c4e236a43', 100, 'BRL', '2e484230-cdcc-4374-bf16-272075696744', 'ee544d49-9c9c-4930-b2c7-d500f093e909', 'ee544d49-9c9c-4930-b2c7-d500f093e909', '2024-08-21 16:44:23.861', '2024-08-21 16:44:23.861');

