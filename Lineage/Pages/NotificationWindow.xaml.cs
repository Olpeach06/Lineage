using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Lineage.Pages
{
    public partial class NotificationWindow : Window
    {
        // Базовый класс для ViewModel уведомлений
        public abstract class NotificationViewModelBase { }
        private NotificationWindow()
        {
            InitializeComponent();
            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                    Close();
            };

            // Закрытие при клике на затемнённую область (опционально)
            MouseDown += (s, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    // Проверяем, что клик был не по контенту
                    var hitTestResult = VisualTreeHelper.HitTest(this, e.GetPosition(this));
                    if (hitTestResult?.VisualHit == this)
                    {
                        Close();
                    }
                }
            };
        }
        // ViewModel для ошибки
        public class ErrorViewModel : NotificationViewModelBase
        {
            public string Message { get; set; }
            public string Details { get; set; }
            public Visibility HasDetails => string.IsNullOrWhiteSpace(Details) ? Visibility.Collapsed : Visibility.Visible;
        }

        // ViewModel для успеха
        public class SuccessViewModel : NotificationViewModelBase
        {
            public string Message { get; set; }
        }

        // ViewModel для информации
        public class InfoViewModel : NotificationViewModelBase
        {
            public string Message { get; set; }
        }

        // ViewModel для предупреждения
        public class WarningViewModel : NotificationViewModelBase
        {
            public string Message { get; set; }
        }

        // ViewModel для подтверждения
        public class ConfirmViewModel : NotificationViewModelBase
        {
            public string Question { get; set; }
            public Action<bool> Callback { get; set; }
        }

        // ============= СТАТИЧЕСКИЕ МЕТОДЫ (синхронные) =============

        /// <summary>Показать уведомление об ошибке</summary>
        public static void ShowError(string message, string details = null, Window owner = null)
        {
            var window = new NotificationWindow();
            window.Owner = owner ?? Application.Current.MainWindow;
            window.NotificationContent.ContentTemplate = window.FindResource("ErrorTemplate") as DataTemplate;
            window.NotificationContent.Content = new ErrorViewModel { Message = message, Details = details };
            window.ShowDialog();
        }

        /// <summary>Показать уведомление об успехе</summary>
        public static void ShowSuccess(string message, Window owner = null)
        {
            var window = new NotificationWindow();
            window.Owner = owner ?? Application.Current.MainWindow;
            window.NotificationContent.ContentTemplate = window.FindResource("SuccessTemplate") as DataTemplate;
            window.NotificationContent.Content = new SuccessViewModel { Message = message };
            window.ShowDialog();
        }

        /// <summary>Показать информационное уведомление</summary>
        public static void ShowInfo(string message, Window owner = null)
        {
            var window = new NotificationWindow();
            window.Owner = owner ?? Application.Current.MainWindow;
            window.NotificationContent.ContentTemplate = window.FindResource("InfoTemplate") as DataTemplate;
            window.NotificationContent.Content = new InfoViewModel { Message = message };
            window.ShowDialog();
        }

        /// <summary>Показать предупреждение</summary>
        public static void ShowWarning(string message, Window owner = null)
        {
            var window = new NotificationWindow();
            window.Owner = owner ?? Application.Current.MainWindow;
            window.NotificationContent.ContentTemplate = window.FindResource("WarningTemplate") as DataTemplate;
            window.NotificationContent.Content = new WarningViewModel { Message = message };
            window.ShowDialog();
        }

        /// <summary>Показать диалог подтверждения (синхронно, блокирующий)</summary>
        public static bool ShowConfirmationSync(string question, Window owner = null)
        {
            bool result = false;
            var window = new NotificationWindow();
            window.Owner = owner ?? Application.Current.MainWindow;
            window.NotificationContent.ContentTemplate = window.FindResource("ConfirmTemplate") as DataTemplate;

            var vm = new ConfirmViewModel
            {
                Question = question,
                Callback = (res) =>
                {
                    result = res;
                    window.Close();
                }
            };

            window.NotificationContent.Content = vm;
            window.ShowDialog();

            return result;
        }

        /// <summary>Показать диалог подтверждения (асинхронно)</summary>
        public static async Task<bool> ShowConfirmation(string question, Window owner = null)
        {
            return await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => ShowConfirmationSync(question, owner));
        }

        // ============= АСИНХРОННЫЕ ВЕРСИИ =============

        public static async Task ShowErrorAsync(string message, string details = null, Window owner = null)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => ShowError(message, details, owner));
        }

        public static async Task ShowSuccessAsync(string message, Window owner = null)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => ShowSuccess(message, owner));
        }

        public static async Task ShowInfoAsync(string message, Window owner = null)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => ShowInfo(message, owner));
        }

        public static async Task ShowWarningAsync(string message, Window owner = null)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => ShowWarning(message, owner));
        }

        // ============= ОБРАБОТЧИКИ КНОПОК =============

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CloseWindowButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ConfirmYes_Click(object sender, RoutedEventArgs e)
        {
            var vm = NotificationContent.Content as ConfirmViewModel;
            vm?.Callback?.Invoke(true);
        }

        private void ConfirmNo_Click(object sender, RoutedEventArgs e)
        {
            var vm = NotificationContent.Content as ConfirmViewModel;
            vm?.Callback?.Invoke(false);
        }
    }
}