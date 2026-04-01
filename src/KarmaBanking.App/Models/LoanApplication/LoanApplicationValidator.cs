using System;

public class LoanApplicationValidator
{
    public void Validate(LoanApplicationRequest request)
    {
        if (request == null)
            throw new Exception("Request cannot be null");

        if (request.desiredAmount <= 0)
            throw new Exception("Desired amount must be greater than 0");

        if (!Enum.IsDefined(typeof(LoanType),request.loanType))
            throw new Exception("Invalid Loan Type");

        if (request.preferredTermMonths <= 0)
            throw new Exception("Term must be greater than 0");

        if (string.IsNullOrWhiteSpace(request.purpose))
            throw new Exception("Purpose is required");
    }
}