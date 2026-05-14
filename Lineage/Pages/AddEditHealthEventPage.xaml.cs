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
    public partial class AddEditHealthEventPage : Page
    {
        private int animalId;
        private int? eventId; // null = добавление, не null = редактирование

        public AddEditHealthEventPage(int animalId, int? eventId = null)
        {
            InitializeComponent();
            this.animalId = animalId;
            this.eventId = eventId;

            // Устанавливаем сегодняшнюю дату
            dpEventDate.SelectedDate = DateTime.Today;

            LoadEventTypes();

            if (eventId.HasValue)
            {
                txtTitle.Text = "РЕДАКТИРОВАНИЕ ВЕТЕРИНАРНОГО СОБЫТИЯ";
                LoadEventData();
            }
            else
            {
                txtTitle.Text = "ДОБАВЛЕНИЕ ВЕТЕРИНАРНОГО СОБЫТИЯ";
            }
        }

        private void LoadEventTypes()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    var eventTypes = context.VeterinaryEventTypes.OrderBy(t => t.Id).ToList();
                    cmbEventType.ItemsSource = eventTypes;
                    if (eventTypes.Any())
                        cmbEventType.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки типов событий: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadEventData()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    var vetEvent = context.VeterinaryEvents.Find(eventId.Value);
                    if (vetEvent == null)
                    {
                        MessageBox.Show("Событие не найдено", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        NavigationService.GoBack();
                        return;
                    }

                    dpEventDate.SelectedDate = vetEvent.EventDate;
                    cmbEventType.SelectedValue = vetEvent.EventTypeId;
                    txtMedicineName.Text = vetEvent.MedicineName ?? "";
                    txtDosage.Text = vetEvent.Dosage ?? "";
                    txtVetName.Text = vetEvent.VetName ?? "";
                    dpNextDueDate.SelectedDate = vetEvent.NextDueDate;
                    txtCost.Text = vetEvent.Cost?.ToString("F2") ?? "";
                    txtNotes.Text = vetEvent.Notes ?? "";
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
                if (!dpEventDate.SelectedDate.HasValue)
                {
                    MessageBox.Show("Укажите дату события!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (cmbEventType.SelectedItem == null)
                {
                    MessageBox.Show("Выберите тип события!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    VeterinaryEvents vetEvent;

                    if (eventId.HasValue)
                    {
                        // Режим редактирования
                        vetEvent = context.VeterinaryEvents.Find(eventId.Value);
                        if (vetEvent == null)
                        {
                            MessageBox.Show("Событие не найдено", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    else
                    {
                        // Режим добавления
                        vetEvent = new VeterinaryEvents();
                        context.VeterinaryEvents.Add(vetEvent);
                    }

                    vetEvent.AnimalId = animalId;
                    vetEvent.EventDate = dpEventDate.SelectedDate.Value;
                    vetEvent.EventTypeId = (int)cmbEventType.SelectedValue;
                    vetEvent.MedicineName = string.IsNullOrWhiteSpace(txtMedicineName.Text) ? null : txtMedicineName.Text.Trim();
                    vetEvent.Dosage = string.IsNullOrWhiteSpace(txtDosage.Text) ? null : txtDosage.Text.Trim();
                    vetEvent.VetName = string.IsNullOrWhiteSpace(txtVetName.Text) ? null : txtVetName.Text.Trim();
                    vetEvent.NextDueDate = dpNextDueDate.SelectedDate;
                    vetEvent.Cost = ParseDecimal(txtCost.Text);
                    vetEvent.Notes = string.IsNullOrWhiteSpace(txtNotes.Text) ? null : txtNotes.Text.Trim();
                    vetEvent.CreatedByUserId = Session.UserId;
                    vetEvent.CreatedAt = DateTime.Now;

                    context.SaveChanges();

                    MessageBox.Show(eventId.HasValue ? "Событие успешно обновлено!" : "Событие успешно добавлено!",
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

        private decimal? ParseDecimal(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            if (decimal.TryParse(text, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal result))
                return result;

            return null;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}