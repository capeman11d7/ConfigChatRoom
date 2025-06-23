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
using System.Windows.Media.Imaging;

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
        private Dictionary<string, string> userEmojis = new Dictionary<string, string>();
        private Random rng = new Random();

        private readonly string[] emojiPool = new string[]
        {
            "😀", "😃", "😄", "😁", "😆", "😅", "😂", "🤣", "😊", "😇", "🙂", "🙃", "😉", "😌", "😍", "🥰",
            "😘", "😗", "😙", "😚", "😋", "😛", "😝", "😜", "🤪", "🧐", "🤓", "😎", "🥸", "😏", "😒",
            "😞", "😔", "😟", "😕", "🙁", "☹️", "😣", "😖", "😫", "😩", "🥱", "😤", "😠", "😡", "🤬",
            "😶", "😐", "😑", "😯", "😦", "😧", "😮", "😲", "😵", "🤯", "😳", "🥵", "🥶", "😱", "😨",
            "😰", "😥", "😓", "🤗", "🤔", "🤭", "🤫", "🤥", "😬", "🙄", "😯", "😴", "😪", "🐶", "🐱",
            "🐭", "🐹", "🐰", "🦊", "🐻", "🐼", "🐨", "🐯", "🦁", "🐮", "🐷", "🐸", "🐵", "🦄", "🐔",
            "🐧", "🐦", "🐤", "🐣", "🦆", "🦅", "🦉", "🦇", "🐺", "🐗", "🐴", "🐢", "🐍", "🦎"
        };

        public MainWindow()
        {
            InitializeComponent();
            Title = "Chat About It";
            Directory.CreateDirectory(chatDirectory);
            LoadChatRooms();

            chatRefreshTimer = new DispatcherTimer();
            chatRefreshTimer.Interval = TimeSpan.FromSeconds(2);
            chatRefreshTimer.Tick += ChatRefreshTimer_Tick;
            chatRefreshTimer.Start();
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
            var prompt = new NamePrompt();
            prompt.Owner = this;

            if (prompt.ShowDialog() == true && !string.IsNullOrWhiteSpace(prompt.Result))
            {
                string safeName = string.Join("_", prompt.Result.Split(Path.GetInvalidFileNameChars()));
                string fileName = safeName + ".txt";
                string path = Path.Combine(chatDirectory, fileName);

                File.WriteAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm}] {userName} created the room.\n");
                LoadChatRooms();
            }
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

        private void UploadFile_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(currentChatRoom)) return;

            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                string fileName = System.IO.Path.GetFileName(openFileDialog.FileName);
                string chatRoomDir = System.IO.Path.GetDirectoryName(currentChatRoom);
                string filesDir = System.IO.Path.Combine(chatRoomDir, "files");
                Directory.CreateDirectory(filesDir);

                string destinationPath = System.IO.Path.Combine(filesDir, fileName);
                File.Copy(openFileDialog.FileName, destinationPath, overwrite: true);

                string logLine = $"*{fileName}|{userName}";
                File.AppendAllText(currentChatRoom, logLine + "\n");
                LoadFormattedChat();
            }
        }

        private void LoadFormattedChat()
        {
            ChatHistory.Document.Blocks.Clear();

            if (string.IsNullOrWhiteSpace(currentChatRoom) || !File.Exists(currentChatRoom))
                return;

            var lines = File.ReadAllLines(currentChatRoom);
            string chatRoomDir = Path.GetDirectoryName(currentChatRoom);
            string filesDir = Path.Combine(chatRoomDir, "files");

            foreach (var line in lines)
            {
                var paragraph = new Paragraph();
                paragraph.Margin = new Thickness(0, 0, 0, 5);
                paragraph.Padding = new Thickness(5);
                paragraph.TextAlignment = TextAlignment.Left;

                if (line.StartsWith("*"))
                {
                    string[] parts = line.TrimStart('*').Split('|');
                    string filename = parts[0];
                    string sender = parts.Length > 1 ? parts[1] : "Unknown";
                    string fullPath = Path.Combine(filesDir, filename);

                    if (File.Exists(fullPath))
                    {
                        paragraph.Inlines.Add(new Bold(new Run($"{sender} sent:")) { FontSize = 14 });
                        paragraph.Inlines.Add(new LineBreak());

                        if (IsImageFile(filename))
                        {
                            var bitmap = new BitmapImage(new Uri(fullPath));
                            var image = new Image { Source = bitmap, MaxHeight = 450, Margin = new Thickness(0, 5, 0, 5) };
                            image.MouseLeftButtonDown += (s, e) =>
                            {
                                var popup = new Window
                                {
                                    Title = filename,
                                    Width = 800,
                                    Height = 800,
                                    Content = new Image
                                    {
                                        Source = new BitmapImage(new Uri(fullPath)),
                                        Stretch = Stretch.Uniform
                                    }
                                };
                                popup.ShowDialog();
                            };

                            paragraph.Inlines.Add(new InlineUIContainer(image));
                        }
                        else
                        {
                            var button = new Button
                            {
                                Content = "Open Location",
                                Tag = fullPath,
                                FontSize = 12,
                                Padding = new Thickness(5),
                                Margin = new Thickness(5, 5, 0, 0)
                            };
                            button.Click += (s, e) =>
                            {
                                try
                                {
                                    string argument = "/select,\"" + fullPath + "\"";
                                    System.Diagnostics.Process.Start("explorer.exe", argument);
                                }
                                catch { }
                            };
                            paragraph.Inlines.Add(new InlineUIContainer(button));
                        }
                    }
                    else
                    {
                        paragraph.Inlines.Add(new Run("[Missing file]"));
                    }

                    ChatHistory.Document.Blocks.Add(paragraph);
                    continue;
                }

                string user = ExtractUsername(line);
                string emoji = user != null ? GetEmojiForUser(user) : "";
                string lineToDisplay = user != null ? line.Replace($"] {user}:", $"] {emoji} {user}:") : line;

                SolidColorBrush color = line.Contains($"] {userName}:")
                    ? new SolidColorBrush(Color.FromRgb(173, 216, 230))
                    : new SolidColorBrush(Color.FromRgb(211, 211, 211));
                paragraph.Background = color;

                var messageRun = new Run(lineToDisplay);
                paragraph.Inlines.Add(messageRun);

                if (user == userName)
                {
                    var deleteRun = new Run(" 🗑️") { FontSize = 14, Cursor = Cursors.Hand };
                    deleteRun.MouseLeftButtonDown += (s, e) => DeleteMessage(line);
                    paragraph.Inlines.Add(deleteRun);
                }

                ChatHistory.Document.Blocks.Add(paragraph);
            }

            ChatHistory.ScrollToEnd();
        }

        private bool IsImageFile(string fileName)
        {
            string ext = Path.GetExtension(fileName).ToLower();
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".gif" || ext == ".bmp" || ext == ".webp";
        }

        private void DeleteMessage(string targetLine)
        {
            var lines = new List<string>(File.ReadAllLines(currentChatRoom));
            lines.RemoveAll(l => l == targetLine);
            File.WriteAllLines(currentChatRoom, lines);
            LoadFormattedChat();
        }

        private string GetEmojiForUser(string name)
        {
            if (!userEmojis.ContainsKey(name))
            {
                userEmojis[name] = emojiPool[rng.Next(emojiPool.Length)];
            }
            return userEmojis[name];
        }

        private string ExtractUsername(string line)
        {
            try
            {
                int closingBracket = line.IndexOf("]");
                int colonIndex = line.IndexOf(":", closingBracket + 2);
                if (closingBracket != -1 && colonIndex != -1)
                {
                    return line.Substring(closingBracket + 2, colonIndex - closingBracket - 2);
                }
            }
            catch { }
            return null;
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