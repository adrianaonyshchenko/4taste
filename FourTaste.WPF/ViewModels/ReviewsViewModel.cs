using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Models;

namespace FourTaste.WPF.ViewModels
{
    public sealed class ReviewsViewModel : INotifyPropertyChanged
    {
        private readonly IReviewService _reviewService;
        private readonly int _recipeId;
        private readonly int? _viewerId;
        private readonly bool _isAdminMode;

        private string _recipeTitle = string.Empty;
        private bool _isBusy;

        public ReviewsViewModel(IReviewService reviewService, int recipeId, string recipeTitle, int? viewerId, bool isAdminMode)
        {
            _reviewService = reviewService ?? throw new ArgumentNullException(nameof(reviewService));
            _recipeId = recipeId;
            _recipeTitle = $"Відгуки: {recipeTitle}";
            _viewerId = viewerId;
            _isAdminMode = isAdminMode;

            DeleteReviewByAdminCommand = new ParameterizedRelayCommand<ReviewCardViewModel>(
                 execute: card => _ = DeleteByAdminAsync(card),
                 canExecute: card => card is not null && !_isBusy && _isAdminMode);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<ReviewCardViewModel> Reviews { get; } = new ();

        public ObservableCollection<KeyValuePair<string, int>> DnaTags { get; } = new ();

        public ParameterizedRelayCommand<ReviewCardViewModel> DeleteReviewByAdminCommand { get; }

        public string WindowTitle
        {
            get => _recipeTitle;
            set
            {
                if (_recipeTitle == value)
                {
                    return;
                }

                _recipeTitle = value;
                OnPropertyChanged();
            }
        }

        public bool IsEmpty => Reviews.Count == 0;

        public bool IsDnaEmpty => DnaTags.Count == 0;

        public bool IsAdminMode => _isAdminMode;

        public void SetReviews(IEnumerable<RecipeReview> reviews)
        {
            Reviews.Clear();
            foreach (var review in reviews)
            {
                Reviews.Add(new ReviewCardViewModel(review, _viewerId, _isAdminMode));
            }

            RecalculateDnaTags();
            OnPropertyChanged(nameof(IsEmpty));
        }

        public void RecalculateDnaTags()
        {
            DnaTags.Clear();
            var counts = Reviews
                .SelectMany(r => r.Tags)
                .GroupBy(t => t)
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key);

            foreach (var group in counts)
            {
                DnaTags.Add(new KeyValuePair<string, int>(group.Key, group.Count()));
            }

            OnPropertyChanged(nameof(IsDnaEmpty));
        }

        private async Task DeleteByAdminAsync(ReviewCardViewModel? card)
        {
            if (card is null || _isBusy)
            {
                return;
            }

            var confirm = MessageBox.Show(
                $"Видалити відгук користувача «{card.AuthorName}»?",
                "Модерація відгуків",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            _isBusy = true;
            DeleteReviewByAdminCommand.RaiseCanExecuteChanged();

            try
            {
                var result = await _reviewService.DeleteReviewByAdminAsync(card.Review.Id);
                if (result.IsFailure)
                {
                    MessageBox.Show(result.Error, "Помилка видалення", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Reviews.Remove(card);
                RecalculateDnaTags();
                OnPropertyChanged(nameof(IsEmpty));
            }
            finally
            {
                _isBusy = false;
                DeleteReviewByAdminCommand.RaiseCanExecuteChanged();
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}