using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Models;
using FourTaste.ApplicationCore.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace FourTaste.WPF
{
    public partial class ReviewsWindow : Window
    {
        private readonly IReviewService _reviewService;
        private readonly int _recipeId;

        private ReviewsOverview? _overview;
        private RecipeReview? _editingReview;
        private byte[]? _photo;
        private int _selectedRating;

        public ReviewsWindow(IReviewService reviewService, int recipeId, string recipeTitle)
        {
            InitializeComponent();
            _reviewService = reviewService ?? throw new ArgumentNullException(nameof(reviewService));
            _recipeId = recipeId;

            TitleTextBlock.Text = $"Відгуки: {recipeTitle}";
            ReviewTextBox.MaxLength = ReviewOptions.MaxTextLength;

            var currentUser = SessionContext.CurrentUser;
            FormAuthorTextBlock.Text = currentUser?.Username ?? string.Empty;
            FormInitialTextBlock.Text = ReviewCardViewModel.GetInitial(currentUser?.Username);

            BuildTagChips();
            UpdateStars();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var result = await _reviewService.GetOverviewAsync(_recipeId, SessionContext.CurrentUser?.ID);

            if (result.IsFailure)
            {
                MessageBox.Show(result.Error, "Не вдалося відкрити відгуки", MessageBoxButton.OK, MessageBoxImage.Warning);
                Close();
                return;
            }

            _overview = result.Value!;
            Render();
        }

        private void StarButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: string tag })
            {
                _selectedRating = Convert.ToInt32(tag, CultureInfo.InvariantCulture);
                UpdateStars();
            }
        }

        private void ToggleTagsButton_Click(object sender, RoutedEventArgs e)
        {
            TagChipsPanel.Visibility = TagChipsPanel.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void AddPhotoButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Зображення (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png",
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                _photo = ReviewPhotoHelper.CompressToJpeg(dialog.FileName);
            }
            catch (Exception ex) when (ex is NotSupportedException or FileFormatException or IOException or UnauthorizedAccessException)
            {
                Log.Warning(ex, "Не вдалося прочитати зображення для відгуку: {FileName}", dialog.FileName);
                MessageBox.Show("Не вдалося відкрити це зображення. Спробуйте інший файл.", "Фото", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ShowPhotoPreview();
        }

        private void RemovePhotoButton_Click(object sender, RoutedEventArgs e)
        {
            _photo = null;
            ShowPhotoPreview();
        }

        private void EditReviewButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: ReviewCardViewModel card })
            {
                _editingReview = card.Review;
                FillForm(card.Review);
                PublishButton.Content = "Зберегти зміни";
                CancelEditButton.Visibility = Visibility.Visible;
                UpdateFormState(SessionContext.CurrentUser?.ID);
            }
        }

        private void CancelEditButton_Click(object sender, RoutedEventArgs e)
        {
            ResetForm();
            UpdateFormState(SessionContext.CurrentUser?.ID);
        }

        private async void DeleteReviewButton_Click(object sender, RoutedEventArgs e)
        {
            var currentUser = SessionContext.CurrentUser;
            if (currentUser is null || _overview is null)
            {
                return;
            }

            var confirm = MessageBox.Show("Видалити ваш відгук?", "Підтвердження", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            if (sender is Button button)
            {
                button.IsEnabled = false;
            }

            var result = await _reviewService.DeleteReviewAsync(_recipeId, currentUser.ID);

            if (result.IsFailure)
            {
                if (sender is Button failedButton)
                {
                    failedButton.IsEnabled = true;
                }

                MessageBox.Show(result.Error, "Не вдалося видалити відгук", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _overview.Reviews.RemoveAll(r => r.UserId == currentUser.ID);
            ResetForm();
            Render();
        }

        private async void PublishButton_Click(object sender, RoutedEventArgs e)
        {
            var currentUser = SessionContext.CurrentUser;
            if (currentUser is null || _overview is null)
            {
                MessageBox.Show("Увійдіть в акаунт, щоб залишити відгук.", "Відгуки", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            PublishButton.IsEnabled = false;

            try
            {
                var tags = GetSelectedTags();

                var request = new ReviewRequest
                {
                    RecipeId = _recipeId,
                    UserId = currentUser.ID,
                    Rating = _selectedRating,
                    Text = ReviewTextBox.Text,
                    Tags = tags,
                    Photo = _photo,
                };

                var review = new RecipeReview
                {
                    RecipeId = _recipeId,
                    UserId = currentUser.ID,
                    Rating = _selectedRating,
                    Text = ReviewTextBox.Text.Trim(),
                    Photo = _photo,
                    CreatedAt = DateTime.UtcNow,
                    AuthorName = currentUser.Username,
                    Tags = tags.Select(t => new ReviewTag { Name = t }).ToList(),
                };

                if (_editingReview is null)
                {
                    var addResult = await _reviewService.AddReviewAsync(request);
                    if (addResult.IsFailure)
                    {
                        MessageBox.Show(addResult.Error, "Не вдалося опублікувати відгук", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    review.Id = addResult.Value;
                    _overview.Reviews.Insert(0, review);
                }
                else
                {
                    var updateResult = await _reviewService.UpdateReviewAsync(request);
                    if (updateResult.IsFailure)
                    {
                        MessageBox.Show(updateResult.Error, "Не вдалося зберегти зміни", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    review.Id = _editingReview.Id;
                    review.CreatedAt = _editingReview.CreatedAt;

                    int index = _overview.Reviews.FindIndex(r => r.UserId == currentUser.ID);
                    if (index >= 0)
                    {
                        _overview.Reviews[index] = review;
                    }
                }

                ResetForm();
                Render();
            }
            finally
            {
                PublishButton.IsEnabled = true;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Render()
        {
            if (_overview is null)
            {
                return;
            }

            int? viewerId = SessionContext.CurrentUser?.ID;

            var cards = _overview.Reviews
                .Select(r => new ReviewCardViewModel(r, viewerId))
                .ToList();

            ReviewsItemsControl.ItemsSource = cards;
            EmptyTextBlock.Visibility = cards.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            _ = LoadAvatarsAsync(cards);

            var counts = _overview.TagCounts;
            DnaItemsControl.ItemsSource = counts;
            DnaEmptyTextBlock.Visibility = counts.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            UpdateFormState(viewerId);
        }

        private async Task LoadAvatarsAsync(IReadOnlyList<ReviewCardViewModel> cards)
        {
            try
            {
                int? viewerId = SessionContext.CurrentUser?.ID;

                var missing = cards
                    .Select(c => c.Review.UserId)
                    .Distinct()
                    .Where(id => id != viewerId && !ReviewAvatarCache.TryGet(id, out _))
                    .ToList();

                if (missing.Count > 0)
                {
                    var result = await _reviewService.GetAuthorAvatarsAsync(missing);
                    if (result.IsFailure)
                    {
                        Log.Warning("Не вдалося завантажити аватарки авторів відгуків: {Error}", result.Error);
                        return;
                    }

                    var bytesById = result.Value!;
                    var decoded = await Task.Run(() => missing.ToDictionary(
                        id => id,
                        id => bytesById.TryGetValue(id, out var bytes) ? ReviewPhotoHelper.ToAvatarImage(bytes) : null));

                    foreach (var pair in decoded)
                    {
                        ReviewAvatarCache.Set(pair.Key, pair.Value);
                    }
                }

                if (!IsLoaded)
                {
                    return;
                }

                foreach (var card in cards)
                {
                    if (viewerId.HasValue && card.Review.UserId == viewerId.Value)
                    {
                        card.Avatar = ReviewPhotoHelper.ToAvatarImage(SessionContext.CurrentUser?.Avatar);
                    }
                    else if (ReviewAvatarCache.TryGet(card.Review.UserId, out var avatar))
                    {
                        card.Avatar = avatar;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Помилка завантаження аватарок у відгуках");
            }
        }
        private void UpdateFormState(int? viewerId)
        {
            if (_overview is null)
            {
                return;
            }

            if (viewerId is null)
            {
                ShowInfo("Увійдіть в акаунт, щоб залишити відгук.");
            }
            else if (_overview.IsRecipeOwner)
            {
                ShowInfo("Це ваш рецепт: відгуки на нього можуть залишати лише інші користувачі.");
            }
            else if (_editingReview is null && _overview.Reviews.Any(r => r.UserId == viewerId))
            {
                ShowInfo("Ваш відгук опубліковано. Його можна змінити або видалити в списку.");
            }
            else
            {
                InfoTextBlock.Visibility = Visibility.Collapsed;
                WriteReviewCard.Visibility = Visibility.Visible;
            }
        }

        private void ShowInfo(string message)
        {
            InfoTextBlock.Text = message;
            InfoTextBlock.Visibility = Visibility.Visible;
            WriteReviewCard.Visibility = Visibility.Collapsed;
        }

        private void BuildTagChips()
        {
            var style = (Style)FindResource("TagChipStyle");

            foreach (string tag in _reviewService.GetAvailableTags())
            {
                TagChipsPanel.Children.Add(new ToggleButton
                {
                    Content = tag,
                    Style = style,
                    Margin = new Thickness(0, 0, 6, 6),
                });
            }
        }

        private System.Collections.Generic.List<string> GetSelectedTags()
        {
            return TagChipsPanel.Children
                .OfType<ToggleButton>()
                .Where(t => t.IsChecked == true)
                .Select(t => (string)t.Content)
                .ToList();
        }

        private void FillForm(RecipeReview review)
        {
            _selectedRating = review.Rating;
            UpdateStars();
            ReviewTextBox.Text = review.Text;
            _photo = review.Photo;
            ShowPhotoPreview();

            var names = review.Tags.Select(t => t.Name).ToHashSet();
            foreach (var chip in TagChipsPanel.Children.OfType<ToggleButton>())
            {
                chip.IsChecked = names.Contains((string)chip.Content);
            }

            TagChipsPanel.Visibility = names.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ResetForm()
        {
            _editingReview = null;
            _selectedRating = 0;
            UpdateStars();
            ReviewTextBox.Clear();
            _photo = null;
            ShowPhotoPreview();

            foreach (var chip in TagChipsPanel.Children.OfType<ToggleButton>())
            {
                chip.IsChecked = false;
            }

            TagChipsPanel.Visibility = Visibility.Collapsed;
            PublishButton.Content = "Опублікувати";
            CancelEditButton.Visibility = Visibility.Collapsed;
        }

        private void ShowPhotoPreview()
        {
            var image = ReviewPhotoHelper.ToBitmapImage(_photo);
            PhotoPreviewImage.Source = image;
            PhotoPreviewPanel.Visibility = image is null ? Visibility.Collapsed : Visibility.Visible;
            AddPhotoButton.Content = image is null ? "+ Фото" : "Змінити фото";
        }

        private void UpdateStars()
        {
            var filled = (Brush)FindResource("StarFilledBrush");
            var empty = (Brush)FindResource("StarEmptyBrush");

            foreach (var star in StarsPanel.Children.OfType<Button>())
            {
                int value = Convert.ToInt32(star.Tag, CultureInfo.InvariantCulture);
                star.Foreground = value <= _selectedRating ? filled : empty;
            }
        }
    }
}