USE KarmaBankingDb;

-- ─── 1. ALTER SavingsAccount — câmpuri lipsă ────────────────────────────────
ALTER TABLE SavingsAccount
ADD updatedAt DATETIME2 NULL;
GO

-- ─── 2. ALTER AutoDeposit — câmpuri lipsă ───────────────────────────────────
ALTER TABLE AutoDeposit
ADD
    sourceAccountId INT          NULL,
    dayOfMonth      INT          NULL,
    dayOfWeek       INT          NULL,
    updatedAt       DATETIME2    NULL;
GO

ALTER TABLE AutoDeposit
ADD CONSTRAINT CK_AutoDeposit_DayOfMonth
    CHECK (dayOfMonth IS NULL OR (dayOfMonth >= 1 AND dayOfMonth <= 28));
GO

-- ─── 3. CREATE SavingsTransaction ───────────────────────────────────────────
CREATE TABLE SavingsTransaction (
    id              INT           PRIMARY KEY IDENTITY(1,1),
    accountId       INT           NOT NULL,
    transactionType NVARCHAR(20)  NOT NULL,
        -- 'Deposit' | 'Withdrawal' | 'Interest' | 'Transfer' | 'Closure'
    amount          DECIMAL(18,2) NOT NULL,
    balanceAfter    DECIMAL(18,2) NOT NULL,
    source          NVARCHAR(50)  NULL,
        -- 'Manual' | 'Recurring' | 'Interest' | 'Transfer' | 'Closure'
    description     NVARCHAR(255) NULL,
    createdAt       DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_SavingsTx_Account
        FOREIGN KEY (accountId) REFERENCES SavingsAccount(id)
);
GO

CREATE INDEX IX_SavingsTx_AccountId_CreatedAt
    ON SavingsTransaction (accountId, createdAt DESC);
GO

-- ─── 4. CREATE InterestLog ───────────────────────────────────────────────────
CREATE TABLE InterestLog (
    id              INT           PRIMARY KEY IDENTITY(1,1),
    accountId       INT           NOT NULL,
    interestAmount  DECIMAL(18,2) NOT NULL,
    balanceBefore   DECIMAL(18,2) NOT NULL,
    balanceAfter    DECIMAL(18,2) NOT NULL,
    rateApplied     DECIMAL(5,4)  NOT NULL,
    periodMonth     NVARCHAR(7)   NOT NULL,
        -- format: 'YYYY-MM', ex: '2024-11'
    creditedAt      DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_InterestLog_Account
        FOREIGN KEY (accountId) REFERENCES SavingsAccount(id),
    CONSTRAINT UQ_InterestLog_AccountPeriod
        UNIQUE (accountId, periodMonth)   -- previne dubla creditare
);
GO

CREATE INDEX IX_InterestLog_AccountId
    ON InterestLog (accountId);
GO