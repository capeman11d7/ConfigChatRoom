using System;
using System.IO;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Documents;
using System.Windows.Media;

namespace OfflineChatApp
{
    public partial class MainWindow : Window
    {
        private string chatDirectory = @"\\SharedDrive\ChatRooms";
        private string versionCheckFile = @"\\SharedDrive\ChatRooms\latest_version.txt";
        private string currentChatRoom = "";
        private string userName = Environment.UserName;
        private string localVersion = "1.0.0";
        private DispatcherTimer chatRefreshTimer;
        private Dictionary<string, DateTime> lastSeenTimestamps = new Dictionary<string, DateTime>();

        public MainWindow()
        {
            InitializeComponent();
            CheckForLatestVersion();
            Directory.CreateDirectory(chatDirectory);
            LoadChatRooms();

            chatRefreshTimer = new DispatcherTimer();
            chatRefreshTimer.Interval = TimeSpan.FromSeconds(2);
            chatRefreshTimer.Tick += ChatRefreshTimer_Tick;
            chatRefreshTimer.Start();
        }

        private void CheckForLatestVersion()
        {
            try
            {
                if (File.Exists(versionCheckFile))
                {
                    string latestVersion = File.ReadAllText(versionCheckFile).Trim();
                    if (latestVersion != localVersion)
                    {
                        MessageBox.Show($"A new version ({latestVersion}) is available. Please update.", "Update Available", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch { }
        }

        private void LoadChatRooms()
        {
            RefreshChatRoomList();
        }

        private void RefreshChatRoomList()
        {
            bool hasUnread = false;
            ChatRoomList.Items.Clear();

            foreach (var file in Directory.GetFiles(chatDirectory, "*.txt"))
            {
                string name = Path.GetFileName(file);
                string label = name;
                DateTime lastWrite = File.GetLastWriteTime(file);

                if (lastSeenTimestamps.TryGetValue(name, out DateTime lastSeen))
                {
                    if (lastWrite > lastSeen)
                    {
                        label = "❗ " + name;
                        hasUnread = true;
                    }
                }

                ChatRoomList.Items.Add(label);
            }

            if (hasUnread)
                FlashWindow(this);
        }

        private void ChatRoomList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChatRoomList.SelectedItem != null)
            {
                string selected = ChatRoomList.SelectedItem.ToString().Replace("❗ ", "");
                currentChatRoom = Path.Combine(chatDirectory, selected);
                LoadFormattedChat();

                if (File.Exists(currentChatRoom))
                    lastSeenTimestamps[selected] = File.GetLastWriteTime(currentChatRoom);

                RefreshChatRoomList();
            }
        }

        private void AddChatRoom_Click(object sender, RoutedEventArgs e)
        {
            var name = "ChatRoom_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt";
            var path = Path.Combine(chatDirectory, name);
            File.WriteAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm}] {userName} created the room.\n");
            LoadChatRooms();
        }

        private void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private void SendMessage()
        {
            if (string.IsNullOrWhiteSpace(currentChatRoom)) return;
            var message = $"[{DateTime.Now:yyyy-MM-dd HH:mm}] {userName}: {MessageBox.Text}\n";
            File.AppendAllText(currentChatRoom, message);
            LoadFormattedChat();
            MessageBox.Clear();

            string name = Path.GetFileName(currentChatRoom);
            lastSeenTimestamps[name] = File.GetLastWriteTime(currentChatRoom);
            RefreshChatRoomList();
            FlashWindow(this);
        }

        private void LoadFormattedChat()
        {
            ChatHistory.Document.Blocks.Clear();

            if (string.IsNullOrWhiteSpace(currentChatRoom) || !File.Exists(currentChatRoom))
                return;

            var lines = File.ReadAllLines(currentChatRoom);
            foreach (var line in lines)
            {
                var paragraph = new Paragraph();
                SolidColorBrush color;

                if (line.Contains($"] {userName}:"))
                    color = new SolidColorBrush(Color.FromRgb(173, 216, 230)); // Light blue
                else
                    color = new SolidColorBrush(Color.FromRgb(211, 211, 211)); // Light gray

                paragraph.Background = color;
                paragraph.Margin = new Thickness(0, 0, 0, 5);
                paragraph.Padding = new Thickness(5);
                paragraph.Inlines.Add(new Run(line));

                ChatHistory.Document.Blocks.Add(paragraph);
            }

            ChatHistory.ScrollToEnd();
        }

        private void ChatRefreshTimer_Tick(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(currentChatRoom) && File.Exists(currentChatRoom))
            {
                LoadFormattedChat();
            }
            RefreshChatRoomList();
        }

        private void MessageBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftShift))
            {
                e.Handled = true;
                SendMessage();
            }
        }

        [DllImport("user32.dll")]
        private static extern bool FlashWindow(IntPtr hwnd, bool bInvert);

        private void FlashWindow(Window win)
        {
            var helper = new WindowInteropHelper(win);
            FlashWindow(helper.Handle, true);
        }
    }
}