using System;

namespace MySubjectProject.Exceptions
{
    public class PaymentNotFoundException : Exception
    {
        public PaymentNotFoundException(int paymentId) 
            : base($"Payment with id {paymentId} not found")
        {
        }

        public PaymentNotFoundException(string message) 
            : base(message)
        {
        }
    }
}

