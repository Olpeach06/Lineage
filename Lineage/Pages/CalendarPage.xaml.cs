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
    public partial class CalendarPage : Page
    {
        private int treeId;
        private string treeName;

        public class BirthdayItem
        {
            public string FullName { get; set; }
            public string DayMonth { get; set; }
            public string AgeText { get; set; }
            public string BackgroundColor { get; set; }
            public DateTime BirthDate { get; set; }
            public int Month { get; set; }
            public int Day { get; set; }
            public bool IsAlive { get; set; }
        }

        public CalendarPage(int treeId, string treeName)
        {
            InitializeComponent();
            this.treeId = treeId;
            this.treeName = treeName;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (!Session.IsFamilyMode)
            {
                NotificationWindow.ShowWarning("Календарь дней рождения доступен только в режиме семейного древа!");
                NavigationService.GoBack();
                return;
            }

            txtTreeInfo.Text = $"Проект: {treeName}";
            LoadBirthdays();
        }

        private void LoadBirthdays()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities2())
                {
                    var persons = context.Persons
                        .Where(p => p.TreeId == treeId && p.BirthDate.HasValue)
                        .ToList();

                    if (!persons.Any())
                    {
                        lvBirthdays.ItemsSource = null;
                        return;
                    }

                    var today = DateTime.Today;
                    var birthdays = new List<BirthdayItem>();

                    foreach (var person in persons)
                    {
                        var birthDate = person.BirthDate.Value;
                        string dayMonth = birthDate.ToString("dd.MM");
                        bool isAlive = !person.DeathDate.HasValue;

                        string ageText;
                        if (isAlive)
                        {
                            int age = today.Year - birthDate.Year;
                            if (today < birthDate.AddYears(age)) age--;
                            ageText = $"{age} лет";
                        }
                        else
                        {
                            int ageAtDeath = person.DeathDate.Value.Year - birthDate.Year;
                            if (person.DeathDate.Value < birthDate.AddYears(ageAtDeath)) ageAtDeath--;
                            ageText = $"{ageAtDeath} лет (умер)";
                        }

                        bool isToday = birthDate.Month == today.Month && birthDate.Day == today.Day;
                        string bgColor;

                        if (isAlive)
                        {
                            bgColor = isToday ? "#FFE4B5" : "#FDF8F0";
                        }
                        else
                        {
                            bgColor = isToday ? "#D3D3D3" : "#E8E8E8";
                        }

                        birthdays.Add(new BirthdayItem
                        {
                            FullName = $"{person.LastName} {person.FirstName} {person.Patronymic}".Trim(),
                            DayMonth = dayMonth,
                            AgeText = ageText,
                            BackgroundColor = bgColor,
                            BirthDate = birthDate,
                            Month = birthDate.Month,
                            Day = birthDate.Day,
                            IsAlive = isAlive
                        });
                    }

                    birthdays = birthdays
                        .OrderBy(b => b.Month)
                        .ThenBy(b => b.Day)
                        .ToList();

                    lvBirthdays.ItemsSource = birthdays;
                }
            }
            catch (Exception ex)
            {
                NotificationWindow.ShowError("Ошибка загрузки календаря", ex.Message);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}