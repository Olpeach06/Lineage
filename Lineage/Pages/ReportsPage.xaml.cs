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
using Microsoft.Win32;

namespace Lineage.Pages
{
    public partial class ReportsPage : Page
    {
        private int currentTreeId = 0;
        private List<TreeItem> trees = new List<TreeItem>();
        private List<AnimalFilterItem> allAnimals = new List<AnimalFilterItem>();
        private List<PersonReportItem> allPersons = new List<PersonReportItem>();

        // Классы для фильтров
        public class TreeItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class SpeciesItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class BreedItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class ClassItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        // Класс для отображения персоны в отчете
        public class PersonReportItem
        {
            public int Id { get; set; }
            public string LastName { get; set; }
            public string FirstName { get; set; }
            public string Patronymic { get; set; }
            public string Gender { get; set; }
            public string BirthDate { get; set; }
            public string DeathDate { get; set; }
            public string BirthPlace { get; set; }
            public string DeathPlace { get; set; }
        }

        // Класс для отображения животного в отчете
        public class AnimalFilterItem
        {
            public int Id { get; set; }
            public string Nickname { get; set; }
            public string InventoryNumber { get; set; }
            public int SpeciesId { get; set; }
            public string SpeciesName { get; set; }
            public int? BreedId { get; set; }
            public string BreedName { get; set; }
            public int? ColorId { get; set; }
            public string ColorName { get; set; }
            public int GenderId { get; set; }
            public string GenderName { get; set; }
            public DateTime? BirthDate { get; set; }
            public string BirthDateStr { get; set; }
            public int? PedigreeClassId { get; set; }
            public string ClassName { get; set; }
            public bool IsBreedingStock { get; set; }
            public string IsBreedingStockStr { get; set; }
            public bool IsAlive { get; set; }
            public string Status { get; set; }
        }

        // Класс для распределения по классам
        public class ClassDistributionItem
        {
            public string ClassName { get; set; }
            public int Count { get; set; }
            public string Percent { get; set; }
        }

        public ReportsPage()
        {
            InitializeComponent();
            Loaded += Page_Loaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Настройка UI в зависимости от режима
            if (AppSettings.IsFamilyMode)
            {
                txtPageTitle.Text = "СЕМЕЙНОЕ ДРЕВО - ОТЧЕТЫ И СТАТИСТИКА";
                panelFamilyStats.Visibility = Visibility.Visible;
                panelAnimalStats.Visibility = Visibility.Collapsed;
                btnCalendar.Visibility = Visibility.Visible;

                // Скрываем фильтры для животных
                panelSpecies.Visibility = Visibility.Collapsed;
                panelBreed.Visibility = Visibility.Collapsed;
                panelStatus.Visibility = Visibility.Collapsed;
                panelClass.Visibility = Visibility.Collapsed;

                // Настраиваем фильтр пола для людей
                SetupGenderFilterForFamily();
            }
            else
            {
                txtPageTitle.Text = "ПЛЕМЕННАЯ КНИГА - ОТЧЕТЫ И СТАТИСТИКА";
                panelFamilyStats.Visibility = Visibility.Collapsed;
                panelAnimalStats.Visibility = Visibility.Visible;
                btnCalendar.Visibility = Visibility.Collapsed;

                // Показываем фильтры для животных
                panelSpecies.Visibility = Visibility.Visible;
                panelBreed.Visibility = Visibility.Visible;
                panelStatus.Visibility = Visibility.Visible;
                panelClass.Visibility = Visibility.Visible;

                // Настраиваем фильтр пола для животных
                SetupGenderFilterForAnimals();
            }

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
            LoadFilters();

            if (AppSettings.IsFamilyMode)
            {
                LoadPersons();
                ApplyPersonFilters();
            }
            else
            {
                LoadAnimals();
                ApplyAnimalFilters();
            }
        }
        private void SetupGenderFilterForFamily()
        {
            cmbFilterGender.Items.Clear();
            cmbFilterGender.Items.Add("Все");
            cmbFilterGender.Items.Add("Мужской");
            cmbFilterGender.Items.Add("Женский");
            cmbFilterGender.SelectedIndex = 0;
        }

