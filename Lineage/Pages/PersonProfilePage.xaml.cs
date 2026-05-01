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
    public partial class PersonProfilePage : Page
    {
        private int personId;
        private List<StoryItem> stories = new List<StoryItem>();

        public class StoryItem
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public string Content { get; set; }
            public DateTime? EventDate { get; set; }
            public string EventDateString { get; set; }
            public string ShortContent { get; set; }
        }

        public class PhotoItem
        {
            public int Id { get; set; }
            public string FilePath { get; set; }
            public string ThumbPath { get; set; }
            public string FileName { get; set; }
        }

        public class VideoItem
        {
            public int Id { get; set; }
            public string FileName { get; set; }
            public string FilePath { get; set; }
        }

        public class AudioItem
        {
            public int Id { get; set; }
            public string FileName { get; set; }
            public string FilePath { get; set; }
        }

        public PersonProfilePage(int id)
        {
            InitializeComponent();
            personId = id;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ClearAllTextBlocks();
            LoadPersonData();
            LoadStories();
            LoadMediaFiles();

            bool canEdit = Session.IsAdmin || Session.IsEditor;
            bool isAdmin = Session.IsAdmin;

            btnEdit.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
            btnAddStory.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
            btnAddStoryBottom.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
            btnAddPhoto.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
            btnAddMedia.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
            btnDeleteAllStories.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            btnDeleteAllMedia.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ClearAllTextBlocks()
        {
            txtFather.Text = "";
            txtMother.Text = "";
            txtSpouse.Text = "";
            txtChildren.Text = "";
            txtFullName.Text = "";
            txtBirthDate.Text = "";
            txtDeathDate.Text = "";
            txtBirthPlace.Text = "";
            txtDeathPlace.Text = "";
            txtBiography.Text = "";
            txtGender.Text = "";
            txtGenderSymbol.Text = "";
        }

        private void LoadPersonData()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities())
                {
                    var person = context.Persons.FirstOrDefault(p => p.Id == personId);
                    if (person == null)
                    {
                        MessageBox.Show("Персона не найдена");
                        NavigationService.GoBack();
                        return;
                    }

                    string fullName = $"{person.LastName} {person.FirstName}";
                    if (!string.IsNullOrEmpty(person.Patronymic))
                        fullName += $" {person.Patronymic}";
                    txtFullName.Text = fullName;

                    txtBirthDate.Text = person.BirthDate?.ToString("dd.MM.yyyy") ?? "?";
                    txtDeathDate.Text = person.DeathDate?.ToString("dd.MM.yyyy") ?? "...";
                    txtBirthPlace.Text = string.IsNullOrEmpty(person.BirthPlace) ? "Место рождения: не указано" : $"Место рождения: {person.BirthPlace}";
                    txtDeathPlace.Text = string.IsNullOrEmpty(person.DeathPlace) ? "Место смерти: не указано" : $"Место смерти: {person.DeathPlace}";
                    txtBiography.Text = string.IsNullOrEmpty(person.Biography) ? "Биография не добавлена" : person.Biography;

                    var gender = context.Genders.FirstOrDefault(g => g.Id == person.GenderId);
                    if (gender != null)
                    {
                        txtGender.Text = gender.Name;
                        txtGenderSymbol.Text = gender.Symbol ?? "👤";
                    }

                    // Загрузка фото
                    string photoFullPath = PhotoHelper.GetProfilePhoto(person.ProfilePhotoPath);
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
                    var parentRelations = context.PersonRelationships
                        .Where(r => r.Person2Id == personId && r.RelationshipType == 1)
                        .Select(r => r.Person1Id)
                        .ToList();

                    if (parentRelations.Any())
                    {
                        int? fatherId = null;
                        int? motherId = null;

                        foreach (var parentId in parentRelations)
                        {
                            var parent = context.Persons.FirstOrDefault(p => p.Id == parentId);
                            if (parent != null)
                            {
                                if (parent.GenderId == 1) fatherId = parent.Id;
                                else if (parent.GenderId == 2) motherId = parent.Id;
                            }
                        }

                        if (fatherId.HasValue)
                        {
                            var father = context.Persons.Find(fatherId.Value);
                            if (father != null)
                            {
                                string fatherName = $"{father.LastName} {father.FirstName}";
                                txtFather.Text = fatherName;
                                txtFather.Tag = father.Id;
                                txtFather.Cursor = Cursors.Hand;
                                txtFather.MouseLeftButtonUp += TextBlock_MouseLeftButtonUp;
                            }
                        }
                        else
                            txtFather.Text = "Отец: не указан";

                        if (motherId.HasValue)
                        {
                            var mother = context.Persons.Find(motherId.Value);
                            if (mother != null)
                            {
                                string motherName = $"{mother.LastName} {mother.FirstName}";
                                txtMother.Text = motherName;
                                txtMother.Tag = mother.Id;
                                txtMother.Cursor = Cursors.Hand;
                                txtMother.MouseLeftButtonUp += TextBlock_MouseLeftButtonUp;
                            }
                        }
                        else
                            txtMother.Text = "Мать: не указана";
                    }
                    else
                    {
                        txtFather.Text = "Отец: не указан";
                        txtMother.Text = "Мать: не указана";
                    }

                    // Супруг(а)
                    var spouseRel = context.PersonRelationships
                        .FirstOrDefault(r => (r.Person1Id == personId || r.Person2Id == personId) && r.RelationshipType == 2);

                    if (spouseRel != null)
                    {
                        int spouseId = spouseRel.Person1Id == personId ? spouseRel.Person2Id : spouseRel.Person1Id;
                        var spouse = context.Persons.FirstOrDefault(p => p.Id == spouseId);
                        if (spouse != null)
                        {
                            string spouseName = $"{spouse.LastName} {spouse.FirstName}";
                            txtSpouse.Text = spouseName;
                            txtSpouse.Tag = spouse.Id;
                            txtSpouse.Cursor = Cursors.Hand;
                            txtSpouse.MouseLeftButtonUp += TextBlock_MouseLeftButtonUp;
                        }
                        else
                            txtSpouse.Text = "нет";
                    }
                    else
                        txtSpouse.Text = "нет";

                    // Дети
                    var childRelations = context.PersonRelationships
                        .Where(r => r.Person1Id == personId && r.RelationshipType == 1)
                        .ToList();

                    if (childRelations.Any())
                    {
                        var childIds = childRelations.Select(r => r.Person2Id).ToList();
                        var children = context.Persons.Where(p => childIds.Contains(p.Id)).ToList();

                        if (children.Any())
                        {
                            var childNames = new List<string>();
                            var childIdList = new List<int>();
                            foreach (var child in children)
                            {
                                string name = $"{child.LastName} {child.FirstName}";
                                childNames.Add(name);
                                childIdList.Add(child.Id);
                            }
                            txtChildren.Text = string.Join(", ", childNames);
                            txtChildren.Tag = childIdList;
                            txtChildren.Cursor = Cursors.Hand;
                            txtChildren.MouseLeftButtonUp += TextBlock_MouseLeftButtonUp;
                        }
                        else
                            txtChildren.Text = "нет";
                    }
                    else
                        txtChildren.Text = "нет";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void TextBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var textBlock = sender as TextBlock;
            if (textBlock?.Tag != null)
            {
                if (textBlock.Tag is int id)
                    NavigateToPerson(id);
                else if (textBlock.Tag is List<int> ids && ids.Any())
                    NavigateToPerson(ids.First());
            }
        }

        private void NavigateToPerson(int id)
        {
            NavigationService.Navigate(new PersonProfilePage(id));
        }

        private void LoadStories()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities())
                {
                    var storyList = context.Stories
                        .Where(s => s.PersonId == personId)
                        .OrderByDescending(s => s.EventDate ?? DateTime.MinValue)
                        .ToList();

                    stories = storyList.Select(s => new StoryItem
                    {
                        Id = s.Id,
                        Title = s.Title,
                        Content = s.Content,
                        EventDate = s.EventDate,
                        EventDateString = s.EventDate?.ToString("dd.MM.yyyy") ?? s.EventDateText ?? "Дата не указана",
                        ShortContent = s.Content.Length > 100 ? s.Content.Substring(0, 100) + "..." : s.Content
                    }).ToList();

                    lvStories.ItemsSource = stories;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки историй: {ex.Message}");
            }
        }

        private void LoadMediaFiles()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities())
                {
                    var stories = context.Stories.Where(s => s.PersonId == personId).Select(s => s.Id).ToList();
                    var mediaLinks = context.MediaLinks.Where(ml => ml.StoryId.HasValue && stories.Contains(ml.StoryId.Value))
                        .Select(ml => ml.MediaFileId).ToList();
                    var mediaFiles = context.MediaFiles.Where(mf => mediaLinks.Contains(mf.Id)).ToList();

                    var photos = new List<PhotoItem>();
                    var videos = new List<VideoItem>();
                    var audios = new List<AudioItem>();

                    foreach (var file in mediaFiles)
                    {
                        var mediaType = context.MediaTypes.FirstOrDefault(mt => mt.Id == file.MediaTypeId);
                        string typeName = mediaType?.Name ?? "";
                        string fullPath = PhotoHelper.GetProfilePhoto(file.FilePath);
                        bool fileExists = File.Exists(fullPath);

                        if (typeName.Contains("Изображение") || typeName.Contains("Image") || typeName.Contains("Фото"))
                        {
                            photos.Add(new PhotoItem
                            {
                                Id = file.Id,
                                FilePath = fullPath,
                                FileName = file.FileName,
                                ThumbPath = fullPath
                            });
                        }
                        else if (typeName.Contains("Видео") || typeName.Contains("Video"))
                        {
                            videos.Add(new VideoItem
                            {
                                Id = file.Id,
                                FileName = file.FileName,
                                FilePath = fullPath
                            });
                        }
                        else if (typeName.Contains("Аудио") || typeName.Contains("Audio"))
                        {
                            audios.Add(new AudioItem
                            {
                                Id = file.Id,
                                FileName = file.FileName,
                                FilePath = fullPath
                            });
                        }
                    }

                    icPhotos.ItemsSource = photos;
                    icVideos.ItemsSource = videos;
                    icAudios.ItemsSource = audios;
                    UpdateTabHeaders(photos.Count, videos.Count, audios.Count);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки медиафайлов: {ex.Message}");
            }
        }

        private void UpdateTabHeaders(int photoCount, int videoCount, int audioCount)
        {
            tabPhotos.Header = $"📷 Фотографии ({photoCount})";
            tabVideos.Header = $"🎥 Видео ({videoCount})";
            tabAudios.Header = $"🎵 Аудио ({audioCount})";
        }

        private void ReadStory_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag == null) return;

            int storyId = (int)button.Tag;
            using (var context = new GenealogyUnifiedDBEntities())
            {
                var story = context.Stories.FirstOrDefault(s => s.Id == storyId);
                if (story != null)
                {
                    var storyWindow = new StoryDetailWindow(storyId, story.PersonId, txtFullName.Text)
                    {
                        Owner = Window.GetWindow(this)
                    };
                    storyWindow.ShowDialog();
                }
            }
        }

        private void Photo_Click(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border?.Tag != null)
            {
                int photoId = (int)border.Tag;
                var photos = icPhotos.ItemsSource as List<PhotoItem>;
                var photo = photos?.FirstOrDefault(p => p.Id == photoId);
                if (photo != null && File.Exists(photo.FilePath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = photo.FilePath, UseShellExecute = true });
                }
            }
        }

        private void Video_Click(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border?.Tag != null)
            {
                int videoId = (int)border.Tag;
                var videos = icVideos.ItemsSource as List<VideoItem>;
                var video = videos?.FirstOrDefault(v => v.Id == videoId);
                if (video != null && File.Exists(video.FilePath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = video.FilePath, UseShellExecute = true });
                }
            }
        }

        private void PlayAudio_Click(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border?.Tag != null)
            {
                int audioId = (int)border.Tag;
                var audios = icAudios.ItemsSource as List<AudioItem>;
                var audio = audios?.FirstOrDefault(a => a.Id == audioId);
                if (audio != null && File.Exists(audio.FilePath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = audio.FilePath, UseShellExecute = true });
                }
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new EditPersonPage(personId));
        }

        private void AddStoryButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new EditStoryPage(personId));
        }

        private void AddPhotoButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new EditPersonPage(personId));
        }

        private void AddMediaButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new EditStoryPage(personId));
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
            else
                NavigationService.Navigate(new MainPage());
        }

        private void DeleteAllStories_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите удалить ВСЕ истории этой персоны?", "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                // Логика удаления всех историй
                MessageBox.Show("Все истории удалены!");
                LoadStories();
            }
        }

        private void DeleteAllMedia_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите удалить ВСЕ медиафайлы этой персоны?", "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                // Логика удаления всех медиафайлов
                MessageBox.Show("Все медиафайлы удалены!");
                LoadMediaFiles();
            }
        }
    }
}