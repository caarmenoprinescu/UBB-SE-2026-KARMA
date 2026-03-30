USE KarmaBankingDb;

ALTER TABLE SavingsAccount
ADD
    accountName NVARCHAR(100) NULL,
    fundingAccountId INT NULL,
    targetAmount DECIMAL(18,2) NULL,
    targetDate DATE NULL;
GO
