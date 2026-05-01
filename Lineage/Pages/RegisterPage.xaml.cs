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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Lineage.AppData;
using Lineage.Classes;
using System.Text.RegularExpressions;

namespace Lineage.Pages
{
    public partial class RegisterPage : Page
    {
        private bool _isProcessing = false;

        public RegisterPage()
        {
            InitializeComponent();
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            PerformRegistration();
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                PerformRegistration();
            }
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                PerformRegistration();
            }
        }

        private void PerformRegistration()
        {
            if (_isProcessing)
                return;

            try
            {
                _isProcessing = true;
                string login = txtLogin.Text.Trim();
                string email = txtEmail.Text.Trim();
                string password = txtPassword.Password;
                string confirm = txtConfirmPassword.Password;

                // Проверки
                if (string.IsNullOrEmpty(login))
                {
                    MessageBox.Show("Введите логин!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtLogin.Focus();
                    return;
                }

                if (login.Length < 3)
                {
                    MessageBox.Show("Логин должен содержать не менее 3 символов!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtLogin.Focus();
                    return;
                }

                if (!Regex.IsMatch(login, @"^[a-zA-Z0-9_]+$"))
                {
                    MessageBox.Show("Логин может содержать только буквы латинского алфавита, цифры и символ подчеркивания!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtLogin.Focus();
                    return;
                }

                if (string.IsNullOrEmpty(email))
                {
                    MessageBox.Show("Введите email!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtEmail.Focus();
                    return;
                }

                if (!IsValidEmail(email))
                {
                    MessageBox.Show("Введите корректный email адрес!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtEmail.Focus();
                    return;
                }

                if (string.IsNullOrEmpty(password))
                {
                    MessageBox.Show("Введите пароль!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtPassword.Focus();
                    return;
                }

                if (password != confirm)
                {
                    MessageBox.Show("Пароли не совпадают!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtConfirmPassword.Focus();
                    return;
                }

                if (password.Length < 4)
                {
                    MessageBox.Show("Пароль должен быть не менее 4 символов!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtPassword.Focus();
                    return;
                }

                using (var context = new GenealogyUnifiedDBEntities())
                {
                    // Проверка уникальности логина
                    var existingUser = context.Users
                        .Where(u => u.Username.ToLower() == login.ToLower())
                        .FirstOrDefault();

                    if (existingUser != null)
                    {
                        MessageBox.Show("Пользователь с таким логином уже существует!", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        txtLogin.Focus();
                        txtLogin.SelectAll();
                        return;
                    }

                    // Проверка уникальности email
                    var existingEmail = context.Users
                        .Where(u => u.Email != null && u.Email.ToLower() == email.ToLower())
                        .FirstOrDefault();

                    if (existingEmail != null)
                    {
                        MessageBox.Show("Этот email уже используется!", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        txtEmail.Focus();
                        txtEmail.SelectAll();
                        return;
                    }

                    // Определение роли (первый пользователь - администратор)
                    int roleId = 3; // Зритель по умолчанию
                    bool isFirstUser = !context.Users.Any();

                    if (isFirstUser)
                    {
                        roleId = 1; // Администратор
                    }

                    // Создание пользователя
                    var user = new Users
                    {
                        Username = login,
                        Email = email,
                        PasswordHash = password,
                        RoleId = roleId,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };

                    context.Users.Add(user);
                    context.SaveChanges();

                    string message = isFirstUser
                        ? "Регистрация успешна! Вы администратор."
                        : "Регистрация успешна!";

                    MessageBox.Show(message, "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    // Переход на страницу входа
                    NavigationService.Navigate(new LoginPage());
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

        private void LoginHyperlink_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new LoginPage());
        }
    }
}