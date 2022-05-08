﻿using ProjectVFront.Crosscutting.Dtos;

namespace ProjectVFront.Application.Services;
public interface ITransactionService
{
    Task<IEnumerable<TransactionCategoryDto>> GetAllTransactionsWithCategoryInfo(string userId, GetTransactionsRequestDto dto);
    Task<TransactionsSummaryDto> GetSummary(string userId, GetTransactionsSummaryRequestDto dto);
    Task<IEnumerable<TransactionsSumGroupByCategoryDto>> GetTransactionsSumGroupByCategory(string userId, GetTransactionsRequestDto dto);
    Task<TransactionDto> Add(string userId, AddTransactionRequestDto dto);
    Task<TransactionDto> Edit(string userId, EditTransactionRequestDto dto);
    Task<TransactionDto> Delete(string userId, int transactionId);
}
