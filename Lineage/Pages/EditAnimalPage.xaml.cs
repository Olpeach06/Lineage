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
using Newtonsoft.Json;
using System.IO;

namespace Lineage.Pages
{
    public partial class EditAnimalPage : Page
    {
        private int? editAnimalId = null;
        private int currentTreeId;
        private string selectedPhotoPath = null;
        private string originalPhotoPath = null;
        private List<AnimalComboItem> allAnimals = new List<AnimalComboItem>();

        public class AnimalComboItem
        {
            public int Id { get; set; }
            public string Nickname { get; set; }
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
            public int? SpeciesId { get; set; }
        }

        public class ColorItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int? SpeciesId { get; set; }
        }

        public class GenderItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class PedigreeClassItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public EditAnimalPage()
        {
            InitializeComponent();
            txtTitle.Text = "ДОБАВЛЕНИЕ ЖИВОТНОГО";
        }

        public EditAnimalPage(int animalId) : this()
        {
            editAnimalId = animalId;
            txtTitle.Text = "РЕДАКТИРОВАНИЕ ЖИВОТНОГО";
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            currentTreeId = Session.CurrentTreeId;

            LoadSpecies();
            LoadGenders();
            LoadPedigreeClasses();
            LoadAnimalsForParents();

            if (editAnimalId.HasValue)
                LoadAnimalData(editAnimalId.Value);
        }

