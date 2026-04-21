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
                UserId = 1,
                SavingsType = "Standard",
                AccountName = "My Savings",
                InitialDeposit = 1000m,
                FundingAccountId = 123,
            };

            var outputDto = new SavingsAccount
            {
                Id = 1,
                UserId = inputDto.UserId,
                AccountName = inputDto.AccountName,
                Balance = inputDto.InitialDeposit,
                SavingsType = inputDto.SavingsType,
                AccountStatus = "Active"
            };

            repository.CreateSavingsAccountAsync(inputDto, DEFAULT_APY).Returns(Task.FromResult(outputDto));
            repository.GetSavingsAccountsByUserIdAsync(inputDto.UserId, false).Returns(Task.FromResult(new List<SavingsAccount>()));

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
                UserId = 1,
                SavingsType = "GoalSavings",
                AccountName = "My Savings",
                InitialDeposit = 1000m,
                FundingAccountId = 123,
                TargetAmount = 5000m,
                TargetDate = DateTime.UtcNow.AddDays(30)
            };

            var outputDto = new SavingsAccount
            {
                Id = 1,
                UserId = inputDto.UserId,
                AccountName = inputDto.AccountName,
                Balance = inputDto.InitialDeposit,
                SavingsType = inputDto.SavingsType,
                AccountStatus = "Active",
                TargetAmount = 5000m,
                TargetDate = DateTime.UtcNow.AddDays(30)
            };

            repository.CreateSavingsAccountAsync(inputDto, GOAL_SAVINGS_APY).Returns(Task.FromResult(outputDto));
            repository.GetSavingsAccountsByUserIdAsync(inputDto.UserId, false).Returns(Task.FromResult(new List<SavingsAccount>()));

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
                UserId = 1,
                SavingsType = "FixedDeposit",
                AccountName = "My Savings",
                InitialDeposit = 1000m,
                FundingAccountId = 123,
            };

            var outputDto = new SavingsAccount
            {
                Id = 1,
                UserId = inputDto.UserId,
                AccountName = inputDto.AccountName,
                Balance = inputDto.InitialDeposit,
                SavingsType = inputDto.SavingsType,
                AccountStatus = "Active"
            };

            repository.CreateSavingsAccountAsync(inputDto, FIXED_DEPOSIT_APY).Returns(Task.FromResult(outputDto));
            repository.GetSavingsAccountsByUserIdAsync(inputDto.UserId, false).Returns(Task.FromResult(new List<SavingsAccount>()));

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
                UserId = 1,
                SavingsType = "HighYield",
                AccountName = "My Savings",
                InitialDeposit = 1000m,
                FundingAccountId = 123,
            };

            var outputDto = new SavingsAccount
            {
                Id = 1,
                UserId = inputDto.UserId,
                AccountName = inputDto.AccountName,
                Balance = inputDto.InitialDeposit,
                SavingsType = inputDto.SavingsType,
                AccountStatus = "Active"
            };

            repository.CreateSavingsAccountAsync(inputDto, HIGH_YIELD_APY).Returns(Task.FromResult(outputDto));
            repository.GetSavingsAccountsByUserIdAsync(inputDto.UserId, false).Returns(Task.FromResult(new List<SavingsAccount>()));

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
                new SavingsAccount { Id = 1, UserId = userId, AccountStatus = "Active" },
                new SavingsAccount { Id = 2, UserId = userId, AccountStatus = "Active" },
                new SavingsAccount { Id = 3, UserId = userId, AccountStatus = "Active" },
                new SavingsAccount { Id = 4, UserId = userId, AccountStatus = "Active" },
                new SavingsAccount { Id = 5, UserId = userId, AccountStatus = "Active" }
            };
            repository.GetSavingsAccountsByUserIdAsync(userId, false).Returns(Task.FromResult(activeAccounts));

            var dto = new CreateSavingsAccountDto { UserId = userId, SavingsType = "Standard" };

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
                UserId = 1,
                SavingsType = "GoalSavings",
                AccountName = "My Goal Savings",
                InitialDeposit = 1000m,
                FundingAccountId = 123,
                TargetAmount = 5000m
            };

            repository.GetSavingsAccountsByUserIdAsync(dto.UserId, false).Returns(Task.FromResult(new List<SavingsAccount>()));

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
                UserId = 1,
                SavingsType = "GoalSavings",
                AccountName = "My Goal Savings",
                InitialDeposit = 1000m,
                FundingAccountId = 123,
                TargetAmount = 5000m,
                TargetDate = DateTime.UtcNow.AddDays(-1)
            };

            repository.GetSavingsAccountsByUserIdAsync(dto.UserId, false).Returns(Task.FromResult(new List<SavingsAccount>()));

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
                UserId = 1,
                SavingsType = "GoalSavings",
                AccountName = "My Goal Savings",
                InitialDeposit = 1000m,
                FundingAccountId = 123,
                TargetDate = DateTime.UtcNow.AddDays(30)
            };

            repository.GetSavingsAccountsByUserIdAsync(dto.UserId, false).Returns(Task.FromResult(new List<SavingsAccount>()));

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
                UserId = 1,
                SavingsType = "GoalSavings",
                AccountName = "My Goal Savings",
                InitialDeposit = 1000m,
                FundingAccountId = 123,
                TargetDate = DateTime.UtcNow.AddDays(30),
                TargetAmount = -5000m
            };

            repository.GetSavingsAccountsByUserIdAsync(dto.UserId, false).Returns(Task.FromResult(new List<SavingsAccount>()));

            var ex = await Assert.ThrowsAsync<ArgumentException>(async () => await service.CreateAccountAsync(dto));
            Assert.Equal("GoalSavings accounts require a positive target amount.", ex.Message);
            await repository.DidNotReceive().CreateSavingsAccountAsync(Arg.Any<CreateSavingsAccountDto>(), Arg.Any<decimal>());
        }
    }
}
