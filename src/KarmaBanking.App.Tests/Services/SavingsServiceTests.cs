using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NSubstitute;
using KarmaBanking.App.Repositories.Interfaces;
using Xunit;
using KarmaBanking.App.Models.DTOs;
using KarmaBanking.App.Services;
using KarmaBanking.App.Models;
using KarmaBanking.App.Models.Enums;

namespace KarmaBanking.App.Tests.Services
{
    public class SavingsServiceTests
    {
        private const decimal FIXED_DEPOSIT_APY = 0.04m;
        private const decimal GOAL_SAVINGS_APY = 0.03m;
        private const decimal HIGH_YIELD_APY = 0.03m;
        private const decimal DEFAULT_APY = 0.02m;

        private const decimal DECIMAL_EARLY_CLOSURE_PENALTY = 0.02m;
        private const decimal DECIMAL_EARLY_WITHDRAWAL_PENALTY = 0.02m;

        [Fact]
        public async Task CreateAccountAsync_StandardAccountIsCreated_ReturnsCreatedAccount()
        {
            var repository = Substitute.For<ISavingsRepository>();
            var service = new SavingsService(repository);

            var inputDto = new CreateSavingsAccountDto
            {
                UserIdentificationNumber = 1,
                SavingsType = "Standard",
                AccountName = "My Savings",
                InitialDeposit = 1000m,
                FundingAccountId = 123,
            };

            var outputDto = new SavingsAccount
            {
                IdentificationNumber = 1,
                UserIdentificationNumber = inputDto.UserIdentificationNumber,
                AccountName = inputDto.AccountName,
                Balance = inputDto.InitialDeposit,
                SavingsType = inputDto.SavingsType,
                AccountStatus = "Active"
            };

            repository.CreateSavingsAccountAsync(inputDto, DEFAULT_APY).Returns(Task.FromResult(outputDto));
            repository.GetSavingsAccountsByUserIdAsync(inputDto.UserIdentificationNumber, false).Returns(Task.FromResult(new List<SavingsAccount>()));

            Assert.Equal(outputDto, await service.CreateAccountAsync(inputDto));
            await repository.Received(1).CreateSavingsAccountAsync(inputDto, DEFAULT_APY);
        }

        [Fact]
        public async Task CreateAccountAsync_GoalSavingsAccountIsCreated_ReturnsCreatedAccount()
        {
            var repository = Substitute.For<ISavingsRepository>();
            var service = new SavingsService(repository);

            var inputDto = new CreateSavingsAccountDto
            {
                UserIdentificationNumber = 1,
                SavingsType = "GoalSavings",
                AccountName = "My Savings",
                InitialDeposit = 1000m,
                FundingAccountId = 123,
                TargetAmount = 5000m,
                TargetDate = DateTime.UtcNow.AddDays(30)
            };

            var outputDto = new SavingsAccount
            {
                IdentificationNumber = 1,
                UserIdentificationNumber = inputDto.UserIdentificationNumber,
                AccountName = inputDto.AccountName,
                Balance = inputDto.InitialDeposit,
                SavingsType = inputDto.SavingsType,
                AccountStatus = "Active",
                TargetAmount = 5000m,
                TargetDate = DateTime.UtcNow.AddDays(30)
            };

            repository.CreateSavingsAccountAsync(inputDto, GOAL_SAVINGS_APY).Returns(Task.FromResult(outputDto));
            repository.GetSavingsAccountsByUserIdAsync(inputDto.UserIdentificationNumber, false).Returns(Task.FromResult(new List<SavingsAccount>()));

            Assert.Equal(outputDto, await service.CreateAccountAsync(inputDto));
            await repository.Received(1).CreateSavingsAccountAsync(inputDto, GOAL_SAVINGS_APY);
        }

