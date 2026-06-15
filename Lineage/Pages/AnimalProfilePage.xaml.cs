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
using System.Diagnostics;

namespace Lineage.Pages
{
    public partial class AnimalProfilePage : Page
    {
        private int animalId;
        private int currentTreeId;
        private bool canEdit;     // Может ли пользователь редактировать
        private bool isAdmin;     // Является ли администратором
        private bool canDelete;   // Может ли удалять

        public class BreedingItem
        {
            public int Id { get; set; }
            public string BreedingDate { get; set; }
            public string Info { get; set; }
            public Visibility EditButtonVisibility { get; set; }
            public Visibility DeleteButtonVisibility { get; set; }
        }

        public class ExhibitionItem
        {
            public int Id { get; set; }
            public string ExhibitionDate { get; set; }
            public string Name { get; set; }
            public Visibility EditButtonVisibility { get; set; }
            public Visibility DeleteButtonVisibility { get; set; }
        }

        public class AssessmentItem
        {
            public int Id { get; set; }
            public string AssessmentDate { get; set; }
            public string ClassName { get; set; }
            public int ClassId { get; set; }
            public Visibility EditButtonVisibility { get; set; }
            public Visibility DeleteButtonVisibility { get; set; }
        }

        public class HealthEventItem
        {
            public int Id { get; set; }
            public string EventDate { get; set; }
            public string EventType { get; set; }
            public string MedicineName { get; set; }
            public string Notes { get; set; }
            public Visibility EditButtonVisibility { get; set; }
            public Visibility DeleteButtonVisibility { get; set; }
        }

