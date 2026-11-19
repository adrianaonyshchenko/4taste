using System;
using System.Windows;

namespace FourTaste.WPF
{
    public partial class RoomCreatedDialog : Window
    {
        private readonly string _code;

        public RoomCreatedDialog(string roomName, string inviteCode)
            : this(roomName, inviteCode, $"Кімнату «{roomName}» успішно створено!", "Кімнату створено")
        {
        }

        public RoomCreatedDialog(
            string roomName,
            string inviteCode,
            string headerText,
            string windowTitle = "Код кімнати")
        {
            InitializeComponent();
            _code = inviteCode;
            Title = windowTitle;
            TitleTextBlock.Text = headerText;
            InviteCodeTextBox.Text = inviteCode;

            Loaded += (_, _) => CopyCodeToClipboard();
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            CopyCodeToClipboard();
            CopyButton.Content = "Скопійовано! ✓";
        }

        private void CopyCodeToClipboard()
        {
            InviteCodeTextBox.SelectAll();
            InviteCodeTextBox.Focus();
            Clipboard.SetDataObject(_code, true);
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}