        private void SetupGenderFilterForAnimals()
        {
            cmbFilterGender.Items.Clear();
            cmbFilterGender.Items.Add("Все");
            cmbFilterGender.Items.Add("Самец");
            cmbFilterGender.Items.Add("Самка");
            cmbFilterGender.SelectedIndex = 0;
        }

        private void LoadTrees()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    var userTrees = context.FamilyTrees
                        .Where(t => t.CreatedByUserId == Session.UserId)
                        .OrderBy(t => t.Name)
                        .ToList();

                    trees.Clear();
                    foreach (var tree in userTrees)
                    {
                        // Показываем только проекты соответствующего типа
                        if ((AppSettings.IsFamilyMode && tree.ProjectTypeId == 1) ||
                            (!AppSettings.IsFamilyMode && tree.ProjectTypeId == 2))
                        {
                            trees.Add(new TreeItem { Id = tree.Id, Name = tree.Name });
                        }
                    }

                    cmbFilterTree.ItemsSource = trees;
                    if (trees.Any())
                    {
                        var selected = trees.FirstOrDefault(t => t.Id == currentTreeId);
                        cmbFilterTree.SelectedItem = selected ?? trees.First();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки проектов: {ex.Message}");
            }
        }

        private void LoadFilters()
        {
            if (!AppSettings.IsFamilyMode)
            {
                try
                {
                    using (var context = new GenealogyUnifiedDBEntities1())
                    {
                        // Виды
                        var species = context.Species
                            .Select(s => new SpeciesItem { Id = s.Id, Name = s.Name })
                            .OrderBy(s => s.Name)
                            .ToList();
                        species.Insert(0, new SpeciesItem { Id = 0, Name = "Все виды" });
                        cmbFilterSpecies.ItemsSource = species;
                        cmbFilterSpecies.SelectedIndex = 0;

                        // Племенные классы
                        var classes = context.PedigreeClasses
                            .Select(c => new ClassItem { Id = c.Id, Name = c.Name })
                            .OrderBy(c => c.Name)
                            .ToList();
                        classes.Insert(0, new ClassItem { Id = 0, Name = "Все классы" });
                        cmbFilterClass.ItemsSource = classes;
                        cmbFilterClass.SelectedIndex = 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки фильтров: {ex.Message}");
                }
            }
        }

        private void LoadBreeds(int speciesId)
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    var breeds = context.Breeds
                        .Where(b => speciesId == 0 || b.SpeciesId == speciesId)
                        .Select(b => new BreedItem { Id = b.Id, Name = b.Name })
                        .OrderBy(b => b.Name)
                        .ToList();
                    breeds.Insert(0, new BreedItem { Id = 0, Name = "Все породы" });
                    cmbFilterBreed.ItemsSource = breeds;
                    cmbFilterBreed.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки пород: {ex.Message}");
            }
        }

        // ==================== ЗАГРУЗКА ДАННЫХ ДЛЯ ЛЮДЕЙ ====================

        private void LoadPersons()
        {
            try
            {
                if (cmbFilterTree.SelectedItem == null) return;

                int treeId = (int)cmbFilterTree.SelectedValue;
                allPersons.Clear();

                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    var persons = context.Persons
                        .Where(p => p.TreeId == treeId)
                        .ToList();

                    var genders = context.Genders.ToDictionary(g => g.Id, g => g.Name);

                    foreach (var person in persons)
                    {
                        string birthDateStr = "—";
                        if (person.BirthDate.HasValue)
                        {
                            birthDateStr = person.BirthDate.Value.ToString("dd.MM.yyyy");
                        }

                        string deathDateStr = "—";
                        if (person.DeathDate.HasValue)
                        {
                            deathDateStr = person.DeathDate.Value.ToString("dd.MM.yyyy");
                        }

                        allPersons.Add(new PersonReportItem
                        {
                            Id = person.Id,
                            LastName = person.LastName,
                            FirstName = person.FirstName,
                            Patronymic = person.Patronymic ?? "",
                            Gender = genders.ContainsKey(person.GenderId) ? genders[person.GenderId] : "—",
                            BirthDate = birthDateStr,
                            DeathDate = deathDateStr,
                            BirthPlace = person.BirthPlace ?? "—",
                            DeathPlace = person.DeathPlace ?? "—"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки персон: {ex.Message}");
            }
        }

        // ==================== ЗАГРУЗКА ДАННЫХ ДЛЯ ЖИВОТНЫХ ====================

        private void LoadAnimals()
        {
            try
            {
                if (cmbFilterTree.SelectedItem == null) return;

                int treeId = (int)cmbFilterTree.SelectedValue;
                allAnimals.Clear();

                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    var animals = context.Animals
                        .Where(a => a.TreeId == treeId)
                        .ToList();

                    var speciesList = context.Species.ToDictionary(s => s.Id, s => s.Name);
                    var breedsList = context.Breeds.ToDictionary(b => b.Id, b => b.Name);
                    var gendersList = context.AnimalGenders.ToDictionary(g => g.Id, g => g.Name);
                    var classesList = context.PedigreeClasses.ToDictionary(c => c.Id, c => c.Name);

                    foreach (var animal in animals)
                    {
                        allAnimals.Add(new AnimalFilterItem
                        {
                            Id = animal.Id,
                            Nickname = animal.Nickname,
                            InventoryNumber = animal.InventoryNumber ?? "—",
                            SpeciesId = animal.SpeciesId,
                            SpeciesName = speciesList.ContainsKey(animal.SpeciesId) ? speciesList[animal.SpeciesId] : "Неизвестно",
                            BreedId = animal.BreedId,
                            BreedName = animal.BreedId.HasValue && breedsList.ContainsKey(animal.BreedId.Value) ? breedsList[animal.BreedId.Value] : "—",
                            ColorId = animal.ColorId,
                            ColorName = animal.ColorId.HasValue ? "—" : "—",
                            GenderId = animal.GenderId,
                            GenderName = gendersList.ContainsKey(animal.GenderId) ? gendersList[animal.GenderId] : "—",
                            BirthDate = animal.BirthDate,
                            BirthDateStr = animal.BirthDate?.ToString("dd.MM.yyyy") ?? "—",
                            PedigreeClassId = animal.PedigreeClassId,
                            ClassName = animal.PedigreeClassId.HasValue && classesList.ContainsKey(animal.PedigreeClassId.Value) ? classesList[animal.PedigreeClassId.Value] : "—",
                            IsBreedingStock = animal.IsBreedingStock == true,
                            IsBreedingStockStr = animal.IsBreedingStock == true ? "Да" : "Нет",
                            IsAlive = animal.IsAlive == true,
                            Status = animal.IsAlive == true ? "Живое" : (animal.DeathDate.HasValue ? "Выбыло" : "—")
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки животных: {ex.Message}");
            }
        }

        // ==================== ФИЛЬТРЫ ====================

        private void FilterTree_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbFilterTree.SelectedItem != null)
            {
                int treeId = (int)cmbFilterTree.SelectedValue;
                currentTreeId = treeId;
                Session.CurrentTreeId = treeId;

                if (AppSettings.IsFamilyMode)
                {
                    LoadPersons();
                    ApplyPersonFilters();
                }
                else
                {
                    LoadAnimals();
                    ApplyAnimalFilters();
                }
            }
        }

        private void FilterSpecies_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbFilterSpecies.SelectedItem != null)
            {
                int speciesId = (int)cmbFilterSpecies.SelectedValue;
                LoadBreeds(speciesId);
                ApplyAnimalFilters();
            }
        }

        private void ApplyFilters_Click(object sender, RoutedEventArgs e)
        {
            if (AppSettings.IsFamilyMode)
            {
                ApplyPersonFilters();
            }
            else
            {
                ApplyAnimalFilters();
            }
        }

        private void ApplyPersonFilters()
        {
            if (!allPersons.Any())
            {
                ShowEmptyPersonReports();
                return;
            }

            var filtered = allPersons.AsEnumerable();

            // Фильтр по полу (для людей)
            string selectedGender = cmbFilterGender.SelectedItem?.ToString();
            if (selectedGender == "Мужской")
            {
                filtered = filtered.Where(p => p.Gender == "Мужской");
            }
            else if (selectedGender == "Женский")
            {
                filtered = filtered.Where(p => p.Gender == "Женский");
            }

            var resultList = filtered.ToList();

            UpdatePersonStatistics(resultList);
            UpdatePersonNames(resultList);

            lvPersons.ItemsSource = resultList;
            txtNoPersons.Visibility = resultList.Any() ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ApplyAnimalFilters()
        {
            if (!allAnimals.Any())
            {
                ShowEmptyAnimalReports();
                return;
            }

            var filtered = allAnimals.AsEnumerable();

            // Фильтр по виду
            if (cmbFilterSpecies.SelectedItem != null && (int)cmbFilterSpecies.SelectedValue > 0)
            {
                int speciesId = (int)cmbFilterSpecies.SelectedValue;
                filtered = filtered.Where(a => a.SpeciesId == speciesId);
            }

            // Фильтр по породе
            if (cmbFilterBreed.SelectedItem != null && (int)cmbFilterBreed.SelectedValue > 0)
            {
                int breedId = (int)cmbFilterBreed.SelectedValue;
                filtered = filtered.Where(a => a.BreedId == breedId);
            }

            // Фильтр по классу
            if (cmbFilterClass.SelectedItem != null && (int)cmbFilterClass.SelectedValue > 0)
            {
                int classId = (int)cmbFilterClass.SelectedValue;
                filtered = filtered.Where(a => a.PedigreeClassId == classId);
            }

            // Фильтр по статусу
            if (cmbFilterStatus.SelectedIndex == 1)
            {
                filtered = filtered.Where(a => a.IsAlive);
            }
            else if (cmbFilterStatus.SelectedIndex == 2)
            {
                filtered = filtered.Where(a => a.IsBreedingStock);
            }
            else if (cmbFilterStatus.SelectedIndex == 3)
            {
                filtered = filtered.Where(a => !a.IsAlive);
            }

            // Фильтр по полу (для животных)
            string selectedGender = cmbFilterGender.SelectedItem?.ToString();
            if (selectedGender == "Самец")
            {
                filtered = filtered.Where(a => a.GenderName == "Самец");
            }
            else if (selectedGender == "Самка")
            {
                filtered = filtered.Where(a => a.GenderName == "Самка");
            }

            var resultList = filtered.ToList();

            UpdateAnimalStatistics(resultList);
            UpdateClassDistribution(resultList);
            UpdateBreedingStatistics();
            UpdateProductivityStatistics();

            lvAnimals.ItemsSource = resultList;
            txtNoAnimals.Visibility = resultList.Any() ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ResetFilters_Click(object sender, RoutedEventArgs e)
        {
            cmbFilterGender.SelectedIndex = 0;

            if (!AppSettings.IsFamilyMode)
            {
                cmbFilterSpecies.SelectedIndex = 0;
                LoadBreeds(0);
                cmbFilterClass.SelectedIndex = 0;
                cmbFilterStatus.SelectedIndex = 0;
            }

            ApplyFilters_Click(null, null);
        }

        // ==================== СТАТИСТИКА ДЛЯ ЛЮДЕЙ ====================

        private void UpdatePersonStatistics(List<PersonReportItem> persons)
        {
            int total = persons.Count;
            int men = persons.Count(p => p.Gender == "Мужской");
            int women = persons.Count(p => p.Gender == "Женский");
            int deceased = persons.Count(p => p.DeathDate != "—");

            // Расчёт количества браков (RelationshipType = 2)
            int marriages = 0;
            try
            {
                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    // Получаем ID персон из отфильтрованного списка
                    var personIds = persons.Select(p => p.Id).ToList();

                    // Считаем уникальные пары супругов (RelationshipType = 2)
                    // Каждый брак представлен одной записью, поэтому просто считаем количество
                    marriages = context.PersonRelationships
                        .Where(r => personIds.Contains(r.Person1Id) &&
                                    personIds.Contains(r.Person2Id) &&
                                    r.RelationshipType == 2)
                        .Count();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка расчёта браков: {ex.Message}");
            }

            // Расчёт количества поколений
            int generations = 0;
            try
            {
                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    var personIds = persons.Select(p => p.Id).ToList();
                    var allRelationships = context.PersonRelationships
                        .Where(r => personIds.Contains(r.Person1Id) && personIds.Contains(r.Person2Id))
                        .ToList();

                    // Строим дерево для определения поколений
                    var childrenDict = new Dictionary<int, List<int>>();
                    foreach (var person in persons)
                    {
                        childrenDict[person.Id] = new List<int>();
                    }

                    foreach (var rel in allRelationships.Where(r => r.RelationshipType == 1))
                    {
                        if (!childrenDict.ContainsKey(rel.Person1Id))
                            childrenDict[rel.Person1Id] = new List<int>();
                        childrenDict[rel.Person1Id].Add(rel.Person2Id);
                    }

                    // Находим корни (те, у кого нет родителей)
                    var allChildren = childrenDict.Values.SelectMany(v => v).Distinct().ToHashSet();
                    var roots = persons.Where(p => !allChildren.Contains(p.Id)).ToList();

                    // Вычисляем глубину дерева
                    var generationMap = new Dictionary<int, int>();
                    foreach (var root in roots)
                    {
                        CalculateGenerationDepth(root.Id, 0, childrenDict, generationMap);
                    }

                    generations = generationMap.Any() ? generationMap.Values.Max() + 1 : 1;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка расчёта поколений: {ex.Message}");
                generations = 1;
            }

            txtTotalPersons.Text = total.ToString();
            txtPersonGenderRatio.Text = $"{men}/{women}";
            txtMarriages.Text = marriages.ToString();
            txtDeceased.Text = deceased.ToString();
            txtGenerations.Text = generations.ToString();

            // Возрастная статистика
            var today = DateTime.Today;
            var personsWithAge = persons
                .Where(p => p.BirthDate != "—" && p.BirthDate != "?")
                .Select(p => new { Person = p, BirthDate = DateTime.ParseExact(p.BirthDate, "dd.MM.yyyy", null) })
                .ToList();

            if (personsWithAge.Any())
            {
                // Средний возраст
                var livingPersons = personsWithAge
                    .Where(p => p.Person.DeathDate == "—" || p.Person.DeathDate == "...")
                    .ToList();

                if (livingPersons.Any())
                {
                    double totalAge = 0;
                    foreach (var p in livingPersons)
                    {
                        int age = today.Year - p.BirthDate.Year;
                        if (today < p.BirthDate.AddYears(age)) age--;
                        totalAge += age;
                    }
                    double avgAge = totalAge / livingPersons.Count;
                    txtAvgAgePerson.Text = $"{avgAge:F1} лет";
                }
                else
                {
                    txtAvgAgePerson.Text = "Нет живых";
                }

                // Самый старший
                var oldest = personsWithAge.OrderBy(p => p.BirthDate).FirstOrDefault();
                if (oldest != null)
                {
                    int age = today.Year - oldest.BirthDate.Year;
                    if (today < oldest.BirthDate.AddYears(age)) age--;
                    txtOldestPerson.Text = $"{oldest.Person.LastName} {oldest.Person.FirstName} ({age} лет)";
                }

                // Самый младший (живой)
                var youngest = livingPersons.OrderByDescending(p => p.BirthDate).FirstOrDefault();
                if (youngest != null)
                {
                    int age = today.Year - youngest.BirthDate.Year;
                    if (today < youngest.BirthDate.AddYears(age)) age--;
                    txtYoungestPerson.Text = $"{youngest.Person.LastName} {youngest.Person.FirstName} ({age} лет)";
                }
                else
                {
                    txtYoungestPerson.Text = "—";
                }
            }
            else
            {
                txtAvgAgePerson.Text = "—";
                txtOldestPerson.Text = "—";
                txtYoungestPerson.Text = "—";
            }

            // Распределение по полу
            double menPercent = total > 0 ? (men * 100.0 / total) : 0;
            double womenPercent = total > 0 ? (women * 100.0 / total) : 0;
            progressMen.Value = menPercent;
            progressWomen.Value = womenPercent;
            txtMenPercent.Text = $"{menPercent:F1}% мужчин";
            txtWomenPercent.Text = $"{womenPercent:F1}% женщин";
        }

        // Вспомогательный метод для расчёта глубины поколений
        private void CalculateGenerationDepth(int personId, int depth, Dictionary<int, List<int>> childrenDict, Dictionary<int, int> generationMap)
        {
            if (generationMap.ContainsKey(personId) && generationMap[personId] >= depth)
                return;

            generationMap[personId] = depth;

            if (childrenDict.ContainsKey(personId))
            {
                foreach (var childId in childrenDict[personId])
                {
                    CalculateGenerationDepth(childId, depth + 1, childrenDict, generationMap);
                }
            }
        }

        private void UpdatePersonNames(List<PersonReportItem> persons)
        {
            var maleNames = persons.Where(p => p.Gender == "Мужской")
                .GroupBy(p => p.FirstName)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();

            var femaleNames = persons.Where(p => p.Gender == "Женский")
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

            icMaleNames.ItemsSource = maleNames.Select(n => $"{n.Name} — {n.Count}");
            icFemaleNames.ItemsSource = femaleNames.Select(n => $"{n.Name} — {n.Count}");
            icSurnames.ItemsSource = surnames.Select(n => $"{n.Name} — {n.Count}");
        }

        // ==================== СТАТИСТИКА ДЛЯ ЖИВОТНЫХ ====================

        private void UpdateAnimalStatistics(List<AnimalFilterItem> animals)
        {
            int total = animals.Count;
            int males = animals.Count(a => a.GenderName == "Самец");
            int females = animals.Count(a => a.GenderName == "Самка");
            int breedingStock = animals.Count(a => a.IsBreedingStock);
            int alive = animals.Count(a => a.IsAlive);
            int elite = animals.Count(a => a.ClassName == "Элита");

            // Средний возраст (в месяцах)
            double avgAgeMonths = 0;
            var animalsWithBirthDate = animals.Where(a => a.BirthDate.HasValue && a.IsAlive).ToList();
            if (animalsWithBirthDate.Any())
            {
                var today = DateTime.Today;
                double totalMonths = 0;
                foreach (var a in animalsWithBirthDate)
                {
                    var age = today.Year - a.BirthDate.Value.Year;
                    if (today < a.BirthDate.Value.AddYears(age)) age--;
                    totalMonths += age * 12;
                }
                avgAgeMonths = totalMonths / animalsWithBirthDate.Count;
            }

            txtTotalAnimals.Text = total.ToString();
            txtAnimalGenderRatio.Text = $"{males}/{females}";
            txtBreedingStock.Text = breedingStock.ToString();
            txtAliveAnimals.Text = alive.ToString();
            txtAvgAgeAnimal.Text = avgAgeMonths > 0 ? $"{avgAgeMonths:F0}" : "0";
            txtEliteCount.Text = elite.ToString();
        }

        private void UpdateClassDistribution(List<AnimalFilterItem> animals)
        {
            var classDistribution = animals
                .Where(a => a.PedigreeClassId.HasValue && a.ClassName != "—")
                .GroupBy(a => a.ClassName)
                .Select(g => new ClassDistributionItem
                {
                    ClassName = g.Key,
                    Count = g.Count(),
                    Percent = animals.Any() ? $"{(g.Count() * 100.0 / animals.Count):F1}%" : "0%"
                })
                .OrderByDescending(c => c.Count)
                .ToList();

            int noClassCount = animals.Count(a => !a.PedigreeClassId.HasValue || a.ClassName == "—");
            if (noClassCount > 0)
            {
                classDistribution.Add(new ClassDistributionItem
                {
                    ClassName = "Не указан",
                    Count = noClassCount,
                    Percent = animals.Any() ? $"{(noClassCount * 100.0 / animals.Count):F1}%" : "0%"
                });
            }

            icClassDistribution.ItemsSource = classDistribution;
        }

        private void UpdateBreedingStatistics()
        {
            try
            {
                if (cmbFilterTree.SelectedItem == null) return;

                int treeId = (int)cmbFilterTree.SelectedValue;

                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    var breedings = context.Breedings
                        .Where(b => b.TreeId == treeId)
                        .ToList();

                    int total = breedings.Count;
                    int successful = breedings.Count(b => b.IsSuccessful == true);
                    double successRate = total > 0 ? (successful * 100.0 / total) : 0;
                    int totalOffspring = breedings.Sum(b => b.OffspringCount ?? 0);
                    int totalAlive = breedings.Sum(b => b.AliveCount ?? 0);

                    txtTotalBreedings.Text = total.ToString();
                    txtSuccessfulBreedings.Text = successful.ToString();
                    txtSuccessRate.Text = $"{successRate:F1}%";
                    txtTotalOffspring.Text = totalOffspring.ToString();
                    txtAliveOffspring.Text = totalAlive.ToString();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки статистики воспроизводства: {ex.Message}");
            }
        }

        private void UpdateProductivityStatistics()
        {
            try
            {
                if (cmbFilterTree.SelectedItem == null) return;

                int treeId = (int)cmbFilterTree.SelectedValue;

                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    var animalIds = context.Animals
                        .Where(a => a.TreeId == treeId)
                        .Select(a => a.Id)
                        .ToList();

                    var productivityRecords = context.ProductivityRecords
                        .Where(p => animalIds.Contains(p.AnimalId) && p.RecordType == "lactation")
                        .ToList();

                    if (productivityRecords.Any())
                    {
                        double avgMilk = (double)productivityRecords.Average(p => p.MilkYield ?? 0);
                        double avgFat = (double)productivityRecords.Average(p => p.FatContent ?? 0);
                        double avgProtein = (double)productivityRecords.Average(p => p.ProteinContent ?? 0);
                        double maxMilk = (double)productivityRecords.Max(p => p.MilkYield ?? 0);

                        txtAvgMilkYield.Text = $"{avgMilk:F0} кг";
                        txtAvgFatContent.Text = $"{avgFat:F2}%";
                        txtAvgProteinContent.Text = $"{avgProtein:F2}%";
                        txtMaxMilkYield.Text = $"{maxMilk:F0} кг";
                    }
                    else
                    {
                        txtAvgMilkYield.Text = "Нет данных";
                        txtAvgFatContent.Text = "Нет данных";
                        txtAvgProteinContent.Text = "Нет данных";
                        txtMaxMilkYield.Text = "Нет данных";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки продуктивности: {ex.Message}");
            }
        }

        // ==================== ПУСТЫЕ СОСТОЯНИЯ ====================

        private void ShowEmptyPersonReports()
        {
            txtTotalPersons.Text = "0";
            txtPersonGenderRatio.Text = "0/0";
            txtMarriages.Text = "0";
            txtDeceased.Text = "0";
            txtGenerations.Text = "0";
            txtAvgAgePerson.Text = "—";
            txtOldestPerson.Text = "—";
            txtYoungestPerson.Text = "—";
            progressMen.Value = 0;
            progressWomen.Value = 0;
            txtMenPercent.Text = "0% мужчин";
            txtWomenPercent.Text = "0% женщин";
            icMaleNames.ItemsSource = null;
            icFemaleNames.ItemsSource = null;
            icSurnames.ItemsSource = null;
            lvPersons.ItemsSource = null;
            txtNoPersons.Visibility = Visibility.Visible;
        }

        private void ShowEmptyAnimalReports()
        {
            txtTotalAnimals.Text = "0";
            txtAnimalGenderRatio.Text = "0/0";
            txtBreedingStock.Text = "0";
            txtAliveAnimals.Text = "0";
            txtAvgAgeAnimal.Text = "0";
            txtEliteCount.Text = "0";
            icClassDistribution.ItemsSource = null;
            txtTotalBreedings.Text = "0";
            txtSuccessfulBreedings.Text = "0";
            txtSuccessRate.Text = "0%";
            txtTotalOffspring.Text = "0";
            txtAliveOffspring.Text = "0";
            txtAvgMilkYield.Text = "Нет данных";
            txtAvgFatContent.Text = "Нет данных";
            txtAvgProteinContent.Text = "Нет данных";
            txtMaxMilkYield.Text = "Нет данных";
            lvAnimals.ItemsSource = null;
            txtNoAnimals.Visibility = Visibility.Visible;
        }

        // ==================== ЭКСПОРТ ====================

        private void ExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "CSV файлы (*.csv)|*.csv",
                FileName = AppSettings.IsFamilyMode
                    ? $"Отчет_персоны_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                    : $"Отчет_животные_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var sb = new System.Text.StringBuilder();

                    if (AppSettings.IsFamilyMode)
                    {
                        if (lvPersons.ItemsSource == null) return;
                        var items = lvPersons.ItemsSource as List<PersonReportItem>;
                        if (items == null || !items.Any())
                        {
                            MessageBox.Show("Нет данных для экспорта!");
                            return;
                        }

                        sb.AppendLine("ID;Фамилия;Имя;Отчество;Пол;Дата рождения;Дата смерти;Место рождения;Место смерти");
                        foreach (var item in items)
                        {
                            sb.AppendLine($"{item.Id};{item.LastName};{item.FirstName};{item.Patronymic};{item.Gender};{item.BirthDate};{item.DeathDate};{item.BirthPlace};{item.DeathPlace}");
                        }
                    }
                    else
                    {
                        if (lvAnimals.ItemsSource == null) return;
                        var items = lvAnimals.ItemsSource as List<AnimalFilterItem>;
                        if (items == null || !items.Any())
                        {
                            MessageBox.Show("Нет данных для экспорта!");
                            return;
                        }

                        sb.AppendLine("ID;Кличка;Инв.номер;Вид;Порода;Пол;Дата рождения;Класс;Племенное;Статус");
                        foreach (var item in items)
                        {
                            sb.AppendLine($"{item.Id};{item.Nickname};{item.InventoryNumber};{item.SpeciesName};{item.BreedName};{item.GenderName};{item.BirthDateStr};{item.ClassName};{item.IsBreedingStockStr};{item.Status}");
                        }
                    }

                    System.IO.File.WriteAllText(saveDialog.FileName, sb.ToString(), System.Text.Encoding.UTF8);
                    MessageBox.Show($"Экспорт выполнен!\nФайл сохранён: {saveDialog.FileName}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ==================== НАВИГАЦИЯ ====================

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
        private void CalendarButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbFilterTree.SelectedItem == null)
                {
                    MessageBox.Show("Выберите проект!", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                int treeId = (int)cmbFilterTree.SelectedValue;
                string treeName = (cmbFilterTree.SelectedItem as TreeItem)?.Name ?? "Неизвестно";

                var calendarWindow = new CalendarWindow(treeId, treeName)
                {
                    Owner = Window.GetWindow(this)
                };
                calendarWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия календаря: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}