using System;

namespace MySubjectProject.Exceptions
{
    public class PaymentFailedException : Exception
    {
        public PaymentFailedException(string reason) 
            : base($"Payment failed: {reason}")
        {
        }

        public PaymentFailedException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }
}

