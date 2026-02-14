using System;
using System.Threading.Tasks;

namespace MySubjectProject.Payment
{
    public class PaymentGateway : IPaymentGateway
    {
        public async Task<PaymentResult> ProcessPaymentAsync(decimal amount, string paymentMethod, string transactionId)
        {
            await Task.Delay(100);

            var result = new PaymentResult
            {
                TransactionId = transactionId
            };

            if (string.IsNullOrWhiteSpace(paymentMethod))
            {
                result.IsSuccessful = false;
                result.ErrorMessage = "Payment method cannot be empty";
                return result;
            }

            var method = paymentMethod.ToLower();
            if (method == "card" || method == "wallet")
            {
                result.IsSuccessful = true;
            }
            else if (method == "cash")
            {
                result.IsSuccessful = false;
                result.ErrorMessage = "Cash payments are not supported online";
            }
            else
            {
                result.IsSuccessful = false;
                result.ErrorMessage = $"Unsupported payment method: {paymentMethod}";
            }

            return result;
        }
    }
}

