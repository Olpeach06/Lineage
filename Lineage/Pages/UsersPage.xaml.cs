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
    public partial class UsersPage : Page
    {
        private List<UserItem> allUsers = new List<UserItem>();
        private List<RoleItem> roles = new List<RoleItem>();
        private string currentSearchText = "";
        private bool isPageLoaded = false;

        public class UserItem
        {
            public int Id { get; set; }
            public string Username { get; set; }
            public string Email { get; set; }
            public int RoleId { get; set; }
            public string RoleName { get; set; }
            public int? PersonId { get; set; }
            public string PersonName { get; set; }
            public bool IsActive { get; set; }
            public string CreatedDate { get; set; }
            public string LastLoginDate { get; set; }
            public Visibility CanEdit => Session.IsAdmin || Session.IsEditor ? Visibility.Visible : Visibility.Collapsed;
            public Visibility IsAdmin => Session.IsAdmin ? Visibility.Visible : Visibility.Collapsed;
        }

        public class RoleItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public UsersPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (!Session.IsAdmin && !Session.IsEditor)
            {
                MessageBox.Show("У вас нет доступа к этой странице!", "Доступ запрещен",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NavigationService.GoBack();
                return;
            }

            txtUserName.Text = Session.IsGuest ? "Гость" : Session.Username;
            btnAddUser.Visibility = Session.IsAdmin ? Visibility.Visible : Visibility.Collapsed;

            LoadRoles();
            LoadUsers();
            isPageLoaded = true;
        }

        private void LoadRoles()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities())
                {
                    roles = context.UserRoles
                        .Select(r => new RoleItem { Id = r.Id, Name = r.Name })
                        .OrderBy(r => r.Id)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки ролей: {ex.Message}");
            }
        }

        private void LoadUsers()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities())
                {
                    var users = context.Users.OrderBy(u => u.Id).ToList();
                    var personIds = users.Where(u => u.PersonId.HasValue).Select(u => u.PersonId.Value).ToList();
                    var persons = new Dictionary<int, string>();

                    if (personIds.Any())
                    {
                        persons = context.Persons
                            .Where(p => personIds.Contains(p.Id))
                            .ToDictionary(p => p.Id, p => $"{p.LastName} {p.FirstName}");
                    }

                    allUsers = users.Select(u => new UserItem
                    {
                        Id = u.Id,
                        Username = u.Username,
                        Email = u.Email ?? "---",
                        RoleId = u.RoleId,
                        RoleName = roles.FirstOrDefault(r => r.Id == u.RoleId)?.Name ?? "---",
                        PersonId = u.PersonId,
                        PersonName = u.PersonId.HasValue && persons.ContainsKey(u.PersonId.Value) ? persons[u.PersonId.Value] : "---",
                        IsActive = u.IsActive,
                        CreatedDate = u.CreatedAt.ToString("dd.MM.yyyy"),
                        LastLoginDate = u.LastLoginAt?.ToString("dd.MM.yyyy HH:mm") ?? "---"
                    }).ToList();

                    ApplyFilter();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки пользователей: {ex.Message}");
            }
        }

        private void ApplyFilter()
        {
            if (lvUsers == null || !isPageLoaded) return;

            var filtered = allUsers.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(currentSearchText))
            {
                filtered = filtered.Where(u =>
                    u.Username.ToLower().Contains(currentSearchText) ||
                    (u.Email?.ToLower().Contains(currentSearchText) ?? false) ||
                    u.PersonName.ToLower().Contains(currentSearchText));
            }

            lvUsers.ItemsSource = filtered.ToList();
            txtUsersCount.Text = $"Всего пользователей: {filtered.Count()}";
        }

        private void txtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtSearch.Text == "Поиск...") txtSearch.Text = "";
        }

        private void txtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text)) txtSearch.Text = "Поиск...";
        }

        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PerformSearch();
                e.Handled = true;
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!isPageLoaded) return;

            if (txtSearch.Text != "Поиск..." && !string.IsNullOrWhiteSpace(txtSearch.Text))
                btnClearSearch.Visibility = Visibility.Visible;
            else
                btnClearSearch.Visibility = Visibility.Collapsed;

            currentSearchText = txtSearch.Text != "Поиск..." ? txtSearch.Text.ToLower() : "";
            ApplyFilter();
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "Поиск...";
            currentSearchText = "";
            ApplyFilter();
            btnClearSearch.Visibility = Visibility.Collapsed;
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            PerformSearch();
        }

        private void PerformSearch()
        {
            if (!isPageLoaded) return;
            currentSearchText = txtSearch.Text != "Поиск..." ? txtSearch.Text.ToLower() : "";
            ApplyFilter();
        }

        private void EditUser_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag == null) return;
            int userId = (int)button.Tag;
            NavigationService.Navigate(new EditUserPage(userId));
        }

        private void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag == null) return;
            int userId = (int)button.Tag;

            if (!Session.IsAdmin)
            {
                MessageBox.Show("Только администратор может удалять пользователей!", "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (userId == Session.UserId)
            {
                MessageBox.Show("Нельзя удалить свою учетную запись!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var targetUser = allUsers.FirstOrDefault(u => u.Id == userId);
            if (targetUser == null) return;

            var result = MessageBox.Show($"Вы уверены, что хотите удалить пользователя {targetUser.Username}?", "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new GenealogyUnifiedDBEntities())
                    {
                        var userTrees = context.FamilyTrees.Where(t => t.CreatedByUserId == userId).ToList();
                        foreach (var tree in userTrees)
                        {
                            var persons = context.Persons.Where(p => p.TreeId == tree.Id).ToList();
                            var personIds = persons.Select(p => p.Id).ToList();

                            var mediaLinks = context.MediaLinks.Where(ml => personIds.Contains(ml.PersonId ?? 0)).ToList();
                            context.MediaLinks.RemoveRange(mediaLinks);

                            var stories = context.Stories.Where(s => personIds.Contains(s.PersonId)).ToList();
                            context.Stories.RemoveRange(stories);

                            var relationships = context.PersonRelationships
                                .Where(r => personIds.Contains(r.Person1Id) || personIds.Contains(r.Person2Id))
                                .ToList();
                            context.PersonRelationships.RemoveRange(relationships);

                            context.Persons.RemoveRange(persons);
                        }

                        context.FamilyTrees.RemoveRange(userTrees);
                        var user = context.Users.Find(userId);
                        if (user != null) context.Users.Remove(user);
                        context.SaveChanges();

                        MessageBox.Show("Пользователь удален!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadUsers();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления пользователя: {ex.Message}");
                }
            }
        }

        private void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new EditUserPage());
        }

        private void LinkPerson_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag == null) return;
            int userId = (int)button.Tag;

            if (!(Session.IsAdmin || Session.IsEditor))
            {
                MessageBox.Show("У вас нет прав для привязки пользователя к персоне!", "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            NavigationService.Navigate(new LinkUserToPersonPage(userId));
        }

        private void UserDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selectedUser = lvUsers?.SelectedItem as UserItem;
            if (selectedUser != null)
            {
                if (Session.IsAdmin || Session.IsEditor)
                    NavigationService.Navigate(new EditUserPage(selectedUser.Id));
                else
                    MessageBox.Show($"Пользователь: {selectedUser.Username}\nEmail: {selectedUser.Email}\nРоль: {selectedUser.RoleName}", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void MainPageButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new MainPage());
        }

        private void TreesButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new TreesPage());
        }

        private void ReportsButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ReportsPage());
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Выйти из аккаунта?", "Выход", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                Session.Clear();
                NavigationService.Navigate(new LoginPage());
            }
        }
    }
}