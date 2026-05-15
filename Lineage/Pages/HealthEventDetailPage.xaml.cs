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

namespace Lineage.Pages
{
    public partial class HealthEventDetailPage : Page
    {
        private int eventId;

        public HealthEventDetailPage(int eventId)
        {
            InitializeComponent();
            this.eventId = eventId;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadEventData();
        }

        private void LoadEventData()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    var vetEvent = context.VeterinaryEvents.FirstOrDefault(v => v.Id == eventId);
                    if (vetEvent == null)
                    {
                        MessageBox.Show("Событие не найдено", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var eventType = context.VeterinaryEventTypes.Find(vetEvent.EventTypeId);
                    var animal = context.Animals.Find(vetEvent.AnimalId);
                    var user = context.Users.Find(vetEvent.CreatedByUserId);

                    // Животное
                    txtAnimalName.Text = animal?.Nickname ?? $"ID: {vetEvent.AnimalId}";

                    // Дата события
                    txtEventDate.Text = vetEvent.EventDate.ToString("dd.MM.yyyy");

                    // Тип события
                    txtEventType.Text = eventType?.Name ?? "—";

                    // Препарат
                    txtMedicineName.Text = string.IsNullOrEmpty(vetEvent.MedicineName) ? "—" : vetEvent.MedicineName;

                    // Дозировка
                    txtDosage.Text = string.IsNullOrEmpty(vetEvent.Dosage) ? "—" : vetEvent.Dosage;

                    // Ветеринар
                    txtVetName.Text = string.IsNullOrEmpty(vetEvent.VetName) ? "—" : vetEvent.VetName;

                    // Срок ожидания
                    if (vetEvent.WithdrawalDays.HasValue)
                        txtWithdrawalDays.Text = $"{vetEvent.WithdrawalDays} дней";
                    else
                        txtWithdrawalDays.Text = "—";

                    // Следующая обработка
                    if (vetEvent.NextDueDate.HasValue)
                        txtNextDueDate.Text = vetEvent.NextDueDate.Value.ToString("dd.MM.yyyy");
                    else
                        txtNextDueDate.Text = "—";

                    // Стоимость
                    if (vetEvent.Cost.HasValue)
                        txtCost.Text = $"{vetEvent.Cost.Value:F2} руб.";
                    else
                        txtCost.Text = "—";

                    // Примечания
                    txtNotes.Text = string.IsNullOrEmpty(vetEvent.Notes) ? "—" : vetEvent.Notes;

                    // Кто добавил
                    txtCreatedBy.Text = user?.Username ?? "Неизвестный пользователь";

                    // Дата добавления
                    txtCreatedAt.Text = vetEvent.CreatedAt.ToString("dd.MM.yyyy HH:mm");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}