using System.Security.Cryptography;
using System.Text;

namespace Antital.Test.Helpers;

public static class PaystackTestHelper
{
    public const string TestSecretKey = "paystack_test_secret_for_hmac";

    public static string ComputeSignature(string payload, string secret = TestSecretKey)
    {
        var hash = HMACSHA512.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string BuildChargeSuccessPayload(string reference, int amountKobo, string channel = "card") =>
        $$"""
          {
            "event": "charge.success",
            "data": {
              "reference": "{{reference}}",
              "amount": {{amountKobo}},
              "channel": "{{channel}}",
              "status": "success"
            }
          }
          """;
}
