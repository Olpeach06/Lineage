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
using System.IO;

namespace Lineage.Pages
{
    public partial class MainPage : Page
    {
        private int currentTreeId = 0;
        private int? selectedItemId = null;
        private Dictionary<int, Border> itemCards = new Dictionary<int, Border>();
        private List<Line> relationshipLines = new List<Line>();

        // Для перетаскивания карточек
        private bool isDragging = false;
        private Border draggedCard = null;
        private Point dragStartPoint;

        // Для перетаскивания канваса
        private bool isCanvasDragging = false;
        private Point canvasDragStart;
        private ScrollViewer parentScrollViewer;

        // Для поиска
        private string currentSearchText = "";
        private System.Windows.Threading.DispatcherTimer searchTimer;

        // Данные для отображения связей
        private List<PersonRelationships> personRelationships = new List<PersonRelationships>();
        private List<AnimalPedigree> animalPedigrees = new List<AnimalPedigree>();

        // Структура для позиционирования
        private Dictionary<int, List<int>> familyTreeStructure = new Dictionary<int, List<int>>();
        private Dictionary<int, int> generations = new Dictionary<int, int>();

        public MainPage()
        {
            InitializeComponent();
            Loaded += Page_Loaded;
            searchTimer = new System.Windows.Threading.DispatcherTimer();
            searchTimer.Interval = TimeSpan.FromMilliseconds(500);
            searchTimer.Tick += SearchTimer_Tick;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (Session.IsGuest)
            {
                txtUserName.Text = "Гость";
                btnAdd.Visibility = Visibility.Collapsed;
                btnEdit.Visibility = Visibility.Collapsed;
                btnDelete.Visibility = Visibility.Collapsed;
                btnSideEdit.Visibility = Visibility.Collapsed;
                btnSideStory.Visibility = Visibility.Collapsed;
                btnUsers.Visibility = Visibility.Collapsed;
            }
            else
            {
                txtUserName.Text = Session.Username;
                bool canEdit = Session.IsAdmin || Session.IsEditor;
                bool isAdmin = Session.IsAdmin;

                btnAdd.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
                btnEdit.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
                btnDelete.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
                btnSideEdit.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
                btnSideStory.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
                btnUsers.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
            }

            currentTreeId = Session.CurrentTreeId;
            parentScrollViewer = FindVisualChild<ScrollViewer>(this);

            SetupFilter();
            LoadTree();
        }

        private void SetupFilter()
        {
            cmbFilter.Items.Clear();

            if (AppSettings.IsFamilyMode)
            {
                cmbFilter.Items.Add("Все поколения");
                cmbFilter.Items.Add("Поколение 1");
                cmbFilter.Items.Add("Поколение 2");
                cmbFilter.Items.Add("Поколение 3");
                cmbFilter.Items.Add("Поколение 4");
                cmbFilter.Items.Add("Поколение 5+");
            }
            else
            {
                cmbFilter.Items.Add("Все породы");
                cmbFilter.Items.Add("Все виды");
                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    var animals = context.Animals.Where(a => a.TreeId == currentTreeId).ToList();
                    var breedIds = animals.Where(a => a.BreedId.HasValue).Select(a => a.BreedId.Value).Distinct().ToList();

                    var breeds = context.Breeds
                        .Where(b => breedIds.Contains(b.Id))
                        .Select(b => b.Name)
                        .OrderBy(b => b)
                        .ToList();

                    foreach (var breed in breeds)
                    {
                        cmbFilter.Items.Add(breed);
                    }
                }
            }
            cmbFilter.SelectedIndex = 0;
        }

        private void LoadTree()
        {
            if (treeCanvas == null) return;
            treeCanvas.Children.Clear();
            itemCards.Clear();
            relationshipLines.Clear();
            familyTreeStructure.Clear();
            generations.Clear();

            if (AppSettings.IsFamilyMode)
            {
                LoadPersonsTree();
            }
            else
            {
                LoadAnimalsTree();
            }

            if (selectedItemId.HasValue)
            {
                ShowItemDetails(selectedItemId.Value);
            }
        }

        // ==================== ЛЮДИ ====================

        private void LoadPersonsTree()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    var persons = context.Persons
                        .Where(p => p.TreeId == currentTreeId)
                        .ToList();

                    if (!persons.Any())
                    {
                        ShowEmptyMessage();
                        return;
                    }

                    // Получаем ID всех персон
                    var personIds = persons.Select(p => p.Id).ToList();

                    // Загружаем связи - используем Contains с int, а не с объектами
                    personRelationships = context.PersonRelationships
                        .Where(r => personIds.Contains(r.Person1Id) && personIds.Contains(r.Person2Id))
                        .ToList();

                    // Словарь для быстрого доступа
                    var personDict = persons.ToDictionary(p => p.Id, p => p);

                    // Строим иерархию: для каждого человека определяем детей
                    var childrenDict = new Dictionary<int, List<int>>();
                    foreach (var person in persons)
                    {
                        childrenDict[person.Id] = new List<int>();
                    }

                    foreach (var rel in personRelationships)
                    {
                        if (rel.RelationshipType == 1) // родитель-ребенок
                        {
                            if (!childrenDict.ContainsKey(rel.Person1Id))
                                childrenDict[rel.Person1Id] = new List<int>();
                            childrenDict[rel.Person1Id].Add(rel.Person2Id);
                        }
                    }

                    // Определяем корневые элементы (те, у кого нет родителей)
                    var allChildren = childrenDict.Values.SelectMany(v => v).Distinct().ToHashSet();
                    var roots = persons.Where(p => !allChildren.Contains(p.Id)).ToList();

