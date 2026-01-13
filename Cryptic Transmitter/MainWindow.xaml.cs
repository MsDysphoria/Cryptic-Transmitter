using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Controls;
using System.Threading;
using System.Windows.Media.Animation;

namespace Cryptic_Transmitter
{
    public partial class MainWindow : Window
    {

        #region "UI Elements"
        public BitmapImage GeneralButtonHover;
        public BitmapImage GeneralButtonDefault;
        public BitmapImage GeneralButtonDisabled;
        public BitmapImage StartButtonHover;
        public BitmapImage StartButtonDefault;
        public BitmapImage StopButtonHover;
        public BitmapImage StopButtonDefault;
        public BitmapImage PanelDisabled;
        public BitmapImage PanelEnabled;
        public BitmapImage IconVisible;
        public BitmapImage IconInvisible;

        private BitmapImage discordD;
        private BitmapImage discordH;
        private BitmapImage patreonD;
        private BitmapImage patreonH;
        private BitmapImage githubD;
        private BitmapImage githubH;
        private BitmapImage webD;
        private BitmapImage webH;
        private BitmapImage deviantArtD;
        private BitmapImage deviantArtH;
        private BitmapImage artstationD;
        private BitmapImage artstationH;
        private BitmapImage buttonD;
        private BitmapImage buttonH;
        private void LoadImages()
        {
            GeneralButtonDisabled = new BitmapImage(new Uri("/Images/GeneralButtonDisabled.png", UriKind.Relative));
            GeneralButtonDefault = new BitmapImage(new Uri("/Images/GeneralButtonD.png", UriKind.Relative));
            GeneralButtonHover = new BitmapImage(new Uri("/Images/GeneralButtonH.png", UriKind.Relative));
            StartButtonHover = new BitmapImage(new Uri("/Images/StoppedH.png", UriKind.Relative));
            StartButtonDefault = new BitmapImage(new Uri("/Images/StoppedD.png", UriKind.Relative));
            StopButtonHover = new BitmapImage(new Uri("/Images/StartedH.png", UriKind.Relative));
            StopButtonDefault = new BitmapImage(new Uri("/Images/StartedD.png", UriKind.Relative));
            PanelDisabled = new BitmapImage(new Uri("/Images/PanelDisabled.png", UriKind.Relative));
            PanelEnabled = new BitmapImage(new Uri("/Images/Panel.png", UriKind.Relative));
            IconVisible = new BitmapImage(new Uri("/Images/Visible.png", UriKind.Relative));
            IconInvisible = new BitmapImage(new Uri("/Images/Invisible.png", UriKind.Relative));

            discordD = new BitmapImage(new Uri("/Images/Discord.png", UriKind.Relative));
            discordH = new BitmapImage(new Uri("/Images/Discord_H.png", UriKind.Relative));
            patreonD = new BitmapImage(new Uri("/Images/Patreon.png", UriKind.Relative));
            patreonH = new BitmapImage(new Uri("/Images/Patreon_H.png", UriKind.Relative));
            githubD = new BitmapImage(new Uri("/Images/Github.png", UriKind.Relative));
            githubH = new BitmapImage(new Uri("/Images/Github_H.png", UriKind.Relative));
            webD = new BitmapImage(new Uri("/Images/Website.png", UriKind.Relative));
            webH = new BitmapImage(new Uri("/Images/Website_H.png", UriKind.Relative));
            deviantArtD = new BitmapImage(new Uri("/Images/DeviantArt.png", UriKind.Relative));
            deviantArtH = new BitmapImage(new Uri("/Images/DeviantArt_H.png", UriKind.Relative));
            artstationD = new BitmapImage(new Uri("/Images/Artstation.png", UriKind.Relative));
            artstationH = new BitmapImage(new Uri("/Images/Artstation_H.png", UriKind.Relative));
            buttonD = new BitmapImage(new Uri("/Images/GeneralButton.png", UriKind.Relative));
            buttonH = new BitmapImage(new Uri("/Images/GeneralButton_H.png", UriKind.Relative));
        }
        #endregion
        #region Windows Basic Functions
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Window window = Window.GetWindow(this);

            if (e.LeftButton == MouseButtonState.Pressed)
                window.DragMove();
        }

