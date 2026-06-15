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
using System.Diagnostics;

namespace Lineage.Pages
{
    public partial class PersonProfilePage : Page
    {
        private int personId;
        private int currentTreeId;
        private bool canEdit; // Может ли пользователь редактировать
        private bool isAdmin; // Является ли администратором
        private List<StoryItem> stories = new List<StoryItem>();

        public class StoryItem
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public string Content { get; set; }
            public DateTime? EventDate { get; set; }
            public string EventDateString { get; set; }
            public string ShortContent { get; set; }
            public Visibility EditButtonVisibility { get; set; }  // Видимость кнопки редактирования
            public Visibility DeleteButtonVisibility { get; set; } // Видимость кнопки удаления
        }

        public class PhotoItem
        {
            public int Id { get; set; }
            public string FilePath { get; set; }
            public string ThumbPath { get; set; }
            public string FileName { get; set; }
            public Visibility EditButtonVisibility { get; set; }
            public Visibility DeleteButtonVisibility { get; set; }
        }

        public class VideoItem
        {
            public int Id { get; set; }
            public string FileName { get; set; }
            public string FilePath { get; set; }
            public Visibility EditButtonVisibility { get; set; }
            public Visibility DeleteButtonVisibility { get; set; }
        }

        public class AudioItem
        {
            public int Id { get; set; }
            public string FileName { get; set; }
            public string FilePath { get; set; }
            public Visibility EditButtonVisibility { get; set; }
            public Visibility DeleteButtonVisibility { get; set; }
        }

        public PersonProfilePage(int id)
        {
            InitializeComponent();
            personId = id;
            this.Loaded += Page_Loaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (!Session.IsFamilyMode)
            {
                MessageBox.Show("Эта страница доступна только в режиме семейного древа!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NavigationService.GoBack();
                return;
            }

            // Определяем права пользователя
            isAdmin = Session.IsAdmin;
            canEdit = Session.IsAdmin || Session.IsEditor;

            // Получаем TreeId персоны
            using (var context = new GenealogyUnifiedDBEntities2())
            {
                var person = context.Persons.Find(personId);
                if (person != null)
                {
                    currentTreeId = person.TreeId;
                    var tree = context.FamilyTrees.Find(currentTreeId);
                    // Право на редактирование: админ ИЛИ создатель дерева
                    canEdit = Session.IsAdmin || (tree != null && tree.CreatedByUserId == Session.UserId);
                }
            }

            NavigationService.Navigated += NavigationService_Navigated;

            LoadAllData();

            // Показываем кнопки добавления только если есть права на редактирование
            bool canAdd = canEdit;

            btnEdit.Visibility = canAdd ? Visibility.Visible : Visibility.Collapsed;
            btnAddStory.Visibility = canAdd ? Visibility.Visible : Visibility.Collapsed;
            btnAddStoryBottom.Visibility = canAdd ? Visibility.Visible : Visibility.Collapsed;
            btnAddPhoto.Visibility = canAdd ? Visibility.Visible : Visibility.Collapsed;
            btnAddMedia.Visibility = canAdd ? Visibility.Visible : Visibility.Collapsed;

            // Кнопки "Удалить все" только для администратора
            btnDeleteAllStories.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            btnDeleteAllMedia.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
        }

        private void NavigationService_Navigated(object sender, NavigationEventArgs e)
        {
            if (e.Content == this)
            {
                LoadAllData();
            }
        }

        private void LoadAllData()
        {
            ClearAllTextBlocks();
            LoadPersonData();
            LoadStories();
            LoadMediaFiles();
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
            txtProfession.Text = "";
        }

        private void LoadPersonData()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities2())
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

                    txtProfession.Text = string.IsNullOrEmpty(person.Profession) ? "Профессия не указана" : person.Profession;

