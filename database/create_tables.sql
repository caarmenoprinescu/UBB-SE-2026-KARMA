USE KarmaBankingDb;

CREATE TABLE Loan (
    id INT PRIMARY KEY IDENTITY(1,1),
    userId INT NOT NULL,
    loanType NVARCHAR(50),
    principal DECIMAL(18,2),
    outstandingBalance DECIMAL(18,2),
    interestRate DECIMAL(5,2),
    monthlyInstallment DECIMAL(18,2),
    remainingMonths INT,
    loanStatus NVARCHAR(30)
);

CREATE TABLE SavingsAccount (
    id INT PRIMARY KEY IDENTITY(1,1),
    userId INT NOT NULL,
    savingsType NVARCHAR(50),
    balance DECIMAL(18,2),
    accruedInterest DECIMAL(18,2),
    apy DECIMAL(18,2),
    maturityDate DATE,
    accountStatus NVARCHAR(30),
    createdAt DATETIME2
);

CREATE TABLE Portfolio (
    id INT PRIMARY KEY IDENTITY(1,1),
    totalValue DECIMAL(18,2),
    totalGainLoss DECIMAL(18,2),
    gainLossPercent DECIMAL(18,2)
);

CREATE TABLE ChatSession (
    id INT PRIMARY KEY IDENTITY(1,1),
    userId INT,
    issueCategory NVARCHAR(50),
    sessionStatus NVARCHAR(30),
    rating INT,
    startedAt DATETIME2,
    endedAt DATETIME2
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

CREATE TABLE InvestmentHolding (
    id INT PRIMARY KEY IDENTITY(1,1),
    portfolioId INT NOT NULL,
    ticker NVARCHAR(50),
    assetType NVARCHAR(50),
    quantity DECIMAL(18,2),
    avgPurchasePrice DECIMAL(18,2),
    currentPrice DECIMAL(18,2),
    unrealizedGainLoss DECIMAL(18,2),
    FOREIGN KEY (portfolioId) REFERENCES Portfolio(id)
);

CREATE TABLE InvestmentTransaction (
    id INT PRIMARY KEY IDENTITY(1,1),
    holdingId INT NOT NULL,
    ticker NVARCHAR(50),
    actionType NVARCHAR(20),
    quantity DECIMAL(18,2),
    pricePerUnit DECIMAL(18,2),
    fees DECIMAL(18,2),
    orderType NVARCHAR(20),
    executedAt DATETIME2,
    FOREIGN KEY (holdingId) REFERENCES InvestmentHolding(id)
);

CREATE TABLE ChatMessage (
    id INT PRIMARY KEY IDENTITY(1,1),
    sessionId INT NOT NULL,
    senderType NVARCHAR(20),
    content NVARCHAR(MAX),
    sentAt DATETIME2,
    FOREIGN KEY (sessionId) REFERENCES ChatSession(id)
);

CREATE TABLE ChatAttachment (
    id INT PRIMARY KEY IDENTITY(1,1),
    messageId INT NOT NULL,
    attachmentName NVARCHAR(255),
    fileType NVARCHAR(50),
    fileSizeBytes INT,
    storageUrl NVARCHAR(255),
    FOREIGN KEY (messageId) REFERENCES ChatMessage(id)
);

CREATE TABLE AutoDeposit (
    id INT PRIMARY KEY IDENTITY(1,1),
    savingsAccountId INT NOT NULL,
    frequency NVARCHAR(50),
    amount DECIMAL(18,2),
    isActive BIT,
    nextRunDate DATE,
    FOREIGN KEY (savingsAccountId) REFERENCES SavingsAccount(id)
);

CREATE TABLE AmortizationRow (
    id INT PRIMARY KEY IDENTITY(1,1),
    loanId INT NOT NULL,
    installmentNumber INT,
    dueDate DATE,
    principalPortion DECIMAL(18,2),
    interestPortion DECIMAL(18,2),
    remainingBalance DECIMAL(18,2),
    FOREIGN KEY (loanId) REFERENCES Loan(id)
);
