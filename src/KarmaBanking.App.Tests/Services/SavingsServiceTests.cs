// <copyright file="SavingsServiceTests.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Tests.Services
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using KarmaBanking.App.Models;
    using KarmaBanking.App.Models.DTOs;
    using KarmaBanking.App.Repositories.Interfaces;
    using KarmaBanking.App.Services;
    using Moq;
    using Xunit;

    public class SavingsServiceTests
    {
        private const decimal DefaultAnnualPercentageYield = 0.02m;
        private readonly Mock<ISavingsRepository> savingsRepositoryMock;
        private readonly SavingsService savingsService;

        public SavingsServiceTests()
        {
            this.savingsRepositoryMock = new Mock<ISavingsRepository>();
            this.savingsService = new SavingsService(this.savingsRepositoryMock.Object);
        }

        [Fact]
        public async Task CreateAccountAsync_StandardAccountIsCreated_ReturnsCreatedAccount()
        {
            // Arrange
            var createAccountDataTransferObject = new CreateSavingsAccountDto
            {
                UserIdentificationNumber = 1,
                SavingsType = "Standard",
                AccountName = "My Savings",
                InitialDeposit = 1000m
            };

            var expectedSavingsAccount = new SavingsAccount
            {
                IdentificationNumber = 1,
                UserIdentificationNumber = 1,
                AccountName = "My Savings",
                Balance = 1000m,
                AccountStatus = "Active"
            };

            this.savingsRepositoryMock.Setup(repository => repository.CreateSavingsAccountAsync(createAccountDataTransferObject, DefaultAnnualPercentageYield))
                .ReturnsAsync(expectedSavingsAccount);
            this.savingsRepositoryMock.Setup(repository => repository.GetSavingsAccountsByUserIdAsync(1, false))
                .ReturnsAsync(new List<SavingsAccount>());

            // Act
            var actualSavingsAccount = await this.savingsService.CreateAccountAsync(createAccountDataTransferObject);

            // Assert
            Assert.Equal(expectedSavingsAccount, actualSavingsAccount);
            this.savingsRepositoryMock.Verify(repository => repository.CreateSavingsAccountAsync(createAccountDataTransferObject, DefaultAnnualPercentageYield), Times.Once);
        }

        [Fact]
        public async Task CreateAccountAsync_UserHasMaxActiveAccounts_ThrowsInvalidOperationException()
        {
            // Arrange
            int userIdentificationNumber = 1;
            var activeAccountsList = new List<SavingsAccount>();
            for (int i = 0; i < 5; i++)
            {
                activeAccountsList.Add(new SavingsAccount { AccountStatus = "Active" });
            }

            this.savingsRepositoryMock.Setup(repository => repository.GetSavingsAccountsByUserIdAsync(userIdentificationNumber, false))
                .ReturnsAsync(activeAccountsList);

            var createAccountDataTransferObject = new CreateSavingsAccountDto { UserIdentificationNumber = userIdentificationNumber, SavingsType = "Standard" };

            // Act & Assert
            var validationException = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await this.savingsService.CreateAccountAsync(createAccountDataTransferObject));

            Assert.Equal("You cannot have more than 5 active savings accounts.", validationException.Message);
            this.savingsRepositoryMock.Verify(repository => repository.CreateSavingsAccountAsync(It.IsAny<CreateSavingsAccountDto>(), It.IsAny<decimal>()), Times.Never);
        }

        [Fact]
        public async Task DepositAsync_ValidDeposit_ReturnsDepositResponse()
        {
            // Arrange
            int userIdentificationNumber = 1;
            int accountIdentificationNumber = 1;
            decimal depositAmountValue = 100m;
            string fundingSourceDescription = "Source";

            var existingAccountInstance = new SavingsAccount
            {
                IdentificationNumber = accountIdentificationNumber,
                UserIdentificationNumber = userIdentificationNumber,
                AccountStatus = "Active"
            };
            var expectedDepositResponse = new DepositResponseDto { NewBalance = 100m };

            this.savingsRepositoryMock.Setup(repository => repository.GetSavingsAccountsByUserIdAsync(userIdentificationNumber, true))
                .ReturnsAsync(new List<SavingsAccount> { existingAccountInstance });
            this.savingsRepositoryMock.Setup(repository => repository.DepositAsync(accountIdentificationNumber, depositAmountValue, fundingSourceDescription))
                .ReturnsAsync(expectedDepositResponse);

            // Act
            var actualDepositResponse = await this.savingsService.DepositAsync(accountIdentificationNumber, depositAmountValue, fundingSourceDescription, userIdentificationNumber);

            // Assert
            Assert.Equal(expectedDepositResponse, actualDepositResponse);
        }
    }
}