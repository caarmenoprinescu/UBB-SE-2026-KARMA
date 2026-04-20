/* 
    PASUL 1: Crearea bazei de date
    Dacă baza de date nu există, o creăm.
*/
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'KarmaBankingDb')
BEGIN
    CREATE DATABASE KarmaBankingDb;
END
GO

USE KarmaBankingDb;
GO

/* 
    PASUL 2: Ștergerea tabelelor existente (în ordine inversă a cheilor externe)
    Aceasta permite rularea scriptului de mai multe ori fără erori.
*/
IF OBJECT_ID('InterestLog', 'U') IS NOT NULL DROP TABLE InterestLog;
IF OBJECT_ID('SavingsTransaction', 'U') IS NOT NULL DROP TABLE SavingsTransaction;
IF OBJECT_ID('AutoDeposit', 'U') IS NOT NULL DROP TABLE AutoDeposit;
IF OBJECT_ID('ChatAttachment', 'U') IS NOT NULL DROP TABLE ChatAttachment;
IF OBJECT_ID('ChatMessage', 'U') IS NOT NULL DROP TABLE ChatMessage;
IF OBJECT_ID('InvestmentTransaction', 'U') IS NOT NULL DROP TABLE InvestmentTransaction;
IF OBJECT_ID('InvestmentHolding', 'U') IS NOT NULL DROP TABLE InvestmentHolding;
IF OBJECT_ID('Portfolio', 'U') IS NOT NULL DROP TABLE Portfolio;
IF OBJECT_ID('AmortizationRow', 'U') IS NOT NULL DROP TABLE AmortizationRow;
IF OBJECT_ID('Loan', 'U') IS NOT NULL DROP TABLE Loan;
IF OBJECT_ID('SavingsAccount', 'U') IS NOT NULL DROP TABLE SavingsAccount;
IF OBJECT_ID('ChatSession', 'U') IS NOT NULL DROP TABLE ChatSession;
IF OBJECT_ID('LoanApplication', 'U') IS NOT NULL DROP TABLE LoanApplication;

/* 
    PASUL 3: Crearea Tabelelor (cu toate câmpurile necesare incluse)
*/

-- 1. Portofoliu (Modulul tău - Investments)
CREATE TABLE Portfolio (
    id INT PRIMARY KEY IDENTITY(1,1),
    userId INT NOT NULL,
    totalValue DECIMAL(18,2) DEFAULT 0,
    totalGainLoss DECIMAL(18,2) DEFAULT 0,
    gainLossPercent DECIMAL(18,2) DEFAULT 0
);

-- 2. Împrumuturi
CREATE TABLE Loan (
    id INT PRIMARY KEY IDENTITY(1,1),
    userId INT NOT NULL,
    loanType NVARCHAR(50),
    principal DECIMAL(18,2),
    outstandingBalance DECIMAL(18,2),
    interestRate DECIMAL(5,2),
    monthlyInstallment DECIMAL(18,2),
    remainingMonths INT,
    loanStatus NVARCHAR(30),
    TermInMonths INT NULL,
    StartDate DATETIME2 NULL
);

-- 3. Conturi de Economii
CREATE TABLE SavingsAccount (
    id INT PRIMARY KEY IDENTITY(1,1),
    userId INT NOT NULL,
    savingsType NVARCHAR(50),
    balance DECIMAL(18,2),
    accruedInterest DECIMAL(18,2),
    apy DECIMAL(18,2),
    maturityDate DATE NULL,
    accountStatus NVARCHAR(30),
    createdAt DATETIME2 DEFAULT GETUTCDATE(),
    updatedAt DATETIME2 NULL,
    accountName NVARCHAR(100) NULL,
    fundingAccountId INT NULL,
    targetAmount DECIMAL(18,2) NULL,
    targetDate DATE NULL
);

-- 4. Investiții (Detaliu per activ)
CREATE TABLE InvestmentHolding (
    id INT PRIMARY KEY IDENTITY(1,1),
    portfolioId INT NOT NULL,
    ticker NVARCHAR(50),
    assetType NVARCHAR(50),
    quantity DECIMAL(18,2),
    avgPurchasePrice DECIMAL(18,2),
    currentPrice DECIMAL(18,2),
    unrealizedGainLoss DECIMAL(18,2),
    CONSTRAINT FK_InvestmentHolding_Portfolio FOREIGN KEY (portfolioId) REFERENCES Portfolio(id)
);

-- 5. Tranzacții Investiții
CREATE TABLE InvestmentTransaction (
    id INT PRIMARY KEY IDENTITY(1,1),
    holdingId INT NOT NULL,
    ticker NVARCHAR(50),
    actionType NVARCHAR(20),
    quantity DECIMAL(18,2),
    pricePerUnit DECIMAL(18,2),
    fees DECIMAL(18,2),
    orderType NVARCHAR(20),
    executedAt DATETIME2 DEFAULT GETDATE(),
    CONSTRAINT FK_InvTrans_Holding FOREIGN KEY (holdingId) REFERENCES InvestmentHolding(id)
);

