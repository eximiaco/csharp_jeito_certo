using System;
using System.Threading.Tasks;
using Gymerp.Application.Models;
using Gymerp.Domain.Entities;

namespace Gymerp.Application.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentResult> ProcessAsync(Enrollment enrollment);
    }
} 