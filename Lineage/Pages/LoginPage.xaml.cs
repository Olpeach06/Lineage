using Lineage.AppData;
using Lineage.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Lineage.Pages
{
    /// <summary>
    /// Логика взаимодействия для LoginPage.xaml
    /// </summary>
    public partial class LoginPage : Page
    {
        private bool _isProcessing = false;
        public LoginPage()
        {
            InitializeComponent();
        }
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            PerformLogin();
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                PerformLogin();
            }
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                PerformLogin();
            }
        }

        private void PerformLogin()
        {
            if (_isProcessing)
                return;

            try
            {
                _isProcessing = true;
                string login = txtLogin.Text.Trim();
                string password = txtPassword.Password;

                if (string.IsNullOrEmpty(login))
                {
                    MessageBox.Show("Введите логин или email!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtLogin.Focus();
                    return;
                }

                if (string.IsNullOrEmpty(password))
                {
                    MessageBox.Show("Введите пароль!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtPassword.Focus();
                    return;
                }

                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    Users user = null;

                    if (IsValidEmail(login))
                    {
                        var possibleUsers = context.Users
                            .Where(u => u.Email != null && u.Email.ToLower() == login.ToLower())
                            .ToList();
                        user = possibleUsers.FirstOrDefault(u => u.Email == login);
                    }
                    else
                    {
                        var possibleUsers = context.Users
                            .Where(u => u.Username.ToLower() == login.ToLower())
                            .ToList();
                        user = possibleUsers.FirstOrDefault(u => u.Username == login);
                    }

                    if (user == null)
                    {
                        MessageBox.Show("Неверный логин/email или пароль!", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        txtLogin.Focus();
                        txtLogin.SelectAll();
                        return;
                    }

                    if (user.PasswordHash != password)
                    {
                        MessageBox.Show("Неверный логин/email или пароль!", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        txtPassword.Focus();
                        txtPassword.Password = "";
                        return;
                    }

                    if (user.IsActive == false)
                    {
                        MessageBox.Show("Аккаунт заблокирован! Обратитесь к администратору.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Сохраняем в сессию
                    Session.UserId = user.Id;
                    Session.Username = user.Username;
                    Session.Email = user.Email;
                    Session.RoleId = user.RoleId;
                    Session.IsGuest = false;

                    // Получаем LastUsedMode из БД
                    int? lastUsedMode = user.LastUsedMode;

                    MessageBox.Show($"Добро пожаловать, {user.Username}!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    // Переходим на страницу выбора режима
                    NavigationService.Navigate(new SelectionPage(user.Id, lastUsedMode));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                return Regex.IsMatch(email, pattern);
            }
            catch
            {
                return false;
            }
        }

        private void GuestLoginButton_Click(object sender, RoutedEventArgs e)
        {
            Session.IsGuest = true;
            Session.Username = "Гость";
            Session.RoleId = 3;
            NavigationService.Navigate(new SelectionPage(0, 1));
        }

        private void RegisterHyperlink_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new RegisterPage());
        }
    }
}

