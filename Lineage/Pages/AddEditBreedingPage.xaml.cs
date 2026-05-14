using Lineage.AppData;
using Lineage.Classes;
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

namespace Lineage.Pages
{
    public partial class AddEditBreedingPage : Page
    {
        private int animalId;           // ID текущего животного (для которого открыта страница)
        private int? breedingId;        // null = добавление, не null = редактирование
        private int currentAnimalTreeId;

        public AddEditBreedingPage(int animalId, int? breedingId = null)
        {
            InitializeComponent();
            this.animalId = animalId;
            this.breedingId = breedingId;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadAnimals();

            if (breedingId.HasValue)
            {
                txtTitle.Text = "РЕДАКТИРОВАНИЕ ВЯЗКИ";
                LoadBreedingData();
            }
            else
            {
                txtTitle.Text = "ДОБАВЛЕНИЕ ВЯЗКИ";
            }
        }

        private void LoadAnimals()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    // Получаем текущее животное
                    var currentAnimal = context.Animals.Find(animalId);
                    if (currentAnimal == null)
                    {
                        MessageBox.Show("Животное не найдено", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        NavigationService.GoBack();
                        return;
                    }

                    currentAnimalTreeId = currentAnimal.TreeId;

                    // Загружаем всех самцов из этого же проекта
                    var males = context.Animals
                        .Where(a => a.TreeId == currentAnimalTreeId && a.GenderId == 1)
                        .ToList();

                    // Загружаем всех самок из этого же проекта
                    var females = context.Animals
                        .Where(a => a.TreeId == currentAnimalTreeId && a.GenderId == 2)
                        .ToList();

                    cmbMale.ItemsSource = males;
                    cmbFemale.ItemsSource = females;

                    // Если это редактирование, не сбрасываем выбор
                    if (!breedingId.HasValue)
                    {
                        // Если текущее животное - самец, выбираем его в cmbMale
                        if (currentAnimal.GenderId == 1)
                        {
                            cmbMale.SelectedValue = animalId;
                            cmbFemale.SelectedIndex = -1;
                        }
                        // Если текущее животное - самка, выбираем её в cmbFemale
                        else if (currentAnimal.GenderId == 2)
                        {
                            cmbFemale.SelectedValue = animalId;
                            cmbMale.SelectedIndex = -1;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки животных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadBreedingData()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    var breeding = context.Breedings.Find(breedingId.Value);
                    if (breeding == null)
                    {
                        MessageBox.Show("Вязка не найдена", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        NavigationService.GoBack();
                        return;
                    }

                    // Выбираем самца
                    cmbMale.SelectedValue = breeding.MaleId;

                    // Выбираем самку
                    cmbFemale.SelectedValue = breeding.FemaleId;

                    // Заполняем остальные поля
                    dpBreedingDate.SelectedDate = breeding.BreedingDate;
                    dpExpectedBirth.SelectedDate = breeding.ExpectedBirthDate;
                    dpActualBirth.SelectedDate = breeding.ActualBirthDate;
                    txtOffspringCount.Text = breeding.OffspringCount?.ToString() ?? "";
                    txtAliveCount.Text = breeding.AliveCount?.ToString() ?? "";
                    chkIsSuccessful.IsChecked = breeding.IsSuccessful;
                    txtNotes.Text = breeding.Notes ?? "";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверка обязательных полей
                if (cmbMale.SelectedItem == null)
                {
                    MessageBox.Show("Выберите производителя (самца)!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (cmbFemale.SelectedItem == null)
                {
                    MessageBox.Show("Выберите матку (самку)!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!dpBreedingDate.SelectedDate.HasValue)
                {
                    MessageBox.Show("Укажите дату вязки!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int maleId = (int)cmbMale.SelectedValue;
                int femaleId = (int)cmbFemale.SelectedValue;

                // Получаем количество потомков
                int? offspringCount = null;
                if (!string.IsNullOrWhiteSpace(txtOffspringCount.Text))
                {
                    if (int.TryParse(txtOffspringCount.Text, out int oc))
                        offspringCount = oc;
                    else
                    {
                        MessageBox.Show("Количество родившихся должно быть целым числом!", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // Получаем количество выживших
                int? aliveCount = null;
                if (!string.IsNullOrWhiteSpace(txtAliveCount.Text))
                {
                    if (int.TryParse(txtAliveCount.Text, out int ac))
                        aliveCount = ac;
                    else
                    {
                        MessageBox.Show("Количество выживших должно быть целым числом!", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    Breedings breeding;

                    if (breedingId.HasValue)
                    {
                        // Режим редактирования
                        breeding = context.Breedings.Find(breedingId.Value);
                        if (breeding == null)
                        {
                            MessageBox.Show("Вязка не найдена", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    else
                    {
                        // Режим добавления
                        breeding = new Breedings();
                        context.Breedings.Add(breeding);
                    }

                    breeding.TreeId = currentAnimalTreeId;
                    breeding.MaleId = maleId;
                    breeding.FemaleId = femaleId;
                    breeding.BreedingDate = dpBreedingDate.SelectedDate.Value;
                    breeding.ExpectedBirthDate = dpExpectedBirth.SelectedDate;
                    breeding.ActualBirthDate = dpActualBirth.SelectedDate;
                    breeding.IsSuccessful = chkIsSuccessful.IsChecked;
                    breeding.OffspringCount = offspringCount;
                    breeding.AliveCount = aliveCount;
                    breeding.Notes = string.IsNullOrWhiteSpace(txtNotes.Text) ? null : txtNotes.Text;
                    breeding.CreatedByUserId = Session.UserId;
                    breeding.CreatedAt = DateTime.Now;

                    context.SaveChanges();

                    MessageBox.Show(breedingId.HasValue ? "Вязка успешно обновлена!" : "Вязка успешно добавлена!",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    NavigationService.GoBack();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}