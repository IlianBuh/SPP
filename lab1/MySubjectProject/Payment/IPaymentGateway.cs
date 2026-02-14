using System.Threading.Tasks;

namespace MySubjectProject.Payment
{
    public interface IPaymentGateway
    {
        Task<PaymentResult> ProcessPaymentAsync(decimal amount, string paymentMethod, string transactionId);
    }

    public class PaymentResult
    {
        public bool IsSuccessful { get; set; }
        public string TransactionId { get; set; }
        public string ErrorMessage { get; set; }
    }
}

