using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ChatAboutIt
{
    public partial class MainWindow : Window
    {
        private string chatDirectory = "";
        private string currentChatRoom = "";
        private string userName = Environment.UserName;
        private Dictionary<string, string> userAvatars = new Dictionary<string, string>();
        private static readonly string[] emojis = new[] { "🐶", "🐱", "🐸", "🐵", "🦊", "🐼", "🐨", "🐯", "🐰", "🐮", "🐔", "🦁", "🐧", "🐢", "🐙", "🦄" };

        public MainWindow()
        {
            InitializeComponent();

            var folderDialog = new System.Windows.Forms.FolderBrowserDialog();
            folderDialog.Description = "Choose chat directory:";
            var result = folderDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                chatDirectory = folderDialog.SelectedPath;
                LoadChatRooms();
            }
            else
            {
                Close();
            }
        }

        private void LoadChatRooms()
        {
            ChatRoomList.Items.Clear();
            foreach (var file in Directory.GetFiles(chatDirectory, "*.txt"))
            {
                ChatRoomList.Items.Add(Path.GetFileName(file));
            }
        }

        private void ChatRoomList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChatRoomList.SelectedItem != null)
            {
                currentChatRoom = Path.Combine(chatDirectory, ChatRoomList.SelectedItem.ToString());
                LoadChatMessages();
            }
        }

        private void AddChatRoom_Click(object sender, RoutedEventArgs e)
        {
            var name = "ChatRoom_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt";
            var path = Path.Combine(chatDirectory, name);
            File.WriteAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm}] {userName} created the room.
");
            LoadChatRooms();
        }

        private void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(currentChatRoom)) return;
            var message = $"[{DateTime.Now:yyyy-MM-dd HH:mm}] {userName}: {MessageBox.Text}";
            File.AppendAllText(currentChatRoom, message + Environment.NewLine);
            MessageBox.Clear();
            LoadChatMessages();
        }

        private void MessageBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                SendMessage_Click(null, null);
                e.Handled = true;
            }
        }

        private void LoadChatMessages()
        {
            ChatList.Items.Clear();
            if (!string.IsNullOrEmpty(currentChatRoom) && File.Exists(currentChatRoom))
            {
                var lines = File.ReadAllLines(currentChatRoom);
                foreach (var line in lines)
                {
                    var user = GetUserFromLine(line);
                    if (string.IsNullOrEmpty(user)) continue;

                    if (!userAvatars.ContainsKey(user))
                        userAvatars[user] = emojis[new Random(user.GetHashCode()).Next(emojis.Length)];

                    var isMine = user == userName;
                    var align = isMine ? HorizontalAlignment.Right : HorizontalAlignment.Left;
                    var bgColor = isMine ? "#D0EBFF" : "#F0F0F0";

                    var avatar = new TextBlock
                    {
                        Text = userAvatars[user],
                        FontSize = 24,
                        Margin = new Thickness(5),
                        VerticalAlignment = VerticalAlignment.Top
                    };

                    var messageText = new TextBox
                    {
                        Text = line,
                        Background = Brushes.Transparent,
                        BorderThickness = new Thickness(0),
                        IsReadOnly = true,
                        FontFamily = new FontFamily("Segoe UI Emoji"),
                        TextWrapping = TextWrapping.Wrap,
                        AcceptsReturn = true,
                        Cursor = System.Windows.Input.Cursors.IBeam
                    };

                    var timeText = new TextBlock
                    {
                        Text = GetTimestampFromLine(line),
                        FontSize = 10,
                        Foreground = Brushes.Gray,
                        Margin = new Thickness(5, 2, 5, 5)
                    };

                    var bubble = new StackPanel();
                    bubble.Children.Add(messageText);
                    bubble.Children.Add(timeText);

                    var border = new Border
                    {
                        Background = (Brush)new BrushConverter().ConvertFromString(bgColor),
                        CornerRadius = new CornerRadius(12),
                        Padding = new Thickness(10),
                        Margin = new Thickness(5),
                        HorizontalAlignment = align,
                        MaxWidth = 400,
                        Child = bubble
                    };

                    var messageRow = new StackPanel { Orientation = Orientation.Horizontal };
                    if (isMine)
                    {
                        messageRow.Children.Add(border);
                        messageRow.Children.Add(avatar);
                    }
                    else
                    {
                        messageRow.Children.Add(avatar);
                        messageRow.Children.Add(border);
                    }

                    ChatList.Items.Add(messageRow);
                }

                if (ChatList.Items.Count > 0)
                    ChatList.ScrollIntoView(ChatList.Items[ChatList.Items.Count - 1]);
            }
        }

        private string GetUserFromLine(string line)
        {
            var parts = line.Split(']');
            if (parts.Length < 2) return null;
            var rest = parts[1].Trim();
            var idx = rest.IndexOf(':');
            if (idx == -1) return null;
            return rest.Substring(0, idx);
        }

        private string GetTimestampFromLine(string line)
        {
            var start = line.IndexOf('[');
            var end = line.IndexOf(']');
            if (start == -1 || end == -1 || end <= start) return "";
            return line.Substring(start + 1, end - start - 1);
        }
    }
}