        [Fact]
        public async Task CreateAccountAsync_FixedDepositAccountIsCreated_ReturnsCreatedAccount()
        {
            var repository = Substitute.For<ISavingsRepository>();
            var service = new SavingsService(repository);

            var inputDto = new CreateSavingsAccountDto
            {
                UserIdentificationNumber = 1,
                SavingsType = "FixedDeposit",
                AccountName = "My Savings",
                InitialDeposit = 1000m,
                FundingAccountId = 123,
            };

            var outputDto = new SavingsAccount
            {
                IdentificationNumber = 1,
                UserIdentificationNumber = inputDto.UserIdentificationNumber,
                AccountName = inputDto.AccountName,
                Balance = inputDto.InitialDeposit,
                SavingsType = inputDto.SavingsType,
                AccountStatus = "Active"
            };

            repository.CreateSavingsAccountAsync(inputDto, FIXED_DEPOSIT_APY).Returns(Task.FromResult(outputDto));
            repository.GetSavingsAccountsByUserIdAsync(inputDto.UserIdentificationNumber, false).Returns(Task.FromResult(new List<SavingsAccount>()));

            Assert.Equal(outputDto, await service.CreateAccountAsync(inputDto));
            await repository.Received(1).CreateSavingsAccountAsync(inputDto, FIXED_DEPOSIT_APY);
        }

        [Fact]
        public async Task CreateAccountAsync_HighYieldAccountIsCreated_ReturnsCreatedAccount()
        {
            var repository = Substitute.For<ISavingsRepository>();
            var service = new SavingsService(repository);

            var inputDto = new CreateSavingsAccountDto
            {
                UserIdentificationNumber = 1,
                SavingsType = "HighYield",
                AccountName = "My Savings",
                InitialDeposit = 1000m,
                FundingAccountId = 123,
            };

            var outputDto = new SavingsAccount
            {
                IdentificationNumber = 1,
                UserIdentificationNumber = inputDto.UserIdentificationNumber,
                AccountName = inputDto.AccountName,
                Balance = inputDto.InitialDeposit,
                SavingsType = inputDto.SavingsType,
                AccountStatus = "Active"
            };

            repository.CreateSavingsAccountAsync(inputDto, HIGH_YIELD_APY).Returns(Task.FromResult(outputDto));
            repository.GetSavingsAccountsByUserIdAsync(inputDto.UserIdentificationNumber, false).Returns(Task.FromResult(new List<SavingsAccount>()));

            Assert.Equal(outputDto, await service.CreateAccountAsync(inputDto));
            await repository.Received(1).CreateSavingsAccountAsync(inputDto, HIGH_YIELD_APY);
        }

        [Fact]
        public async Task CreateAccountAsync_UserHasMaxActiveAccounts_ThrowsInvalidOperationException()
        {
            var repository = Substitute.For<ISavingsRepository>();
            var service = new SavingsService(repository);
            var userId = 1;

            var activeAccounts = new List<SavingsAccount>
            {
                new SavingsAccount { IdentificationNumber = 1, UserIdentificationNumber = userId, AccountStatus = "Active" },
                new SavingsAccount { IdentificationNumber = 2, UserIdentificationNumber = userId, AccountStatus = "Active" },
                new SavingsAccount { IdentificationNumber = 3, UserIdentificationNumber = userId, AccountStatus = "Active" },
                new SavingsAccount { IdentificationNumber = 4, UserIdentificationNumber = userId, AccountStatus = "Active" },
                new SavingsAccount { IdentificationNumber = 5, UserIdentificationNumber = userId, AccountStatus = "Active" }
            };
            repository.GetSavingsAccountsByUserIdAsync(userId, false).Returns(Task.FromResult(activeAccounts));

            var dto = new CreateSavingsAccountDto { UserIdentificationNumber = userId, SavingsType = "Standard" };

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await service.CreateAccountAsync(dto));
            Assert.Equal("You cannot have more than 5 active savings accounts.", ex.Message);
            await repository.DidNotReceive().CreateSavingsAccountAsync(Arg.Any<CreateSavingsAccountDto>(), Arg.Any<decimal>());
        }

