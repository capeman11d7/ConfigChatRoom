# ConfigChatRoom


<Window x:Class="OfflineChatApp.NamePrompt"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Name New Chat" Height="150" Width="300" WindowStartupLocation="CenterOwner">
    <StackPanel Margin="10">
        <TextBlock Text="Enter chat room name:" Margin="0,0,0,10"/>
        <TextBox x:Name="InputBox" Margin="0,0,0,10"/>
        <StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
            <Button Content="OK" Width="60" Margin="5" Click="Ok_Click"/>
            <Button Content="Cancel" Width="60" Margin="5" Click="Cancel_Click"/>
        </StackPanel>
    </StackPanel>
</Window>



using System.Windows;

namespace OfflineChatApp
{
    public partial class NamePrompt : Window
    {
        public string Result { get; private set; }

        public NamePrompt()
        {
            InitializeComponent();
            InputBox.Focus();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Result = InputBox.Text;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}



