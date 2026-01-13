using NSec.Cryptography;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cryptic_Transmitter
{
    internal class Transmission
    {
        private readonly CryptoEngine crypto;
        private readonly Action<string> chat, log;
        private readonly Action<string, string, string, string> chatlog;

        public Transmission(
            CryptoEngine cryptoEngine,
            Action<string> logCallback,
            Action<string> chatCallback,
            Action<string, string, string, string> chatLogCallback)
        {
            crypto = cryptoEngine;
            log = logCallback;
            chat = chatCallback;
            chatlog = chatLogCallback;
        }

        private TcpListener listener;
        private CancellationTokenSource cts;
        private IPAddress targetIP;
        private int targetPort;
        public async Task StartListenerAsync(
            string localIP,
            string targetIP,
            string port,
            string targetPort,
            bool dualMode,
            string key
        )
        {
            cts = new CancellationTokenSource();
            listener = new TcpListener(IPAddress.Parse(localIP), int.Parse(port));

            if (dualMode)
                listener.Server.DualMode = true;

            this.targetIP = IPAddress.Parse(targetIP);
            this.targetPort = int.Parse(targetPort);
            crypto.SetKey(key);

            listener.Start();
            log("Listener started.");

            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    _ = HandleClientAsync(client);
                }
            }
            catch (ObjectDisposedException) { }
            catch (Exception ex)
            {
                log("Listener error: " + ex.Message);
            }
        }

        public void Stop()
        {
            cts?.Cancel();
            listener?.Stop();
            log("Listener stopped.");
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            using (client)  
            using (var stream = client.GetStream())
            {
                byte[] lengthBytes = await ReadExactAsync(stream, 4);
                int length = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(lengthBytes, 0));

                if (length <= 0 || length > 4096)
                    throw new InvalidOperationException("Invalid payload size.");

                byte[] payload = await ReadExactAsync(stream, length);
                string encrypted = Encoding.UTF8.GetString(payload);

                string decrypted;
                try
                {
                    decrypted = crypto.Decrypt(encrypted);
                }
                catch (CryptographicException)
                {
                    log("Invalid or tampered message received.");
                    return;
                }

                string nickname = "Unknown";
                string message = decrypted;

                string[] parts = decrypted.Split('\n', 2);

                if (parts.Length == 2)
                {
                    nickname = parts[0];
                    message = parts[1];
                }

                chat($"{nickname}: {message}");

                chatlog(nickname, encrypted, message, crypto.GetIV());
            }
        }

        public async Task SendMessage(string nickname, string message)
        {
            if (string.IsNullOrWhiteSpace(nickname))
                nickname = "Anonymous";

            string combined = nickname + "\n" + message;
            string encrypted = crypto.Encrypt(combined);

            byte[] payload = Encoding.UTF8.GetBytes(encrypted);
            byte[] length = BitConverter.GetBytes(
                IPAddress.HostToNetworkOrder(payload.Length));

            try
            {
                using var client = new TcpClient(targetIP.AddressFamily);
                await client.ConnectAsync(targetIP, targetPort);

                using var stream = client.GetStream();
                await stream.WriteAsync(length);
                await stream.WriteAsync(payload);

                chat($"{nickname}: {message}");
                chatlog(nickname, encrypted, message, crypto.GetIV());
            }
            catch (Exception ex)
            {
                log("Send failed: " + ex.Message);
            }
        }
        private static async Task<byte[]> ReadExactAsync(NetworkStream stream, int size)
        {
            byte[] buffer = new byte[size];
            int offset = 0;

            while (offset < size)
            {
                int read = await stream.ReadAsync(buffer, offset, size - offset);
                if (read == 0)
                    throw new IOException("Connection closed.");
                offset += read;
            }

            return buffer;
        }


    }
}
