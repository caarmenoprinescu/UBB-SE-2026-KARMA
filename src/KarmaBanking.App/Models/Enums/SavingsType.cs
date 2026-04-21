// <copyright file="SavingsType.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Models.Enums
{
    /// <summary>
    /// Represents the types of savings accounts available in the application.
    /// </summary>
    public enum SavingsType
    {
        /// <summary>
        /// Represents the standard savings account type with a default APY. 
        /// This is the most basic type of savings account, suitable for general saving purposes without specific goals or restrictions.
        /// </summary>
        Standard,

        /// <summary>
        /// Represents a goal-oriented savings account type designed for users who want to save towards a specific financial goal.
        /// </summary>
        GoalSavings,

        /// <summary>
        /// Represents a fixed deposit savings account type that typically offers a higher APY in exchange for locking funds for a predetermined period.
        /// </summary>
        FixedDeposit,

        /// <summary>
        /// Represents a high-yield savings account type that offers a higher APY compared to standard savings accounts, usually with certain conditions or requirements.
        /// </summary>
        HighYield,
    }
}