        [Fact]
        public async Task CreateAccountAsync_GoalSavingsWithoutTargetDate_ThrowsArgumentException()
        {
            var repository = Substitute.For<ISavingsRepository>();
            var service = new SavingsService(repository);
            var dto = new CreateSavingsAccountDto
            {
                UserIdentificationNumber = 1,
                SavingsType = "GoalSavings",
                AccountName = "My Goal Savings",
                InitialDeposit = 1000m,
                FundingAccountId = 123,
                TargetAmount = 5000m
            };

            repository.GetSavingsAccountsByUserIdAsync(dto.UserIdentificationNumber, false).Returns(Task.FromResult(new List<SavingsAccount>()));

            var ex = await Assert.ThrowsAsync<ArgumentException>(async () => await service.CreateAccountAsync(dto));
            Assert.Equal("GoalSavings accounts require a target date.", ex.Message);
            await repository.DidNotReceive().CreateSavingsAccountAsync(Arg.Any<CreateSavingsAccountDto>(), Arg.Any<decimal>());
        }

        [Fact]
        public async Task CreateAccountAsync_GoalSavingsWithPastTargetDate_ThrowsArgumentException()
        {
            var repository = Substitute.For<ISavingsRepository>();
            var service = new SavingsService(repository);
            var dto = new CreateSavingsAccountDto
            {
                UserIdentificationNumber = 1,
                SavingsType = "GoalSavings",
                AccountName = "My Goal Savings",
                InitialDeposit = 1000m,
                FundingAccountId = 123,
                TargetAmount = 5000m,
                TargetDate = DateTime.UtcNow.AddDays(-1)
            };

            repository.GetSavingsAccountsByUserIdAsync(dto.UserIdentificationNumber, false).Returns(Task.FromResult(new List<SavingsAccount>()));

            var ex = await Assert.ThrowsAsync<ArgumentException>(async () => await service.CreateAccountAsync(dto));
            Assert.Equal("Target date must be in the future.", ex.Message);
            await repository.DidNotReceive().CreateSavingsAccountAsync(Arg.Any<CreateSavingsAccountDto>(), Arg.Any<decimal>());
        }

        [Fact]
        public async Task CreateAccountAsync_GoalSavingsWithoutTargetAmount_ThrowsArgumentException()
        {
            var repository = Substitute.For<ISavingsRepository>();
            var service = new SavingsService(repository);
            var dto = new CreateSavingsAccountDto
            {
                UserIdentificationNumber = 1,
                SavingsType = "GoalSavings",
                AccountName = "My Goal Savings",
                InitialDeposit = 1000m,
                FundingAccountId = 123,
                TargetDate = DateTime.UtcNow.AddDays(30)
            };

            repository.GetSavingsAccountsByUserIdAsync(dto.UserIdentificationNumber, false).Returns(Task.FromResult(new List<SavingsAccount>()));

            var ex = await Assert.ThrowsAsync<ArgumentException>(async () => await service.CreateAccountAsync(dto));
            Assert.Equal("GoalSavings accounts require a positive target amount.", ex.Message);
            await repository.DidNotReceive().CreateSavingsAccountAsync(Arg.Any<CreateSavingsAccountDto>(), Arg.Any<decimal>());
        }

        [Fact]
        public async Task CreateAccountAsync_GoalSavingsWithNegativeTargetAmount_ThrowsArgumentException()
        {
            var repository = Substitute.For<ISavingsRepository>();
            var service = new SavingsService(repository);
            var dto = new CreateSavingsAccountDto
            {
                UserIdentificationNumber = 1,
                SavingsType = "GoalSavings",
                AccountName = "My Goal Savings",
                InitialDeposit = 1000m,
                FundingAccountId = 123,
                TargetDate = DateTime.UtcNow.AddDays(30),
                TargetAmount = -5000m
            };

            repository.GetSavingsAccountsByUserIdAsync(dto.UserIdentificationNumber, false).Returns(Task.FromResult(new List<SavingsAccount>()));

            var ex = await Assert.ThrowsAsync<ArgumentException>(async () => await service.CreateAccountAsync(dto));
            Assert.Equal("GoalSavings accounts require a positive target amount.", ex.Message);
            await repository.DidNotReceive().CreateSavingsAccountAsync(Arg.Any<CreateSavingsAccountDto>(), Arg.Any<decimal>());
        }

