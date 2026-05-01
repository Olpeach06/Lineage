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
    public partial class ReportsPage : Page
    {
        private int currentTreeId = 1;
        private List<TreeItem> trees = new List<TreeItem>();

        public class TreeItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class PersonReportItem
        {
            public int Id { get; set; }
            public string LastName { get; set; }
            public string FirstName { get; set; }
            public string Patronymic { get; set; }
            public string BirthDate { get; set; }
            public string DeathDate { get; set; }
            public string Gender { get; set; }
            public string BirthPlace { get; set; }
            public string DeathPlace { get; set; }
        }

        public class AnimalReportItem
        {
            public int Id { get; set; }
            public string Nickname { get; set; }
            public string InventoryNumber { get; set; }
            public string Species { get; set; }
            public string Breed { get; set; }
            public string Color { get; set; }
            public string Gender { get; set; }
            public string BirthDate { get; set; }
            public string PedigreeClass { get; set; }
        }

        public ReportsPage()
        {
            InitializeComponent();
            Loaded += Page_Loaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (Session.IsGuest)
            {
                txtUserName.Text = "Гость";
                btnUsers.Visibility = Visibility.Collapsed;
            }
            else
            {
                txtUserName.Text = Session.Username;
                bool canEdit = Session.IsAdmin || Session.IsEditor;
                btnUsers.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
            }

            currentTreeId = Session.CurrentTreeId;
            LoadTrees();
            LoadReports();
            UpdateUIByMode();
        }

        private void UpdateUIByMode()
        {
            if (AppSettings.IsFamilyMode)
            {
                txtLabel1.Text = "Всего персон";
                txtLabel2.Text = "Семей";
                txtLabel3.Text = "Браков";
                txtLabel4.Text = "Умерших";
                txtLabel5.Text = "Поколений";
                txtDemographyTitle.Text = "ДЕМОГРАФИЯ";
                txtExtraStatsTitle.Text = "ПОПУЛЯРНЫЕ ИМЕНА И ФАМИЛИИ";
            }
            else
            {
                txtLabel1.Text = "Всего животных";
                txtLabel2.Text = "Видов";
                txtLabel3.Text = "Пород";
                txtLabel4.Text = "Самцов/Самок";
                txtLabel5.Text = "Племенных";
                txtDemographyTitle.Text = "РАСПРЕДЕЛЕНИЕ ПО ВИДАМ";
                txtExtraStatsTitle.Text = "ПОРОДЫ И КЛАССЫ";
            }
        }

        private void LoadTrees()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities())
                {
                    var userTrees = context.FamilyTrees
                        .Where(t => t.CreatedByUserId == Session.UserId)
                        .OrderBy(t => t.Name)
                        .ToList();

                    trees.Clear();
                    foreach (var tree in userTrees)
                    {
                        trees.Add(new TreeItem { Id = tree.Id, Name = tree.Name });
                    }

                    cmbTrees.ItemsSource = trees;

                    if (trees.Any())
                    {
                        cmbTrees.SelectedItem = trees.FirstOrDefault(t => t.Id == currentTreeId) ?? trees.First();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки списка проектов: {ex.Message}");
            }
        }

        private void LoadReports()
        {
            if (AppSettings.IsFamilyMode)
            {
                LoadFamilyReports();
            }
            else
            {
                LoadAnimalReports();
            }
        }

        private void LoadFamilyReports()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities())
                {
                    var persons = context.Persons.Where(p => p.TreeId == currentTreeId).ToList();

                    if (!persons.Any())
                    {
                        ShowEmptyReports();
                        return;
                    }

                    var personIds = persons.Select(p => p.Id).ToList();
                    var relationships = context.PersonRelationships
                        .Where(r => personIds.Contains(r.Person1Id) && personIds.Contains(r.Person2Id))
                        .ToList();

                    // Общая статистика
                    int totalPersons = persons.Count;
                    int totalDeceased = persons.Count(p => p.DeathDate.HasValue);
                    int totalFamilies = relationships.Where(r => r.RelationshipType == 1)
                        .GroupBy(r => r.Person1Id).Select(g => g.Key).Count();
                    int totalMarriages = relationships.Count(r => r.RelationshipType == 2) / 2;

                    txtTotalCount1.Text = totalPersons.ToString();
                    txtTotalCount2.Text = totalFamilies.ToString();
                    txtTotalCount3.Text = totalMarriages.ToString();
                    txtTotalCount4.Text = totalDeceased.ToString();

                    // Поколения (грубая оценка по годам)
                    int maxGeneration = persons.Any(p => p.BirthDate.HasValue)
                        ? persons.Max(p => GetGenerationByYear(p.BirthDate?.Year))
                        : 1;
                    txtTotalCount5.Text = maxGeneration.ToString();

                    // Демография по полу
                    int menCount = persons.Count(p => p.GenderId == 1);
                    int womenCount = persons.Count(p => p.GenderId == 2);
                    double menPercent = totalPersons > 0 ? (menCount * 100.0 / totalPersons) : 0;
                    double womenPercent = totalPersons > 0 ? (womenCount * 100.0 / totalPersons) : 0;

                    progressMen.Value = menPercent;
                    progressWomen.Value = womenPercent;
                    txtMenPercent.Text = $"{menPercent:F1}%";
                    txtWomenPercent.Text = $"{womenPercent:F1}%";

                    // Возрастная статистика
                    var livingPersons = persons.Where(p => !p.DeathDate.HasValue && p.BirthDate.HasValue).ToList();
                    if (livingPersons.Any())
                    {
                        int totalAge = 0;
                        foreach (var p in livingPersons)
                        {
                            int age = DateTime.Now.Year - p.BirthDate.Value.Year;
                            if (DateTime.Now < p.BirthDate.Value.AddYears(age)) age--;
                            totalAge += age;
                        }
                        txtAverageAge.Text = $"{totalAge / livingPersons.Count} лет";

                        var oldest = persons.Where(p => p.BirthDate.HasValue).OrderBy(p => p.BirthDate).FirstOrDefault();
                        if (oldest != null)
                        {
                            int age = DateTime.Now.Year - oldest.BirthDate.Value.Year;
                            if (DateTime.Now < oldest.BirthDate.Value.AddYears(age)) age--;
                            txtOldestPerson.Text = $"{oldest.FirstName} {oldest.LastName} ({age} лет)";
                        }

                        var youngest = persons.Where(p => p.BirthDate.HasValue).OrderByDescending(p => p.BirthDate).FirstOrDefault();
                        if (youngest != null)
                        {
                            int age = DateTime.Now.Year - youngest.BirthDate.Value.Year;
                            if (DateTime.Now < youngest.BirthDate.Value.AddYears(age)) age--;
                            txtYoungestPerson.Text = $"{youngest.FirstName} {youngest.LastName} ({age} лет)";
                        }
                    }

                    // Дополнительная статистика: популярные имена
                    var maleNames = persons.Where(p => p.GenderId == 1)
                        .GroupBy(p => p.FirstName)
                        .Select(g => new { Name = g.Key, Count = g.Count() })
                        .OrderByDescending(x => x.Count)
                        .Take(5)
                        .ToList();

                    var femaleNames = persons.Where(p => p.GenderId == 2)
                        .GroupBy(p => p.FirstName)
                        .Select(g => new { Name = g.Key, Count = g.Count() })
                        .OrderByDescending(x => x.Count)
                        .Take(5)
                        .ToList();

                    var surnames = persons.GroupBy(p => p.LastName)
                        .Select(g => new { Name = g.Key, Count = g.Count() })
                        .OrderByDescending(x => x.Count)
                        .Take(5)
                        .ToList();

                    var extraPanel = new StackPanel();
                    extraPanel.Children.Add(new TextBlock { Text = "Мужские имена:", FontWeight = FontWeights.Bold, Foreground = FindResource("Brush6B5E4A") as System.Windows.Media.Brush });
                    foreach (var name in maleNames)
                        extraPanel.Children.Add(new TextBlock { Text = $"{name.Name} — {name.Count}", Margin = new Thickness(10, 2, 0, 0) });
                    extraPanel.Children.Add(new TextBlock { Text = "Женские имена:", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 10, 0, 0) });
                    foreach (var name in femaleNames)
                        extraPanel.Children.Add(new TextBlock { Text = $"{name.Name} — {name.Count}", Margin = new Thickness(10, 2, 0, 0) });
                    extraPanel.Children.Add(new TextBlock { Text = "Популярные фамилии:", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 10, 0, 0) });
                    foreach (var surname in surnames)
                        extraPanel.Children.Add(new TextBlock { Text = $"{surname.Name} — {surname.Count}", Margin = new Thickness(10, 2, 0, 0) });

                    panelExtraStats.Children.Clear();
                    panelExtraStats.Children.Add(extraPanel);

                    // Список всех персон
                    var reportItems = persons.Select(p => new PersonReportItem
                    {
                        Id = p.Id,
                        LastName = p.LastName,
                        FirstName = p.FirstName,
                        Patronymic = p.Patronymic ?? "",
                        BirthDate = p.BirthDate?.ToString("dd.MM.yyyy") ?? "—",
                        DeathDate = p.DeathDate?.ToString("dd.MM.yyyy") ?? "—",
                        Gender = p.GenderId == 1 ? "Мужской" : (p.GenderId == 2 ? "Женский" : "—"),
                        BirthPlace = p.BirthPlace ?? "—",
                        DeathPlace = p.DeathPlace ?? "—"
                    }).ToList();

                    lvItems.ItemsSource = reportItems;
                    UpdateGridViewColumns(true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки отчётов: {ex.Message}");
            }
        }

        private void LoadAnimalReports()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities())
                {
                    var animals = context.Animals.Where(a => a.TreeId == currentTreeId).ToList();

                    if (!animals.Any())
                    {
                        ShowEmptyReports();
                        return;
                    }

                    int totalAnimals = animals.Count;
                    int totalSpecies = animals.Select(a => a.SpeciesId).Distinct().Count();
                    int totalBreeds = animals.Where(a => a.BreedId.HasValue).Select(a => a.BreedId).Distinct().Count();
                    int totalMales = animals.Count(a => a.GenderId == 1);
                    int totalFemales = animals.Count(a => a.GenderId == 2);
                    int totalBreedingStock = animals.Count(a => a.IsBreedingStock == true);

                    txtTotalCount1.Text = totalAnimals.ToString();
                    txtTotalCount2.Text = totalSpecies.ToString();
                    txtTotalCount3.Text = totalBreeds.ToString();
                    txtTotalCount4.Text = $"{totalMales}/{totalFemales}";
                    txtTotalCount5.Text = totalBreedingStock.ToString();

                    // Распределение по видам
                    var speciesDistribution = animals.GroupBy(a => a.SpeciesId)
                        .Select(g => new { SpeciesId = g.Key, Count = g.Count() })
                        .ToList();

                    var extraPanel = new StackPanel();
                    extraPanel.Children.Add(new TextBlock { Text = "Распределение по видам:", FontWeight = FontWeights.Bold });
                    foreach (var item in speciesDistribution)
                    {
                        string speciesName = GetSpeciesName(item.SpeciesId, context);
                        extraPanel.Children.Add(new TextBlock { Text = $"{speciesName} — {item.Count}", Margin = new Thickness(10, 2, 0, 0) });
                    }

                    // Распределение по классам
                    var classDistribution = animals.Where(a => a.PedigreeClassId.HasValue)
                        .GroupBy(a => a.PedigreeClassId)
                        .Select(g => new { ClassId = g.Key, Count = g.Count() })
                        .ToList();

                    if (classDistribution.Any())
                    {
                        extraPanel.Children.Add(new TextBlock { Text = "Распределение по племенным классам:", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 10, 0, 0) });
                        foreach (var item in classDistribution)
                        {
                            string className = GetPedigreeClassName(item.ClassId.Value, context);
                            extraPanel.Children.Add(new TextBlock { Text = $"{className} — {item.Count}", Margin = new Thickness(10, 2, 0, 0) });
                        }
                    }

                    panelExtraStats.Children.Clear();
                    panelExtraStats.Children.Add(extraPanel);

                    var reportItems = animals.Select(a => new AnimalReportItem
                    {
                        Id = a.Id,
                        Nickname = a.Nickname,
                        InventoryNumber = a.InventoryNumber ?? "—",
                        Species = GetSpeciesName(a.SpeciesId, context),
                        Breed = GetBreedName(a.BreedId, context),
                        Color = GetColorName(a.ColorId, context),
                        Gender = GetAnimalGenderName(a.GenderId, context),
                        BirthDate = a.BirthDate?.ToString("dd.MM.yyyy") ?? "—",
                        PedigreeClass = GetPedigreeClassName(a.PedigreeClassId, context)
                    }).ToList();

                    lvItems.ItemsSource = reportItems;
                    UpdateGridViewColumns(false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки отчётов: {ex.Message}");
            }
        }

        private int GetGenerationByYear(int? year)
        {
            if (!year.HasValue) return 1;
            if (year < 1950) return 1;
            if (year < 1980) return 2;
            if (year < 2000) return 3;
            if (year < 2020) return 4;
            return 5;
        }

        private string GetSpeciesName(int speciesId, GenealogyUnifiedDBEntities context)
        {
            var species = context.Species.Find(speciesId);
            return species?.Name ?? "Неизвестно";
        }

        private string GetBreedName(int? breedId, GenealogyUnifiedDBEntities context)
        {
            if (breedId == null) return "—";
            var breed = context.Breeds.Find(breedId);
            return breed?.Name ?? "—";
        }

        private string GetColorName(int? colorId, GenealogyUnifiedDBEntities context)
        {
            if (colorId == null) return "—";
            var color = context.Colors.Find(colorId);
            return color?.Name ?? "—";
        }

        private string GetAnimalGenderName(int genderId, GenealogyUnifiedDBEntities context)
        {
            var gender = context.AnimalGenders.Find(genderId);
            return gender?.Name ?? "—";
        }

        private string GetPedigreeClassName(int? classId, GenealogyUnifiedDBEntities context)
        {
            if (classId == null) return "—";
            var pc = context.PedigreeClasses.Find(classId);
            return pc?.Name ?? "—";
        }

        private void UpdateGridViewColumns(bool isFamilyMode)
        {
            gridViewColumns.Columns.Clear();

            if (isFamilyMode)
            {
                gridViewColumns.Columns.Add(new GridViewColumn { Header = "ID", Width = 50, DisplayMemberBinding = new System.Windows.Data.Binding("Id") });
                gridViewColumns.Columns.Add(new GridViewColumn { Header = "Фамилия", Width = 120, DisplayMemberBinding = new System.Windows.Data.Binding("LastName") });
                gridViewColumns.Columns.Add(new GridViewColumn { Header = "Имя", Width = 100, DisplayMemberBinding = new System.Windows.Data.Binding("FirstName") });
                gridViewColumns.Columns.Add(new GridViewColumn { Header = "Отчество", Width = 100, DisplayMemberBinding = new System.Windows.Data.Binding("Patronymic") });
                gridViewColumns.Columns.Add(new GridViewColumn { Header = "Дата рождения", Width = 100, DisplayMemberBinding = new System.Windows.Data.Binding("BirthDate") });
                gridViewColumns.Columns.Add(new GridViewColumn { Header = "Дата смерти", Width = 100, DisplayMemberBinding = new System.Windows.Data.Binding("DeathDate") });
                gridViewColumns.Columns.Add(new GridViewColumn { Header = "Пол", Width = 80, DisplayMemberBinding = new System.Windows.Data.Binding("Gender") });
                gridViewColumns.Columns.Add(new GridViewColumn { Header = "Место рождения", Width = 150, DisplayMemberBinding = new System.Windows.Data.Binding("BirthPlace") });
                gridViewColumns.Columns.Add(new GridViewColumn { Header = "Место смерти", Width = 150, DisplayMemberBinding = new System.Windows.Data.Binding("DeathPlace") });
            }
            else
            {
                gridViewColumns.Columns.Add(new GridViewColumn { Header = "ID", Width = 50, DisplayMemberBinding = new System.Windows.Data.Binding("Id") });
                gridViewColumns.Columns.Add(new GridViewColumn { Header = "Кличка", Width = 120, DisplayMemberBinding = new System.Windows.Data.Binding("Nickname") });
                gridViewColumns.Columns.Add(new GridViewColumn { Header = "Инв. номер", Width = 100, DisplayMemberBinding = new System.Windows.Data.Binding("InventoryNumber") });
                gridViewColumns.Columns.Add(new GridViewColumn { Header = "Вид", Width = 100, DisplayMemberBinding = new System.Windows.Data.Binding("Species") });
                gridViewColumns.Columns.Add(new GridViewColumn { Header = "Порода", Width = 100, DisplayMemberBinding = new System.Windows.Data.Binding("Breed") });
                gridViewColumns.Columns.Add(new GridViewColumn { Header = "Масть", Width = 100, DisplayMemberBinding = new System.Windows.Data.Binding("Color") });
                gridViewColumns.Columns.Add(new GridViewColumn { Header = "Пол", Width = 80, DisplayMemberBinding = new System.Windows.Data.Binding("Gender") });
                gridViewColumns.Columns.Add(new GridViewColumn { Header = "Дата рождения", Width = 100, DisplayMemberBinding = new System.Windows.Data.Binding("BirthDate") });
                gridViewColumns.Columns.Add(new GridViewColumn { Header = "Класс", Width = 80, DisplayMemberBinding = new System.Windows.Data.Binding("PedigreeClass") });
            }
        }

        private void ShowEmptyReports()
        {
            txtTotalCount1.Text = "0";
            txtTotalCount2.Text = "0";
            txtTotalCount3.Text = "0";
            txtTotalCount4.Text = "0";
            txtTotalCount5.Text = "0";
            progressMen.Value = 0;
            progressWomen.Value = 0;
            txtMenPercent.Text = "0%";
            txtWomenPercent.Text = "0%";
            txtAverageAge.Text = "---";
            txtOldestPerson.Text = "---";
            txtYoungestPerson.Text = "---";
            lvItems.ItemsSource = null;
            panelExtraStats.Children.Clear();
            panelExtraStats.Children.Add(new TextBlock
            {
                Text = "Нет данных для отображения",
                Foreground = (System.Windows.Media.Brush)FindResource("Brush8B7E6B"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(20)
            });
        }

        private void TreeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbTrees.SelectedItem != null)
            {
                var selectedTree = cmbTrees.SelectedItem as TreeItem;
                if (selectedTree != null)
                {
                    currentTreeId = selectedTree.Id;
                    LoadReports();
                }
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