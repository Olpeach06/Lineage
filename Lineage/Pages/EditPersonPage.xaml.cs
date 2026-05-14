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
using System.IO;

namespace Lineage.Pages
{
    public partial class EditPersonPage : Page
    {
        private int? editPersonId = null;
        private int currentTreeId = 1;
        private string selectedPhotoPath = null;
        private string originalPhotoPath = null;
        private List<PersonComboItem> allPersons = new List<PersonComboItem>();

        // Ограничения на длину полей
        private const int MAX_LASTNAME_LENGTH = 100;
        private const int MAX_FIRSTNAME_LENGTH = 100;
        private const int MAX_PATRONYMIC_LENGTH = 100;
        private const int MAX_MAIDENNAME_LENGTH = 100;
        private const int MAX_BIRTHPLACE_LENGTH = 200;
        private const int MAX_DEATHPLACE_LENGTH = 200;
        private const int MAX_BIOGRAPHY_LENGTH = 5000;

        public class PersonComboItem
        {
            public int Id { get; set; }
            public string DisplayName { get; set; }
        }

        public EditPersonPage()
        {
            InitializeComponent();
            txtTitle.Text = "ДОБАВЛЕНИЕ ПЕРСОНЫ";
            SetupTextValidation();
        }

        public EditPersonPage(int personId) : this()
        {
            editPersonId = personId;
            txtTitle.Text = "РЕДАКТИРОВАНИЕ ПЕРСОНЫ";
        }

        private void SetupTextValidation()
        {
            txtLastName.MaxLength = MAX_LASTNAME_LENGTH;
            txtFirstName.MaxLength = MAX_FIRSTNAME_LENGTH;
            txtPatronymic.MaxLength = MAX_PATRONYMIC_LENGTH;
            txtMaidenName.MaxLength = MAX_MAIDENNAME_LENGTH;
            txtBirthPlace.MaxLength = MAX_BIRTHPLACE_LENGTH;
            txtDeathPlace.MaxLength = MAX_DEATHPLACE_LENGTH;
            txtBiography.MaxLength = MAX_BIOGRAPHY_LENGTH;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Проверка: страница доступна только в режиме семейного древа
            if (!Session.IsFamilyMode)
            {
                MessageBox.Show("Эта страница доступна только в режиме семейного древа!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NavigationService.GoBack();
                return;
            }

            if (Session.CurrentTreeId > 0)
                currentTreeId = Session.CurrentTreeId;
            else
            {
                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    var firstTree = context.FamilyTrees.Where(t => t.CreatedByUserId == Session.UserId).FirstOrDefault();
                    if (firstTree != null)
                    {
                        currentTreeId = firstTree.Id;
                        Session.CurrentTreeId = firstTree.Id;
                    }
                    else
                    {
                        MessageBox.Show("У вас нет доступных проектов. Сначала создайте проект.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        NavigationService.GoBack();
                        return;
                    }
                }
            }

            dpBirthDate.DisplayDateEnd = DateTime.Today;
            dpDeathDate.DisplayDateEnd = DateTime.Today;

            LoadPersonsForCombo();

            if (editPersonId.HasValue)
                LoadPersonData(editPersonId.Value);
        }

        private void LoadPersonsForCombo()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    var persons = context.Persons.Where(p => p.TreeId == currentTreeId).ToList();
                    allPersons = persons.Select(p => new PersonComboItem
                    {
                        Id = p.Id,
                        DisplayName = $"{p.LastName} {p.FirstName} ({p.BirthDate?.Year})".Trim()
                    }).ToList();

                    allPersons.Insert(0, new PersonComboItem { Id = 0, DisplayName = "--- Не выбрано ---" });

                    cmbFather.ItemsSource = allPersons;
                    cmbMother.ItemsSource = allPersons;
                    cmbSpouse.ItemsSource = allPersons;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки списка персон: {ex.Message}");
            }
        }

        private void LoadPersonData(int personId)
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    var person = context.Persons.FirstOrDefault(p => p.Id == personId);
                    if (person == null)
                    {
                        MessageBox.Show("Персона не найдена!");
                        return;
                    }

                    currentTreeId = person.TreeId;
                    txtLastName.Text = person.LastName;
                    txtFirstName.Text = person.FirstName;
                    txtPatronymic.Text = person.Patronymic;
                    txtMaidenName.Text = person.MaidenName;

