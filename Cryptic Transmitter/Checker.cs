using System.Net.Sockets;
using System.Net;
namespace Cryptic_Transmitter
{
    internal class Checker
    {
        public static bool IsPrivateIPv4(IPAddress ip)
        {
            byte[] b = ip.GetAddressBytes();
            return
                b[0] == 10 ||
                (b[0] == 172 && b[1] >= 16 && b[1] <= 31) ||
                (b[0] == 192 && b[1] == 168);
        }
        public static bool IsValidIPv4(string ip, out IPAddress address)
        {
            if (!IPAddress.TryParse(ip, out address))
                return false;

            if (address.AddressFamily != AddressFamily.InterNetwork)
                return false;

            return true;
        }

        public static bool IsValidPort(string portText, out int port)
        {
            if (!int.TryParse(portText, out port))
                return false;

            return port >= 1 && port <= 65535;
        }

        public static bool IsRunningAsAdmin()
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
    }
}
