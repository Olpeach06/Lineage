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

        public AnimalProfilePage(int id)
        {
            InitializeComponent();
            animalId = id;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadAnimalData();
            LoadBreedings();
            LoadExhibitions();
            LoadAssessments();

            bool canEdit = Session.IsAdmin || Session.IsEditor;
            btnEdit.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
            btnAddBreeding.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
            btnAddExhibition.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LoadAnimalData()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities())
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

                    // Вид и порода
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

                    // Пол
                    var gender = context.AnimalGenders.Find(animal.GenderId);
                    txtGender.Text = gender?.Name ?? "Не указан";
                    txtGenderSymbol.Text = animal.GenderId == 1 ? "♂" : (animal.GenderId == 2 ? "♀" : "⚲");

                    // Племенной класс
                    if (animal.PedigreeClassId.HasValue)
                    {
                        var pedigreeClass = context.PedigreeClasses.Find(animal.PedigreeClassId);
                        txtClass.Text = pedigreeClass?.Name ?? "Не указан";
                    }
                    else
                        txtClass.Text = "Не указан";

                    txtBreedingValue.Text = animal.BreedingValue?.ToString("F2") ?? "—";
                    txtIsBreedingStock.Text = animal.IsBreedingStock == true ? "Да" : "Нет";

                    // Промеры
                    string measurements = "";
                    if (animal.HeightAtWithers.HasValue) measurements += $"{animal.HeightAtWithers.Value} см";
                    if (animal.ChestGirth.HasValue) measurements += $" / {animal.ChestGirth.Value} см";
                    if (string.IsNullOrEmpty(measurements)) measurements = "не указаны";
                    txtMeasurements.Text = measurements;

                    // Продуктивность (JSON)
                    if (!string.IsNullOrEmpty(animal.ProductivityData))
                    {
                        try
                        {
                            var productivity = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(animal.ProductivityData);
                            var prodText = new List<string>();
                            foreach (var item in productivity)
                            {
                                if (item.Key == "milkYield") prodText.Add($"Удой: {item.Value} кг");
                                else if (item.Key == "fatContent") prodText.Add($"Жирность: {item.Value}%");
                                else if (item.Key == "proteinContent") prodText.Add($"Белок: {item.Value}%");
                                else if (item.Key == "offspringCount") prodText.Add($"Потомство: {item.Value}");
                                else if (item.Key == "wins") prodText.Add($"Побед: {item.Value}");
                                else if (item.Key == "racingClass") prodText.Add($"Класс: {item.Value}");
                                else prodText.Add($"{item.Key}: {item.Value}");
                            }
                            txtProductivity.Text = string.Join(" | ", prodText);
                        }
                        catch
                        {
                            txtProductivity.Text = animal.ProductivityData;
                        }
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
                using (var context = new GenealogyUnifiedDBEntities())
                {
                    var breedingsAsMale = context.Breedings
                        .Where(b => b.MaleId == animalId)
                        .Select(b => new BreedingItem
                        {
                            Id = b.Id,
                            BreedingDate = b.BreedingDate.ToString("dd.MM.yyyy"),
                            Info = $"Вязка с самкой ID: {b.FemaleId}"
                        }).ToList();

                    var breedingsAsFemale = context.Breedings
                        .Where(b => b.FemaleId == animalId)
                        .Select(b => new BreedingItem
                        {
                            Id = b.Id,
                            BreedingDate = b.BreedingDate.ToString("dd.MM.yyyy"),
                            Info = $"Вязка с самцом ID: {b.MaleId}"
                        }).ToList();

                    var allBreedings = breedingsAsMale.Concat(breedingsAsFemale)
                        .OrderByDescending(b => b.BreedingDate)
                        .ToList();

                    icBreedings.ItemsSource = allBreedings;
                    tabBreedings.Header = $"🔗 Вязки ({allBreedings.Count})";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки вязок: {ex.Message}");
            }
        }

        private void LoadExhibitions()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities())
                {
                    var exhibitions = context.Exhibitions
                        .Where(e => e.AnimalId == animalId)
                        .Select(e => new ExhibitionItem
                        {
                            Id = e.Id,
                            ExhibitionDate = e.ExhibitionDate.ToString("dd.MM.yyyy"),
                            Name = e.ExhibitionName
                        })
                        .OrderByDescending(e => e.ExhibitionDate)
                        .ToList();

                    icExhibitions.ItemsSource = exhibitions;
                    tabExhibitions.Header = $"🏆 Выставки ({exhibitions.Count})";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки выставок: {ex.Message}");
            }
        }

        private void LoadAssessments()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities())
                {
                    var assessments = context.AnimalAssessments
                        .Where(a => a.AnimalId == animalId)
                        .Select(a => new AssessmentItem
                        {
                            Id = a.Id,
                            AssessmentDate = a.AssessmentDate.ToString("dd.MM.yyyy"),
                            Class = a.ClassId.ToString()
                        })
                        .OrderByDescending(a => a.AssessmentDate)
                        .ToList();

                    // Получаем названия классов
                    foreach (var assessment in assessments)
                    {
                        int classId = int.Parse(assessment.Class);
                        var pc = context.PedigreeClasses.Find(classId);
                        assessment.Class = pc?.Name ?? $"Класс {classId}";
                    }

                    icAssessments.ItemsSource = assessments;
                    tabAssessments.Header = $"⭐ Оценки ({assessments.Count})";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки оценок: {ex.Message}");
            }
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
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
            else
                NavigationService.Navigate(new MainPage());
        }
    }
}