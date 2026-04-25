// <copyright file="ChatCategoryServiceTests.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Tests.Services
{
    using KarmaBanking.App.Services;
    using Xunit;

    public class ChatCategoryServiceTests
    {
        [Theory]
        [InlineData("I forgot my password")]
        [InlineData("PASSWORD reset please")]
        [InlineData("PassWord")]
        public void InferCategory_ContainsPassword_ReturnsAccount(string question)
        {
            var service = new ChatCategoryService();
            var result = service.InferCategory(question);
            Assert.Equal("Account", result);
        }

        [Theory]
        [InlineData("I lost my card")]
        [InlineData("CARD delivery status")]
        public void InferCategory_ContainsCard_ReturnsCards(string question)
        {
            var service = new ChatCategoryService();
            var result = service.InferCategory(question);
            Assert.Equal("Cards", result);
        }

        [Theory]
        [InlineData("How to transfer money?")]
        [InlineData("Wire TRANSFER")]
        public void InferCategory_ContainsTransfer_ReturnsTransfers(string question)
        {
            var service = new ChatCategoryService();
            var result = service.InferCategory(question);
            Assert.Equal("Transfers", result);
        }

        [Theory]
        [InlineData("The app is crashing, technical issue")]
        [InlineData("TECHNICAL support")]
        public void InferCategory_ContainsTechnical_ReturnsTechnicalIssue(string question)
        {
            var service = new ChatCategoryService();
            var result = service.InferCategory(question);
            Assert.Equal("Technical Issue", result);
        }

        [Theory]
        [InlineData("Hello there")]
        [InlineData("What are your branch hours?")]
        [InlineData("")]
        public void InferCategory_NoMatch_ReturnsOther(string question)
        {
            var service = new ChatCategoryService();
            var result = service.InferCategory(question);
            Assert.Equal("Other", result);
        }
    }
}