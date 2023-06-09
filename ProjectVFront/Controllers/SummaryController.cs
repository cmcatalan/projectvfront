﻿using AutoMapper;
using Flurl.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectVFront.Application.Services;
using ProjectVFront.Crosscutting.Dtos;
using ProjectVFront.Crosscutting.Utils.enums;
using ProjectVFront.WebClient.ViewModels;
using System.Diagnostics;
using System.Text.Json;

namespace ProjectVFront.Controllers
{
    [Authorize]
    public class SummaryController : Controller
    {
        private readonly ILogger<AuthController> _logger;
        private readonly ITransactionService _transactionService;
        private readonly ICategoryService _categoryService;
        private readonly IMapper _mapper;
        private readonly IFormatProvider _formatProvider;

        public SummaryController(
            ILogger<AuthController> logger,
            ITransactionService transactionService,
            ICategoryService categoryService,
            IMapper mapper,
            IFormatProvider formatProvider)
        {
            _logger = logger;
            _transactionService = transactionService;
            _categoryService = categoryService;
            _mapper = mapper;
            _formatProvider = formatProvider;
        }

        public async Task<IActionResult> Index([FromQuery] int? year, int? month)
        {
            var viewmodel = new SummaryViewModel(_formatProvider)
            {
                Categories = new List<CategoryDto>(),
                TransactionsSumGroups = new List<TransactionsSumGroupByCategoryDto>()
            };
            try
            {
                var now = DateTime.Now;
                var from = new DateTime(year ?? now.Year, month ?? now.Month, 1);
                var lastDayOfMonth = from.AddMonths(1).AddDays(-1);
                var to = new DateTime(lastDayOfMonth.Year, lastDayOfMonth.Month, lastDayOfMonth.Day, 23, 59, 59);

                var summaryRequest = new GetTransactionsSummaryRequestDto(from, to);
                var summary = await _transactionService.GetSummary(summaryRequest);

                var groupedRequest = new GetTransactionsRequestDto(from, to, null);
                var transactionsSumGroupByCategory = await _transactionService.GetTransactionsSumGroupByCategory(groupedRequest);

                var beforeMonth = from.AddMonths(-1);
                var afterMonth = from.AddMonths(1);

                ViewBag.BeforeMonth = beforeMonth.Month;
                ViewBag.BeforeYear = beforeMonth.Year;

                ViewBag.AfterMonth = afterMonth.Month;
                ViewBag.AfterYear = afterMonth.Year;

                ViewBag.YearNum = from.Year;
                ViewBag.MonthNum = from.Month;


                ViewBag.Year = from.ToString("yyyy", _formatProvider);
                ViewBag.Month = from.ToString("MMMM", _formatProvider);
                ViewBag.Summary = new
                {
                    incomes = summary.Incomes.ToString("0.00", _formatProvider),
                    expenses = summary.Expenses.ToString("0.00", _formatProvider),
                    total = summary.Total.ToString("0.00", _formatProvider),
                    totalValue = summary.Total
                };

                var labels = new List<string>();
                var data = new List<double>();
                var backgroundColors = new List<string>();
                var borderColors = new List<string>();

                foreach (var group in transactionsSumGroupByCategory)
                {
                    labels.Add(group.CategoryName);
                    data.Add(group.TransactionsSum);
                    backgroundColors.Add(group.CategoryType == CategoryType.Expense.ToString() ? "rgba(255, 99, 132, 0.2)" : "rgba(75, 192, 192, 0.2)");
                    borderColors.Add(group.CategoryType == CategoryType.Expense.ToString() ? "rgba(255, 99, 132)" : "rgb(75, 192, 192)");
                }

                ViewBag.TransactionsSumLabels = JsonSerializer.Serialize(labels);
                ViewBag.TransactionsSumData = JsonSerializer.Serialize(data);
                ViewBag.BackgroundColors = JsonSerializer.Serialize(backgroundColors);
                ViewBag.BorderColors = JsonSerializer.Serialize(borderColors);

                viewmodel.Categories = await _categoryService.GetAllCategoriesAsync();
                viewmodel.TransactionsSumGroups = transactionsSumGroupByCategory;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return View(viewmodel);
        }

        [HttpPost]
        public async Task<IActionResult> AddTransaction(SummaryViewModel viewmodel)
        {
            try
            {
                await _transactionService.Add(viewmodel.AddTransactionRequestDto);
            }
            catch (FlurlHttpException ex)
            {
                TempData["Error"] = await GetErrorMessageAsync(ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return Redirect("index");
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private async Task<string> GetErrorMessageAsync(FlurlHttpException ex)
        {
            if (ex.StatusCode == 400)
            {
                var httpError = await ex.GetResponseJsonAsync<HttpError>();
                return httpError.Message;
            }

            throw ex;
        }
    }
}