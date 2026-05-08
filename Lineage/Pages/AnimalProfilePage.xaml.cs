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
    public partial class AnimalProfilePage : Page
    {
        private int animalId;

        public class BreedingItem
        {
            public int Id { get; set; }
            public string BreedingDate { get; set; }
            public string Info { get; set; }
        }

        public class ExhibitionItem
        {
            public int Id { get; set; }
            public string ExhibitionDate { get; set; }
            public string Name { get; set; }
        }

        public class AssessmentItem
        {
            public int Id { get; set; }
            public string AssessmentDate { get; set; }
            public string Class { get; set; }
        }

        public class HealthEventItem
        {
            public int Id { get; set; }
            public string EventDate { get; set; }
            public string EventType { get; set; }
            public string MedicineName { get; set; }
            public string Notes { get; set; }
        }

        public AnimalProfilePage(int id)
        {
            InitializeComponent();
            animalId = id;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"=== ЗАГРУЗКА ПРОФИЛЯ ЖИВОТНОГО ID: {animalId} ===");

            LoadAnimalData();
            LoadBreedings();
            LoadExhibitions();
            LoadAssessments();
            LoadHealthEvents();

            bool canEdit = Session.IsAdmin || Session.IsEditor;
            btnEdit.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
            btnAddBreeding.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
            btnAddExhibition.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LoadAnimalData()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities1())
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

                    // Продуктивность
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

                    // Фото
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

                    // Родители
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

                    // Потомство
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
                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    // Сначала получаем данные из БД без форматирования
                    var breedingsAsMaleRaw = context.Breedings
                        .Where(b => b.MaleId == animalId)
                        .ToList();

                    var breedingsAsFemaleRaw = context.Breedings
                        .Where(b => b.FemaleId == animalId)
                        .ToList();

                    var allBreedings = new List<BreedingItem>();

                    foreach (var b in breedingsAsMaleRaw)
                    {
                        allBreedings.Add(new BreedingItem
                        {
                            Id = b.Id,
                            BreedingDate = b.BreedingDate.ToString("dd.MM.yyyy"),
                            Info = $"Вязка с самкой: {GetAnimalNickname(b.FemaleId, context)}"
                        });
                    }

                    foreach (var b in breedingsAsFemaleRaw)
                    {
                        allBreedings.Add(new BreedingItem
                        {
                            Id = b.Id,
                            BreedingDate = b.BreedingDate.ToString("dd.MM.yyyy"),
                            Info = $"Вязка с самцом: {GetAnimalNickname(b.MaleId, context)}"
                        });
                    }

                    allBreedings = allBreedings.OrderByDescending(b => b.BreedingDate).ToList();

                    icBreedings.ItemsSource = allBreedings;
                    tabBreedings.Header = $"🔗 Вязки ({allBreedings.Count})";

                    System.Diagnostics.Debug.WriteLine($"Загружено вязок: {allBreedings.Count}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки вязок: {ex.Message}");
                MessageBox.Show($"Ошибка загрузки вязок: {ex.Message}");
            }
        }

        // Вспомогательный метод для получения клички животного по ID
        private string GetAnimalNickname(int animalId, GenealogyUnifiedDBEntities1 context)
        {
            var animal = context.Animals.Find(animalId);
            return animal?.Nickname ?? $"ID:{animalId}";
        }

        private void LoadExhibitions()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    // Сначала получаем данные из БД без форматирования
                    var exhibitionsRaw = context.Exhibitions
                        .Where(e => e.AnimalId == animalId)
                        .ToList();

                    var exhibitions = new List<ExhibitionItem>();

                    foreach (var e in exhibitionsRaw)
                    {
                        exhibitions.Add(new ExhibitionItem
                        {
                            Id = e.Id,
                            ExhibitionDate = e.ExhibitionDate.ToString("dd.MM.yyyy"),
                            Name = e.ExhibitionName
                        });
                    }

                    exhibitions = exhibitions.OrderByDescending(e => e.ExhibitionDate).ToList();

                    icExhibitions.ItemsSource = exhibitions;
                    tabExhibitions.Header = $"🏆 Выставки ({exhibitions.Count})";

                    System.Diagnostics.Debug.WriteLine($"Загружено выставок: {exhibitions.Count}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки выставок: {ex.Message}");
                MessageBox.Show($"Ошибка загрузки выставок: {ex.Message}");
            }
        }

        private void LoadAssessments()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    // Сначала получаем данные из БД без форматирования
                    var assessmentsRaw = context.AnimalAssessments
                        .Where(a => a.AnimalId == animalId)
                        .ToList();

                    var assessments = new List<AssessmentItem>();

                    foreach (var a in assessmentsRaw)
                    {
                        assessments.Add(new AssessmentItem
                        {
                            Id = a.Id,
                            AssessmentDate = a.AssessmentDate.ToString("dd.MM.yyyy"),
                            Class = a.ClassId.ToString()
                        });
                    }

                    assessments = assessments.OrderByDescending(a => a.AssessmentDate).ToList();

                    // Получаем названия классов
                    foreach (var assessment in assessments)
                    {
                        if (int.TryParse(assessment.Class, out int classId))
                        {
                            using (var tempContext = new GenealogyUnifiedDBEntities1())
                            {
                                var pc = tempContext.PedigreeClasses.Find(classId);
                                assessment.Class = pc?.Name ?? $"Класс {classId}";
                            }
                        }
                    }

                    icAssessments.ItemsSource = assessments;
                    tabAssessments.Header = $"⭐ Оценки ({assessments.Count})";

                    System.Diagnostics.Debug.WriteLine($"Загружено оценок: {assessments.Count}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки оценок: {ex.Message}");
                MessageBox.Show($"Ошибка загрузки оценок: {ex.Message}");
            }
        }

        private void LoadHealthEvents()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    var vetEvents = context.VeterinaryEvents
                        .Where(v => v.AnimalId == animalId)
                        .OrderByDescending(v => v.EventDate)
                        .ToList();

                    var eventTypes = context.VeterinaryEventTypes.ToDictionary(t => t.Id, t => t.Name);

                    var healthItems = new List<HealthEventItem>();

                    foreach (var ev in vetEvents)
                    {
                        healthItems.Add(new HealthEventItem
                        {
                            Id = ev.Id,
                            EventDate = ev.EventDate.ToString("dd.MM.yyyy"),
                            EventType = eventTypes.ContainsKey(ev.EventTypeId) ? eventTypes[ev.EventTypeId] : "—",
                            MedicineName = ev.MedicineName ?? "—",
                            Notes = ev.Notes ?? "—"
                        });
                    }

                    icHealthEvents.ItemsSource = healthItems;
                    tabHealth.Header = $"💊 Здоровье ({healthItems.Count})";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки ветеринарных событий: {ex.Message}");
            }
        }

        private void HealthDetails_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag == null) return;
            int eventId = (int)button.Tag;
            // NavigationService.Navigate(new VeterinaryEventDetailPage(eventId));
            MessageBox.Show($"Детали события ID: {eventId}\n(Страница деталей будет реализована позже)", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new EditAnimalPage(animalId));
        }

        private void AddBreedingButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new AddBreedingPage(animalId));
        }

        private void AddExhibitionButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new AddExhibitionPage(animalId));
        }

        private void BreedingDetails_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag == null) return;
            int breedingId = (int)button.Tag;
            NavigationService.Navigate(new BreedingDetailPage(breedingId));
        }

        private void ExhibitionDetails_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag == null) return;
            int exhibitionId = (int)button.Tag;
            NavigationService.Navigate(new ExhibitionDetailPage(exhibitionId));
        }

        private void AssessmentDetails_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag == null) return;
            int assessmentId = (int)button.Tag;
            NavigationService.Navigate(new AssessmentDetailPage(assessmentId));
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new MainPage());
        }
    }
}