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
            public int ProjectTypeId { get; set; }
            public int PersonsCount { get; set; }
            public int AnimalsCount { get; set; }
            public int StoriesCount { get; set; }
            public int MediaCount { get; set; }
            public bool IsCurrent { get; set; }
            public Visibility ShowDeleteButton { get; set; }
            public int CreatedByUserId { get; set; }
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
                    // Получаем проекты, созданные пользователем (независимо от режима)
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
                        // Подсчитываем статистику в зависимости от типа проекта
                        string statsText = "";
                        int personsCount = 0;
                        int animalsCount = 0;
                        int storiesCount = 0;
                        int mediaCount = 0;

                        if (tree.ProjectTypeId == 1) // Семейное древо
                        {
                            personsCount = context.Persons.Count(p => p.TreeId == tree.Id);
                            var personIds = context.Persons
                                .Where(p => p.TreeId == tree.Id)
                                .Select(p => p.Id)
                                .ToList();
                            storiesCount = context.Stories.Count(s => personIds.Contains(s.PersonId));
                            mediaCount = context.MediaLinks.Count(ml => personIds.Contains(ml.PersonId ?? 0));

                            statsText = $"👥 {personsCount} персон | 📖 {storiesCount} историй | 📷 {mediaCount} фото";
                        }
                        else // Племенная книга (животные)
                        {
                            animalsCount = context.Animals.Count(a => a.TreeId == tree.Id);
                            // Для животных считаем количество вязок, выставок и оценок
                            var animalIds = context.Animals
                                .Where(a => a.TreeId == tree.Id)
                                .Select(a => a.Id)
                                .ToList();

                            int breedingsCount = context.Breedings.Count(b => b.TreeId == tree.Id);
                            int exhibitionsCount = context.Exhibitions.Count(e => animalIds.Contains(e.AnimalId));
                            int assessmentsCount = context.AnimalAssessments.Count(a => animalIds.Contains(a.AnimalId));

                            statsText = $"🐄 {animalsCount} животных | 🔗 {breedingsCount} вязок | 🏆 {exhibitionsCount} выставок | ⭐ {assessmentsCount} оценок";
                        }

                        bool isCurrent = (tree.Id == Session.CurrentTreeId);

                        // Право на удаление: только администратор ИЛИ создатель проекта
                        bool canDelete = Session.IsAdmin || tree.CreatedByUserId == Session.UserId;

                        allTrees.Add(new TreeItem
                        {
                            Id = tree.Id,
                            Name = tree.Name,
                            Description = tree.Description ?? "Нет описания",
                            Stats = statsText,
                            CreatedDate = $"Создано: {tree.CreatedAt:dd.MM.yyyy}",
                            ProjectTypeId = tree.ProjectTypeId,
                            PersonsCount = personsCount,
                            AnimalsCount = animalsCount,
                            StoriesCount = storiesCount,
                            MediaCount = mediaCount,
                            IsCurrent = isCurrent,
                            ShowDeleteButton = canDelete ? Visibility.Visible : Visibility.Collapsed,
                            CreatedByUserId = tree.CreatedByUserId
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

                        // Право на удаление текущего дерева
                        bool canDeleteCurrent = Session.IsAdmin || currentTree.CreatedByUserId == Session.UserId;
                        btnDeleteCurrent.Visibility = canDeleteCurrent ? Visibility.Visible : Visibility.Collapsed;
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

            // Проверяем, имеет ли пользователь доступ к этому дереву
            using (var context = new GenealogyUnifiedDBEntities1())
            {
                var tree = context.FamilyTrees.Find(treeId);
                if (tree != null && (tree.IsPublic || tree.CreatedByUserId == Session.UserId || Session.IsAdmin))
                {
                    Session.CurrentTreeId = treeId;

                    // Устанавливаем режим в соответствии с типом проекта
                    AppSettings.CurrentMode = tree.ProjectTypeId;
                    Session.CurrentMode = tree.ProjectTypeId;
                }
            }

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

                    // Проверяем права на редактирование (только создатель или админ)
                    if (tree.CreatedByUserId != Session.UserId && !Session.IsAdmin)
                    {
                        MessageBox.Show("Вы можете редактировать только свои проекты!", "Доступ запрещён",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

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

            // Проверяем права на удаление
            using (var checkContext = new GenealogyUnifiedDBEntities1())
            {
                var tree = checkContext.FamilyTrees.Find(treeId);
                if (tree == null) return;

                // Только создатель или администратор может удалять
                if (tree.CreatedByUserId != Session.UserId && !Session.IsAdmin)
                {
                    MessageBox.Show("Вы можете удалять только свои проекты!", "Доступ запрещён",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            var result = MessageBox.Show("Вы уверены, что хотите удалить этот проект?\nВсе связанные данные будут также удалены!",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new GenealogyUnifiedDBEntities1())
                    {
                        var tree = context.FamilyTrees.FirstOrDefault(t => t.Id == treeId);
                        if (tree == null) return;

                        if (tree.ProjectTypeId == 1) // Семейное древо
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
                        }
                        else // Племенная книга (животные)
                        {
                            var animals = context.Animals.Where(a => a.TreeId == treeId).ToList();
                            var animalIds = animals.Select(a => a.Id).ToList();

                            // Удаляем вязки
                            var breedings = context.Breedings.Where(b => b.TreeId == treeId).ToList();
                            context.Breedings.RemoveRange(breedings);

                            // Удаляем выставки (исправлено: переменная ex вместо e)
                            var exhibitions = context.Exhibitions.Where(ex => animalIds.Contains(ex.AnimalId)).ToList();
                            context.Exhibitions.RemoveRange(exhibitions);

                            // Удаляем оценки
                            var assessments = context.AnimalAssessments.Where(a => animalIds.Contains(a.AnimalId)).ToList();
                            context.AnimalAssessments.RemoveRange(assessments);

                            // Удаляем родословные
                            var pedigrees = context.AnimalPedigree.Where(p => animalIds.Contains(p.AnimalId)).ToList();
                            context.AnimalPedigree.RemoveRange(pedigrees);

                            // Удаляем ветеринарные события
                            var vetEvents = context.VeterinaryEvents.Where(v => animalIds.Contains(v.AnimalId)).ToList();
                            context.VeterinaryEvents.RemoveRange(vetEvents);

                            // Удаляем записи продуктивности
                            var productivityRecords = context.ProductivityRecords.Where(p => animalIds.Contains(p.AnimalId)).ToList();
                            context.ProductivityRecords.RemoveRange(productivityRecords);

                            // Удаляем медиа связи для животных
                            var mediaLinks = context.MediaLinks.Where(ml => animalIds.Contains(ml.AnimalId ?? 0)).ToList();
                            context.MediaLinks.RemoveRange(mediaLinks);

                            context.Animals.RemoveRange(animals);
                        }

                        context.FamilyTrees.Remove(tree);
                        context.SaveChanges();

                        if (treeId == Session.CurrentTreeId)
                        {
                            var anyTree = context.FamilyTrees
                                .Where(t => t.CreatedByUserId == Session.UserId)
                                .FirstOrDefault();
                            Session.CurrentTreeId = anyTree?.Id ?? 0;

                            if (anyTree != null)
                            {
                                AppSettings.CurrentMode = anyTree.ProjectTypeId;
                                Session.CurrentMode = anyTree.ProjectTypeId;
                            }
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