                    var gender = context.Genders.FirstOrDefault(g => g.Id == person.GenderId);
                    if (gender != null)
                    {
                        txtGender.Text = gender.Name;
                        txtGenderSymbol.Text = gender.Symbol ?? "👤";
                    }

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
                    else
                    {
                        imgProfile.Source = null;
                        imgProfile.Visibility = Visibility.Collapsed;
                        txtNoProfilePhoto.Visibility = Visibility.Visible;
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
                                txtFather.Text = $"Отец: {fatherName}";
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
                                txtMother.Text = $"Мать: {motherName}";
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
                using (var context = new GenealogyUnifiedDBEntities2())
                {
                    var storyList = context.Stories
                        .Where(s => s.PersonId == personId)
                        .OrderByDescending(s => s.EventDate ?? DateTime.MinValue)
                        .ToList();

                    // Определяем видимость кнопок для историй
                    Visibility editDeleteVisibility = canEdit ? Visibility.Visible : Visibility.Collapsed;

                    stories = storyList.Select(s => new StoryItem
                    {
                        Id = s.Id,
                        Title = s.Title,
                        Content = s.Content,
                        EventDate = s.EventDate,
                        EventDateString = s.EventDate?.ToString("dd.MM.yyyy") ?? s.EventDateText ?? "Дата не указана",
                        ShortContent = s.Content.Length > 100 ? s.Content.Substring(0, 100) + "..." : s.Content,
                        EditButtonVisibility = editDeleteVisibility,
                        DeleteButtonVisibility = editDeleteVisibility
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
                using (var context = new GenealogyUnifiedDBEntities2())
                {
                    var stories = context.Stories.Where(s => s.PersonId == personId).Select(s => s.Id).ToList();
                    var mediaLinks = context.MediaLinks.Where(ml => ml.StoryId.HasValue && stories.Contains(ml.StoryId.Value))
                        .Select(ml => ml.MediaFileId).ToList();
                    var mediaFiles = context.MediaFiles.Where(mf => mediaLinks.Contains(mf.Id)).ToList();

                    var photos = new List<PhotoItem>();
                    var videos = new List<VideoItem>();
                    var audios = new List<AudioItem>();

                    // Определяем видимость кнопок для медиафайлов
                    Visibility editDeleteVisibility = canEdit ? Visibility.Visible : Visibility.Collapsed;

                    foreach (var file in mediaFiles)
                    {
                        var mediaType = context.MediaTypes.FirstOrDefault(mt => mt.Id == file.MediaTypeId);
                        string typeName = mediaType?.Name ?? "";

                        if (typeName.Contains("Изображение") || typeName.Contains("Image") || typeName.Contains("Фото"))
                        {
                            photos.Add(new PhotoItem
                            {
                                Id = file.Id,
                                FilePath = file.FilePath,
                                FileName = file.FileName,
                                ThumbPath = PhotoHelper.GetProfilePhoto(file.FilePath),
                                EditButtonVisibility = editDeleteVisibility,
                                DeleteButtonVisibility = editDeleteVisibility
                            });
                        }
                        else if (typeName.Contains("Видео") || typeName.Contains("Video"))
                        {
                            videos.Add(new VideoItem
                            {
                                Id = file.Id,
                                FileName = file.FileName,
                                FilePath = file.FilePath,
                                EditButtonVisibility = editDeleteVisibility,
                                DeleteButtonVisibility = editDeleteVisibility
                            });
                        }
                        else if (typeName.Contains("Аудио") || typeName.Contains("Audio"))
                        {
                            audios.Add(new AudioItem
                            {
                                Id = file.Id,
                                FileName = file.FileName,
                                FilePath = file.FilePath,
                                EditButtonVisibility = editDeleteVisibility,
                                DeleteButtonVisibility = editDeleteVisibility
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

        private void Photo_Loaded(object sender, RoutedEventArgs e)
        {
            var image = sender as Image;
            if (image?.Tag != null)
            {
                int photoId = (int)image.Tag;
                var photos = icPhotos.ItemsSource as List<PhotoItem>;
                var photo = photos?.FirstOrDefault(p => p.Id == photoId);
                if (photo != null)
                {
                    string foundPath = FindFile(photo.FilePath, photo.FileName);
                    if (File.Exists(foundPath))
                    {
                        try
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.UriSource = new Uri(foundPath, UriKind.Absolute);
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();
                            image.Source = bitmap;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Ошибка загрузки изображения: {ex.Message}");
                            image.Source = null;
                        }
                    }
                    else
                    {
                        image.Source = null;
                    }
                }
            }
        }

        private void UpdateTabHeaders(int photoCount, int videoCount, int audioCount)
        {
            tabPhotos.Header = $"📷 Фотографии ({photoCount})";
            tabVideos.Header = $"🎥 Видео ({videoCount})";
            tabAudios.Header = $"🎵 Аудио ({audioCount})";
        }

        private string FindFile(string storedPath, string fileName)
        {
            string fileNameOnly = System.IO.Path.GetFileName(storedPath);
            if (string.IsNullOrEmpty(fileNameOnly)) fileNameOnly = fileName;

            var possiblePaths = new List<string>();
            string currentDir = AppDomain.CurrentDomain.BaseDirectory;

            possiblePaths.Add(storedPath);
            possiblePaths.Add(System.IO.Path.Combine(currentDir, fileNameOnly));
            possiblePaths.Add(System.IO.Path.Combine(currentDir, "Media", fileNameOnly));
            possiblePaths.Add(System.IO.Path.Combine(currentDir, "Media", fileName));
            possiblePaths.Add(System.IO.Path.Combine(currentDir, "Photos", fileNameOnly));
            possiblePaths.Add(System.IO.Path.Combine(currentDir, "Photos", fileName));
            possiblePaths.Add(System.IO.Path.Combine(currentDir, "Images", fileNameOnly));
            possiblePaths.Add(System.IO.Path.Combine(currentDir, "Images", fileName));

            string projectDir = System.IO.Path.GetDirectoryName(currentDir);
            if (!string.IsNullOrEmpty(projectDir))
            {
                possiblePaths.Add(System.IO.Path.Combine(projectDir, "Media", fileNameOnly));
                possiblePaths.Add(System.IO.Path.Combine(projectDir, "Media", fileName));
                possiblePaths.Add(System.IO.Path.Combine(projectDir, "Photos", fileNameOnly));
                possiblePaths.Add(System.IO.Path.Combine(projectDir, "Photos", fileName));
            }

            for (int i = 0; i < 5; i++)
            {
                string rootDir = currentDir;
                for (int j = 0; j < i; j++)
                    rootDir = System.IO.Path.GetDirectoryName(rootDir);

                if (!string.IsNullOrEmpty(rootDir))
                {
                    possiblePaths.Add(System.IO.Path.Combine(rootDir, "Media", fileNameOnly));
                    possiblePaths.Add(System.IO.Path.Combine(rootDir, "Media", fileName));
                }
            }

            foreach (string path in possiblePaths.Distinct())
            {
                try
                {
                    if (!string.IsNullOrEmpty(path))
                    {
                        string normalizedPath = System.IO.Path.GetFullPath(path);
                        if (File.Exists(normalizedPath))
                            return normalizedPath;
                    }
                }
                catch { }
            }

            return storedPath;
        }

        private void OpenFileWithDefaultProgram(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    MessageBox.Show("Путь к файлу не указан", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (File.Exists(filePath))
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    };
                    Process.Start(startInfo);
                }
                else
                {
                    MessageBox.Show($"Файл не найден по пути:\n{filePath}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть файл:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ============================================
        // МЕТОДЫ ДЛЯ ОТКРЫТИЯ МЕДИАФАЙЛОВ
        // ============================================

        private void Photo_Click(object sender, MouseButtonEventArgs e)
        {
            var image = sender as Image;
            if (image?.Tag != null)
            {
                int photoId = (int)image.Tag;
                var photos = icPhotos.ItemsSource as List<PhotoItem>;
                var photo = photos?.FirstOrDefault(p => p.Id == photoId);
                if (photo != null)
                {
                    string fullPath = FindFile(photo.FilePath, photo.FileName);
                    OpenFileWithDefaultProgram(fullPath);
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
                if (video != null)
                {
                    string fullPath = FindFile(video.FilePath, video.FileName);
                    OpenFileWithDefaultProgram(fullPath);
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
                if (audio != null)
                {
                    string fullPath = FindFile(audio.FilePath, audio.FileName);
                    OpenFileWithDefaultProgram(fullPath);
                }
            }
        }

        // ============================================
        // РЕДАКТИРОВАНИЕ И УДАЛЕНИЕ ИСТОРИЙ
        // ============================================

        private void EditStory_Click(object sender, RoutedEventArgs e)
        {
            if (!canEdit)
            {
                MessageBox.Show("У вас нет прав на редактирование этой истории!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var button = sender as Button;
            if (button?.Tag == null) return;
            int storyId = (int)button.Tag;
            NavigationService.Navigate(new EditStoryPage(personId, storyId));
        }

        private async void DeleteStory_Click(object sender, RoutedEventArgs e)
        {
            if (!canEdit)
            {
                MessageBox.Show("У вас нет прав на удаление этой истории!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var button = sender as Button;
            if (button?.Tag == null) return;
            int storyId = (int)button.Tag;

            var result = MessageBox.Show("Вы уверены, что хотите удалить эту историю?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new GenealogyUnifiedDBEntities2())
                    {
                        var story = context.Stories.Find(storyId);
                        if (story != null)
                        {
                            context.Stories.Remove(story);
                            await context.SaveChangesAsync();
                            LoadStories();
                            MessageBox.Show("История удалена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ============================================
        // РЕДАКТИРОВАНИЕ МЕДИАФАЙЛОВ (открытие страницы редактирования истории)
        // ============================================

        private void EditPhoto_Click(object sender, RoutedEventArgs e)
        {
            if (!canEdit)
            {
                MessageBox.Show("У вас нет прав на редактирование этого файла!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var button = sender as Button;
            if (button?.Tag == null) return;
            int mediaFileId = (int)button.Tag;

            int storyId = GetStoryIdByMediaFileId(mediaFileId);
            if (storyId > 0)
            {
                NavigationService.Navigate(new EditStoryPage(personId, storyId));
            }
            else
            {
                MessageBox.Show("Не удалось найти историю, связанную с этим файлом.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void EditVideo_Click(object sender, RoutedEventArgs e)
        {
            if (!canEdit)
            {
                MessageBox.Show("У вас нет прав на редактирование этого файла!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var button = sender as Button;
            if (button?.Tag == null) return;
            int mediaFileId = (int)button.Tag;

            int storyId = GetStoryIdByMediaFileId(mediaFileId);
            if (storyId > 0)
            {
                NavigationService.Navigate(new EditStoryPage(personId, storyId));
            }
            else
            {
                MessageBox.Show("Не удалось найти историю, связанную с этим файлом.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void EditAudio_Click(object sender, RoutedEventArgs e)
        {
            if (!canEdit)
            {
                MessageBox.Show("У вас нет прав на редактирование этого файла!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var button = sender as Button;
            if (button?.Tag == null) return;
            int mediaFileId = (int)button.Tag;

            int storyId = GetStoryIdByMediaFileId(mediaFileId);
            if (storyId > 0)
            {
                NavigationService.Navigate(new EditStoryPage(personId, storyId));
            }
            else
            {
                MessageBox.Show("Не удалось найти историю, связанную с этим файлом.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ============================================
        // УДАЛЕНИЕ МЕДИАФАЙЛОВ
        // ============================================

        private async void DeletePhoto_Click(object sender, RoutedEventArgs e)
        {
            if (!canEdit)
            {
                MessageBox.Show("У вас нет прав на удаление этого файла!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var button = sender as Button;
            if (button?.Tag == null) return;
            int mediaFileId = (int)button.Tag;

            var result = MessageBox.Show("Вы уверены, что хотите удалить это фото?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                await DeleteMediaFile(mediaFileId);
                LoadMediaFiles();
            }
        }

        private async void DeleteVideo_Click(object sender, RoutedEventArgs e)
        {
            if (!canEdit)
            {
                MessageBox.Show("У вас нет прав на удаление этого файла!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var button = sender as Button;
            if (button?.Tag == null) return;
            int mediaFileId = (int)button.Tag;

            var result = MessageBox.Show("Вы уверены, что хотите удалить это видео?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                await DeleteMediaFile(mediaFileId);
                LoadMediaFiles();
            }
        }

        private async void DeleteAudio_Click(object sender, RoutedEventArgs e)
        {
            if (!canEdit)
            {
                MessageBox.Show("У вас нет прав на удаление этого файла!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var button = sender as Button;
            if (button?.Tag == null) return;
            int mediaFileId = (int)button.Tag;

            var result = MessageBox.Show("Вы уверены, что хотите удалить этот аудиофайл?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                await DeleteMediaFile(mediaFileId);
                LoadMediaFiles();
            }
        }

        // ============================================
        // ВСПОМОГАТЕЛЬНЫЙ МЕТОД ДЛЯ ПОЛУЧЕНИЯ ID ИСТОРИИ ПО ID МЕДИАФАЙЛА
        // ============================================

        private int GetStoryIdByMediaFileId(int mediaFileId)
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities2())
                {
                    var mediaLink = context.MediaLinks
                        .FirstOrDefault(ml => ml.MediaFileId == mediaFileId && ml.StoryId.HasValue);

                    if (mediaLink != null && mediaLink.StoryId.HasValue)
                    {
                        return mediaLink.StoryId.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка получения истории: {ex.Message}");
            }
            return 0;
        }

        private async Task DeleteMediaFile(int mediaFileId)
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities2())
                {
                    var mediaLinks = context.MediaLinks.Where(ml => ml.MediaFileId == mediaFileId).ToList();
                    context.MediaLinks.RemoveRange(mediaLinks);

                    var mediaFile = context.MediaFiles.Find(mediaFileId);
                    if (mediaFile != null)
                    {
                        string fullPath = PhotoHelper.GetProfilePhoto(mediaFile.FilePath);
                        if (File.Exists(fullPath))
                        {
                            try { File.Delete(fullPath); } catch { }
                        }

                        context.MediaFiles.Remove(mediaFile);
                    }

                    await context.SaveChangesAsync();
                    MessageBox.Show("Медиафайл удалён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ============================================
        // ОСТАЛЬНЫЕ МЕТОДЫ
        // ============================================

        private void ReadStory_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag == null) return;

            int storyId = (int)button.Tag;

            using (var context = new GenealogyUnifiedDBEntities2())
            {
                var story = context.Stories.FirstOrDefault(s => s.Id == storyId);
                if (story != null)
                {
                    NavigationService.Navigate(new StoryDetailPage(storyId, story.PersonId, txtFullName.Text));
                }
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (!canEdit)
            {
                MessageBox.Show("У вас нет прав на редактирование этой персоны!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            NavigationService.Navigate(new EditPersonPage(personId));
        }

        private void AddStoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (!canEdit)
            {
                MessageBox.Show("У вас нет прав на добавление историй!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            NavigationService.Navigate(new EditStoryPage(personId));
        }

        private void AddPhotoButton_Click(object sender, RoutedEventArgs e)
        {
            if (!canEdit)
            {
                MessageBox.Show("У вас нет прав на добавление фото!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            NavigationService.Navigate(new EditPersonPage(personId));
        }

        private void AddMediaButton_Click(object sender, RoutedEventArgs e)
        {
            if (!canEdit)
            {
                MessageBox.Show("У вас нет прав на добавление медиафайлов!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            NavigationService.Navigate(new EditStoryPage(personId));
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new MainPage());
        }

        private async void DeleteAllStories_Click(object sender, RoutedEventArgs e)
        {
            if (!isAdmin)
            {
                MessageBox.Show("Только администратор может удалить все истории!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show("Вы уверены, что хотите удалить ВСЕ истории этой персоны?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                using (var context = new GenealogyUnifiedDBEntities2())
                {
                    var storiesToDelete = context.Stories.Where(s => s.PersonId == personId).ToList();
                    context.Stories.RemoveRange(storiesToDelete);
                    await context.SaveChangesAsync();
                }
                MessageBox.Show("Все истории удалены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadAllData();
            }
        }

        private async void DeleteAllMedia_Click(object sender, RoutedEventArgs e)
        {
            if (!isAdmin)
            {
                MessageBox.Show("Только администратор может удалить все медиафайлы!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show("Вы уверены, что хотите удалить ВСЕ медиафайлы этой персоны?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                using (var context = new GenealogyUnifiedDBEntities2())
                {
                    var stories = context.Stories.Where(s => s.PersonId == personId).Select(s => s.Id).ToList();
                    var mediaLinks = context.MediaLinks.Where(ml => ml.StoryId.HasValue && stories.Contains(ml.StoryId.Value)).ToList();
                    var mediaFileIds = mediaLinks.Select(ml => ml.MediaFileId).ToList();
                    var mediaFiles = context.MediaFiles.Where(mf => mediaFileIds.Contains(mf.Id)).ToList();

                    foreach (var file in mediaFiles)
                    {
                        string fullPath = PhotoHelper.GetProfilePhoto(file.FilePath);
                        if (File.Exists(fullPath))
                        {
                            try { File.Delete(fullPath); } catch { }
                        }
                    }

                    context.MediaLinks.RemoveRange(mediaLinks);
                    context.MediaFiles.RemoveRange(mediaFiles);
                    await context.SaveChangesAsync();
                }
                MessageBox.Show("Все медиафайлы удалены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadAllData();
            }
        }
    }
}