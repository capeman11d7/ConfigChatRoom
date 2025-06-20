using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input; // 🔵 NEW
using System.Windows.Threading;

namespace OfflineChatApp
{
    public partial class MainWindow : Window
    {
        private string chatDirectory = @"\\SharedDrive\ChatRooms"; // CHANGE THIS TO YOUR SHARED PATH
        private string versionCheckFile = @"\\SharedDrive\ChatRooms\latest_version.txt"; // 🔵 NEW
        private string currentChatRoom = "";
        private string userName = Environment.UserName;
        private DispatcherTimer chatRefreshTimer;

        public MainWindow()
        {
            InitializeComponent();

            // 🔵 NEW: Check for version update
            CheckForLatestVersion();

            Directory.CreateDirectory(chatDirectory);
            LoadChatRooms();

            chatRefreshTimer = new DispatcherTimer();
            chatRefreshTimer.Interval = TimeSpan.FromSeconds(2);
            chatRefreshTimer.Tick += ChatRefreshTimer_Tick;
            chatRefreshTimer.Start();
        }

        private void CheckForLatestVersion() // 🔵 NEW
        {
            try
            {
                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                DateTime currentModified = File.GetLastWriteTime(exePath);
                if (File.Exists(versionCheckFile))
                {
                    DateTime latestModified = File.GetLastWriteTime(versionCheckFile);
                    if (latestModified > currentModified)
                    {
                        MessageBox.Show("A newer version of this app is available. Please update.", "Update Available", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch { /* Silent fail if path is invalid */ }
        }

        private void LoadChatRooms()
        {
            ChatRoomList.Items.Clear();
            foreach (var file in Directory.GetFiles(chatDirectory, "*.txt"))
            {
                ChatRoomList.Items.Add(System.IO.Path.GetFileName(file));
            }
        }

        private void ChatRoomList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChatRoomList.SelectedItem != null)
            {
                currentChatRoom = System.IO.Path.Combine(chatDirectory, ChatRoomList.SelectedItem.ToString());
                ChatHistory.Text = File.ReadAllText(currentChatRoom);
            }
        }

        private void AddChatRoom_Click(object sender, RoutedEventArgs e)
        {
            var name = "ChatRoom_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt";
            var path = System.IO.Path.Combine(chatDirectory, name);
            File.WriteAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm}] {userName} created the room.\n"); // 🔵 MODIFIED
            LoadChatRooms();
        }

        private void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private void SendMessage() // 🔵 NEW METHOD
        {
            if (string.IsNullOrWhiteSpace(currentChatRoom)) return;
            var message = $"[{DateTime.Now:yyyy-MM-dd HH:mm}] {userName}: {MessageBox.Text}\n"; // 🔵 MODIFIED
            File.AppendAllText(currentChatRoom, message);
            ChatHistory.Text = File.ReadAllText(currentChatRoom);
            MessageBox.Clear();
        }

        private void ChatRefreshTimer_Tick(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(currentChatRoom) && File.Exists(currentChatRoom))
            {
                ChatHistory.Text = File.ReadAllText(currentChatRoom);
            }
        }

        private void MessageBox_KeyDown(object sender, KeyEventArgs e) // 🔵 NEW
        {
            if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftShift)) // Shift+Enter for newline
            {
                e.Handled = true;
                SendMessage();
            }
        }
    }
}