        [Fact]
        public async Task GetAccountsAsync_NegativeUserId_ThrowsArgumentException()
        {
            var repository = Substitute.For<ISavingsRepository>();
            var service = new SavingsService(repository);

            var ex = await Assert.ThrowsAsync<ArgumentException>(async () => await service.GetAccountsAsync(-1));
            Assert.Equal("User ID must be a positive integer.", ex.Message);
            await repository.DidNotReceive().GetSavingsAccountsByUserIdAsync(Arg.Any<int>(), Arg.Any<bool>());
        }

        [Fact]
        public async Task GetAccountsAsync_ValidUserId_ReturnsAccounts()
        {
            var repository = Substitute.For<ISavingsRepository>();
            var service = new SavingsService(repository);

            var userId = 1;
            var accounts = new List<SavingsAccount>
            {
                new SavingsAccount { IdentificationNumber = 1, UserIdentificationNumber = userId, AccountName = "Savings 1" },
                new SavingsAccount { IdentificationNumber = 2, UserIdentificationNumber = userId, AccountName = "Savings 2" },
            };

            repository.GetSavingsAccountsByUserIdAsync(userId, false).Returns(Task.FromResult(accounts));

            var result = await service.GetAccountsAsync(userId);
            Assert.Equal(accounts, result);
            await repository.Received(1).GetSavingsAccountsByUserIdAsync(userId, false);
        }

        [Fact]
        public async Task DepositAsync_NegativeAmount_ThrowsArgumentException()
        {
            var repository = Substitute.For<ISavingsRepository>();
            var service = new SavingsService(repository);
            var accountId = 1;
            var userId = 1;

            var ex = await Assert.ThrowsAsync<ArgumentException>(async () => await service.DepositAsync(accountId, -100m, "Source", userId));
            Assert.Equal("Deposit amount must be positive.", ex.Message);
            await repository.DidNotReceive().DepositAsync(Arg.Any<int>(), Arg.Any<decimal>(), Arg.Any<string>());
        }