                    // Если нет корневых, берем самых старших по дате рождения
                    if (!roots.Any() && persons.Any(p => p.BirthDate.HasValue))
                    {
                        var oldestBirthYear = persons.Where(p => p.BirthDate.HasValue).Min(p => p.BirthDate.Value.Year);
                        roots = persons.Where(p => p.BirthDate.HasValue && p.BirthDate.Value.Year == oldestBirthYear).ToList();
                    }

                    if (!roots.Any() && persons.Any())
                    {
                        roots = persons.Take(1).ToList();
                    }

                    // Вычисляем поколения рекурсивно
                    var generationMap = new Dictionary<int, int>();
                    foreach (var root in roots)
                    {
                        CalculateGenerations(root.Id, 0, childrenDict, generationMap);
                    }

                    // Для оставшихся (если есть) присваиваем поколение по году рождения
                    foreach (var person in persons)
                    {
                        if (!generationMap.ContainsKey(person.Id))
                        {
                            if (person.BirthDate.HasValue)
                            {
                                int year = person.BirthDate.Value.Year;
                                if (year < 1950) generationMap[person.Id] = 0;
                                else if (year < 1980) generationMap[person.Id] = 1;
                                else if (year < 2000) generationMap[person.Id] = 2;
                                else if (year < 2020) generationMap[person.Id] = 3;
                                else generationMap[person.Id] = 4;
                            }
                            else
                            {
                                generationMap[person.Id] = 0;
                            }
                        }
                    }

                    generations = generationMap;

                    // Группируем по поколениям
                    var personsByGeneration = persons
                        .GroupBy(p => generationMap[p.Id])
                        .OrderBy(g => g.Key)
                        .ToList();

                    // Вычисляем позиции
                    int startX = 100;
                    int startY = 80;
                    int verticalSpacing = 140;
                    int horizontalSpacing = 220;
                    int maxInGeneration = personsByGeneration.Any() ? personsByGeneration.Max(g => g.Count()) : 0;

                    foreach (var genGroup in personsByGeneration)
                    {
                        int genLevel = genGroup.Key;
                        var personsInGen = genGroup.ToList();
                        int itemsCount = personsInGen.Count;

                        // Центрируем поколение
                        int totalWidth = itemsCount * horizontalSpacing;
                        int xOffset = Math.Max(0, (maxInGeneration - itemsCount) * horizontalSpacing / 2);
                        int currentX = startX + xOffset;

                        foreach (var person in personsInGen)
                        {
                            string genderSymbol = person.GenderId == 1 ? "♂" : (person.GenderId == 2 ? "♀" : "👤");
                            string birthYear = person.BirthDate?.Year.ToString() ?? "?";
                            string deathYear = person.DeathDate?.Year.ToString() ?? "";
                            string dateInfo = birthYear + (string.IsNullOrEmpty(deathYear) ? "" : $" - {deathYear}");

                            var card = CreatePersonCard(person.Id, $"{person.LastName} {person.FirstName}".Trim(),
                                $"{genderSymbol} {dateInfo}", person.ProfilePhotoPath);

                            Canvas.SetLeft(card, currentX);
                            Canvas.SetTop(card, startY + genLevel * verticalSpacing);
                            treeCanvas.Children.Add(card);
                            itemCards[person.Id] = card;

                            currentX += horizontalSpacing;
                        }
                    }

