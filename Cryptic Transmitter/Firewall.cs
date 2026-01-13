using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;

namespace Cryptic_Transmitter
{
    public enum IPVersion
    {
        IPv4,
        IPv6
    }

    public record FirewallResult(bool Success, string Message);

    internal class Firewall
    {
        public static IPAddress GetPrimaryPrivateIP(IPVersion version = IPVersion.IPv4)
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up) continue;
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                var props = nic.GetIPProperties();
                if (props.GatewayAddresses.Count == 0) continue;

                foreach (var addr in props.UnicastAddresses)
                {
                    if (version == IPVersion.IPv4 && addr.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                    if (version == IPVersion.IPv6 && addr.Address.AddressFamily != AddressFamily.InterNetworkV6) continue;

                    if (version == IPVersion.IPv4 && Checker.IsPrivateIPv4(addr.Address))
                        return addr.Address;

                    if (version == IPVersion.IPv6)
                        return addr.Address;
                }
            }

            return null;
        }

        public static readonly List<string> createdFirewallRules = new List<string>();
        public static async Task<FirewallResult> OpenPortAsync(string portString, string remoteIP = "", IPVersion ipVersion = IPVersion.IPv4, string ruleName = "")
        {
            int port = int.Parse(portString);

            if (!Checker.IsRunningAsAdmin())
            {
                MessageBox.Show(
                    "Please start the software with administrative privileges for this function or open the port manually.",
                    "Administrator Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return new FirewallResult(false, "Not running as administrator.");
            }

            if (port < 1 || port > 65535)
                return new FirewallResult(false, "Port must be between 1 and 65535.");

            remoteIP = remoteIP?.Trim() ?? "";
            if (!string.IsNullOrWhiteSpace(remoteIP))
            {
                bool validIP = ipVersion == IPVersion.IPv4
                    ? Checker.IsValidIPv4(remoteIP, out _)
                    : IPAddress.TryParse(remoteIP, out IPAddress tmp) && tmp.AddressFamily == AddressFamily.InterNetworkV6;

                if (!validIP)
                    return new FirewallResult(false, $"Remote IP is not a valid {ipVersion} address.");
            }

            if (string.IsNullOrWhiteSpace(ruleName))
                ruleName = "Cryptic Transmitter - " + remoteIP + "_" + portString;

            string remoteIpArg = string.IsNullOrWhiteSpace(remoteIP) ? "any" : remoteIP;

            MessageBoxResult confirmation = MessageBox.Show(
                $"Create firewall rule?\n\nPort: {port}\nRemote IP: {remoteIpArg}\nIP Version: {ipVersion}",
                "Firewall Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmation != MessageBoxResult.Yes)
                return new FirewallResult(false, "User cancelled firewall rule creation.");

            string protocol = "TCP";
            string args = $"advfirewall firewall add rule name=\"{ruleName}\" dir=in action=allow protocol={protocol} localport={port} remoteip={remoteIpArg}";

            if (ipVersion == IPVersion.IPv6)
                args += " profile=any";

            var psi = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            try
            {
                return await Task.Run(() =>
                {
                    using var process = Process.Start(psi);
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        createdFirewallRules.Add(ruleName);
                        return new FirewallResult(true, $"Firewall rule added:\nPort {port}, Remote IP: {remoteIpArg}");
                    }
                    else
                        return new FirewallResult(false, $"Firewall rule creation failed: {error}");
                });
            }
            catch (Exception ex)
            {
                return new FirewallResult(false, $"Error creating firewall rule: {ex.Message}");
            }


        }
        public static async Task ClearRulesAsync()
        {
            foreach (string ruleName in createdFirewallRules)
            {
                try
                {
                    await DeleteRuleAsync(ruleName);
                }
                catch (Exception ex){}
            }
        }


        public static async Task DeleteRuleAsync(string ruleName)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = $"advfirewall firewall delete rule name=\"{ruleName}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            process.WaitForExit();

            if (process.ExitCode != 0)
                throw new InvalidOperationException(error);
        }

    }
}
