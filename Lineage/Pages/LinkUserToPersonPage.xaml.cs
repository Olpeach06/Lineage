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

namespace Lineage.Pages
{
    public partial class LinkUserToPersonPage : Page
    {
        private int userId;

        public class PersonItem
        {
            public int Id { get; set; }
            public string DisplayName { get; set; }
        }

        public LinkUserToPersonPage(int userId)
        {
            InitializeComponent();
            this.userId = userId;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadUserInfo();
            LoadPersons();
        }

        private void LoadUserInfo()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    var user = context.Users.Find(userId);
                    if (user != null)
                    {
                        txtUserName.Text = $"{user.Username} (ID: {user.Id})";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки пользователя: {ex.Message}");
            }
        }

        private void LoadPersons()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    var currentTreeId = Session.CurrentTreeId;

                    var persons = context.Persons
                        .Where(p => p.TreeId == currentTreeId)
                        .ToList();

                    var personItems = persons.Select(p => new PersonItem
                    {
                        Id = p.Id,
                        DisplayName = $"{p.LastName} {p.FirstName} {p.Patronymic} ({p.BirthDate?.Year})".Trim()
                    })
                    .OrderBy(p => p.DisplayName)
                    .ToList();

                    personItems.Insert(0, new PersonItem { Id = 0, DisplayName = "--- Не выбрано ---" });

                    cmbPerson.ItemsSource = personItems;
                    cmbPerson.SelectedValuePath = "Id";
                    cmbPerson.DisplayMemberPath = "DisplayName";

                    var user = context.Users.Find(userId);
                    if (user?.PersonId.HasValue == true && personItems.Any(p => p.Id == user.PersonId.Value))
                    {
                        cmbPerson.SelectedValue = user.PersonId.Value;
                    }
                    else
                    {
                        cmbPerson.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки персон: {ex.Message}");
            }
        }

        private void ClearLinkButton_Click(object sender, RoutedEventArgs e)
        {
            cmbPerson.SelectedIndex = 0;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int? selectedPersonId = null;
                if (cmbPerson.SelectedValue != null && (int)cmbPerson.SelectedValue > 0)
                {
                    selectedPersonId = (int)cmbPerson.SelectedValue;
                }

                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    var user = context.Users.Find(userId);
                    if (user != null)
                    {
                        user.PersonId = selectedPersonId;
                        context.SaveChanges();

                        string message = selectedPersonId.HasValue ? "Пользователь привязан к персоне!" : "Привязка пользователя удалена!";
                        MessageBox.Show(message, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}