        private async void btnClose_Click(object sender, RoutedEventArgs e)
        {
            try { await Firewall.ClearRulesAsync(); }

            catch{}

            finally { Application.Current.Shutdown(); }
        }
        private void btnInfo_Click(object sender, RoutedEventArgs e)
        { ShowInformation(); }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
        #endregion

        private bool transmissionStarted = false;


        public MainWindow()
        {
            InitializeComponent();

            crypto = new CryptoEngine(UpdateConsoleWindow, OnIVGenerated);

            UpdateChatWindow("(Messages sent & received will appear in this box)");
            LoadImages();
            SendBtn.IsEnabled = false;
            MessageInput.IsEnabled = false;

            SetLocalIPAddress();
            UpdateLetterCounter();

        }

        #region "Helpers"
        private void SetLocalIPAddress()
        {
            var ip = Firewall.GetPrimaryPrivateIP();
            ReceiverIP.Text = ip?.ToString() ?? "No network";
        }
        private void UpdateLogForMessage(string nickname, string encryptedMessage, string message, string iv)
        {
            string timeStamp = DateTime.Now.ToString("HH:mm");
            if (AdvancedConsole.IsChecked == true)
            {
                UpdateConsoleWindow("▰▰▰▰▰▰▰▰▰▰▰▰▰▰▰▰▰▰▰▰▰▰▰▰▰▰▰▰▰▰▰▰▰▰▰▰▰▰▰▰\nUser: " + nickname + "\nTimestamp: " + timeStamp + "\nIV Key: " + iv + "\nEncrypted Message: " + encryptedMessage + "\nDecrypted Message: " + message);
            }
        }

