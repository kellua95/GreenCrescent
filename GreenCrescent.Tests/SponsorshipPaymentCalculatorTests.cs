using GreenCrescent.Core.Services;

namespace GreenCrescent.Tests
{
    public sealed class SponsorshipPaymentCalculatorTests
    {
        [Fact]
        public void CalculateMonths_InvalidMultiple_ThrowsException()
        {
            Action action = () =>
            {
                _ = SponsorshipPaymentCalculator.CalculateMonths(
                    100m,
                    [25m, 30m]);
            };

            var exception = Assert.Throws<InvalidOperationException>(action);

            Assert.Contains("مضاعفات", exception.Message);
        }

        [Fact]
        public void CalculateMonths_NoActiveSponsorships_ThrowsException()
        {
            Action action = () =>
            {
                _ = SponsorshipPaymentCalculator.CalculateMonths(
                    100m,
                    []);
            };

            Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void CalculateMonths_ZeroPayment_ThrowsException()
        {
            Action action = () =>
            {
                _ = SponsorshipPaymentCalculator.CalculateMonths(
                    0m,
                    [25m]);
            };

            Assert.Throws<ArgumentOutOfRangeException>(action);
        }
        [Fact]
        public void CalculateMonths_OneSponsorship_ReturnsCorrectMonths()
        {
            var result = SponsorshipPaymentCalculator.CalculateMonths(
                100m,
                [25m]);

            Assert.Equal(4, result);
        }

        [Fact]
        public void CalculateMonths_MultipleSponsorships_ReturnsCorrectMonths()
        {
            var result = SponsorshipPaymentCalculator.CalculateMonths(
                220m,
                [25m, 30m]);

            Assert.Equal(4, result);
        }
    }
}
