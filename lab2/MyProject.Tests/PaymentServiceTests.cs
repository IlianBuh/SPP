using System;
using System.Linq;
using System.Threading.Tasks;
using MyTestFramework;
using MyTestFramework.Attributes;
using MySubjectProject.Services;
using MySubjectProject.Repositories;
using MySubjectProject.Payment;
using MySubjectProject.Exceptions;

namespace MyProject.Tests
{
    [TestClass]
    public class PaymentServiceTests
    {
        private PaymentService paymentService;
        private IPaymentRepository paymentRepository;
        private IPaymentGateway paymentGateway;

        [TestInitialize]
        private void Setup()
        {
            paymentRepository = new PaymentRepository();
            paymentGateway = new PaymentGateway();
            paymentService = new PaymentService(paymentRepository, paymentGateway);
        }

        [TestCleanup]
        private void Cleanup()
        {
            paymentService.Clear();
        }

        [TestMethod]
        public async Task ProcessPaymentAsync_WithCard_ShouldSucceed()
        {
            var payment = await paymentService.ProcessPaymentAsync(1, 100m, "card");
            Assert.IsTrue(payment.IsSuccessful);
            Assert.AreEqual(100m, payment.Amount);
            Assert.AreEqual("card", payment.PaymentMethod);
        }

        [TestMethod]
        public async Task ProcessPaymentAsync_WithWallet_ShouldSucceed()
        {
            var payment = await paymentService.ProcessPaymentAsync(1, 50m, "wallet");
            Assert.IsTrue(payment.IsSuccessful);
        }

        [TestMethod]
        public async Task ProcessPaymentAsync_WithCash_ShouldThrowPaymentFailedException()
        {
            await Assert.ThrowsAsync<PaymentFailedException>(() => paymentService.ProcessPaymentAsync(1, 100m, "cash"));
        }

        [TestMethod]
        public async Task ProcessPaymentAsync_WithInvalidRideId_ShouldThrowArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => paymentService.ProcessPaymentAsync(0, 100m, "card"));
        }

        [TestMethod]
        public async Task ProcessPaymentAsync_WithInvalidAmount_ShouldThrowArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => paymentService.ProcessPaymentAsync(1, -10m, "card"));
        }

        [TestMethod]
        public async Task ProcessPaymentAsync_WithEmptyPaymentMethod_ShouldThrowArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => paymentService.ProcessPaymentAsync(1, 100m, ""));
        }

        [TestMethod]
        public async Task GetPaymentById_ShouldReturnPayment()
        {
            var payment = await paymentService.ProcessPaymentAsync(1, 100m, "card");
            var retrieved = paymentService.GetPaymentById(payment.Id);
            Assert.AreEqual(payment.Id, retrieved.Id);
            Assert.AreEqual(payment.Amount, retrieved.Amount);
        }

        [TestMethod]
        public void GetPaymentById_WithInvalidId_ShouldThrowPaymentNotFoundException()
        {
            Assert.Throws<PaymentNotFoundException>(() => paymentService.GetPaymentById(999));
        }

        [TestMethod]
        public async Task GetPaymentsByRideId_ShouldReturnAllPayments()
        {
            await paymentService.ProcessPaymentAsync(1, 100m, "card");
            await paymentService.ProcessPaymentAsync(1, 50m, "wallet");
            await paymentService.ProcessPaymentAsync(2, 200m, "card");

            var payments = paymentService.GetPaymentsByRideId(1);
            Assert.AreEqual(2, payments.Count);
        }

        [TestMethod]
        public async Task GetTotalRevenue_ShouldReturnSumOfSuccessfulPayments()
        {
            await paymentService.ProcessPaymentAsync(1, 100m, "card");
            await paymentService.ProcessPaymentAsync(2, 50m, "wallet");
            
            try
            {
                await paymentService.ProcessPaymentAsync(3, 200m, "cash");
            }
            catch (PaymentFailedException)
            {
            }

            var revenue = paymentService.GetTotalRevenue();
            Assert.AreEqual(150m, revenue);
        }

        [TestMethod]
        public void ProcessPaymentAsync_WithCash_ShouldNotSucceed()
        {
            try
            {
                paymentService.ProcessPaymentAsync(1, 100m, "cash").GetAwaiter().GetResult();
                Assert.Fail("Expected PaymentFailedException was not thrown");
            }
            catch (PaymentFailedException)
            {
            }
        }
    }
}
