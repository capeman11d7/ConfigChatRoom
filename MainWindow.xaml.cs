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