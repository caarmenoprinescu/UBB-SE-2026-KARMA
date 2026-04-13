
EXEC sp_rename 'Loan.TermInMonths', 'termInMonths', 'COLUMN';
EXEC sp_rename 'Loan.StartDate', 'startDate', 'COLUMN';

ALTER TABLE LoanApplication 
ADD userId INT NOT NULL DEFAULT 0;

