using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using FourTaste.ApplicationCore.Entities;

namespace FourTaste.WPF
{
    public class ReviewCardViewModel : INotifyPropertyChanged
    {
        private const int MaxStars = 5;

        private BitmapImage? avatar;

        public ReviewCardViewModel(RecipeReview review, int? viewerUserId)
        {
            int rating = Math.Clamp(review.Rating, 0, MaxStars);

            Review = review;
            IsOwn = viewerUserId.HasValue && review.UserId == viewerUserId.Value;
            AuthorName = review.AuthorName;
            Initial = GetInitial(review.AuthorName);
            FilledStars = new string('★', rating);
            EmptyStars = new string('★', MaxStars - rating);
            Text = review.Text;
            HasText = !string.IsNullOrWhiteSpace(review.Text);
            Photo = ReviewPhotoHelper.ToBitmapImage(review.Photo);
            HasPhoto = Photo is not null;
            DateText = review.CreatedAt.ToLocalTime().ToString("d MMMM yyyy");
            Tags = review.Tags.Select(t => t.Name).ToList();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public RecipeReview Review { get; }
        public bool IsOwn { get; }
        public string AuthorName { get; }
        public string Initial { get; }
        public string FilledStars { get; }
        public string EmptyStars { get; }
        public string Text { get; }
        public bool HasText { get; }
        public BitmapImage? Photo { get; }
        public bool HasPhoto { get; }
        public string DateText { get; }
        public IReadOnlyList<string> Tags { get; }
        public BitmapImage? Avatar
        {
            get => this.avatar;
            set
            {
                if (ReferenceEquals(this.avatar, value))
                {
                    return;
                }

                this.avatar = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Avatar)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.InitialVisibility)));
            }
        }

        public Visibility InitialVisibility => this.avatar is null ? Visibility.Visible : Visibility.Collapsed;

        public static string GetInitial(string? name)
        {
            return string.IsNullOrWhiteSpace(name)
                ? "?"
                : name.Trim()[0].ToString().ToUpperInvariant();
        }
    }
}