                    // Рисуем связи
                    DrawPersonRelationships();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}");
            }
        }

        private void CalculateGenerations(int personId, int genLevel, Dictionary<int, List<int>> childrenDict, Dictionary<int, int> generationMap)
        {
            if (generationMap.ContainsKey(personId) && generationMap[personId] >= genLevel)
                return;

            generationMap[personId] = genLevel;

            if (childrenDict.ContainsKey(personId))
            {
                foreach (var childId in childrenDict[personId])
                {
                    CalculateGenerations(childId, genLevel + 1, childrenDict, generationMap);
                }
            }
        }

        private void DrawPersonRelationships()
        {
            foreach (var rel in personRelationships)
            {
                // Проверяем, что оба ID есть в itemCards
                if (rel.RelationshipType == 1 && itemCards.ContainsKey(rel.Person1Id) && itemCards.ContainsKey(rel.Person2Id))
                {
                    var parentCard = itemCards[rel.Person1Id];
                    var childCard = itemCards[rel.Person2Id];

                    if (parentCard != null && childCard != null)
                    {
                        Point parentBottom = new Point(
                            Canvas.GetLeft(parentCard) + parentCard.Width / 2,
                            Canvas.GetTop(parentCard) + parentCard.Height);

                        Point childTop = new Point(
                            Canvas.GetLeft(childCard) + childCard.Width / 2,
                            Canvas.GetTop(childCard));

                        var line = new Line
                        {
                            X1 = parentBottom.X,
                            Y1 = parentBottom.Y,
                            X2 = childTop.X,
                            Y2 = childTop.Y,
                            Stroke = new SolidColorBrush(System.Windows.Media.Colors.Gray),
                            StrokeThickness = 2,
                            Tag = $"line_{rel.Person1Id}_{rel.Person2Id}"
                        };

                        Canvas.SetZIndex(line, 0);
                        treeCanvas.Children.Add(line);
                        relationshipLines.Add(line);
                    }
                }
                else if (rel.RelationshipType == 2 && itemCards.ContainsKey(rel.Person1Id) && itemCards.ContainsKey(rel.Person2Id))
                {
                    var card1 = itemCards[rel.Person1Id];
                    var card2 = itemCards[rel.Person2Id];

                    if (card1 != null && card2 != null)
                    {
                        Point p1 = new Point(
                            Canvas.GetLeft(card1) + card1.Width,
                            Canvas.GetTop(card1) + card1.Height / 2);

                        Point p2 = new Point(
                            Canvas.GetLeft(card2),
                            Canvas.GetTop(card2) + card2.Height / 2);

                        var line = new Line
                        {
                            X1 = p1.X,
                            Y1 = p1.Y,
                            X2 = p2.X,
                            Y2 = p2.Y,
                            Stroke = new SolidColorBrush(System.Windows.Media.Colors.Gold),
                            StrokeThickness = 2,
                            StrokeDashArray = new DoubleCollection { 4, 2 },
                            Tag = $"spouse_{rel.Person1Id}_{rel.Person2Id}"
                        };

                        Canvas.SetZIndex(line, 0);
                        treeCanvas.Children.Add(line);
                        relationshipLines.Add(line);
                    }
                }
            }
        }

        // ==================== ЖИВОТНЫЕ ====================

        private void LoadAnimalsTree()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    var animals = context.Animals
                        .Where(a => a.TreeId == currentTreeId)
                        .ToList();

                    if (!animals.Any())
                    {
                        ShowEmptyMessage();
                        return;
                    }

                    var animalIds = animals.Select(a => a.Id).ToList();

                    // Загружаем родословные связи - используем Contains с int
                    animalPedigrees = context.AnimalPedigree
                        .Where(p => animalIds.Contains(p.AnimalId))
                        .ToList();

                    var animalDict = animals.ToDictionary(a => a.Id, a => a);

                    // Строим иерархию по родословной
                    var childrenDict = new Dictionary<int, List<int>>();
                    foreach (var animal in animals)
                    {
                        childrenDict[animal.Id] = new List<int>();
                    }

                    foreach (var pedigree in animalPedigrees)
                    {
                        if (pedigree.FatherId.HasValue && childrenDict.ContainsKey(pedigree.FatherId.Value))
                            childrenDict[pedigree.FatherId.Value].Add(pedigree.AnimalId);
                        if (pedigree.MotherId.HasValue && childrenDict.ContainsKey(pedigree.MotherId.Value))
                            childrenDict[pedigree.MotherId.Value].Add(pedigree.AnimalId);
                    }

                    // Находим корневые элементы (те, у кого нет родителей)
                    var allChildren = childrenDict.Values.SelectMany(v => v).Distinct().ToHashSet();
                    var roots = animals.Where(a => !allChildren.Contains(a.Id)).ToList();

                    // Если нет корневых, берем самых старших по дате рождения
                    if (!roots.Any() && animals.Any(a => a.BirthDate.HasValue))
                    {
                        var oldestBirthYear = animals.Where(a => a.BirthDate.HasValue).Min(a => a.BirthDate.Value.Year);
                        roots = animals.Where(a => a.BirthDate.HasValue && a.BirthDate.Value.Year == oldestBirthYear).ToList();
                    }

                    if (!roots.Any() && animals.Any())
                    {
                        roots = animals.Take(1).ToList();
                    }

                    // Вычисляем поколения
                    var generationMap = new Dictionary<int, int>();
                    foreach (var root in roots)
                    {
                        CalculateAnimalGenerations(root.Id, 0, childrenDict, generationMap);
                    }

                    // Для оставшихся присваиваем поколение по году рождения
                    foreach (var animal in animals)
                    {
                        if (!generationMap.ContainsKey(animal.Id))
                        {
                            if (animal.BirthDate.HasValue)
                            {
                                int year = animal.BirthDate.Value.Year;
                                if (year < 2015) generationMap[animal.Id] = 0;
                                else if (year < 2018) generationMap[animal.Id] = 1;
                                else if (year < 2021) generationMap[animal.Id] = 2;
                                else generationMap[animal.Id] = 3;
                            }
                            else
                            {
                                generationMap[animal.Id] = 0;
                            }
                        }
                    }

                    generations = generationMap;

                    // Группируем по поколениям
                    var animalsByGeneration = animals
                        .GroupBy(a => generationMap[a.Id])
                        .OrderBy(g => g.Key)
                        .ToList();

                    int startX = 100;
                    int startY = 80;
                    int verticalSpacing = 140;
                    int horizontalSpacing = 220;
                    int maxInGeneration = animalsByGeneration.Any() ? animalsByGeneration.Max(g => g.Count()) : 0;

                    foreach (var genGroup in animalsByGeneration)
                    {
                        int genLevel = genGroup.Key;
                        var animalsInGen = genGroup.ToList();
                        int itemsCount = animalsInGen.Count;

                        int totalWidth = itemsCount * horizontalSpacing;
                        int xOffset = Math.Max(0, (maxInGeneration - itemsCount) * horizontalSpacing / 2);
                        int currentX = startX + xOffset;

                        foreach (var animal in animalsInGen)
                        {
                            string speciesIcon = GetSpeciesIcon(animal.SpeciesId);
                            string breedName = GetBreedName(animal.BreedId);
                            string genderSymbol = animal.GenderId == 1 ? "♂" : (animal.GenderId == 2 ? "♀" : "⚲");
                            string info = $"{speciesIcon} {genderSymbol} | {breedName}";

                            var card = CreateAnimalCard(animal.Id, animal.Nickname, info, animal.ProfilePhotoPath);

                            Canvas.SetLeft(card, currentX);
                            Canvas.SetTop(card, startY + genLevel * verticalSpacing);
                            treeCanvas.Children.Add(card);
                            itemCards[animal.Id] = card;

                            currentX += horizontalSpacing;
                        }
                    }

                    // Рисуем родословные связи
                    DrawAnimalPedigreeLines();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки животных: {ex.Message}");
            }
        }

        private void CalculateAnimalGenerations(int animalId, int genLevel, Dictionary<int, List<int>> childrenDict, Dictionary<int, int> generationMap)
        {
            if (generationMap.ContainsKey(animalId) && generationMap[animalId] >= genLevel)
                return;

            generationMap[animalId] = genLevel;

            if (childrenDict.ContainsKey(animalId))
            {
                foreach (var childId in childrenDict[animalId])
                {
                    CalculateAnimalGenerations(childId, genLevel + 1, childrenDict, generationMap);
                }
            }
        }

        private void DrawAnimalPedigreeLines()
        {
            foreach (var pedigree in animalPedigrees)
            {
                if (itemCards.ContainsKey(pedigree.AnimalId) && pedigree.FatherId.HasValue && itemCards.ContainsKey(pedigree.FatherId.Value))
                {
                    var childCard = itemCards[pedigree.AnimalId];
                    var fatherCard = itemCards[pedigree.FatherId.Value];

                    Point childTop = new Point(
                        Canvas.GetLeft(childCard) + childCard.Width / 2,
                        Canvas.GetTop(childCard));

                    Point fatherBottom = new Point(
                        Canvas.GetLeft(fatherCard) + fatherCard.Width / 2,
                        Canvas.GetTop(fatherCard) + fatherCard.Height);

                    var line = new Line
                    {
                        X1 = fatherBottom.X,
                        Y1 = fatherBottom.Y,
                        X2 = childTop.X,
                        Y2 = childTop.Y,
                        Stroke = new SolidColorBrush(System.Windows.Media.Colors.CadetBlue),
                        StrokeThickness = 2,
                        Tag = $"pedigree_{pedigree.AnimalId}_{pedigree.FatherId}"
                    };
                    Canvas.SetZIndex(line, 0);
                    treeCanvas.Children.Add(line);
                    relationshipLines.Add(line);
                }

                if (itemCards.ContainsKey(pedigree.AnimalId) && pedigree.MotherId.HasValue && itemCards.ContainsKey(pedigree.MotherId.Value))
                {
                    var childCard = itemCards[pedigree.AnimalId];
                    var motherCard = itemCards[pedigree.MotherId.Value];

                    Point childTop = new Point(
                        Canvas.GetLeft(childCard) + childCard.Width / 2,
                        Canvas.GetTop(childCard));

                    Point motherBottom = new Point(
                        Canvas.GetLeft(motherCard) + motherCard.Width / 2,
                        Canvas.GetTop(motherCard) + motherCard.Height);

                    var line = new Line
                    {
                        X1 = motherBottom.X,
                        Y1 = motherBottom.Y,
                        X2 = childTop.X,
                        Y2 = childTop.Y,
                        Stroke = new SolidColorBrush(System.Windows.Media.Colors.CadetBlue),
                        StrokeThickness = 2,
                        Tag = $"pedigree_{pedigree.AnimalId}_{pedigree.MotherId}"
                    };
                    Canvas.SetZIndex(line, 0);
                    treeCanvas.Children.Add(line);
                    relationshipLines.Add(line);
                }
            }
        }

        private void RedrawAllLines()
        {
            foreach (var line in relationshipLines)
            {
                treeCanvas.Children.Remove(line);
            }
            relationshipLines.Clear();

            if (AppSettings.IsFamilyMode)
            {
                DrawPersonRelationships();
            }
            else
            {
                DrawAnimalPedigreeLines();
            }
        }

        // ==================== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ====================

        private string GetSpeciesIcon(int speciesId)
        {
            switch (speciesId)
            {
                case 1: return "🐄";
                case 2: return "🐎";
                case 3: return "🐕";
                case 4: return "🐈";
                case 5: return "🐑";
                case 6: return "🐖";
                case 7: return "🐐";
                default: return "🐾";
            }
        }

        private string GetBreedName(int? breedId)
        {
            if (breedId == null) return "без породы";
            try
            {
                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    var breed = context.Breeds.Find(breedId);
                    return breed?.Name ?? "неизвестно";
                }
            }
            catch
            {
                return "неизвестно";
            }
        }

        private string GetPedigreeClassName(int? classId)
        {
            if (classId == null) return "не указан";
            using (var context = new GenealogyUnifiedDBEntities1())
            {
                var pc = context.PedigreeClasses.Find(classId);
                return pc?.Name ?? "не указан";
            }
        }

        private string GetSpeciesName(int speciesId)
        {
            using (var context = new GenealogyUnifiedDBEntities1())
            {
                var species = context.Species.Find(speciesId);
                return species?.Name ?? "неизвестно";
            }
        }

        private Border CreatePersonCard(int id, string name, string info, string photoPath)
        {
            var card = new Border
            {
                Width = 200,
                Height = 80,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FDF8F0")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B7A48B")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Tag = id,
                Cursor = Cursors.Hand
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var avatarBorder = new Border
            {
                Width = 50,
                Height = 50,
                CornerRadius = new CornerRadius(25),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8FAA7A")),
                Margin = new Thickness(5),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            string photoFullPath = PhotoHelper.GetProfilePhoto(photoPath);
            if (File.Exists(photoFullPath))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(photoFullPath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    var img = new Image { Source = bitmap, Stretch = Stretch.UniformToFill };
                    avatarBorder.Child = img;
                }
                catch
                {
                    var avatarText = new TextBlock { Text = "👤", FontSize = 24, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                    avatarBorder.Child = avatarText;
                }
            }
            else
            {
                var avatarText = new TextBlock { Text = "👤", FontSize = 24, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                avatarBorder.Child = avatarText;
            }

            Grid.SetColumn(avatarBorder, 0);
            grid.Children.Add(avatarBorder);

            var infoPanel = new StackPanel { Margin = new Thickness(5, 10, 5, 10), VerticalAlignment = VerticalAlignment.Center };
            infoPanel.Children.Add(new TextBlock { Text = name, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5C4E3D")) });
            infoPanel.Children.Add(new TextBlock { Text = info, FontSize = 11, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8B7E6B")) });
            Grid.SetColumn(infoPanel, 1);
            grid.Children.Add(infoPanel);

            var selectButton = new Button
            {
                Content = "Выбрать",
                Width = 60,
                Height = 22,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8FAA7A")),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 10,
                Cursor = Cursors.Hand,
                Tag = id,
                Margin = new Thickness(0, 2, 5, 5),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            selectButton.Click += SelectButton_Click;
            Grid.SetRow(selectButton, 0);
            Grid.SetColumn(selectButton, 1);
            grid.Children.Add(selectButton);

            card.Child = grid;

            card.MouseLeftButtonDown += Card_MouseLeftButtonDown;
            card.MouseLeftButtonUp += Card_MouseLeftButtonUp;
            card.MouseMove += Card_MouseMove;

            return card;
        }

        private Border CreateAnimalCard(int id, string nickname, string info, string photoPath)
        {
            var card = new Border
            {
                Width = 200,
                Height = 80,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FDF8F0")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B7A48B")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Tag = id,
                Cursor = Cursors.Hand
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var avatarBorder = new Border
            {
                Width = 50,
                Height = 50,
                CornerRadius = new CornerRadius(25),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60")),
                Margin = new Thickness(5),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            string photoFullPath = PhotoHelper.GetProfilePhoto(photoPath);
            if (File.Exists(photoFullPath))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(photoFullPath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    var img = new Image { Source = bitmap, Stretch = Stretch.UniformToFill };
                    avatarBorder.Child = img;
                }
                catch
                {
                    var avatarText = new TextBlock { Text = GetSpeciesIcon(GetSpeciesIdFromAnimal(id)), FontSize = 24, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                    avatarBorder.Child = avatarText;
                }
            }
            else
            {
                var avatarText = new TextBlock { Text = GetSpeciesIcon(GetSpeciesIdFromAnimal(id)), FontSize = 24, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                avatarBorder.Child = avatarText;
            }

            Grid.SetColumn(avatarBorder, 0);
            grid.Children.Add(avatarBorder);

            var infoPanel = new StackPanel { Margin = new Thickness(5, 10, 5, 10), VerticalAlignment = VerticalAlignment.Center };
            infoPanel.Children.Add(new TextBlock { Text = nickname, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5C4E3D")) });
            infoPanel.Children.Add(new TextBlock { Text = info, FontSize = 11, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8B7E6B")) });
            Grid.SetColumn(infoPanel, 1);
            grid.Children.Add(infoPanel);

            var selectButton = new Button
            {
                Content = "Выбрать",
                Width = 60,
                Height = 22,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8FAA7A")),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 10,
                Cursor = Cursors.Hand,
                Tag = id,
                Margin = new Thickness(0, 2, 5, 5),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            selectButton.Click += SelectButton_Click;
            Grid.SetRow(selectButton, 0);
            Grid.SetColumn(selectButton, 1);
            grid.Children.Add(selectButton);

            card.Child = grid;

            card.MouseLeftButtonDown += Card_MouseLeftButtonDown;
            card.MouseLeftButtonUp += Card_MouseLeftButtonUp;
            card.MouseMove += Card_MouseMove;

            return card;
        }

        private int GetSpeciesIdFromAnimal(int animalId)
        {
            using (var context = new GenealogyUnifiedDBEntities1())
            {
                var animal = context.Animals.Find(animalId);
                return animal?.SpeciesId ?? 1;
            }
        }

        private void ShowEmptyMessage()
        {
            var tb = new TextBlock
            {
                Text = AppSettings.IsFamilyMode ? "Древо пусто. Нажмите ➕ чтобы добавить персону" : "Племенная книга пуста. Нажмите ➕ чтобы добавить животное",
                FontSize = 18,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8B7E6B")),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Canvas.SetLeft(tb, 400);
            Canvas.SetTop(tb, 300);
            treeCanvas.Children.Add(tb);
        }

        // ==================== ВЫБОР КАРТОЧКИ ====================

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag != null)
            {
                SelectItem((int)button.Tag);
            }
        }

        private void SelectItem(int id)
        {
            selectedItemId = id;

            foreach (var card in itemCards.Values)
            {
                if (card != null)
                    card.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FDF8F0"));
            }

            if (itemCards.ContainsKey(id) && itemCards[id] != null)
            {
                itemCards[id].Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AFC09A"));
            }

            ShowItemDetails(id);
        }

        private void ShowItemDetails(int id)
        {
            if (AppSettings.IsFamilyMode)
            {
                ShowPersonDetails(id);
            }
            else
            {
                ShowAnimalDetails(id);
            }
        }

        private void ShowPersonDetails(int personId)
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    var person = context.Persons.FirstOrDefault(p => p.Id == personId);
                    if (person == null) return;

                    txtPersonName.Text = $"{person.LastName} {person.FirstName} {person.Patronymic}".Trim();
                    txtBirthDate.Text = person.BirthDate?.ToString("dd.MM.yyyy") ?? "?";
                    txtDeathDate.Text = person.DeathDate?.ToString("dd.MM.yyyy") ?? "...";
                    txtExtraInfo1.Text = $"Место рождения: {person.BirthPlace ?? "не указано"}";
                    txtExtraInfo2.Text = $"Место смерти: {person.DeathPlace ?? "не указано"}";

                    var gender = context.Genders.Find(person.GenderId);
                    if (gender != null)
                    {
                        txtGender.Text = gender.Name;
                        txtGenderSymbol.Text = gender.Symbol ?? "👤";
                    }

                    string photoFullPath = PhotoHelper.GetProfilePhoto(person.ProfilePhotoPath);
                    if (File.Exists(photoFullPath))
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(photoFullPath, UriKind.Absolute);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        imgProfile.Source = bitmap;
                        imgProfile.Visibility = Visibility.Visible;
                        txtNoPhoto.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        imgProfile.Visibility = Visibility.Collapsed;
                        txtNoPhoto.Visibility = Visibility.Visible;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
                imgProfile.Visibility = Visibility.Collapsed;
                txtNoPhoto.Visibility = Visibility.Visible;
            }
        }

        private void ShowAnimalDetails(int animalId)
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    var animal = context.Animals.FirstOrDefault(a => a.Id == animalId);
                    if (animal == null) return;

                    txtPersonName.Text = animal.Nickname;
                    txtBirthDate.Text = animal.BirthDate?.ToString("dd.MM.yyyy") ?? "?";
                    txtDeathDate.Text = animal.DeathDate?.ToString("dd.MM.yyyy") ?? "...";
                    txtExtraInfo1.Text = $"Вид: {GetSpeciesName(animal.SpeciesId)} | Порода: {GetBreedName(animal.BreedId)}";
                    txtExtraInfo2.Text = $"Класс: {GetPedigreeClassName(animal.PedigreeClassId)}";

                    var gender = context.AnimalGenders.Find(animal.GenderId);
                    txtGender.Text = gender?.Name ?? "Не указан";
                    txtGenderSymbol.Text = GetSpeciesIcon(animal.SpeciesId);

                    string photoFullPath = PhotoHelper.GetProfilePhoto(animal.ProfilePhotoPath);
                    if (File.Exists(photoFullPath))
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(photoFullPath, UriKind.Absolute);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        imgProfile.Source = bitmap;
                        imgProfile.Visibility = Visibility.Visible;
                        txtNoPhoto.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        imgProfile.Visibility = Visibility.Collapsed;
                        txtNoPhoto.Visibility = Visibility.Visible;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
                imgProfile.Visibility = Visibility.Collapsed;
                txtNoPhoto.Visibility = Visibility.Visible;
            }
        }

        // ==================== ПЕРЕТАСКИВАНИЕ КАРТОЧЕК ====================

        private void Card_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Session.IsGuest || !(Session.IsAdmin || Session.IsEditor))
            {
                e.Handled = true;
                return;
            }

            var border = sender as Border;
            if (border == null) return;

            isDragging = true;
            draggedCard = border;
            dragStartPoint = e.GetPosition(treeCanvas);
            Canvas.SetZIndex(draggedCard, 10);
            border.CaptureMouse();
            e.Handled = true;
        }

        private void Card_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging || draggedCard == null) return;

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                isDragging = false;
                draggedCard.ReleaseMouseCapture();
                Canvas.SetZIndex(draggedCard, 1);
                draggedCard = null;
                return;
            }

            Point currentPos = e.GetPosition(treeCanvas);
            double offsetX = currentPos.X - dragStartPoint.X;
            double offsetY = currentPos.Y - dragStartPoint.Y;

            double newLeft = Canvas.GetLeft(draggedCard) + offsetX;
            double newTop = Canvas.GetTop(draggedCard) + offsetY;

            newLeft = Math.Max(0, Math.Min(treeCanvas.Width - draggedCard.Width, newLeft));
            newTop = Math.Max(0, Math.Min(treeCanvas.Height - draggedCard.Height, newTop));

            Canvas.SetLeft(draggedCard, newLeft);
            Canvas.SetTop(draggedCard, newTop);
            dragStartPoint = currentPos;
            e.Handled = true;
        }

        private void Card_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isDragging && draggedCard != null)
            {
                Canvas.SetZIndex(draggedCard, 1);
                isDragging = false;
                draggedCard.ReleaseMouseCapture();
                draggedCard = null;
                RedrawAllLines();
                e.Handled = true;
            }
        }

        // ==================== ПОИСК ====================

        private void SearchTimer_Tick(object sender, EventArgs e)
        {
            searchTimer.Stop();
            PerformSearch();
        }

        private void txtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtSearch.Text == "Поиск...")
                txtSearch.Text = "";
        }

        private void txtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
                txtSearch.Text = "Поиск...";
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (btnClearSearch == null) return;

            if (txtSearch.Text != "Поиск..." && !string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                btnClearSearch.Visibility = Visibility.Visible;
                searchTimer.Stop();
                searchTimer.Start();
            }
            else
            {
                btnClearSearch.Visibility = Visibility.Collapsed;
                searchTimer.Stop();
                ClearSearchHighlight();
                currentSearchText = "";
            }
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "Поиск...";
            searchTimer.Stop();
            ClearSearchHighlight();
            currentSearchText = "";
            btnClearSearch.Visibility = Visibility.Collapsed;
        }

        private void PerformSearch()
        {
            string searchText = txtSearch.Text.Trim();
            if (string.IsNullOrWhiteSpace(searchText) || searchText == "Поиск...")
            {
                ClearSearchHighlight();
                currentSearchText = "";
                return;
            }

            currentSearchText = searchText.ToLower();

            if (AppSettings.IsFamilyMode)
            {
                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    var matchingPersons = context.Persons
                        .Where(p => p.TreeId == currentTreeId &&
                            (p.LastName.ToLower().Contains(currentSearchText) ||
                             p.FirstName.ToLower().Contains(currentSearchText) ||
                             (p.Patronymic != null && p.Patronymic.ToLower().Contains(currentSearchText))))
                        .ToList();

                    foreach (var card in itemCards.Values)
                    {
                        if (card != null)
                        {
                            card.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FDF8F0"));
                            card.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B7A48B"));
                            card.BorderThickness = new Thickness(1);
                        }
                    }

                    foreach (var person in matchingPersons)
                    {
                        if (itemCards.ContainsKey(person.Id) && itemCards[person.Id] != null)
                        {
                            itemCards[person.Id].Background = new SolidColorBrush(System.Windows.Media.Colors.LightYellow);
                            itemCards[person.Id].BorderBrush = new SolidColorBrush(System.Windows.Media.Colors.Orange);
                            itemCards[person.Id].BorderThickness = new Thickness(2);
                        }
                    }
                }
            }
            else
            {
                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    var matchingAnimals = context.Animals
                        .Where(a => a.TreeId == currentTreeId &&
                            a.Nickname.ToLower().Contains(currentSearchText))
                        .ToList();

                    foreach (var card in itemCards.Values)
                    {
                        if (card != null)
                        {
                            card.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FDF8F0"));
                            card.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B7A48B"));
                            card.BorderThickness = new Thickness(1);
                        }
                    }

                    foreach (var animal in matchingAnimals)
                    {
                        if (itemCards.ContainsKey(animal.Id) && itemCards[animal.Id] != null)
                        {
                            itemCards[animal.Id].Background = new SolidColorBrush(System.Windows.Media.Colors.LightYellow);
                            itemCards[animal.Id].BorderBrush = new SolidColorBrush(System.Windows.Media.Colors.Orange);
                            itemCards[animal.Id].BorderThickness = new Thickness(2);
                        }
                    }
                }
            }
        }

        private void ClearSearchHighlight()
        {
            foreach (var card in itemCards.Values)
            {
                if (card != null)
                {
                    card.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FDF8F0"));
                    card.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B7A48B"));
                    card.BorderThickness = new Thickness(1);
                }
            }
        }

        // ==================== ФИЛЬТРАЦИЯ ====================

        private void GetPersonGeneration(Persons person, out int generation)
        {
            if (generations.ContainsKey(person.Id))
            {
                generation = generations[person.Id];
            }
            else if (person.BirthDate.HasValue)
            {
                int year = person.BirthDate.Value.Year;
                if (year < 1950) generation = 0;
                else if (year < 1980) generation = 1;
                else if (year < 2000) generation = 2;
                else if (year < 2020) generation = 3;
                else generation = 4;
            }
            else
            {
                generation = 0;
            }
        }

        private void Filter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (cmbFilter.SelectedItem == null) return;
            string selectedFilter = cmbFilter.SelectedItem.ToString();

            if (AppSettings.IsFamilyMode)
            {
                int? generation = null;
                switch (selectedFilter)
                {
                    case "Поколение 1": generation = 0; break;
                    case "Поколение 2": generation = 1; break;
                    case "Поколение 3": generation = 2; break;
                    case "Поколение 4": generation = 3; break;
                    case "Поколение 5+": generation = 4; break;
                    default: generation = null; break;
                }

                if (generation.HasValue)
                {
                    using (var context = new GenealogyUnifiedDBEntities1())
                    {
                        var persons = context.Persons.Where(p => p.TreeId == currentTreeId).ToList();
                        var filteredPersons = persons.Where(p =>
                        {
                            GetPersonGeneration(p, out int gen);
                            return gen == generation.Value;
                        }).ToList();

                        foreach (var card in itemCards.Values)
                        {
                            if (card != null) card.Visibility = Visibility.Collapsed;
                        }

                        foreach (var person in filteredPersons)
                        {
                            if (itemCards.ContainsKey(person.Id) && itemCards[person.Id] != null)
                                itemCards[person.Id].Visibility = Visibility.Visible;
                        }
                    }
                }
                else
                {
                    foreach (var card in itemCards.Values)
                    {
                        if (card != null) card.Visibility = Visibility.Visible;
                    }
                }
            }
            else
            {
                if (selectedFilter != "Все породы" && selectedFilter != "Все виды")
                {
                    using (var context = new GenealogyUnifiedDBEntities1())
                    {
                        var breed = context.Breeds.FirstOrDefault(b => b.Name == selectedFilter);
                        if (breed != null)
                        {
                            var filteredAnimals = context.Animals
                                .Where(a => a.TreeId == currentTreeId && a.BreedId == breed.Id)
                                .ToList();

                            foreach (var card in itemCards.Values)
                            {
                                if (card != null) card.Visibility = Visibility.Collapsed;
                            }

                            foreach (var animal in filteredAnimals)
                            {
                                if (itemCards.ContainsKey(animal.Id) && itemCards[animal.Id] != null)
                                    itemCards[animal.Id].Visibility = Visibility.Visible;
                            }
                        }
                    }
                }
                else
                {
                    foreach (var card in itemCards.Values)
                    {
                        if (card != null) card.Visibility = Visibility.Visible;
                    }
                }
            }
        }

        // ==================== ПЕРЕТАСКИВАНИЕ КАНВАСА ====================

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == treeCanvas)
            {
                isCanvasDragging = true;
                canvasDragStart = e.GetPosition(this);
                treeCanvas.CaptureMouse();
                Mouse.OverrideCursor = Cursors.ScrollAll;
                e.Handled = true;
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isCanvasDragging && parentScrollViewer != null)
            {
                Point currentPos = e.GetPosition(this);
                Vector diff = currentPos - canvasDragStart;
                parentScrollViewer.ScrollToHorizontalOffset(parentScrollViewer.HorizontalOffset - diff.X);
                parentScrollViewer.ScrollToVerticalOffset(parentScrollViewer.VerticalOffset - diff.Y);
                canvasDragStart = currentPos;
                e.Handled = true;
            }
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isCanvasDragging)
            {
                isCanvasDragging = false;
                treeCanvas.ReleaseMouseCapture();
                Mouse.OverrideCursor = null;
                e.Handled = true;
            }
        }

        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child != null && child is T)
                    return (T)child;
                else
                {
                    var descendant = FindVisualChild<T>(child);
                    if (descendant != null)
                        return descendant;
                }
            }
            return null;
        }

        // ==================== ОБРАБОТЧИКИ КНОПОК ====================

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (AppSettings.IsFamilyMode)
            {
                NavigationService.Navigate(new EditPersonPage());
            }
            else
            {
                NavigationService.Navigate(new EditAnimalPage());
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedItemId.HasValue)
            {
                if (AppSettings.IsFamilyMode)
                {
                    NavigationService.Navigate(new EditPersonPage(selectedItemId.Value));
                }
                else
                {
                    NavigationService.Navigate(new EditAnimalPage(selectedItemId.Value));
                }
            }
            else
            {
                MessageBox.Show("Выберите элемент");
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (!selectedItemId.HasValue)
            {
                MessageBox.Show("Выберите элемент");
                return;
            }

            var result = MessageBox.Show("Удалить этот элемент? Все связанные данные также будут удалены!",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    if (AppSettings.IsFamilyMode)
                    {
                        using (var context = new GenealogyUnifiedDBEntities1())
                        {
                            var person = context.Persons.Find(selectedItemId.Value);
                            if (person != null)
                            {
                                var relationships = context.PersonRelationships
                                    .Where(r => r.Person1Id == selectedItemId.Value || r.Person2Id == selectedItemId.Value)
                                    .ToList();
                                context.PersonRelationships.RemoveRange(relationships);

                                var stories = context.Stories.Where(s => s.PersonId == selectedItemId.Value).ToList();
                                context.Stories.RemoveRange(stories);

                                context.Persons.Remove(person);
                                context.SaveChanges();

                                MessageBox.Show("Персона удалена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                                LoadTree();
                                selectedItemId = null;
                                txtPersonName.Text = "Выберите элемент";
                            }
                        }
                    }
                    else
                    {
                        using (var context = new GenealogyUnifiedDBEntities1())
                        {
                            var animal = context.Animals.Find(selectedItemId.Value);
                            if (animal != null)
                            {
                                var pedigree = context.AnimalPedigree.Where(p => p.AnimalId == selectedItemId.Value).ToList();
                                context.AnimalPedigree.RemoveRange(pedigree);

                                var breedings = context.Breedings
                                    .Where(b => b.MaleId == selectedItemId.Value || b.FemaleId == selectedItemId.Value)
                                    .ToList();
                                context.Breedings.RemoveRange(breedings);

                                var exhibitions = context.Exhibitions
                                    .Where(ex => ex.AnimalId == selectedItemId.Value)
                                    .ToList();
                                context.Exhibitions.RemoveRange(exhibitions);

                                var assessments = context.AnimalAssessments
                                    .Where(a => a.AnimalId == selectedItemId.Value)
                                    .ToList();
                                context.AnimalAssessments.RemoveRange(assessments);

                                context.Animals.Remove(animal);
                                context.SaveChanges();

                                MessageBox.Show("Животное удалено!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                                LoadTree();
                                selectedItemId = null;
                                txtPersonName.Text = "Выберите элемент";
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DetailsButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedItemId.HasValue)
            {
                if (AppSettings.IsFamilyMode)
                {
                    NavigationService.Navigate(new PersonProfilePage(selectedItemId.Value));
                }
                else
                {
                    NavigationService.Navigate(new AnimalProfilePage(selectedItemId.Value));
                }
            }
            else
            {
                MessageBox.Show("Выберите элемент");
            }
        }

        private void AddStoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedItemId.HasValue && AppSettings.IsFamilyMode)
            {
                NavigationService.Navigate(new EditStoryPage(selectedItemId.Value));
            }
            else if (selectedItemId.HasValue && !AppSettings.IsFamilyMode)
            {
                MessageBox.Show("Для животных истории не предусмотрены. Используйте раздел 'Примечания' в профиле животного.");
            }
            else
            {
                MessageBox.Show("Выберите элемент");
            }
        }

        private void MyTreesButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new TreesPage());
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