        [Fact]
        public async Task DepositAsync_InvalidAccountId_ThrowsInvalidOperationException()
        {
            var repository = Substitute.For<ISavingsRepository>();
            var service = new SavingsService(repository);

            var userId = 1;
            var inexistentAccountId = 999;

            repository.GetSavingsAccountsByUserIdAsync(userId, true)
                .Returns(Task.FromResult(new List<SavingsAccount> { new SavingsAccount { IdentificationNumber = 1, UserIdentificationNumber = userId } }));

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await service.DepositAsync(inexistentAccountId, 100m, "Source", userId));
            Assert.Equal("Account not found or does not belong to you.", ex.Message);
            await repository.Received(1).GetSavingsAccountsByUserIdAsync(userId, true);
        }

        [Fact]
        public async Task DepositAsync_AccountStatusClosed_ThrowsInvalidOperationException()
        {
            var repository = Substitute.For<ISavingsRepository>();
            var service = new SavingsService(repository);

            var userId = 1;
            var accountId = 1;

            repository.GetSavingsAccountsByUserIdAsync(userId, true)
                .Returns(Task.FromResult(new List<SavingsAccount> { new SavingsAccount { IdentificationNumber = accountId, UserIdentificationNumber = userId, AccountStatus = AccountStatus.Closed.ToString() } }));

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await service.DepositAsync(accountId, 100m, "Source", userId));
            Assert.Equal("Cannot deposit into a closed account.", ex.Message);
            await repository.Received(1).GetSavingsAccountsByUserIdAsync(userId, true);
        }

        [Fact]
        public async Task DepositAsync_DisplayStatusMatured_ThrowsInvalidOperationException()
        {
            var repository = Substitute.For<ISavingsRepository>();
            var service = new SavingsService(repository);

            var userId = 1;
            var accountId = 1;

            repository.GetSavingsAccountsByUserIdAsync(userId, true)
                .Returns(Task.FromResult(new List<SavingsAccount> { new SavingsAccount { IdentificationNumber = accountId, UserIdentificationNumber = userId, AccountStatus = AccountStatus.Matured.ToString() } }));

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await service.DepositAsync(accountId, 100m, "Source", userId));
            Assert.Equal("Cannot deposit into a matured account.", ex.Message);
            await repository.Received(1).GetSavingsAccountsByUserIdAsync(userId, true);
        }

        [Fact]
        public async Task DepositAsync_ValidDeposit_ReturnsDepositResponse()
        {
            var repository = Substitute.For<ISavingsRepository>();
            var service = new SavingsService(repository);

            var userId = 1;
            var accountId = 1;
            var amount = 100m;
            var source = "Source";

            var account = new SavingsAccount { IdentificationNumber = accountId, UserIdentificationNumber = userId, AccountStatus = AccountStatus.Active.ToString(), Balance = 0m };
            var expectedResponse = new DepositResponseDto { NewBalance = 100m, TransactionId = 1, Timestamp = DateTime.UtcNow };

            repository.GetSavingsAccountsByUserIdAsync(userId, true).Returns(Task.FromResult(new List<SavingsAccount> { account }));
            repository.DepositAsync(accountId, amount, source).Returns(Task.FromResult(expectedResponse));

            var result = await service.DepositAsync(accountId, amount, source, userId);
            Assert.Equal(expectedResponse, result);
            await repository.Received(1).GetSavingsAccountsByUserIdAsync(userId, true);
            await repository.Received(1).DepositAsync(accountId, amount, source);
        }

        [Fact]
        public async Task CloseAccountAsync_AccountIdNotFound_ThrowsInvalidOperationException()
        {
            var repository = Substitute.For<ISavingsRepository>();
            var service = new SavingsService(repository);

            var userId = 1;
            var accountId = 1;
            var inexistentAccountId = 999;
            var destinationAccountId = 2;

            repository.GetSavingsAccountsByUserIdAsync(userId, true)
                .Returns(Task.FromResult(new List<SavingsAccount> { new SavingsAccount { IdentificationNumber = accountId, UserIdentificationNumber = userId } }));

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await service.CloseAccountAsync(inexistentAccountId, destinationAccountId, userId));
            Assert.Equal("Account not found.", ex.Message);
            await repository.Received(1).GetSavingsAccountsByUserIdAsync(userId, true);
            await repository.DidNotReceive().CloseSavingsAccountAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<decimal>(), Arg.Any<decimal>());
        }

        [Fact]
        public async Task CloseAccountAsync_AccountAlreadyClosed_ThrowsInvalidOperationException()
        {
            var repository = Substitute.For<ISavingsRepository>();
            var service = new SavingsService(repository);

            var userId = 1;
            var accountId = 1;
            var destinationAccountId = 2;

            repository.GetSavingsAccountsByUserIdAsync(userId, true)
                .Returns(Task.FromResult(new List<SavingsAccount> { new SavingsAccount { IdentificationNumber = accountId, UserIdentificationNumber = userId, AccountStatus = AccountStatus.Closed.ToString() } }));

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await service.CloseAccountAsync(accountId, destinationAccountId, userId));
            Assert.Equal("Account already closed.", ex.Message);
            await repository.Received(1).GetSavingsAccountsByUserIdAsync(userId, true);
            await repository.DidNotReceive().CloseSavingsAccountAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<decimal>(), Arg.Any<decimal>());
        }

        [Fact]
        public async Task CloseAccountAsync_DestinationAccountNotFound_ThrowsInvalidOperationException()
        {
            var repository = Substitute.For<ISavingsRepository>();
            var service = new SavingsService(repository);

            var userId = 1;
            var accountId = 1;
            var inexistentDestinationAccountId = 999;

            repository.GetSavingsAccountsByUserIdAsync(userId, true)
                .Returns(Task.FromResult(new List<SavingsAccount> { new SavingsAccount { IdentificationNumber = accountId, UserIdentificationNumber = userId, AccountStatus = AccountStatus.Active.ToString() } }));

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await service.CloseAccountAsync(accountId, inexistentDestinationAccountId, userId));
            Assert.Equal("Destination account not found.", ex.Message);
            await repository.Received(1).GetSavingsAccountsByUserIdAsync(userId, true);
            await repository.DidNotReceive().CloseSavingsAccountAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<decimal>(), Arg.Any<decimal>());
        }

        [Fact]
        public async Task CloseAccountAsync_DestinationAccountAlreadyClosed_ThrowsInvalidOperationException()
        {
            var repository = Substitute.For<ISavingsRepository>();
            var service = new SavingsService(repository);

            var userId = 1;
            var accountId = 1;
            var destinationAccountId = 2;

            repository.GetSavingsAccountsByUserIdAsync(userId, true)
                .Returns(Task.FromResult(new List<SavingsAccount>
                {
                    new SavingsAccount
                    {
                        IdentificationNumber = accountId, UserIdentificationNumber = userId, AccountStatus = AccountStatus.Active.ToString()
                    },
                                                         new SavingsAccount { IdentificationNumber = destinationAccountId, UserIdentificationNumber = userId, AccountStatus = AccountStatus.Closed.ToString() },
                }));

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await service.CloseAccountAsync(accountId, destinationAccountId, userId));
            Assert.Equal("Cannot transfer to a closed account.", ex.Message);
            await repository.Received(1).GetSavingsAccountsByUserIdAsync(userId, true);
            await repository.DidNotReceive().CloseSavingsAccountAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<decimal>(), Arg.Any<decimal>());
        }

        [Fact]
        public async Task CloseAccountAsync_CloseFixedDepositWithPenalty_ReturnsCloseAccountResponse()
        {
            var repository = Substitute.For<ISavingsRepository>();
            var service = new SavingsService(repository);

            var userId = 1;
            var accountId = 1;
            var destinationAccountId = 2;
            var transferedAmount = 98m;

            var sourceAccount = new SavingsAccount
            {
                IdentificationNumber = accountId,
                UserIdentificationNumber = userId,
                AccountStatus = AccountStatus.Active.ToString(),
                Balance = 100m,
                SavingsType = "FixedDeposit",
                MaturityDate = DateTime.UtcNow.AddDays(30),
            };

            var destinationAccount = new SavingsAccount
            {
                IdentificationNumber = destinationAccountId,
                UserIdentificationNumber = userId,
                AccountStatus = AccountStatus.Active.ToString(),
                Balance = 100m,
            };

            var expectedResponse = new ClosureResultDto
            {
                Success = true,
                TransferredAmount = transferedAmount,
                Message = "Account closed with penalty.",
                ClosedAt = DateTime.UtcNow,
                PenaltyApplied = 2,
            };

            repository.GetSavingsAccountsByUserIdAsync(userId, true)
                .Returns(Task.FromResult(new List<SavingsAccount> { sourceAccount, destinationAccount }));
            repository.CloseSavingsAccountAsync(accountId, destinationAccountId, transferedAmount, 2).Returns(Task.FromResult(expectedResponse));

            var result = await service.CloseAccountAsync(accountId, destinationAccountId, userId);
            Assert.Equal(expectedResponse, result);
            await repository.Received(1).GetSavingsAccountsByUserIdAsync(userId, true);
            await repository.Received(1).CloseSavingsAccountAsync(accountId, destinationAccountId, transferedAmount, 2);
        }

        [Fact]
        public async Task CloseAccountAsync_CloseFixedDepositWithoutMaturityDate_ReturnsCloseAccountResponse()
        {
            var repository = Substitute.For<ISavingsRepository>();
            var service = new SavingsService(repository);

            var userId = 1;
            var accountId = 1;
            var destinationAccountId = 2;
            var transferedAmount = 100m;

            var sourceAccount = new SavingsAccount
            {
                IdentificationNumber = accountId,
                UserIdentificationNumber = userId,
                AccountStatus = AccountStatus.Active.ToString(),
                Balance = 100m,
                SavingsType = "FixedDeposit",
            };

            var destinationAccount = new SavingsAccount
            {
                IdentificationNumber = destinationAccountId,
                UserIdentificationNumber = userId,
                AccountStatus = AccountStatus.Active.ToString(),
                Balance = 100m,
            };

            var expectedResponse = new ClosureResultDto
            {
                Success = true,
                TransferredAmount = transferedAmount,
                Message = "Account closed without penalty.",
                ClosedAt = DateTime.UtcNow,
                PenaltyApplied = 0,
            };

            repository.GetSavingsAccountsByUserIdAsync(userId, true)
                .Returns(Task.FromResult(new List<SavingsAccount> { sourceAccount, destinationAccount }));
            repository.CloseSavingsAccountAsync(accountId, destinationAccountId, transferedAmount, 0).Returns(Task.FromResult(expectedResponse));

            var result = await service.CloseAccountAsync(accountId, destinationAccountId, userId);
            Assert.Equal(expectedResponse, result);
            await repository.Received(1).GetSavingsAccountsByUserIdAsync(userId, true);
            await repository.Received(1).CloseSavingsAccountAsync(accountId, destinationAccountId, transferedAmount, 0);
        }

        [Fact]
        public async Task CloseAccountAsync_CloseMaturedFixedDepositWithoutPenalty_ReturnsCloseAccountResponse()
        {
            var repository = Substitute.For<ISavingsRepository>();
            var service = new SavingsService(repository);

            var userId = 1;
            var accountId = 1;
            var destinationAccountId = 2;
            var transferedAmount = 100m;

            var sourceAccount = new SavingsAccount
            {
                IdentificationNumber = accountId,
                UserIdentificationNumber = userId,
                AccountStatus = AccountStatus.Active.ToString(),
                Balance = 100m,
                SavingsType = "FixedDeposit",
                MaturityDate = DateTime.UtcNow.AddDays(-1),
            };

            var destinationAccount = new SavingsAccount
            {
                IdentificationNumber = destinationAccountId,
                UserIdentificationNumber = userId,
                AccountStatus = AccountStatus.Active.ToString(),
                Balance = 100m,
            };

            var expectedResponse = new ClosureResultDto
            {
                Success = true,
                TransferredAmount = transferedAmount,
                Message = "Account closed without penalty.",
                ClosedAt = DateTime.UtcNow,
                PenaltyApplied = 0,
            };

            repository.GetSavingsAccountsByUserIdAsync(userId, true)
                .Returns(Task.FromResult(new List<SavingsAccount> { sourceAccount, destinationAccount }));
            repository.CloseSavingsAccountAsync(accountId, destinationAccountId, transferedAmount, 0).Returns(Task.FromResult(expectedResponse));

            var result = await service.CloseAccountAsync(accountId, destinationAccountId, userId);
            Assert.Equal(expectedResponse, result);
            await repository.Received(1).GetSavingsAccountsByUserIdAsync(userId, true);
            await repository.Received(1).CloseSavingsAccountAsync(accountId, destinationAccountId, transferedAmount, 0);
        }

        [Fact]
        public async Task CloseAccountAsync_CloseStandardAccountWithoutPenalty_ReturnsCloseAccountResponse()
        {
            var repository = Substitute.For<ISavingsRepository>();
            var service = new SavingsService(repository);

            var userId = 1;
            var accountId = 1;
            var destinationAccountId = 2;
            var transferedAmount = 100m;

            var sourceAccount = new SavingsAccount
            {
                IdentificationNumber = accountId,
                UserIdentificationNumber = userId,
                AccountStatus = AccountStatus.Active.ToString(),
                Balance = 100m,
                SavingsType = "Standard",
            };

            var destinationAccount = new SavingsAccount
            {
                IdentificationNumber = destinationAccountId,
                UserIdentificationNumber = userId,
                AccountStatus = AccountStatus.Active.ToString(),
                Balance = 100m,
            };

            var expectedResponse = new ClosureResultDto
            {
                Success = true,
                TransferredAmount = transferedAmount,
                Message = "Account closed without penalty.",
                ClosedAt = DateTime.UtcNow,
                PenaltyApplied = 0,
            };

            repository.GetSavingsAccountsByUserIdAsync(userId, true)
                .Returns(Task.FromResult(new List<SavingsAccount> { sourceAccount, destinationAccount }));
            repository.CloseSavingsAccountAsync(accountId, destinationAccountId, transferedAmount, 0).Returns(Task.FromResult(expectedResponse));

            var result = await service.CloseAccountAsync(accountId, destinationAccountId, userId);
            Assert.Equal(expectedResponse, result);
            await repository.Received(1).GetSavingsAccountsByUserIdAsync(userId, true);
            await repository.Received(1).CloseSavingsAccountAsync(accountId, destinationAccountId, transferedAmount, 0);
        }

        [Fact]
        public async Task CloseAccountAsync_CloseGoalSavingsAccountWithoutPenalty_ReturnsCloseAccountResponse()
        {
            var repository = Substitute.For<ISavingsRepository>();
            var service = new SavingsService(repository);

            var userId = 1;
            var accountId = 1;
            var destinationAccountId = 2;
            var transferedAmount = 100m;

            var sourceAccount = new SavingsAccount
            {
                IdentificationNumber = accountId,
                UserIdentificationNumber = userId,
                AccountStatus = AccountStatus.Active.ToString(),
                Balance = 100m,
                SavingsType = "GoalSavings",
            };

            var destinationAccount = new SavingsAccount
            {
                IdentificationNumber = destinationAccountId,
                UserIdentificationNumber = userId,
                AccountStatus = AccountStatus.Active.ToString(),
                Balance = 100m,
            };

            var expectedResponse = new ClosureResultDto
            {
                Success = true,
                TransferredAmount = transferedAmount,
                Message = "Account closed without penalty.",
                ClosedAt = DateTime.UtcNow,
                PenaltyApplied = 0,
            };

            repository.GetSavingsAccountsByUserIdAsync(userId, true)
                .Returns(Task.FromResult(new List<SavingsAccount> { sourceAccount, destinationAccount }));
            repository.CloseSavingsAccountAsync(accountId, destinationAccountId, transferedAmount, 0).Returns(Task.FromResult(expectedResponse));

            var result = await service.CloseAccountAsync(accountId, destinationAccountId, userId);
            Assert.Equal(expectedResponse, result);
            await repository.Received(1).GetSavingsAccountsByUserIdAsync(userId, true);
            await repository.Received(1).CloseSavingsAccountAsync(accountId, destinationAccountId, transferedAmount, 0);
        }

        [Fact]
        public async Task CloseAccountAsync_CloseHighYieldAccountWithoutPenalty_ReturnsCloseAccountResponse()
        {
            var repository = Substitute.For<ISavingsRepository>();
            var service = new SavingsService(repository);

            var userId = 1;
            var accountId = 1;
            var destinationAccountId = 2;
            var transferedAmount = 100m;

            var sourceAccount = new SavingsAccount
            {
                IdentificationNumber = accountId,
                UserIdentificationNumber = userId,
                AccountStatus = AccountStatus.Active.ToString(),
                Balance = 100m,
                SavingsType = "HighYield",
            };

            var destinationAccount = new SavingsAccount
            {
                IdentificationNumber = destinationAccountId,
                UserIdentificationNumber = userId,
                AccountStatus = AccountStatus.Active.ToString(),
                Balance = 100m,
            };

            var expectedResponse = new ClosureResultDto
            {
                Success = true,
                TransferredAmount = transferedAmount,
                Message = "Account closed without penalty.",
                ClosedAt = DateTime.UtcNow,
                PenaltyApplied = 0,
            };

            repository.GetSavingsAccountsByUserIdAsync(userId, true)
                .Returns(Task.FromResult(new List<SavingsAccount> { sourceAccount, destinationAccount }));
            repository.CloseSavingsAccountAsync(accountId, destinationAccountId, transferedAmount, 0).Returns(Task.FromResult(expectedResponse));

            var result = await service.CloseAccountAsync(accountId, destinationAccountId, userId);
            Assert.Equal(expectedResponse, result);
            await repository.Received(1).GetSavingsAccountsByUserIdAsync(userId, true);
            await repository.Received(1).CloseSavingsAccountAsync(accountId, destinationAccountId, transferedAmount, 0);
        }
    }
}