                    cmbGender.SelectedIndex = person.GenderId == 1 ? 0 : (person.GenderId == 2 ? 1 : 2);

                    if (person.BirthDate.HasValue) dpBirthDate.SelectedDate = person.BirthDate.Value;
                    if (person.DeathDate.HasValue) dpDeathDate.SelectedDate = person.DeathDate.Value;

                    txtBirthPlace.Text = person.BirthPlace;
                    txtDeathPlace.Text = person.DeathPlace;
                    txtBiography.Text = person.Biography;

                    LoadPersonRelationships(personId, context);

                    if (!string.IsNullOrEmpty(person.ProfilePhotoPath))
                    {
                        selectedPhotoPath = person.ProfilePhotoPath;
                        originalPhotoPath = person.ProfilePhotoPath;
                        ShowPhotoPreview(selectedPhotoPath);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void LoadPersonRelationships(int personId, GenealogyUnifiedDBEntities1 context)
        {
            try
            {
                // Родители
                var parentRelations = context.PersonRelationships
                    .Where(r => r.Person2Id == personId && r.RelationshipType == 1)
                    .ToList();

                foreach (var rel in parentRelations)
                {
                    var parent = context.Persons.FirstOrDefault(p => p.Id == rel.Person1Id);
                    if (parent != null)
                    {
                        if (parent.GenderId == 2)
                            cmbMother.SelectedValue = parent.Id;
                        else if (parent.GenderId == 1 && (cmbFather.SelectedValue == null || (int)cmbFather.SelectedValue == 0))
                            cmbFather.SelectedValue = parent.Id;
                    }
                }

                // Супруг(а)
                var spouseRel = context.PersonRelationships
                    .FirstOrDefault(r => (r.Person1Id == personId || r.Person2Id == personId) && r.RelationshipType == 2);

                if (spouseRel != null)
                {
                    int spouseId = spouseRel.Person1Id == personId ? spouseRel.Person2Id : spouseRel.Person1Id;
                    if (spouseId > 0 && allPersons.Any(p => p.Id == spouseId))
                        cmbSpouse.SelectedValue = spouseId;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки родственных связей: {ex.Message}");
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

        private bool ValidateRelationships()
        {
            int fatherId = cmbFather.SelectedValue != null ? (int)cmbFather.SelectedValue : 0;
            int motherId = cmbMother.SelectedValue != null ? (int)cmbMother.SelectedValue : 0;
            int spouseId = cmbSpouse.SelectedValue != null ? (int)cmbSpouse.SelectedValue : 0;

            if (fatherId != 0 && motherId != 0 && fatherId == motherId)
            {
                MessageBox.Show("Отец и мать не могут быть одним человеком!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (fatherId != 0 && spouseId != 0 && fatherId == spouseId)
            {
                MessageBox.Show("Отец и супруг(а) не могут быть одним человеком!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (motherId != 0 && spouseId != 0 && motherId == spouseId)
            {
                MessageBox.Show("Мать и супруг(а) не могут быть одним человеком!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (editPersonId.HasValue)
            {
                int currentPersonId = editPersonId.Value;
                if (fatherId == currentPersonId || motherId == currentPersonId || spouseId == currentPersonId)
                {
                    MessageBox.Show("Персона не может быть связана сама с собой!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }

            return true;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtLastName.Text))
                {
                    MessageBox.Show("Заполните поле Фамилия!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtLastName.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtFirstName.Text))
                {
                    MessageBox.Show("Заполните поле Имя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtFirstName.Focus();
                    return;
                }

                if (!ValidateRelationships())
                    return;

                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    Persons person;

                    if (editPersonId.HasValue)
                    {
                        person = context.Persons.FirstOrDefault(p => p.Id == editPersonId);
                        if (person == null)
                        {
                            MessageBox.Show("Персона не найдена!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    else
                    {
                        person = new Persons
                        {
                            TreeId = currentTreeId,
                            CreatedByUserId = Session.UserId,
                            CreatedAt = DateTime.Now
                        };
                        context.Persons.Add(person);
                    }

                    person.LastName = txtLastName.Text.Trim();
                    person.FirstName = txtFirstName.Text.Trim();
                    person.Patronymic = string.IsNullOrWhiteSpace(txtPatronymic.Text) ? null : txtPatronymic.Text.Trim();
                    person.MaidenName = string.IsNullOrWhiteSpace(txtMaidenName.Text) ? null : txtMaidenName.Text.Trim();
                    person.GenderId = cmbGender.SelectedIndex == 0 ? 1 : (cmbGender.SelectedIndex == 1 ? 2 : 3);
                    person.BirthDate = dpBirthDate.SelectedDate;
                    person.DeathDate = dpDeathDate.SelectedDate;
                    person.BirthPlace = string.IsNullOrWhiteSpace(txtBirthPlace.Text) ? null : txtBirthPlace.Text.Trim();
                    person.DeathPlace = string.IsNullOrWhiteSpace(txtDeathPlace.Text) ? null : txtDeathPlace.Text.Trim();
                    person.Biography = string.IsNullOrWhiteSpace(txtBiography.Text) ? null : txtBiography.Text.Trim();
                    person.UpdatedAt = DateTime.Now;

                    // Обработка фото
                    if (selectedPhotoPath != null && selectedPhotoPath != originalPhotoPath)
                    {
                        string photosFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Photos");
                        if (!Directory.Exists(photosFolder))
                            Directory.CreateDirectory(photosFolder);

                        string fileName = $"{Guid.NewGuid()}_{System.IO.Path.GetFileName(selectedPhotoPath)}";
                        string destPath = System.IO.Path.Combine(photosFolder, fileName);
                        File.Copy(selectedPhotoPath, destPath, true);
                        person.ProfilePhotoPath = destPath;
                    }
                    else if (selectedPhotoPath == null && originalPhotoPath != null)
                    {
                        person.ProfilePhotoPath = null;
                    }
                    else if (selectedPhotoPath == originalPhotoPath)
                    {
                        person.ProfilePhotoPath = originalPhotoPath;
                    }

                    context.SaveChanges();
                    SaveRelationships(context, person.Id);

                    string message = editPersonId.HasValue ? "Изменения сохранены!" : "Персона добавлена!";
                    MessageBox.Show(message, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    NavigationService.GoBack();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveRelationships(GenealogyUnifiedDBEntities1 context, int personId)
        {
            try
            {
                if (editPersonId.HasValue)
                {
                    var oldRelations = context.PersonRelationships
                        .Where(r => r.Person1Id == personId || r.Person2Id == personId)
                        .ToList();
                    context.PersonRelationships.RemoveRange(oldRelations);
                    context.SaveChanges();
                }

                int fatherId = cmbFather.SelectedValue != null ? (int)cmbFather.SelectedValue : 0;
                int motherId = cmbMother.SelectedValue != null ? (int)cmbMother.SelectedValue : 0;
                int spouseId = cmbSpouse.SelectedValue != null ? (int)cmbSpouse.SelectedValue : 0;

                if (fatherId > 0)
                {
                    context.PersonRelationships.Add(new PersonRelationships
                    {
                        Person1Id = fatherId,
                        Person2Id = personId,
                        RelationshipType = 1,
                        Direction = 1,
                        CreatedByUserId = Session.UserId,
                        CreatedAt = DateTime.Now
                    });
                }

                if (motherId > 0)
                {
                    context.PersonRelationships.Add(new PersonRelationships
                    {
                        Person1Id = motherId,
                        Person2Id = personId,
                        RelationshipType = 1,
                        Direction = 1,
                        CreatedByUserId = Session.UserId,
                        CreatedAt = DateTime.Now
                    });
                }

                if (spouseId > 0)
                {
                    context.PersonRelationships.Add(new PersonRelationships
                    {
                        Person1Id = personId,
                        Person2Id = spouseId,
                        RelationshipType = 2,
                        CreatedByUserId = Session.UserId,
                        CreatedAt = DateTime.Now
                    });
                }

                context.SaveChanges();
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка сохранения родственных связей: {ex.Message}");
            }
        }

        private void FindParent_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag == null) return;

            string tag = button.Tag.ToString();
            ComboBox targetCombo = null;

            if (tag == "Father") targetCombo = cmbFather;
            else if (tag == "Mother") targetCombo = cmbMother;
            else if (tag == "Spouse") targetCombo = cmbSpouse;

            if (targetCombo != null)
                targetCombo.IsDropDownOpen = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}