DELETE FROM ChatAttachment;
DELETE FROM ChatMessage;
DELETE FROM ChatSession;
DELETE FROM InvestmentTransaction;
DELETE FROM InvestmentHolding;
DELETE FROM Portfolio;
DELETE FROM AutoDeposit;
DELETE FROM SavingsAccount;
DELETE FROM InterestLog;
DELETE FROM SavingsTransaction;
DELETE FROM AmortizationRow;
DELETE FROM Loan;
DELETE FROM LoanApplication;

INSERT INTO Loan
(userId, loanType, principal, outstandingBalance, interestRate, monthlyInstallment, remainingMonths, loanStatus, TermInMonths, StartDate)
VALUES
(1, 'Personal', 10000, 8000, 5.5, 250, 36, 'Active', 48, '2024-01-01'),
(1, 'Personal', 5000, 500, 4.2, 150, 3, 'Active', 36, '2023-01-01'),
(2, 'Mortgage', 120000, 0, 3.8, 900, 0, 'Passed', 240, '2015-06-01'),
(3, 'Mortgage', 200000, 180000, 4.1, 1200, 300, 'Active', 360, '2022-05-01'),
(2, 'Auto', 20000, 12000, 6.0, 400, 30, 'Active', 60, '2023-07-01'),
(3, 'Student', 15000, 14000, 3.5, 180, 80, 'Active', 120, '2023-09-01');

INSERT INTO AmortizationRow
(loanId, installmentNumber, dueDate, principalPortion, interestPortion, remainingBalance)
VALUES
(1, 1, '2024-02-01', 150, 100, 9850),
(1, 2, '2024-03-01', 155, 95, 9695),
(1, 3, '2024-04-01', 160, 90, 9535),
(1, 4, '2024-05-01', 165, 85, 9370);

INSERT INTO SavingsAccount
(userId, savingsType, balance, accruedInterest, apy, maturityDate, accountStatus, createdAt, updatedAt, accountName, fundingAccountId, targetAmount, targetDate)
VALUES
(1, 'Standard',     5000,  120, 0.025, NULL,         'Active', GETUTCDATE(), NULL, 'Emergency Fund', NULL, 10000, '2026-12-01'),
(1, 'GoalSavings',  3000,   75, 0.030, NULL,         'Active', GETUTCDATE(), NULL, 'Vacation Fund',  NULL, 5000,  '2026-08-01'),
(1, 'FixedDeposit', 8000,  320, 0.040, '2024-01-01', 'Active', GETUTCDATE(), NULL, 'Fixed Matured',  NULL, NULL,  NULL),
(2, 'HighYield',   15000,  500, 0.032, NULL,         'Active', GETUTCDATE(), NULL, 'High Yield',     NULL, 20000, '2027-06-01'),
(3, 'Standard',     2000,   50, 0.020, NULL,         'Closed', GETUTCDATE(), NULL, 'Old Savings',    NULL, NULL,  NULL);

INSERT INTO Portfolio
(totalValue, totalGainLoss, gainLossPercent, userId)
VALUES
(25000, 2000, 8, 1),
(10000, -500, -5, 2);

INSERT INTO InvestmentHolding
(portfolioId, ticker, assetType, quantity, avgPurchasePrice, currentPrice, unrealizedGainLoss)
VALUES
(1, 'AAPL', 'Stock', 10, 150, 180, 300),
(1, 'TSLA', 'Stock', 5, 700, 650, -250),
(2, 'BTC', 'Crypto', 0.5, 30000, 35000, 2500);

INSERT INTO InvestmentTransaction
(holdingId, ticker, actionType, quantity, pricePerUnit, fees, orderType, executedAt)
VALUES
(1, 'AAPL', 'BUY', 10, 150, 5, 'Market', GETDATE()),
(2, 'TSLA', 'BUY', 5, 700, 5, 'Market', GETDATE());

INSERT INTO ChatSession
(userId, issueCategory, sessionStatus, rating, startedAt, endedAt, feedback)
VALUES
(1, 'Loan Inquiry', 'Closed', 5, GETDATE(), GETDATE(), 'Very helpful support');

INSERT INTO ChatMessage
(sessionId, senderType, content, sentAt)
VALUES
(1, 'User', 'I need help with my loan', GETDATE()),
(1, 'Support', 'Sure, how can I assist?', GETDATE());

INSERT INTO ChatAttachment
(messageId, attachmentName, fileType, fileSizeBytes, storageUrl)
VALUES
(1, 'document.pdf', 'pdf', 1024, 'http://example.com/doc.pdf');

INSERT INTO LoanApplication
(loanType, desiredAmount, preferredTermMonths, purpose, applicationStatus, rejectionReason)
VALUES
('Personal', 8000, 24, 'Medical expenses', 'Pending', NULL),
('Mortgage', 150000, 240, 'Buy house', 'Approved', NULL),
('Auto', 15000, 60, 'Buy car', 'Rejected', 'Low credit score');

INSERT INTO AutoDeposit
(savingsAccountId, frequency, amount, isActive, nextRunDate, sourceAccountId, dayOfMonth, dayOfWeek, updatedAt)
VALUES
(1, 'Monthly', 200, 1, '2026-05-01', NULL, 1,    NULL, NULL),
(2, 'Weekly',  100, 1, '2026-04-10', NULL, NULL, 2,    NULL);

INSERT INTO SavingsTransaction
(accountId, transactionType, amount, balanceAfter, source, description, createdAt)
VALUES
(1, 'Deposit',    500,  5000, 'Manual',    'Initial deposit',   GETUTCDATE()),
(1, 'Deposit',    200,  5200, 'Recurring', 'Auto deposit',      GETUTCDATE()),
(2, 'Deposit',   1000,  3000, 'Manual',    'Goal contribution', GETUTCDATE()),
(3, 'Withdrawal', 300,  7700, 'Manual',    'Early withdrawal',  GETUTCDATE()),
(5, 'Closure',   2000,     0, 'Closure',   'Account closed',    GETUTCDATE());

INSERT INTO InterestLog
(accountId, interestAmount, balanceBefore, balanceAfter, rateApplied, periodMonth, creditedAt)
VALUES
(1, 10.42, 5000, 5010.42, 0.025, '2026-03', GETUTCDATE()),
(2,  7.50, 3000, 3007.50, 0.030, '2026-03', GETUTCDATE()),
(3, 40.00, 8000, 8040.00, 0.040, '2026-03', GETUTCDATE());

