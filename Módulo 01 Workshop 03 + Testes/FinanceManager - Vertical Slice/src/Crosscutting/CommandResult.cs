using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace FinanceManager.Crosscutting;

public record struct ErrorMessage(string Code, string Message);