        private void LoadSpecies()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities())
                {
                    var species = context.Species
                        .Select(s => new SpeciesItem { Id = s.Id, Name = s.Name })
                        .ToList();

                    cmbSpecies.ItemsSource = species;
                    cmbSpecies.SelectedValuePath = "Id";
                    cmbSpecies.DisplayMemberPath = "Name";
                    cmbSpecies.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки видов: {ex.Message}");
            }
        }

        private void LoadBreeds(int speciesId)
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities())
                {
                    var breeds = context.Breeds
                        .Where(b => b.SpeciesId == speciesId || b.SpeciesId == null)
                        .Select(b => new BreedItem { Id = b.Id, Name = b.Name, SpeciesId = b.SpeciesId })
                        .ToList();

                    breeds.Insert(0, new BreedItem { Id = 0, Name = "--- Не выбрано ---" });

                    cmbBreed.ItemsSource = breeds;
                    cmbBreed.SelectedValuePath = "Id";
                    cmbBreed.DisplayMemberPath = "Name";
                    cmbBreed.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки пород: {ex.Message}");
            }
        }

        private void LoadColors(int speciesId)
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities())
                {
                    var colors = context.Colors
                        .Where(c => c.SpeciesId == speciesId || c.SpeciesId == null)
                        .Select(c => new ColorItem { Id = c.Id, Name = c.Name, SpeciesId = c.SpeciesId })
                        .ToList();

                    colors.Insert(0, new ColorItem { Id = 0, Name = "--- Не выбрано ---" });

                    cmbColor.ItemsSource = colors;
                    cmbColor.SelectedValuePath = "Id";
                    cmbColor.DisplayMemberPath = "Name";
                    cmbColor.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки цветов: {ex.Message}");
            }
        }

        private void LoadGenders()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities())
                {
                    var genders = context.AnimalGenders
                        .Select(g => new GenderItem { Id = g.Id, Name = g.Name })
                        .ToList();

                    cmbGender.ItemsSource = genders;
                    cmbGender.SelectedValuePath = "Id";
                    cmbGender.DisplayMemberPath = "Name";
                    cmbGender.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки полов: {ex.Message}");
            }
        }

        private void LoadPedigreeClasses()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities())
                {
                    var classes = context.PedigreeClasses
                        .Select(pc => new PedigreeClassItem { Id = pc.Id, Name = pc.Name })
                        .ToList();

                    classes.Insert(0, new PedigreeClassItem { Id = 0, Name = "--- Не выбран ---" });

                    cmbPedigreeClass.ItemsSource = classes;
                    cmbPedigreeClass.SelectedValuePath = "Id";
                    cmbPedigreeClass.DisplayMemberPath = "Name";
                    cmbPedigreeClass.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки племенных классов: {ex.Message}");
            }
        }

        private void LoadAnimalsForParents()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities())
                {
                    var animals = context.Animals
                        .Where(a => a.TreeId == currentTreeId)
                        .Select(a => new AnimalComboItem { Id = a.Id, Nickname = a.Nickname })
                        .ToList();

                    animals.Insert(0, new AnimalComboItem { Id = 0, Nickname = "--- Не выбрано ---" });

                    cmbFather.ItemsSource = animals;
                    cmbFather.SelectedValuePath = "Id";
                    cmbFather.DisplayMemberPath = "Nickname";

                    cmbMother.ItemsSource = animals;
                    cmbMother.SelectedValuePath = "Id";
                    cmbMother.DisplayMemberPath = "Nickname";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки списка животных: {ex.Message}");
            }
        }

        private void Species_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbSpecies.SelectedItem != null)
            {
                int speciesId = (int)cmbSpecies.SelectedValue;
                LoadBreeds(speciesId);
                LoadColors(speciesId);
            }
        }

        private void LoadAnimalData(int animalId)
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities())
                {
                    var animal = context.Animals.FirstOrDefault(a => a.Id == animalId);
                    if (animal == null)
                    {
                        MessageBox.Show("Животное не найдено!");
                        return;
                    }

                    txtNickname.Text = animal.Nickname;
                    txtInventoryNumber.Text = animal.InventoryNumber;
                    cmbSpecies.SelectedValue = animal.SpeciesId;
                    LoadBreeds(animal.SpeciesId);
                    if (animal.BreedId.HasValue) cmbBreed.SelectedValue = animal.BreedId.Value;
                    if (animal.ColorId.HasValue) cmbColor.SelectedValue = animal.ColorId.Value;
                    cmbGender.SelectedValue = animal.GenderId;
                    chkIsCastrated.IsChecked = animal.IsCastrated == true;
                    dpBirthDate.SelectedDate = animal.BirthDate;
                    txtBirthPlace.Text = animal.BirthPlace;
                    dpDeathDate.SelectedDate = animal.DeathDate;
                    txtDeathPlace.Text = animal.DeathPlace;
                    txtDeathReason.Text = animal.DeathReason;
                    if (animal.PedigreeClassId.HasValue) cmbPedigreeClass.SelectedValue = animal.PedigreeClassId.Value;
                    txtBreedingValue.Text = animal.BreedingValue?.ToString();
                    chkIsBreedingStock.IsChecked = animal.IsBreedingStock;
                    txtHeight.Text = animal.HeightAtWithers?.ToString();
                    txtChestGirth.Text = animal.ChestGirth?.ToString();
                    txtWeight.Text = animal.Weight?.ToString();
                    txtChipNumber.Text = animal.ChipNumber;
                    txtProductivityData.Text = animal.ProductivityData;
                    txtDescription.Text = animal.Description;

                    if (!string.IsNullOrEmpty(animal.ProfilePhotoPath))
                    {
                        selectedPhotoPath = animal.ProfilePhotoPath;
                        originalPhotoPath = animal.ProfilePhotoPath;
                        ShowPhotoPreview(selectedPhotoPath);
                    }

                    // Загрузка родителей
                    var pedigree = context.AnimalPedigree.FirstOrDefault(p => p.AnimalId == animalId);
                    if (pedigree != null)
                    {
                        if (pedigree.FatherId.HasValue)
                            cmbFather.SelectedValue = pedigree.FatherId.Value;
                        if (pedigree.MotherId.HasValue)
                            cmbMother.SelectedValue = pedigree.MotherId.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void ShowPhotoPreview(string path)
        {
            try
            {
                string fullPath = PhotoHelper.GetProfilePhoto(path);
                if (File.Exists(fullPath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    imgPreview.Source = bitmap;
                    imgPreview.Visibility = Visibility.Visible;
                    txtNoImage.Visibility = Visibility.Collapsed;
                    btnRemovePhoto.IsEnabled = true;
                }
                else
                {
                    imgPreview.Visibility = Visibility.Collapsed;
                    txtNoImage.Visibility = Visibility.Visible;
                    btnRemovePhoto.IsEnabled = false;
                }
            }
            catch
            {
                imgPreview.Visibility = Visibility.Collapsed;
                txtNoImage.Visibility = Visibility.Visible;
                btnRemovePhoto.IsEnabled = false;
            }
        }

        private void SelectPhoto_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Выберите фотографию",
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.gif;*.bmp|Все файлы|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                selectedPhotoPath = dialog.FileName;
                ShowPhotoPreview(selectedPhotoPath);
            }
        }

        private void RemovePhoto_Click(object sender, RoutedEventArgs e)
        {
            selectedPhotoPath = null;
            imgPreview.Source = null;
            imgPreview.Visibility = Visibility.Collapsed;
            txtNoImage.Visibility = Visibility.Visible;
            btnRemovePhoto.IsEnabled = false;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtNickname.Text))
                {
                    MessageBox.Show("Введите кличку животного!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtNickname.Focus();
                    return;
                }

                if (cmbSpecies.SelectedItem == null)
                {
                    MessageBox.Show("Выберите вид животного!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (var context = new GenealogyUnifiedDBEntities())
                {
                    Animals animal;

                    if (editAnimalId.HasValue)
                    {
                        animal = context.Animals.FirstOrDefault(a => a.Id == editAnimalId);
                        if (animal == null)
                        {
                            MessageBox.Show("Животное не найдено!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    else
                    {
                        animal = new Animals
                        {
                            TreeId = currentTreeId,
                            CreatedByUserId = Session.UserId,
                            CreatedAt = DateTime.Now
                        };
                        context.Animals.Add(animal);
                    }

                    animal.Nickname = txtNickname.Text.Trim();
                    animal.InventoryNumber = string.IsNullOrWhiteSpace(txtInventoryNumber.Text) ? null : txtInventoryNumber.Text.Trim();
                    animal.SpeciesId = (int)cmbSpecies.SelectedValue;
                    animal.BreedId = cmbBreed.SelectedValue != null && (int)cmbBreed.SelectedValue > 0 ? (int?)cmbBreed.SelectedValue : null;
                    animal.ColorId = cmbColor.SelectedValue != null && (int)cmbColor.SelectedValue > 0 ? (int?)cmbColor.SelectedValue : null;
                    animal.GenderId = (int)cmbGender.SelectedValue;
                    animal.IsCastrated = chkIsCastrated.IsChecked;
                    animal.BirthDate = dpBirthDate.SelectedDate;
                    animal.BirthPlace = string.IsNullOrWhiteSpace(txtBirthPlace.Text) ? null : txtBirthPlace.Text.Trim();
                    animal.DeathDate = dpDeathDate.SelectedDate;
                    animal.DeathPlace = string.IsNullOrWhiteSpace(txtDeathPlace.Text) ? null : txtDeathPlace.Text.Trim();
                    animal.DeathReason = string.IsNullOrWhiteSpace(txtDeathReason.Text) ? null : txtDeathReason.Text.Trim();
                    animal.PedigreeClassId = cmbPedigreeClass.SelectedValue != null && (int)cmbPedigreeClass.SelectedValue > 0 ? (int?)cmbPedigreeClass.SelectedValue : null;

                    if (decimal.TryParse(txtBreedingValue.Text, out decimal breedingValue))
                        animal.BreedingValue = breedingValue;
                    else
                        animal.BreedingValue = null;

                    animal.IsBreedingStock = chkIsBreedingStock.IsChecked;
                    animal.ProductivityData = string.IsNullOrWhiteSpace(txtProductivityData.Text) ? null : txtProductivityData.Text.Trim();

                    if (decimal.TryParse(txtHeight.Text, out decimal height))
                        animal.HeightAtWithers = height;
                    if (decimal.TryParse(txtChestGirth.Text, out decimal chestGirth))
                        animal.ChestGirth = chestGirth;
                    if (decimal.TryParse(txtWeight.Text, out decimal weight))
                        animal.Weight = weight;

                    animal.ChipNumber = string.IsNullOrWhiteSpace(txtChipNumber.Text) ? null : txtChipNumber.Text.Trim();
                    animal.Description = string.IsNullOrWhiteSpace(txtDescription.Text) ? null : txtDescription.Text.Trim();
                    animal.UpdatedAt = DateTime.Now;

                    // Обработка фото
                    if (selectedPhotoPath != null && selectedPhotoPath != originalPhotoPath)
                    {
                        string photosFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Photos");
                        if (!Directory.Exists(photosFolder))
                            Directory.CreateDirectory(photosFolder);

                        string fileName = $"{Guid.NewGuid()}_{System.IO.Path.GetFileName(selectedPhotoPath)}";
                        string destPath = System.IO.Path.Combine(photosFolder, fileName);
                        File.Copy(selectedPhotoPath, destPath, true);
                        animal.ProfilePhotoPath = destPath;
                    }
                    else if (selectedPhotoPath == null && originalPhotoPath != null)
                    {
                        animal.ProfilePhotoPath = null;
                    }

                    context.SaveChanges();

                    // Сохранение родителей
                    SavePedigree(context, animal.Id);

                    string message = editAnimalId.HasValue ? "Данные животного обновлены!" : "Животное добавлено!";
                    MessageBox.Show(message, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    NavigationService.GoBack();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SavePedigree(GenealogyUnifiedDBEntities context, int animalId)
        {
            int fatherId = cmbFather.SelectedValue != null ? (int)cmbFather.SelectedValue : 0;
            int motherId = cmbMother.SelectedValue != null ? (int)cmbMother.SelectedValue : 0;

            var existingPedigree = context.AnimalPedigree.FirstOrDefault(p => p.AnimalId == animalId);

            if (existingPedigree != null)
            {
                existingPedigree.FatherId = fatherId > 0 ? (int?)fatherId : null;
                existingPedigree.MotherId = motherId > 0 ? (int?)motherId : null;
                existingPedigree.CreatedByUserId = Session.UserId;
                existingPedigree.CreatedAt = DateTime.Now;
            }
            else if (fatherId > 0 || motherId > 0)
            {
                var pedigree = new AnimalPedigree
                {
                    AnimalId = animalId,
                    FatherId = fatherId > 0 ? (int?)fatherId : null,
                    MotherId = motherId > 0 ? (int?)motherId : null,
                    CreatedByUserId = Session.UserId,
                    CreatedAt = DateTime.Now
                };
                context.AnimalPedigree.Add(pedigree);
            }

            context.SaveChanges();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}