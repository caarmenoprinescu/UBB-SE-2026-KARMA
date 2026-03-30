USE KarmaBankingDb;
GO

-- Add missing columns that align with the BA-44 Loan.cs model
ALTER TABLE Loan 
ADD TermInMonths INT NULL,
    StartDate DATETIME2 NULL;
GO
