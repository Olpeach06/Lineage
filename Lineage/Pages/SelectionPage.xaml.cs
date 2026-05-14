using Lineage.AppData;
using Lineage.Classes;
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

namespace Lineage.Pages
{
    public partial class SelectionPage : Page
    {
        private int _currentUserId;
        private int? _lastMode;

        public SelectionPage(int userId, int? lastMode)
        {
            InitializeComponent();
            _currentUserId = userId;
            _lastMode = lastMode;

            Loaded += SelectionPage_Loaded;
        }

        private void SelectionPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Подсветка последнего выбранного режима
            if (_lastMode == 1)
            {
                FamilyCard.BorderBrush = new SolidColorBrush(System.Windows.Media.Colors.Orange);
                FamilyCard.BorderThickness = new Thickness(3);
            }
            else if (_lastMode == 2)
            {
                BreedingCard.BorderBrush = new SolidColorBrush(System.Windows.Media.Colors.Orange);
                BreedingCard.BorderThickness = new Thickness(3);
            }
        }

        private void FamilyCard_Click(object sender, MouseButtonEventArgs e)
        {
            SelectMode(1);
        }

        private void BreedingCard_Click(object sender, MouseButtonEventArgs e)
        {
            SelectMode(2);
        }

        private void SelectMode(int modeType)
        {
            if (_currentUserId > 0)
            {
                SaveLastUsedMode(modeType);
            }

            // Устанавливаем режим в Session и AppSettings
            Session.CurrentMode = modeType;
            AppSettings.CurrentMode = modeType;

            // Переход на главную страницу
            NavigationService.Navigate(new MainPage());
        }

        private void SaveLastUsedMode(int modeType)
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    var user = context.Users.Find(_currentUserId);
                    if (user != null)
                    {
                        user.LastUsedMode = modeType;
                        context.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения режима: {ex.Message}");
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Выйти из аккаунта?", "Выход",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                Session.Clear();
                NavigationService.Navigate(new LoginPage());
            }
        }
    }
}