        public void UpdateConsoleWindow(string message)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                ConsoleWindow.Text += message + Environment.NewLine;
                ConsoleScrollViewer.ScrollToEnd();
            });
        }

        public void UpdateChatWindow(string message)
        {
            string timeStamp = DateTime.Now.ToString("HH:mm");

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                ChatWindow.Text += $"[{timeStamp}] {message}{Environment.NewLine}";
                ChatWindowScrollViewer.ScrollToEnd();
            });
        }
        #endregion

        #region "Transmission"
        private CryptoEngine crypto;
        private Transmission transmission;
        private async void StartTransmission()
        {
            string targetIp = TargetIP.Text.Trim();
            string receiverIp = ReceiverIP.Text.Trim();
            string targetPort = TargetPort.Text.Trim();
            string receiverPort = ReceiverPort.Text.Trim();

            if (IPType.SelectedIndex == 0)
            {
                if (!Checker.IsValidIPv4(targetIp, out _))
                {
                    MessageBox.Show("Target IP is not a valid IPv4 address.", "Invalid Input");
                    return;
                }

                if (!Checker.IsValidIPv4(receiverIp, out _))
                {
                    MessageBox.Show("Receiver IP is not a valid IPv4 address.", "Invalid Input");
                    return;
                }
            }
            else
            {
                if (!IPAddress.TryParse(targetIp, out IPAddress tmp) || tmp.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6)
                {
                    MessageBox.Show("Target IP is not a valid IPv6 address.", "Invalid Input");
                    return;
                }
            }

            if (!Checker.IsValidPort(targetPort, out _))
            {
                MessageBox.Show("Target port must be a number between 1 and 65535.", "Invalid Input");
                return;
            }

            if (!Checker.IsValidPort(receiverPort, out _))
            {
                MessageBox.Show("Receiver port must be a number between 1 and 65535.", "Invalid Input");
                return;
            }

            if (!CryptoEngine.ValidateBase64(Key1Input.Text, out string errorMessage))
            {
                MessageBox.Show(errorMessage, "Invalid AES Settings", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBoxResult confirmationResult = MessageBox.Show(
                $"Are you sure you want to start the transmission with the settings below?\n\n" +
                $"Target IP: {targetIp}\n" +
                $"Target Port: {targetPort}\n" +
                $"Receiver IP: {receiverIp}\n" +
                $"Receiver Port: {receiverPort}",
                "Confirmation",
                MessageBoxButton.YesNo);

            if (confirmationResult != MessageBoxResult.Yes)
                return;

            transmissionStarted = true;
            LockTransmissionUI();

            crypto = new CryptoEngine(
                UpdateConsoleWindow,
                OnIVGenerated
            );

            crypto.SetKey(Key1Input.Text.Trim());

            transmission = new Transmission(
                crypto,
                UpdateConsoleWindow,
                UpdateChatWindow,
                UpdateLogForMessage
            );

            IPAddress localAddr = IPType.SelectedIndex == 1
                ? IPAddress.IPv6Any
                : IPAddress.Any;

            await transmission.StartListenerAsync(
                receiverIp,
                targetIp,
                receiverPort,
                targetPort,
                IPType.SelectedIndex == 1,
                Key1Input.Text.Trim()
            );
        }

        private void StopTransmission()
        {
            UnlockTransmissionUI();
            transmission?.Stop();
            ChatWindow.Clear();
        }

        public async void SendMessage()
        {
            string message = MessageInput.Text;
            string nickname = Nickname.Text.Trim();

            if (string.IsNullOrWhiteSpace(nickname))
                nickname = "Anonymous";

            MessageInput.Clear();
            UpdateLetterCounter();

            await transmission.SendMessage(nickname, message);
        }
        #endregion

        #region "Save & Load"

        internal sealed class AppSettings
        {
            public string TargetIP { get; set; }
            public string TargetPort { get; set; }
            public string ReceiverIP { get; set; }
            public string ReceiverPort { get; set; }
            public string Key1 { get; set; }
        }

        private void SaveSettings()
        {
            var settings = new AppSettings
            {
                TargetIP = TargetIP.Text,
                TargetPort = TargetPort.Text,
                ReceiverIP = ReceiverIP.Text,
                ReceiverPort = ReceiverPort.Text,
                Key1 = Key1Input.Text
            };

            if (string.IsNullOrWhiteSpace(settings.TargetIP) ||
                string.IsNullOrWhiteSpace(settings.TargetPort) ||
                string.IsNullOrWhiteSpace(settings.ReceiverIP) ||
                string.IsNullOrWhiteSpace(settings.ReceiverPort))
            {
                MessageBox.Show("Please enter valid values for all settings.");
                return;
            }

            string filePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "settings.ct");

            try
            {
                string json = System.Text.Json.JsonSerializer.Serialize(settings);
                byte[] protectedData = CryptoEngine.Protect(json);

                File.WriteAllBytes(filePath, protectedData);

                UpdateConsoleWindow("Settings saved.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving settings:\n" + ex.Message);
            }
        }

        private void LoadSettings()
        {
            string filePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "settings.ct");

            if (!File.Exists(filePath))
                return;

            try
            {
                byte[] protectedData = File.ReadAllBytes(filePath);
                string json = CryptoEngine.Unprotect(protectedData);

                var settings = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json);

                if (settings == null)
                    return;

                TargetIP.Text = settings.TargetIP ?? "";
                TargetPort.Text = settings.TargetPort ?? "";
                ReceiverIP.Text = settings.ReceiverIP ?? "";
                ReceiverPort.Text = settings.ReceiverPort ?? "";
                Key1Input.Text = settings.Key1 ?? "";

                UpdateConsoleWindow("Settings loaded.");
            }
            catch (Exception ex)
            {
                UpdateConsoleWindow(
                    $"An error occurred while loading settings: {ex.Message}");
            }
        }
        #endregion

        #region "Calls"
        private void UpdateLetterCounter(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateLetterCounter();
        }
        private void IP1HelpBtn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("The other person needs to provide their public IPv4 and open a port through 'Advanced Firewall Options' to receive encrypted messages from you.\n\nBoth parties need to know each other's public IP and open port and enter them to create a link for transmission.\n\nPublic IPv4 can be obtained from www.whatismyip.com", "Info");
        }

        private void IP2HelpBtn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("You need to type in your local/private IPv4 (can be obtained from 'ipconfig' command in cmd) and open a port to receive encrypted messages from the others.\n\nBoth parties need to know each other's public IP and open port and enter them to create a link for transmission.", "Info");
        }

        private void SaveBtn_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SaveSettings();
        }

        private void LoadBtn_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            LoadSettings();
        }
        private void StartBtn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            LockTransmissionUI();
            StartTransmission();
        }
        private void SendBtn_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (SendBtn.IsEnabled == false) return;
            if (!transmissionStarted)
            {
                MessageBox.Show("Transmission has not been started.");
                return;
            }

            SendMessage();
        }
        private async void OpenPortBtn_Click(object sender, MouseButtonEventArgs e)
        {
            IPVersion ipVersion = IPType.SelectedIndex == 0 ? IPVersion.IPv4 : IPVersion.IPv6;

            FirewallResult result = await Firewall.OpenPortAsync(
                ReceiverPort.Text,
                remoteIP: TargetIP.Text,
                ipVersion: ipVersion
            );

            UpdateConsoleWindow(result.Message);
        }


        private void TransmissionBtn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (transmissionStarted)
                StopTransmission();
            else StartTransmission();
        }

        private void GenerateKeyBtn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            crypto.GenerateKey();

            if (key1Revealed)
                Key1Input.Text = crypto.GetKey();

            UpdateConsoleWindow("AES key generated.");
        }
        private void RevealKey1Btn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            RevealKey1();
        }
        private void RevealKey2Btn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            RevealKey2();
        }
        private void ClearLogBtn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ConsoleWindow.Clear();
        }
        private void ClearChatBtn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ChatWindow.Clear();
        }
        private void Website_MouseDown(object sender, MouseButtonEventArgs e)
        {
            string patreonLink = "https://msdysphoria.shop/";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = patreonLink,
                UseShellExecute = true
            });
        }

        private void Github_MouseDown(object sender, MouseButtonEventArgs e)
        {
            string patreonLink = "https://github.com/MsDysphoria";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = patreonLink,
                UseShellExecute = true
            });
        }

        private void Discord_MouseDown(object sender, MouseButtonEventArgs e)
        {
            string patreonLink = "https://discord.com/invite/buNr2QxjC6";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = patreonLink,
                UseShellExecute = true
            });
        }
        #endregion

        #region "UI"
        private bool advancedConsoleEnabled = false;
        public static bool key1Revealed = true;
        public static bool key2Revealed = true;
        private void RevealKey1()
        {
            key1Revealed = !key1Revealed;
            RevealKey1Btn_Icon.Source = key1Revealed ? IconVisible : IconInvisible;
            Key1Input.Text = key1Revealed ? crypto.GetKey() : "";
        }

        private void RevealKey2()
        {
            key2Revealed = !key2Revealed;
            RevealKey2Btn_Icon.Source = key2Revealed ? IconVisible : IconInvisible;
            Key2Input.Text = key2Revealed ? crypto.GetIV() : "";
        }

        private void OnIVGenerated(string iv)
        {
            if (key2Revealed)
                Key2Input.Text = iv;
        }

        private void LockTransmissionUI()
        {
            TransmissionBtn.Source = StopButtonDefault;

            Key1Input.IsEnabled = false;
            ReceiverIP.IsEnabled = false;
            ReceiverPort.IsEnabled = false;
            TargetIP.IsEnabled = false;
            TargetPort.IsEnabled = false;
            ListenerFx.Visibility = Visibility.Hidden;
            SendBtn.IsEnabled = true;
            MessageInput.IsEnabled = true;
            TransmissionPanel1.Source = PanelEnabled;
            TransmissionPanel2.Source = PanelEnabled;
            TransmissionPanel3.Source = PanelEnabled;
            TransmissionStatus.Text = "Online";
            SendBtn.Source = GeneralButtonDefault;

            ClearChatBtn.Source = GeneralButtonDefault;
            ClearChatBtn.IsEnabled = true;
        }
        private void UnlockTransmissionUI()
        {
            TransmissionBtn.Source = StartButtonHover;

            ListenerFx.Visibility = Visibility.Visible;
            Key1Input.IsEnabled = true;
            ReceiverIP.IsEnabled = true;
            ReceiverPort.IsEnabled = true;
            TargetIP.IsEnabled = true;
            TargetPort.IsEnabled = true;

            SendBtn.IsEnabled = false;
            MessageInput.IsEnabled = false;
            TransmissionPanel1.Source = PanelDisabled;
            TransmissionPanel2.Source = PanelDisabled;
            TransmissionPanel3.Source = PanelDisabled;
            TransmissionStatus.Text = "Offline";
            SendBtn.Source = GeneralButtonDisabled;

            ClearChatBtn.Source = GeneralButtonDisabled;
            ClearChatBtn.IsEnabled = false;
        }
        private void UpdateLetterCounter()
        {
            if (LetterCounter == null || MessageInput == null) return;
            LetterCounter.Text = MessageInput.Text.Length.ToString();
        }
        private void MessageInput_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (MessageInput.Text == null || MessageInput.Text == "")
                EnterMessage.Visibility = Visibility.Visible;
            else

                EnterMessage.Visibility = Visibility.Hidden;
            UpdateLetterCounter();
        }

        private void ChangeButtonState(object sender, int state)
        {
            if (sender is not System.Windows.Controls.Image img)
                return;

            switch (state)
            {
                case 0:
                    img.Source = GeneralButtonDisabled;
                    break;
                case 1:
                    img.Source = GeneralButtonDefault;
                    break;
                case 2:
                    img.Source = GeneralButtonHover;
                    break;
            }
        }
        private void ChangeTransmissionButtonState(object sender, int state)
        {
            if (sender is not System.Windows.Controls.Image img)
                return;

            switch (state)
            {
                case 1:
                    if (transmissionStarted)
                        img.Source = StopButtonDefault;
                    else img.Source = StartButtonDefault;

                    break;
                case 2:
                    if (transmissionStarted)
                        img.Source = StopButtonHover;
                    else img.Source = StartButtonHover;
                    break;
            }

        }
        #endregion

        private void TransmissionButton_MouseEnter(object sender, MouseEventArgs e)
        {
            ChangeTransmissionButtonState(sender, 2);
        }
        private void TransmissionButton_MouseLeave(object sender, MouseEventArgs e)
        {
            ChangeTransmissionButtonState(sender, 1);
        }
        private void Button_MouseEnter(object sender, MouseEventArgs e)
        {
            ChangeButtonState(sender, 2);
        }
        private void Button_MouseLeave(object sender, MouseEventArgs e)
        {
            ChangeButtonState(sender, 1);
        }

        private void Return_MouseEnter(object sender, MouseEventArgs e)
        { Return.Source = buttonH; }

        private void Return_MouseLeave(object sender, MouseEventArgs e)
        { Return.Source = buttonD; }
        private void Discord_MouseEnter(object sender, MouseEventArgs e)
        { Discord.Source = discordH; }

        private void Discord_MouseLeave(object sender, MouseEventArgs e)
        { Discord.Source = discordD; }

        private void Github_MouseEnter(object sender, MouseEventArgs e)
        { Github.Source = githubH; }

        private void Github_MouseLeave(object sender, MouseEventArgs e)
        { Github.Source = githubD; }

        private void Website_MouseEnter(object sender, MouseEventArgs e)
        { Website.Source = webH; }

        private void Website_MouseLeave(object sender, MouseEventArgs e)
        { Website.Source = webD; }

        public async Task Typewriter(int message, CancellationToken cancellationToken)
        {
            Author.Text = "";
            string msg;
            if (message == 0) { msg = ""; }
            else if (message == 1) { msg = "Created by Ms. Dysphoria"; }
            else { msg = "Discord: msdysphoria"; }

            Random randomDelay = new Random();

            for (int i = 0; i < msg.Length; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                Author.Text += msg[i].ToString();
                int delay = randomDelay.Next(35, 55);
                await Task.Delay(delay, cancellationToken);
            }

            if (message == 0)
            {
                await Task.Delay(2500, cancellationToken);
                await Typewriter(1, cancellationToken);
            }
            else if (message == 1)
            {
                Storyboard fadeInStoryboard = this.FindResource("GlowAuthor") as Storyboard;
                fadeInStoryboard.Begin();
                await Task.Delay(5000, cancellationToken);
                await Typewriter(2, cancellationToken);
            }
            else
            {

                await Task.Delay(5000, cancellationToken);
                await Typewriter(1, cancellationToken);
            }
        }

        private CancellationTokenSource cancellationTokenSource;

        private void Return_MouseDown(object sender, MouseButtonEventArgs e)
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
            Storyboard fadeOutStoryboard = this.FindResource("FadeOut_Info") as Storyboard;
            fadeOutStoryboard?.Begin();
        }
        private void ShowInformation()
        {

            cancellationTokenSource = new CancellationTokenSource();
            _ = Typewriter(0, cancellationTokenSource.Token);

            Storyboard fadeInStoryboard = this.FindResource("FadeIn_Info") as Storyboard;
            fadeInStoryboard.Begin();
        }

        private void btnInfo_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ShowInformation();
        }
    }
}
    