using System;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BtcTurkSocketTest
{
    class Program
    {
        private static async Task HmacTest()
        {
            ClientWebSocket client = new ClientWebSocket();

            Uri _uri = new Uri("wss://ws-feed-pro.btcturk.com");

            await client.ConnectAsync(_uri, CancellationToken.None);

            long nonce = 3000;

            string publicKey = "eb00babe-7af5-4479-92ea-eb44a268f44a";
            string privateKey = "zaPPMCJIpCdGmSIyOY444udQMOGcUgcG";

            string baseString = $"{publicKey}{nonce}";

            string signature = ComputeHash(privateKey, baseString);

            long timestamp = ToUnixTime(DateTime.UtcNow);

            object[] hmacMessageObject = { 114, new { type = 114, publicKey = publicKey, timestamp = timestamp, nonce = nonce, signature = signature } };

            string message = JsonSerializer.Serialize(hmacMessageObject);

            await client.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message),
                                0,
                                message.Length),
                            WebSocketMessageType.Text,
                            true,
                            CancellationToken.None);

            var buffer = new byte[1024 * 20];
            while (true)
            {
                WebSocketReceiveResult result = await client.ReceiveAsync(new ArraySegment<byte>(buffer),
                    CancellationToken.None);
                string resultMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine(resultMessage);
            }

        }

        private static string ComputeHash(string privateKey, string baseString)
        {
            var key = Convert.FromBase64String(privateKey);

            string hashString;
            using (var hmac = new HMACSHA256(key))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(baseString));
                hashString = Convert.ToBase64String(hash);
            }

            return hashString;

        }

        private static long ToUnixTime(DateTime date)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Convert.ToInt64((date - epoch).TotalMilliseconds);
        }

        static async Task Main(string[] args)
        {
            await HmacTest();
        }
    }
}
