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
            }
            else
            {
                cmbFilter.Items.Add("Все виды");
                cmbFilter.Items.Add("КРС");
                cmbFilter.Items.Add("Лошади");
                cmbFilter.Items.Add("Собаки");
                cmbFilter.Items.Add("Кошки");
            }
            cmbFilter.SelectedIndex = 0;
        }

        private void LoadTree()
        {
            if (treeCanvas == null) return;
            treeCanvas.Children.Clear();
            itemCards.Clear();

            if (AppSettings.IsFamilyMode)
            {
                LoadPersonsTree();
            }
            else
            {
                LoadAnimalsTree();
            }
        }

        private void LoadPersonsTree()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities())
                {
                    var persons = context.Persons
                        .Where(p => p.TreeId == currentTreeId)
                        .ToList();

                    if (!persons.Any())
                    {
                        ShowEmptyMessage();
                        return;
                    }

                    int startX = 100;
                    int startY = 100;
                    int yOffset = 0;

                    foreach (var person in persons)
                    {
                        string genderSymbol = person.GenderId == 1 ? "♂" : (person.GenderId == 2 ? "♀" : "👤");
                        string birthYear = person.BirthDate?.Year.ToString() ?? "?";
                        string deathYear = person.DeathDate?.Year.ToString() ?? "";
                        string dateInfo = birthYear + (string.IsNullOrEmpty(deathYear) ? "" : $" - {deathYear}");

                        var card = CreatePersonCard(person.Id, $"{person.LastName} {person.FirstName}".Trim(),
                            $"{genderSymbol} {dateInfo}", person.ProfilePhotoPath);

                        Canvas.SetLeft(card, startX);
                        Canvas.SetTop(card, startY + yOffset);
                        treeCanvas.Children.Add(card);
                        itemCards[person.Id] = card;

                        yOffset += 100;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}");
            }
        }

        private void LoadAnimalsTree()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities())
                {
                    var animals = context.Animals
                        .Where(a => a.TreeId == currentTreeId)
                        .ToList();

                    if (!animals.Any())
                    {
                        ShowEmptyMessage();
                        return;
                    }

                    int startX = 100;
                    int startY = 100;
                    int yOffset = 0;

                    foreach (var animal in animals)
                    {
                        string speciesIcon = GetSpeciesIcon(animal.SpeciesId);
                        string breedName = GetBreedName(animal.BreedId);
                        string info = $"{speciesIcon} | {breedName}";

                        var card = CreateAnimalCard(animal.Id, animal.Nickname, info, animal.ProfilePhotoPath);

                        Canvas.SetLeft(card, startX);
                        Canvas.SetTop(card, startY + yOffset);
                        treeCanvas.Children.Add(card);
                        itemCards[animal.Id] = card;

                        yOffset += 100;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки животных: {ex.Message}");
            }
        }

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
                using (var context = new GenealogyUnifiedDBEntities())
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
            using (var context = new GenealogyUnifiedDBEntities())
            {
                var pc = context.PedigreeClasses.Find(classId);
                return pc?.Name ?? "не указан";
            }
        }

        private string GetSpeciesName(int speciesId)
        {
            using (var context = new GenealogyUnifiedDBEntities())
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

            // Аватар
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
                    var avatarText = new TextBlock
                    {
                        Text = "👤",
                        FontSize = 24,
                        Foreground = Brushes.White,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    avatarBorder.Child = avatarText;
                }
            }
            else
            {
                var avatarText = new TextBlock
                {
                    Text = "👤",
                    FontSize = 24,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                avatarBorder.Child = avatarText;
            }

            Grid.SetColumn(avatarBorder, 0);
            grid.Children.Add(avatarBorder);

            // Информация
            var infoPanel = new StackPanel { Margin = new Thickness(5, 10, 5, 10), VerticalAlignment = VerticalAlignment.Center };
            infoPanel.Children.Add(new TextBlock { Text = name, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5C4E3D")) });
            infoPanel.Children.Add(new TextBlock { Text = info, FontSize = 11, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8B7E6B")) });
            Grid.SetColumn(infoPanel, 1);
            grid.Children.Add(infoPanel);

            card.Child = grid;
            card.MouseLeftButtonDown += ItemCard_MouseLeftButtonDown;
            card.MouseLeftButtonUp += ItemCard_MouseLeftButtonUp;
            card.MouseMove += ItemCard_MouseMove;

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

            // Аватар
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
                    var avatarText = new TextBlock
                    {
                        Text = GetSpeciesIcon(GetSpeciesIdFromAnimal(id)),
                        FontSize = 24,
                        Foreground = Brushes.White,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    avatarBorder.Child = avatarText;
                }
            }
            else
            {
                var avatarText = new TextBlock
                {
                    Text = GetSpeciesIcon(GetSpeciesIdFromAnimal(id)),
                    FontSize = 24,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                avatarBorder.Child = avatarText;
            }

            Grid.SetColumn(avatarBorder, 0);
            grid.Children.Add(avatarBorder);

            // Информация
            var infoPanel = new StackPanel { Margin = new Thickness(5, 10, 5, 10), VerticalAlignment = VerticalAlignment.Center };
            infoPanel.Children.Add(new TextBlock { Text = nickname, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5C4E3D")) });
            infoPanel.Children.Add(new TextBlock { Text = info, FontSize = 11, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8B7E6B")) });
            Grid.SetColumn(infoPanel, 1);
            grid.Children.Add(infoPanel);

            card.Child = grid;
            card.MouseLeftButtonDown += ItemCard_MouseLeftButtonDown;
            card.MouseLeftButtonUp += ItemCard_MouseLeftButtonUp;
            card.MouseMove += ItemCard_MouseMove;

            return card;
        }

        private int GetSpeciesIdFromAnimal(int animalId)
        {
            using (var context = new GenealogyUnifiedDBEntities())
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

        // === ОБРАБОТЧИКИ ВЫБОРА КАРТОЧКИ ===
        private void ItemCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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

        private void ItemCard_MouseMove(object sender, MouseEventArgs e)
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

        private void ItemCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!isDragging || draggedCard == null)
            {
                var border = sender as Border;
                if (border?.Tag != null)
                {
                    SelectItem((int)border.Tag);
                }
            }

            if (draggedCard != null)
            {
                Canvas.SetZIndex(draggedCard, 1);
            }

            isDragging = false;
            draggedCard?.ReleaseMouseCapture();
            draggedCard = null;
            e.Handled = true;
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
                using (var context = new GenealogyUnifiedDBEntities())
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
                using (var context = new GenealogyUnifiedDBEntities())
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

        // === ПОИСК ===
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

            foreach (var card in itemCards.Values)
            {
                if (card != null)
                {
                    card.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FDF8F0"));
                    card.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B7A48B"));
                    card.BorderThickness = new Thickness(1);
                }
            }

            // Поиск по карточкам (упрощённо)
            int foundCount = 0;
            foreach (var kvp in itemCards)
            {
                var card = kvp.Value;
                if (card != null)
                {
                    // Здесь можно реализовать поиск по данным
                    // Для простоты пока подсвечиваем все
                    foundCount++;
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

        // === ФИЛЬТРАЦИЯ ===
        private void Filter_Changed(object sender, SelectionChangedEventArgs e)
        {
            // Логика фильтрации
        }

        // === ПЕРЕТАСКИВАНИЕ КАНВАСА ===
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

        // === ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ===
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

        // === ОБРАБОТЧИКИ КНОПОК ===
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (AppSettings.IsFamilyMode)
            {
                NavigationService.Navigate(new EditPersonPage());
            }
            else
            {
                // NavigationService.Navigate(new EditAnimalPage());
                MessageBox.Show("Страница добавления животного будет реализована позже");
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
                    // NavigationService.Navigate(new EditAnimalPage(selectedItemId.Value));
                    MessageBox.Show("Страница редактирования животного будет реализована позже");
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
                // Логика удаления будет реализована позже
                MessageBox.Show("Элемент удалён!");
                LoadTree();
                selectedItemId = null;
                txtPersonName.Text = "Выберите элемент";
                txtBirthDate.Text = "--";
                txtDeathDate.Text = "--";
                txtGender.Text = "Не указан";
                txtExtraInfo1.Text = "";
                txtExtraInfo2.Text = "";
                imgProfile.Visibility = Visibility.Collapsed;
                txtNoPhoto.Visibility = Visibility.Visible;
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
                    // NavigationService.Navigate(new AnimalProfilePage(selectedItemId.Value));
                    MessageBox.Show("Страница профиля животного будет реализована позже");
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
            else
            {
                MessageBox.Show("Истории доступны только для людей");
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