        public AnimalProfilePage(int id)
        {
            InitializeComponent();
            animalId = id;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (!Session.IsBreedingMode)
            {
                MessageBox.Show("Эта страница доступна только в режиме племенной книги!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NavigationService.GoBack();
                return;
            }

            // Определяем права пользователя
            isAdmin = Session.IsAdmin;
            canEdit = Session.IsAdmin || Session.IsEditor;

            // Получаем TreeId животного
            using (var context = new GenealogyUnifiedDBEntities2())
            {
                var animal = context.Animals.Find(animalId);
                if (animal != null)
                {
                    currentTreeId = animal.TreeId;
                    var tree = context.FamilyTrees.Find(currentTreeId);
                    // Право на редактирование: админ ИЛИ создатель дерева
                    canEdit = Session.IsAdmin || (tree != null && tree.CreatedByUserId == Session.UserId);
                    canDelete = canEdit; // Для удаления тоже нужны права
                }
            }

            LoadAnimalData();
            LoadBreedings();
            LoadExhibitions();
            LoadAssessments();
            LoadHealthEvents();

            // Показываем кнопки действий только если есть права
            btnEdit.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
            btnAddBreeding.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
            btnAddExhibition.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
            btnAddAssessment.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
            btnAddHealthEvent.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
        }

        private bool IsAnimalOwner()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities2())
                {
                    var animal = context.Animals.Find(animalId);
                    if (animal == null) return false;

                    var tree = context.FamilyTrees.Find(animal.TreeId);
                    if (tree == null) return false;

                    return tree.CreatedByUserId == Session.UserId;
                }
            }
            catch
            {
                return false;
            }
        }

        private void LoadAnimalData()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities2())
                {
                    var animal = context.Animals.FirstOrDefault(a => a.Id == animalId);
                    if (animal == null)
                    {
                        MessageBox.Show("Животное не найдено");
                        NavigationService.GoBack();
                        return;
                    }

                    txtNickname.Text = animal.Nickname;
                    txtInventoryNumber.Text = $"Инв. номер: {animal.InventoryNumber ?? "—"}";
                    txtBirthDate.Text = animal.BirthDate?.ToString("dd.MM.yyyy") ?? "?";
                    txtDeathDate.Text = animal.DeathDate?.ToString("dd.MM.yyyy") ?? "...";
                    txtBirthPlace.Text = $"Место рождения: {animal.BirthPlace ?? "не указано"}";

                    var species = context.Species.Find(animal.SpeciesId);
                    txtSpeciesIcon.Text = GetSpeciesIcon(animal.SpeciesId);
                    txtSpecies.Text = species?.Name ?? "Неизвестно";

                    if (animal.BreedId.HasValue)
                    {
                        var breed = context.Breeds.Find(animal.BreedId);
                        txtBreed.Text = breed?.Name ?? "Неизвестно";
                    }
                    else
                        txtBreed.Text = "Не указана";

                    var gender = context.AnimalGenders.Find(animal.GenderId);
                    txtGender.Text = gender?.Name ?? "Не указан";
                    txtGenderSymbol.Text = animal.GenderId == 1 ? "♂" : (animal.GenderId == 2 ? "♀" : "⚲");

                    if (animal.PedigreeClassId.HasValue)
                    {
                        var pedigreeClass = context.PedigreeClasses.Find(animal.PedigreeClassId);
                        txtClass.Text = pedigreeClass?.Name ?? "Не указан";
                    }
                    else
                        txtClass.Text = "Не указан";

                    txtBreedingValue.Text = animal.BreedingValue?.ToString("F2") ?? "—";
                    txtIsBreedingStock.Text = animal.IsBreedingStock == true ? "Да" : "Нет";

                    string measurements = "";
                    if (animal.HeightAtWithers.HasValue) measurements += $"{animal.HeightAtWithers.Value} см";
                    if (animal.ChestGirth.HasValue) measurements += $" / {animal.ChestGirth.Value} см";
                    if (string.IsNullOrEmpty(measurements)) measurements = "не указаны";
                    txtMeasurements.Text = measurements;

                    var productivityRecords = context.ProductivityRecords
                        .Where(p => p.AnimalId == animalId && p.RecordType == "lactation")
                        .OrderByDescending(p => p.RecordDate)
                        .ToList();

                    if (productivityRecords.Any())
                    {
                        var latestRecord = productivityRecords.First();
                        var prodText = new List<string>();

                        if (latestRecord.MilkYield.HasValue)
                            prodText.Add($"Удой: {latestRecord.MilkYield.Value:F0} кг");
                        if (latestRecord.FatContent.HasValue)
                            prodText.Add($"Жирность: {latestRecord.FatContent.Value:F2}%");
                        if (latestRecord.ProteinContent.HasValue)
                            prodText.Add($"Белок: {latestRecord.ProteinContent.Value:F2}%");

                        if (productivityRecords.Count > 1)
                        {
                            prodText.Add($"Записей: {productivityRecords.Count}");
                            var maxMilk = productivityRecords.Max(p => p.MilkYield ?? 0);
                            if (maxMilk > 0)
                                prodText.Add($"Макс. удой: {maxMilk:F0} кг");
                        }

                        txtProductivity.Text = prodText.Any() ? string.Join(" | ", prodText) : "Данные загружены";
                    }
                    else
                        txtProductivity.Text = "Нет данных о продуктивности";

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
                        txtNoProfilePhoto.Visibility = Visibility.Collapsed;
                    }

                    var pedigree = context.AnimalPedigree.FirstOrDefault(p => p.AnimalId == animalId);
                    if (pedigree != null)
                    {
                        if (pedigree.FatherId.HasValue)
                        {
                            var father = context.Animals.Find(pedigree.FatherId.Value);
                            if (father != null)
                            {
                                txtFather.Text = father.Nickname;
                                txtFather.Tag = father.Id;
                                txtFather.Cursor = Cursors.Hand;
                                txtFather.MouseLeftButtonUp += Parent_MouseLeftButtonUp;
                            }
                            else
                                txtFather.Text = "Не указан";
                        }
                        else
                            txtFather.Text = "Не указан";

                        if (pedigree.MotherId.HasValue)
                        {
                            var mother = context.Animals.Find(pedigree.MotherId.Value);
                            if (mother != null)
                            {
                                txtMother.Text = mother.Nickname;
                                txtMother.Tag = mother.Id;
                                txtMother.Cursor = Cursors.Hand;
                                txtMother.MouseLeftButtonUp += Parent_MouseLeftButtonUp;
                            }
                            else
                                txtMother.Text = "Не указана";
                        }
                        else
                            txtMother.Text = "Не указана";
                    }
                    else
                    {
                        txtFather.Text = "Не указан";
                        txtMother.Text = "Не указана";
                    }

                    var offspringList = context.AnimalPedigree
                        .Where(p => p.FatherId == animalId || p.MotherId == animalId)
                        .Select(p => p.AnimalId)
                        .ToList();

                    if (offspringList.Any())
                    {
                        var offspring = context.Animals.Where(a => offspringList.Contains(a.Id)).ToList();
                        var offspringNames = new List<string>();
                        var offspringIds = new List<int>();

                        foreach (var child in offspring)
                        {
                            offspringNames.Add(child.Nickname);
                            offspringIds.Add(child.Id);
                        }

                        txtOffspring.Text = string.Join(", ", offspringNames);
                        txtOffspring.Tag = offspringIds;
                        txtOffspring.Cursor = Cursors.Hand;
                        txtOffspring.MouseLeftButtonUp += Offspring_MouseLeftButtonUp;
                    }
                    else
                        txtOffspring.Text = "Нет";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
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

        private void Parent_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var textBlock = sender as TextBlock;
            if (textBlock?.Tag != null && textBlock.Tag is int id)
            {
                NavigateToAnimal(id);
            }
        }

        private void Offspring_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var textBlock = sender as TextBlock;
            if (textBlock?.Tag != null && textBlock.Tag is List<int> ids && ids.Any())
            {
                NavigateToAnimal(ids.First());
            }
        }

        private void NavigateToAnimal(int id)
        {
            NavigationService.Navigate(new AnimalProfilePage(id));
        }

        private void LoadBreedings()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities2())
                {
                    var breedingsAsMaleRaw = context.Breedings
                        .Where(b => b.MaleId == animalId)
                        .ToList();

                    var breedingsAsFemaleRaw = context.Breedings
                        .Where(b => b.FemaleId == animalId)
                        .ToList();

                    var allBreedings = new List<BreedingItem>();
                    Visibility buttonVisibility = canEdit ? Visibility.Visible : Visibility.Collapsed;

                    foreach (var b in breedingsAsMaleRaw)
                    {
                        allBreedings.Add(new BreedingItem
                        {
                            Id = b.Id,
                            BreedingDate = b.BreedingDate.ToString("dd.MM.yyyy"),
                            Info = $"Вязка с самкой: {GetAnimalNickname(b.FemaleId, context)}",
                            EditButtonVisibility = buttonVisibility,
                            DeleteButtonVisibility = buttonVisibility
                        });
                    }

                    foreach (var b in breedingsAsFemaleRaw)
                    {
                        allBreedings.Add(new BreedingItem
                        {
                            Id = b.Id,
                            BreedingDate = b.BreedingDate.ToString("dd.MM.yyyy"),
                            Info = $"Вязка с самцом: {GetAnimalNickname(b.MaleId, context)}",
                            EditButtonVisibility = buttonVisibility,
                            DeleteButtonVisibility = buttonVisibility
                        });
                    }

                    allBreedings = allBreedings.OrderByDescending(b => b.BreedingDate).ToList();

                    icBreedings.ItemsSource = allBreedings;
                    txtBreedingsCount.Text = $" ({allBreedings.Count})";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки вязок: {ex.Message}");
            }
        }

        private string GetAnimalNickname(int animalId, GenealogyUnifiedDBEntities2 context)
        {
            var animal = context.Animals.Find(animalId);
            return animal?.Nickname ?? $"ID:{animalId}";
        }

        private void LoadExhibitions()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities2())
                {
                    var exhibitionsRaw = context.Exhibitions
                        .Where(e => e.AnimalId == animalId)
                        .ToList();

                    var exhibitions = new List<ExhibitionItem>();
                    Visibility buttonVisibility = canEdit ? Visibility.Visible : Visibility.Collapsed;

                    foreach (var e in exhibitionsRaw)
                    {
                        exhibitions.Add(new ExhibitionItem
                        {
                            Id = e.Id,
                            ExhibitionDate = e.ExhibitionDate.ToString("dd.MM.yyyy"),
                            Name = e.ExhibitionName,
                            EditButtonVisibility = buttonVisibility,
                            DeleteButtonVisibility = buttonVisibility
                        });
                    }

                    exhibitions = exhibitions.OrderByDescending(e => e.ExhibitionDate).ToList();

                    icExhibitions.ItemsSource = exhibitions;
                    txtExhibitionsCount.Text = $" ({exhibitions.Count})";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки выставок: {ex.Message}");
            }
        }

        private void LoadAssessments()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities2())
                {
                    var assessmentsRaw = context.AnimalAssessments
                        .Where(a => a.AnimalId == animalId)
                        .ToList();

                    var classNames = context.PedigreeClasses.ToDictionary(c => c.Id, c => c.Name);
                    var assessments = new List<AssessmentItem>();
                    Visibility buttonVisibility = canEdit ? Visibility.Visible : Visibility.Collapsed;

                    foreach (var a in assessmentsRaw)
                    {
                        assessments.Add(new AssessmentItem
                        {
                            Id = a.Id,
                            AssessmentDate = a.AssessmentDate.ToString("dd.MM.yyyy"),
                            ClassId = a.ClassId,
                            ClassName = classNames.ContainsKey(a.ClassId) ? classNames[a.ClassId] : $"Класс {a.ClassId}",
                            EditButtonVisibility = buttonVisibility,
                            DeleteButtonVisibility = buttonVisibility
                        });
                    }

                    assessments = assessments.OrderByDescending(a => a.AssessmentDate).ToList();

                    icAssessments.ItemsSource = assessments;
                    txtAssessmentsCount.Text = $" ({assessments.Count})";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки оценок: {ex.Message}");
            }
        }

        private void LoadHealthEvents()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities2())
                {
                    var vetEvents = context.VeterinaryEvents
                        .Where(v => v.AnimalId == animalId)
                        .OrderByDescending(v => v.EventDate)
                        .ToList();

                    var eventTypes = context.VeterinaryEventTypes.ToDictionary(t => t.Id, t => t.Name);
                    var healthItems = new List<HealthEventItem>();
                    Visibility buttonVisibility = canEdit ? Visibility.Visible : Visibility.Collapsed;

                    foreach (var ev in vetEvents)
                    {
                        healthItems.Add(new HealthEventItem
                        {
                            Id = ev.Id,
                            EventDate = ev.EventDate.ToString("dd.MM.yyyy"),
                            EventType = eventTypes.ContainsKey(ev.EventTypeId) ? eventTypes[ev.EventTypeId] : "—",
                            MedicineName = ev.MedicineName ?? "—",
                            Notes = ev.Notes ?? "—",
                            EditButtonVisibility = buttonVisibility,
                            DeleteButtonVisibility = buttonVisibility
                        });
                    }

                    icHealthEvents.ItemsSource = healthItems;
                    txtHealthCount.Text = $" ({healthItems.Count})";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки ветеринарных событий: {ex.Message}");
            }
        }

        // ==================== ДЕТАЛИ (открытие страниц с подробной информацией) ====================

        private void BreedingDetails_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag == null) return;

            int breedingId = (int)button.Tag;
            NavigationService.Navigate(new DetailPage(DetailType.Breeding, breedingId));
        }

        private void ExhibitionDetails_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag == null) return;

            int exhibitionId = (int)button.Tag;
            NavigationService.Navigate(new DetailPage(DetailType.Exhibition, exhibitionId));
        }

        private void AssessmentDetails_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag == null) return;

            int assessmentId = (int)button.Tag;
            NavigationService.Navigate(new DetailPage(DetailType.Assessment, assessmentId));
        }

        private void HealthEventDetails_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag == null) return;

            int eventId = (int)button.Tag;
            NavigationService.Navigate(new DetailPage(DetailType.HealthEvent, eventId));
        }

        // ============================================
        // РЕДАКТИРОВАНИЕ
        // ============================================

        private void EditBreeding_Click(object sender, RoutedEventArgs e)
        {
            if (!canEdit)
            {
                MessageBox.Show("У вас нет прав на редактирование этой записи!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var button = sender as Button;
            if (button?.Tag == null) return;
            int breedingId = (int)button.Tag;
            NavigationService.Navigate(new AddEditDetailPage(EditDetailType.Breeding, animalId, breedingId));
        }

        private void EditExhibition_Click(object sender, RoutedEventArgs e)
        {
            if (!canEdit)
            {
                MessageBox.Show("У вас нет прав на редактирование этой записи!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var button = sender as Button;
            if (button?.Tag == null) return;
            int exhibitionId = (int)button.Tag;
            NavigationService.Navigate(new AddEditDetailPage(EditDetailType.Exhibition, animalId, exhibitionId));
        }

        private void EditAssessment_Click(object sender, RoutedEventArgs e)
        {
            if (!canEdit)
            {
                MessageBox.Show("У вас нет прав на редактирование этой записи!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var button = sender as Button;
            if (button?.Tag == null) return;
            int assessmentId = (int)button.Tag;
            NavigationService.Navigate(new AddEditDetailPage(EditDetailType.Assessment, animalId, assessmentId));
        }

        private void EditHealthEvent_Click(object sender, RoutedEventArgs e)
        {
            if (!canEdit)
            {
                MessageBox.Show("У вас нет прав на редактирование этой записи!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var button = sender as Button;
            if (button?.Tag == null) return;
            int eventId = (int)button.Tag;
            NavigationService.Navigate(new AddEditDetailPage(EditDetailType.HealthEvent, animalId, eventId));
        }

        // ============================================
        // УДАЛЕНИЕ
        // ============================================

        private async void BreedingDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!canEdit)
            {
                MessageBox.Show("У вас нет прав на удаление этой записи!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var button = sender as Button;
            if (button?.Tag == null) return;
            int breedingId = (int)button.Tag;

            var result = MessageBox.Show("Вы уверены, что хотите удалить эту вязку?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new GenealogyUnifiedDBEntities2())
                    {
                        var breeding = context.Breedings.Find(breedingId);
                        if (breeding != null)
                        {
                            context.Breedings.Remove(breeding);
                            await context.SaveChangesAsync();
                            LoadBreedings();
                            MessageBox.Show("Вязка удалена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ExhibitionDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!canEdit)
            {
                MessageBox.Show("У вас нет прав на удаление этой записи!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var button = sender as Button;
            if (button?.Tag == null) return;
            int exhibitionId = (int)button.Tag;

            var result = MessageBox.Show("Вы уверены, что хотите удалить эту выставку?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new GenealogyUnifiedDBEntities2())
                    {
                        var exhibition = context.Exhibitions.Find(exhibitionId);
                        if (exhibition != null)
                        {
                            context.Exhibitions.Remove(exhibition);
                            await context.SaveChangesAsync();
                            LoadExhibitions();
                            MessageBox.Show("Выставка удалена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void AssessmentDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!canEdit)
            {
                MessageBox.Show("У вас нет прав на удаление этой записи!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var button = sender as Button;
            if (button?.Tag == null) return;
            int assessmentId = (int)button.Tag;

            var result = MessageBox.Show("Вы уверены, что хотите удалить эту оценку?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new GenealogyUnifiedDBEntities2())
                    {
                        var assessment = context.AnimalAssessments.Find(assessmentId);
                        if (assessment != null)
                        {
                            context.AnimalAssessments.Remove(assessment);
                            await context.SaveChangesAsync();
                            LoadAssessments();
                            MessageBox.Show("Оценка удалена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void HealthEventDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!canEdit)
            {
                MessageBox.Show("У вас нет прав на удаление этой записи!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var button = sender as Button;
            if (button?.Tag == null) return;
            int eventId = (int)button.Tag;

            var result = MessageBox.Show("Вы уверены, что хотите удалить это ветеринарное событие?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new GenealogyUnifiedDBEntities2())
                    {
                        var vetEvent = context.VeterinaryEvents.Find(eventId);
                        if (vetEvent != null)
                        {
                            context.VeterinaryEvents.Remove(vetEvent);
                            await context.SaveChangesAsync();
                            LoadHealthEvents();
                            MessageBox.Show("Ветеринарное событие удалено!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ============================================
        // ДОБАВЛЕНИЕ
        // ============================================

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (!canEdit)
            {
                MessageBox.Show("У вас нет прав на редактирование!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            NavigationService.Navigate(new EditAnimalPage(animalId));
        }

        private void AddBreedingButton_Click(object sender, RoutedEventArgs e)
        {
            if (!canEdit)
            {
                MessageBox.Show("У вас нет прав на добавление!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            NavigationService.Navigate(new AddEditDetailPage(EditDetailType.Breeding, animalId, null));
        }

        private void AddExhibitionButton_Click(object sender, RoutedEventArgs e)
        {
            if (!canEdit)
            {
                MessageBox.Show("У вас нет прав на добавление!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            NavigationService.Navigate(new AddEditDetailPage(EditDetailType.Exhibition, animalId, null));
        }

        private void AddAssessmentButton_Click(object sender, RoutedEventArgs e)
        {
            if (!canEdit)
            {
                MessageBox.Show("У вас нет прав на добавление!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            NavigationService.Navigate(new AddEditDetailPage(EditDetailType.Assessment, animalId, null));
        }

        private void AddHealthEventButton_Click(object sender, RoutedEventArgs e)
        {
            if (!canEdit)
            {
                MessageBox.Show("У вас нет прав на добавление!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            NavigationService.Navigate(new AddEditDetailPage(EditDetailType.HealthEvent, animalId, null));
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new MainPage());
        }
    }
}