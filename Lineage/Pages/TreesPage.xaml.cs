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
    public partial class TreesPage : Page
    {
        private List<TreeItem> allTrees = new List<TreeItem>();

        public class TreeItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string Stats { get; set; }
            public string CreatedDate { get; set; }
            public int PersonsCount { get; set; }
            public int StoriesCount { get; set; }
            public int MediaCount { get; set; }
            public bool IsCurrent { get; set; }
            public Visibility ShowDeleteButton { get; set; }
        }

        public TreesPage()
        {
            InitializeComponent();
            Loaded += Page_Loaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (Session.IsGuest)
            {
                txtUserName.Text = "Гость";
                btnCreateTree.Visibility = Visibility.Collapsed;
                btnUsers.Visibility = Visibility.Collapsed;
            }
            else
            {
                txtUserName.Text = Session.Username;
                bool canEdit = Session.IsAdmin || Session.IsEditor;
                btnCreateTree.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
                btnUsers.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
            }

            LoadTrees();
        }

        private void LoadTrees()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    var trees = context.FamilyTrees
                        .Where(t => t.CreatedByUserId == Session.UserId)
                        .OrderByDescending(t => t.CreatedAt)
                        .ToList();

                    if (!trees.Any())
                    {
                        borderCurrentTree.Visibility = Visibility.Collapsed;
                        borderNoTrees.Visibility = Visibility.Visible;
                        lvTrees.Visibility = Visibility.Collapsed;
                        return;
                    }

                    lvTrees.Visibility = Visibility.Visible;
                    borderNoTrees.Visibility = Visibility.Collapsed;

                    allTrees.Clear();

                    foreach (var tree in trees)
                    {
                        var personsCount = context.Persons.Count(p => p.TreeId == tree.Id);
                        var personIds = context.Persons
                            .Where(p => p.TreeId == tree.Id)
                            .Select(p => p.Id)
                            .ToList();
                        var storiesCount = context.Stories.Count(s => personIds.Contains(s.PersonId));
                        var mediaCount = context.MediaLinks.Count(ml => personIds.Contains(ml.PersonId ?? 0));

                        bool isCurrent = (tree.Id == Session.CurrentTreeId);

                        allTrees.Add(new TreeItem
                        {
                            Id = tree.Id,
                            Name = tree.Name,
                            Description = tree.Description ?? "Нет описания",
                            Stats = $"👥 {personsCount} персон | 📖 {storiesCount} историй | 📷 {mediaCount} фото",
                            CreatedDate = $"Создано: {tree.CreatedAt:dd.MM.yyyy}",
                            PersonsCount = personsCount,
                            StoriesCount = storiesCount,
                            MediaCount = mediaCount,
                            IsCurrent = isCurrent,
                            ShowDeleteButton = Session.IsAdmin ? Visibility.Visible : Visibility.Collapsed
                        });
                    }

                    var currentTree = allTrees.FirstOrDefault(t => t.IsCurrent);
                    var otherTrees = allTrees.Where(t => !t.IsCurrent).ToList();

                    if (currentTree != null)
                    {
                        borderCurrentTree.Visibility = Visibility.Visible;
                        txtCurrentTreeName.Text = currentTree.Name;
                        txtCurrentTreeDesc.Text = currentTree.Description;
                        txtCurrentTreeStats.Text = currentTree.Stats;
                        txtCurrentTreeDate.Text = currentTree.CreatedDate;
                        btnEditCurrent.Tag = currentTree.Id;
                        btnDeleteCurrent.Tag = currentTree.Id;
                        btnDeleteCurrent.Visibility = Session.IsAdmin ? Visibility.Visible : Visibility.Collapsed;
                    }
                    else
                    {
                        borderCurrentTree.Visibility = Visibility.Collapsed;
                    }

                    lvTrees.ItemsSource = otherTrees.Any() ? otherTrees : null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки проектов: {ex.Message}");
            }
        }

        private void CreateTreeButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new TreeEditDialog();
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    using (var context = new GenealogyUnifiedDBEntities1())
                    {
                        int projectTypeId = AppSettings.IsFamilyMode ? 1 : 2;

                        var newTree = new FamilyTrees
                        {
                            Name = dialog.TreeName,
                            Description = dialog.TreeDescription,
                            ProjectTypeId = projectTypeId,
                            CreatedByUserId = Session.UserId,
                            CreatedAt = DateTime.Now,
                            IsPublic = dialog.IsPublic
                        };

                        context.FamilyTrees.Add(newTree);
                        context.SaveChanges();

                        var treeCount = context.FamilyTrees.Count(t => t.CreatedByUserId == Session.UserId);
                        if (treeCount == 1)
                        {
                            Session.CurrentTreeId = newTree.Id;
                        }

                        MessageBox.Show("Новый проект создан!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadTrees();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка создания проекта: {ex.Message}");
                }
            }
        }

        private void SelectTreeButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag == null) return;

            int treeId = (int)button.Tag;
            Session.CurrentTreeId = treeId;
            NavigationService.Navigate(new MainPage());
        }

        private void EditTreeButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag == null) return;

            int treeId = (int)button.Tag;

            try
            {
                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    var tree = context.FamilyTrees.FirstOrDefault(t => t.Id == treeId);
                    if (tree == null) return;

                    var dialog = new TreeEditDialog(tree);
                    if (dialog.ShowDialog() == true)
                    {
                        tree.Name = dialog.TreeName;
                        tree.Description = dialog.TreeDescription;
                        tree.IsPublic = dialog.IsPublic;
                        context.SaveChanges();

                        MessageBox.Show("Проект обновлён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadTrees();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка редактирования проекта: {ex.Message}");
            }
        }

        private void DeleteTreeButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag == null) return;

            int treeId = (int)button.Tag;

            if (!Session.IsAdmin)
            {
                MessageBox.Show("Только администратор может удалять проекты!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show("Вы уверены, что хотите удалить этот проект?\nВсе связанные данные будут также удалены!",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new GenealogyUnifiedDBEntities1())
                    {
                        var persons = context.Persons.Where(p => p.TreeId == treeId).ToList();
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

                        var tree = context.FamilyTrees.FirstOrDefault(t => t.Id == treeId);
                        if (tree != null)
                            context.FamilyTrees.Remove(tree);

                        context.SaveChanges();

                        if (treeId == Session.CurrentTreeId)
                        {
                            var anyTree = context.FamilyTrees
                                .Where(t => t.CreatedByUserId == Session.UserId)
                                .FirstOrDefault();
                            Session.CurrentTreeId = anyTree?.Id ?? 0;
                        }

                        MessageBox.Show("Проект удалён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadTrees();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления проекта: {ex.Message}");
                }
            }
        }

        private void MainPageButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new MainPage());
        }

        private void ReportsButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ReportsPage());
        }

        private void UsersButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new UsersPage());
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