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
    public partial class AddBreedingPage : Page
    {
        private int currentAnimalId;
        private int? currentAnimalGender;
        private int currentTreeId;

        public class AnimalItem
        {
            public int Id { get; set; }
            public string Nickname { get; set; }
            public int GenderId { get; set; }
        }

        public AddBreedingPage(int animalId)
        {
            InitializeComponent();
            currentAnimalId = animalId;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            currentTreeId = Session.CurrentTreeId;
            LoadAnimals();

            // Получаем пол текущего животного
            using (var context = new GenealogyUnifiedDBEntities1())
            {
                var animal = context.Animals.Find(currentAnimalId);
                if (animal != null)
                {
                    currentAnimalGender = animal.GenderId;
                    txtTitle.Text = currentAnimalGender == 1 ? "ДОБАВЛЕНИЕ ВЯЗКИ (ПРОИЗВОДИТЕЛЬ)" : "ДОБАВЛЕНИЕ ВЯЗКИ (МАТКА)";
                }
            }
        }

        private void LoadAnimals()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    var allAnimals = context.Animals
                        .Where(a => a.TreeId == currentTreeId && a.Id != currentAnimalId)
                        .Select(a => new AnimalItem
                        {
                            Id = a.Id,
                            Nickname = a.Nickname,
                            GenderId = a.GenderId
                        })
                        .ToList();

                    // В зависимости от пола текущего животного, показываем противоположный пол
                    if (currentAnimalGender == 1) // текущий самец -> ищем самок
                    {
                        var females = allAnimals.Where(a => a.GenderId == 2).ToList();
                        cmbFemale.ItemsSource = females;
                        cmbFemale.DisplayMemberPath = "Nickname";
                        cmbFemale.SelectedValuePath = "Id";

                        cmbMale.IsEnabled = false;
                        cmbMale.Visibility = Visibility.Collapsed;
                        cmbFemale.IsEnabled = true;

                        // Находим текст для самца
                        var maleAnimal = context.Animals.Find(currentAnimalId);
                        if (maleAnimal != null)
                        {
                            var dummyMale = new List<AnimalItem> { new AnimalItem { Id = currentAnimalId, Nickname = maleAnimal.Nickname, GenderId = 1 } };
                            cmbMale.ItemsSource = dummyMale;
                            cmbMale.SelectedValue = currentAnimalId;
                            cmbMale.IsEnabled = false;
                        }
                    }
                    else if (currentAnimalGender == 2) // текущая самка -> ищем самцов
                    {
                        var males = allAnimals.Where(a => a.GenderId == 1).ToList();
                        cmbMale.ItemsSource = males;
                        cmbMale.DisplayMemberPath = "Nickname";
                        cmbMale.SelectedValuePath = "Id";

                        cmbMale.IsEnabled = true;
                        cmbFemale.IsEnabled = false;
                        cmbFemale.Visibility = Visibility.Collapsed;

                        var femaleAnimal = context.Animals.Find(currentAnimalId);
                        if (femaleAnimal != null)
                        {
                            var dummyFemale = new List<AnimalItem> { new AnimalItem { Id = currentAnimalId, Nickname = femaleAnimal.Nickname, GenderId = 2 } };
                            cmbFemale.ItemsSource = dummyFemale;
                            cmbFemale.SelectedValue = currentAnimalId;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки животных: {ex.Message}");
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!dpBreedingDate.SelectedDate.HasValue)
                {
                    MessageBox.Show("Укажите дату вязки!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int maleId = 0;
                int femaleId = 0;

                if (currentAnimalGender == 1) // текущий самец
                {
                    maleId = currentAnimalId;
                    if (cmbFemale.SelectedValue == null || (int)cmbFemale.SelectedValue == 0)
                    {
                        MessageBox.Show("Выберите самку!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    femaleId = (int)cmbFemale.SelectedValue;
                }
                else if (currentAnimalGender == 2) // текущая самка
                {
                    femaleId = currentAnimalId;
                    if (cmbMale.SelectedValue == null || (int)cmbMale.SelectedValue == 0)
                    {
                        MessageBox.Show("Выберите самца!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    maleId = (int)cmbMale.SelectedValue;
                }
                else
                {
                    MessageBox.Show("Невозможно определить пол животного!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int offspringCount = 0;
                if (!string.IsNullOrWhiteSpace(txtOffspringCount.Text))
                    int.TryParse(txtOffspringCount.Text, out offspringCount);

                int aliveCount = 0;
                if (!string.IsNullOrWhiteSpace(txtAliveCount.Text))
                    int.TryParse(txtAliveCount.Text, out aliveCount);

                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    var breeding = new Breedings
                    {
                        TreeId = currentTreeId,
                        MaleId = maleId,
                        FemaleId = femaleId,
                        BreedingDate = dpBreedingDate.SelectedDate.Value,
                        ExpectedBirthDate = dpExpectedBirth.SelectedDate,
                        ActualBirthDate = dpActualBirth.SelectedDate,
                        IsSuccessful = chkIsSuccessful.IsChecked,
                        OffspringCount = offspringCount > 0 ? (int?)offspringCount : null,
                        AliveCount = aliveCount > 0 ? (int?)aliveCount : null,
                        Notes = string.IsNullOrWhiteSpace(txtNotes.Text) ? null : txtNotes.Text.Trim(),
                        CreatedByUserId = Session.UserId,
                        CreatedAt = DateTime.Now
                    };

                    context.Breedings.Add(breeding);
                    context.SaveChanges();

                    MessageBox.Show("Вязка добавлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    NavigationService.GoBack();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}