-- 6. Suport Chat
CREATE TABLE ChatSession (
    id INT PRIMARY KEY IDENTITY(1,1),
    userId INT,
    issueCategory NVARCHAR(50),
    sessionStatus NVARCHAR(30),
    rating INT NULL,
    startedAt DATETIME2 DEFAULT GETUTCDATE(),
    endedAt DATETIME2 NULL,
    feedback NVARCHAR(255) NULL
);

CREATE TABLE ChatMessage (
    id INT PRIMARY KEY IDENTITY(1,1),
    sessionId INT NOT NULL,
    senderType NVARCHAR(20),
    content NVARCHAR(MAX),
    sentAt DATETIME2 DEFAULT GETUTCDATE(),
    CONSTRAINT FK_ChatMessage_Session FOREIGN KEY (sessionId) REFERENCES ChatSession(id)
);

-- 7. Tranzacții Economii și Loguri
CREATE TABLE SavingsTransaction (
    id INT PRIMARY KEY IDENTITY(1,1),
    accountId INT NOT NULL,
    transactionType NVARCHAR(20) NOT NULL,
    amount DECIMAL(18,2) NOT NULL,
    balanceAfter DECIMAL(18,2) NOT NULL,
    source NVARCHAR(50) NULL,
    description NVARCHAR(255) NULL,
    createdAt DATETIME2 DEFAULT GETUTCDATE(),
    CONSTRAINT FK_SavingsTx_Account FOREIGN KEY (accountId) REFERENCES SavingsAccount(id)
);

CREATE TABLE InterestLog (
    id INT PRIMARY KEY IDENTITY(1,1),
    accountId INT NOT NULL,
    interestAmount DECIMAL(18,2) NOT NULL,
    balanceBefore DECIMAL(18,2) NOT NULL,
    balanceAfter DECIMAL(18,2) NOT NULL,
    rateApplied DECIMAL(5,4) NOT NULL,
    periodMonth NVARCHAR(7) NOT NULL,
    creditedAt DATETIME2 DEFAULT GETUTCDATE(),
    CONSTRAINT FK_InterestLog_Account FOREIGN KEY (accountId) REFERENCES SavingsAccount(id)
);

-- 8. Alte tabele (Amortizare, AutoDeposit, Aplicații)
CREATE TABLE AmortizationRow (
    id INT PRIMARY KEY IDENTITY(1,1),
    loanId INT NOT NULL,
    installmentNumber INT,
    dueDate DATE,
    principalPortion DECIMAL(18,2),
    interestPortion DECIMAL(18,2),
    remainingBalance DECIMAL(18,2),
    CONSTRAINT FK_Amortization_Loan FOREIGN KEY (loanId) REFERENCES Loan(id)
);

CREATE TABLE AutoDeposit (
    id INT PRIMARY KEY IDENTITY(1,1),
    savingsAccountId INT NOT NULL,
    frequency NVARCHAR(50),
    amount DECIMAL(18,2),
    isActive BIT,
    nextRunDate DATE,
    sourceAccountId INT NULL,
    dayOfMonth INT NULL,
    dayOfWeek INT NULL,
    updatedAt DATETIME2 NULL,
    CONSTRAINT FK_AutoDeposit_Savings FOREIGN KEY (savingsAccountId) REFERENCES SavingsAccount(id)
);

CREATE TABLE LoanApplication (
    id INT PRIMARY KEY IDENTITY(1,1),
    loanType NVARCHAR(50),
    desiredAmount DECIMAL(18,2),
    preferredTermMonths INT,
    purpose NVARCHAR(255),
    applicationStatus NVARCHAR(30),
    rejectionReason NVARCHAR(255)
);

/* 
    PASUL 4: Popularea cu date de test
*/

-- Portofoliu pentru User 1 (foarte important pentru testarea ta)
INSERT INTO Portfolio (userId, totalValue, totalGainLoss, gainLossPercent) VALUES (1, 25000, 2000, 8);

-- Holding-uri pentru Portofoliul de mai sus
INSERT INTO InvestmentHolding (portfolioId, ticker, assetType, quantity, avgPurchasePrice, currentPrice, unrealizedGainLoss)
VALUES 
(1, 'AAPL', 'Stock', 10, 150, 180, 300),
(1, 'TSLA', 'Stock', 5, 700, 650, -250),
(1, 'BTC', 'Crypto', 0.5, 30000, 65000, 17500);

-- Tranzacții
INSERT INTO InvestmentTransaction (holdingId, ticker, actionType, quantity, pricePerUnit, fees, orderType, executedAt)
VALUES (1, 'AAPL', 'BUY', 10, 150, 5, 'Market', GETDATE());

-- Conturi de Economii pentru User 1
INSERT INTO SavingsAccount (userId, savingsType, balance, accruedInterest, apy, accountStatus, accountName, targetAmount, targetDate)
VALUES (1, 'Standard', 5000, 120, 0.025, 'Active', 'Emergency Fund', 10000, '2026-12-01');

-- Mesaj de succes
PRINT 'Baza de date KarmaBankingDb a fost creată și populată cu succes!';


USE KarmaBankingDb;
SELECT * FROM Portfolio WHERE userId = 1;
SELECT * FROM InvestmentHolding WHERE portfolioId = (SELECT id FROM Portfolio WHERE userId = 1);



SELECT * FROM Portfolio WHERE userId = 1;