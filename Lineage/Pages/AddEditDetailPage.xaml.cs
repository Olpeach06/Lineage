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
    public enum EditDetailType
    {
        Breeding,
        Exhibition,
        Assessment,
        HealthEvent
    }

    public partial class AddEditDetailPage : Page
    {
        private EditDetailType currentType;
        private int animalId;
        private int? itemId; // null = добавление, не null = редактирование
        private int currentTreeId;

        public AddEditDetailPage(EditDetailType type, int animalId, int? itemId = null)
        {
            InitializeComponent();
            this.currentType = type;
            this.animalId = animalId;
            this.itemId = itemId;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Проверка режима
            if (!Session.IsBreedingMode)
            {
                MessageBox.Show("Эта страница доступна только в режиме племенной книги!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NavigationService.GoBack();
                return;
            }

            // Получаем TreeId животного
            using (var context = new GenealogyUnifiedDBEntities2())
            {
                var animal = context.Animals.Find(animalId);
                if (animal == null)
                {
                    MessageBox.Show("Животное не найдено!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    NavigationService.GoBack();
                    return;
                }
                currentTreeId = animal.TreeId;
            }

            // Настраиваем UI в зависимости от типа
            ConfigureUI();

            // Загружаем данные для ComboBox
            LoadComboBoxData();

            // Если редактирование - загружаем данные
            if (itemId.HasValue)
            {
                LoadData();
            }
        }

        private void ConfigureUI()
        {
            // Скрываем все панели
            panelBreeding.Visibility = Visibility.Collapsed;
            panelExhibition.Visibility = Visibility.Collapsed;
            panelAssessment.Visibility = Visibility.Collapsed;
            panelHealthEvent.Visibility = Visibility.Collapsed;

            // Показываем нужную панель и настраиваем заголовок
            switch (currentType)
            {
                case EditDetailType.Breeding:
                    panelBreeding.Visibility = Visibility.Visible;
                    txtTitle.Text = itemId.HasValue ? "РЕДАКТИРОВАНИЕ ВЯЗКИ" : "ДОБАВЛЕНИЕ ВЯЗКИ";
                    dpBreedingDate.SelectedDate = DateTime.Today;
                    break;

                case EditDetailType.Exhibition:
                    panelExhibition.Visibility = Visibility.Visible;
                    txtTitle.Text = itemId.HasValue ? "РЕДАКТИРОВАНИЕ ВЫСТАВКИ" : "ДОБАВЛЕНИЕ ВЫСТАВКИ";
                    dpExhibitionDate.SelectedDate = DateTime.Today;
                    break;

                case EditDetailType.Assessment:
                    panelAssessment.Visibility = Visibility.Visible;
                    txtTitle.Text = itemId.HasValue ? "РЕДАКТИРОВАНИЕ ОЦЕНКИ" : "ДОБАВЛЕНИЕ ОЦЕНКИ";
                    dpAssessmentDate.SelectedDate = DateTime.Today;
                    break;

                case EditDetailType.HealthEvent:
                    panelHealthEvent.Visibility = Visibility.Visible;
                    txtTitle.Text = itemId.HasValue ? "РЕДАКТИРОВАНИЕ СОБЫТИЯ ЗДОРОВЬЯ" : "ДОБАВЛЕНИЕ СОБЫТИЯ ЗДОРОВЬЯ";
                    dpEventDate.SelectedDate = DateTime.Today;
                    break;
            }
        }

        private void LoadComboBoxData()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities2())
                {
                    // Для вязки - загружаем самцов и самок
                    if (currentType == EditDetailType.Breeding)
                    {
                        var males = context.Animals
                            .Where(a => a.TreeId == currentTreeId && a.GenderId == 1)
                            .ToList();
                        var females = context.Animals
                            .Where(a => a.TreeId == currentTreeId && a.GenderId == 2)
                            .ToList();

                        cmbMale.ItemsSource = males;
                        cmbFemale.ItemsSource = females;

                        // Если это добавление и животное имеет пол - предвыбираем его
                        if (!itemId.HasValue)
                        {
                            var currentAnimal = context.Animals.Find(animalId);
                            if (currentAnimal != null)
                            {
                                if (currentAnimal.GenderId == 1)
                                    cmbMale.SelectedValue = animalId;
                                else if (currentAnimal.GenderId == 2)
                                    cmbFemale.SelectedValue = animalId;
                            }
                        }
                    }

                    // Для оценки - загружаем племенные классы
                    else if (currentType == EditDetailType.Assessment)
                    {
                        var classes = context.PedigreeClasses.OrderBy(c => c.Code).ToList();
                        cmbClass.ItemsSource = classes;
                        if (classes.Any())
                            cmbClass.SelectedIndex = 0;
                    }

                    // Для ветсобытия - загружаем типы событий
                    else if (currentType == EditDetailType.HealthEvent)
                    {
                        var eventTypes = context.VeterinaryEventTypes.OrderBy(t => t.Id).ToList();
                        cmbEventType.ItemsSource = eventTypes;
                        if (eventTypes.Any())
                            cmbEventType.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadData()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities2())
                {
                    switch (currentType)
                    {
                        case EditDetailType.Breeding:
                            var breeding = context.Breedings.Find(itemId.Value);
                            if (breeding != null)
                            {
                                cmbMale.SelectedValue = breeding.MaleId;
                                cmbFemale.SelectedValue = breeding.FemaleId;
                                dpBreedingDate.SelectedDate = breeding.BreedingDate;
                                dpExpectedBirth.SelectedDate = breeding.ExpectedBirthDate;
                                dpActualBirth.SelectedDate = breeding.ActualBirthDate;
                                txtOffspringCount.Text = breeding.OffspringCount?.ToString();
                                txtAliveCount.Text = breeding.AliveCount?.ToString();
                                chkIsSuccessful.IsChecked = breeding.IsSuccessful;
                                txtNotes.Text = breeding.Notes;
                            }
                            break;

                        case EditDetailType.Exhibition:
                            var exhibition = context.Exhibitions.Find(itemId.Value);
                            if (exhibition != null)
                            {
                                txtExhibitionName.Text = exhibition.ExhibitionName;
                                dpExhibitionDate.SelectedDate = exhibition.ExhibitionDate;
                                txtLocation.Text = exhibition.Location;
                                txtResult.Text = exhibition.Result;
                                txtJudgeName.Text = exhibition.JudgeName;
                                txtRating.Text = exhibition.Rating?.ToString();
                                txtCertificateNumber.Text = exhibition.CertificateNumber;
                                txtExhibitionNotes.Text = exhibition.Notes;
                            }
                            break;

                        case EditDetailType.Assessment:
                            var assessment = context.AnimalAssessments.Find(itemId.Value);
                            if (assessment != null)
                            {
                                dpAssessmentDate.SelectedDate = assessment.AssessmentDate;
                                cmbClass.SelectedValue = assessment.ClassId;
                                txtOverallScore.Text = assessment.OverallScore?.ToString("F2");
                                txtExteriorScore.Text = assessment.ExteriorScore?.ToString("F2");
                                txtProductivityScore.Text = assessment.ProductivityScore?.ToString("F2");
                                txtOffspringScore.Text = assessment.OffspringScore?.ToString("F2");
                                txtCommissionMembers.Text = assessment.CommissionMembers;
                                txtAssessmentCertificateNumber.Text = assessment.CertificateNumber;
                                dpValidUntil.SelectedDate = assessment.ValidUntil;
                                txtAssessmentNotes.Text = assessment.Notes;
                            }
                            break;

                        case EditDetailType.HealthEvent:
                            var vetEvent = context.VeterinaryEvents.Find(itemId.Value);
                            if (vetEvent != null)
                            {
                                dpEventDate.SelectedDate = vetEvent.EventDate;
                                cmbEventType.SelectedValue = vetEvent.EventTypeId;
                                txtMedicineName.Text = vetEvent.MedicineName;
                                txtDosage.Text = vetEvent.Dosage;
                                txtVetName.Text = vetEvent.VetName;
                                dpNextDueDate.SelectedDate = vetEvent.NextDueDate;
                                txtCost.Text = vetEvent.Cost?.ToString("F2");
                                txtHealthNotes.Text = vetEvent.Notes;
                            }
                            break;
                    }
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
                using (var context = new GenealogyUnifiedDBEntities2())
                {
                    switch (currentType)
                    {
                        case EditDetailType.Breeding:
                            SaveBreeding(context);
                            break;
                        case EditDetailType.Exhibition:
                            SaveExhibition(context);
                            break;
                        case EditDetailType.Assessment:
                            SaveAssessment(context);
                            break;
                        case EditDetailType.HealthEvent:
                            SaveHealthEvent(context);
                            break;
                    }

                    context.SaveChanges();

                    string message = itemId.HasValue ? "Запись успешно обновлена!" : "Запись успешно добавлена!";
                    MessageBox.Show(message, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    NavigationService.GoBack();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveBreeding(GenealogyUnifiedDBEntities2 context)
        {
            if (cmbMale.SelectedItem == null || cmbFemale.SelectedItem == null)
            {
                MessageBox.Show("Выберите производителя и матку!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!dpBreedingDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Укажите дату вязки!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Breedings breeding;
            if (itemId.HasValue)
            {
                breeding = context.Breedings.Find(itemId.Value);
                if (breeding == null) throw new Exception("Вязка не найдена");
            }
            else
            {
                breeding = new Breedings();
                context.Breedings.Add(breeding);
            }

            breeding.TreeId = currentTreeId;
            breeding.MaleId = (int)cmbMale.SelectedValue;
            breeding.FemaleId = (int)cmbFemale.SelectedValue;
            breeding.BreedingDate = dpBreedingDate.SelectedDate.Value;
            breeding.ExpectedBirthDate = dpExpectedBirth.SelectedDate;
            breeding.ActualBirthDate = dpActualBirth.SelectedDate;
            breeding.IsSuccessful = chkIsSuccessful.IsChecked;
            breeding.OffspringCount = ParseInt(txtOffspringCount.Text);
            breeding.AliveCount = ParseInt(txtAliveCount.Text);
            breeding.Notes = txtNotes.Text;
            breeding.CreatedByUserId = Session.UserId;
            breeding.CreatedAt = DateTime.Now;
        }

        private void SaveExhibition(GenealogyUnifiedDBEntities2 context)
        {
            if (string.IsNullOrWhiteSpace(txtExhibitionName.Text))
            {
                MessageBox.Show("Введите название выставки!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!dpExhibitionDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Укажите дату проведения!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Exhibitions exhibition;
            if (itemId.HasValue)
            {
                exhibition = context.Exhibitions.Find(itemId.Value);
                if (exhibition == null) throw new Exception("Выставка не найдена");
            }
            else
            {
                exhibition = new Exhibitions();
                context.Exhibitions.Add(exhibition);
            }

            exhibition.TreeId = currentTreeId;
            exhibition.AnimalId = animalId;
            exhibition.ExhibitionName = txtExhibitionName.Text.Trim();
            exhibition.ExhibitionDate = dpExhibitionDate.SelectedDate.Value;
            exhibition.Location = txtLocation.Text;
            exhibition.Result = txtResult.Text;
            exhibition.JudgeName = txtJudgeName.Text;
            exhibition.Rating = ParseInt(txtRating.Text);
            exhibition.CertificateNumber = txtCertificateNumber.Text;
            exhibition.Notes = txtExhibitionNotes.Text;
            exhibition.CreatedByUserId = Session.UserId;
            exhibition.CreatedAt = DateTime.Now;
        }

        private void SaveAssessment(GenealogyUnifiedDBEntities2 context)
        {
            if (!dpAssessmentDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Укажите дату оценки!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (cmbClass.SelectedItem == null)
            {
                MessageBox.Show("Выберите племенной класс!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AnimalAssessments assessment;
            if (itemId.HasValue)
            {
                assessment = context.AnimalAssessments.Find(itemId.Value);
                if (assessment == null) throw new Exception("Оценка не найдена");
            }
            else
            {
                assessment = new AnimalAssessments();
                context.AnimalAssessments.Add(assessment);
            }

            assessment.AnimalId = animalId;
            assessment.AssessmentDate = dpAssessmentDate.SelectedDate.Value;
            assessment.ClassId = (int)cmbClass.SelectedValue;
            assessment.OverallScore = ParseDecimal(txtOverallScore.Text);
            assessment.ExteriorScore = ParseDecimal(txtExteriorScore.Text);
            assessment.ProductivityScore = ParseDecimal(txtProductivityScore.Text);
            assessment.OffspringScore = ParseDecimal(txtOffspringScore.Text);
            assessment.CommissionMembers = txtCommissionMembers.Text;
            assessment.CertificateNumber = txtAssessmentCertificateNumber.Text;
            assessment.ValidUntil = dpValidUntil.SelectedDate;
            assessment.Notes = txtAssessmentNotes.Text;
            assessment.CreatedByUserId = Session.UserId;
            assessment.CreatedAt = DateTime.Now;
        }

        private void SaveHealthEvent(GenealogyUnifiedDBEntities2 context)
        {
            if (!dpEventDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Укажите дату события!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (cmbEventType.SelectedItem == null)
            {
                MessageBox.Show("Выберите тип события!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            VeterinaryEvents vetEvent;
            if (itemId.HasValue)
            {
                vetEvent = context.VeterinaryEvents.Find(itemId.Value);
                if (vetEvent == null) throw new Exception("Событие не найдено");
            }
            else
            {
                vetEvent = new VeterinaryEvents();
                context.VeterinaryEvents.Add(vetEvent);
            }

            vetEvent.AnimalId = animalId;
            vetEvent.EventDate = dpEventDate.SelectedDate.Value;
            vetEvent.EventTypeId = (int)cmbEventType.SelectedValue;
            vetEvent.MedicineName = txtMedicineName.Text;
            vetEvent.Dosage = txtDosage.Text;
            vetEvent.VetName = txtVetName.Text;
            vetEvent.NextDueDate = dpNextDueDate.SelectedDate;
            vetEvent.Cost = ParseDecimal(txtCost.Text);
            vetEvent.Notes = txtHealthNotes.Text;
            vetEvent.CreatedByUserId = Session.UserId;
            vetEvent.CreatedAt = DateTime.Now;
        }

        private int? ParseInt(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            if (int.TryParse(text, out int result)) return result;
            return null;
        }

        private decimal? ParseDecimal(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            if (decimal.TryParse(text, out decimal result)) return result;
            return null;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}