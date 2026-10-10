namespace FourTaste.WPF
{
    using System;
    using System.Windows;
    using Serilog;

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string currentUser = "Адріана";
            string moduleName = "CulinaryModule";

            Log.Information(
                "Користувач {UserName} виконав дію у модулі {Module}",
                currentUser,
                moduleName);

            int attemptedValue = -5;

            Log.Warning(
                "Користувач {UserName} ввів некоректне значення кількості: {Value}",
                currentUser,
                attemptedValue);

            try
            {
                throw new InvalidOperationException(
                    "Помилка підключення до бази даних рецепшенa.");
            }
            catch (Exception ex)
            {
                Log.Error(
                    ex,
                    "Сталася критична помилка в модулі {Module} для користувача {UserName}",
                    moduleName,
                    currentUser);
            }

            MessageBox.Show("Різні типи логів надіслано в Seq!");
        }
    }
}