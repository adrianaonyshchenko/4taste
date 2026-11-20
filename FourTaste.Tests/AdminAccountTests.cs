using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Models;
using FourTaste.ApplicationCore.Services;
using FourTaste.Infrastructure.Data;
using FourTaste.Infrastructure.Repositories;
using FourTaste.WPF;
using Microsoft.EntityFrameworkCore;
using Moq;
using Serilog;
using Xunit;

namespace FourTaste.Tests
{
    public class AdminServiceTests
    {
        private readonly Mock<IUserRepository> userRepository = new Mock<IUserRepository>();

        private AdminService CreateService(Mock<ICurrentUserService> currentUser) =>
            new AdminService(this.userRepository.Object, currentUser.Object, new Mock<ILogger>().Object);

        [Fact]
        public async Task SearchUsersAsync_ShouldDenyAccess_WhenUserIsNotAdmin()
        {
            var result = await this.CreateService(CurrentUserMock.Authenticated(5)).SearchUsersAsync(null, UserSearchFilter.All);

            Assert.True(result.IsFailure);
            Assert.Equal(AdminService.AccessDeniedError, result.Error);
            this.userRepository.Verify(r => r.SearchUsersAsync(It.IsAny<string?>(), It.IsAny<UserSearchFilter>()), Times.Never);
        }

        [Fact]
        public async Task SearchUsersAsync_ShouldDenyAccess_WhenNotLoggedIn()
        {
            var result = await this.CreateService(CurrentUserMock.Anonymous()).SearchUsersAsync(null, UserSearchFilter.All);

            Assert.True(result.IsFailure);
            this.userRepository.Verify(r => r.SearchUsersAsync(It.IsAny<string?>(), It.IsAny<UserSearchFilter>()), Times.Never);
        }

        [Fact]
        public async Task SearchUsersAsync_ShouldPassQueryAndFilterToRepository_WhenUserIsAdmin()
        {
            var users = new List<UserSummaryDto> { new UserSummaryDto { Id = 1, Username = "sofia" } };
            this.userRepository.Setup(r => r.SearchUsersAsync("sof", UserSearchFilter.Username)).ReturnsAsync(users);

            var result = await this.CreateService(CurrentUserMock.Authenticated(1, isAdmin: true)).SearchUsersAsync("sof", UserSearchFilter.Username);

            Assert.True(result.IsSuccess);
            Assert.Same(users, result.Value);
        }
    }

    public class UserRepositorySearchTests
    {
        private static UserRepository CreateRepository()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var factory = new TestDbContextFactory(options);

            using (var context = factory.CreateDbContext())
            {
                context.Users.AddRange(
                    new User { ID = 1, Name = "Анна Коваль", Username = "anna", Email = "anna@mail.com", Role = UserRole.User, CreatedAt = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new User { ID = 2, Name = "Богдан", Username = "bohdan_chef", Email = "bohdan@gmail.com", Role = UserRole.User, CreatedAt = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new User { ID = 3, Name = "Софія", Username = "admin_sofia", Email = "sofia@4taste.ua", Role = UserRole.Admin, CreatedAt = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new User { ID = 4, Name = "Старий акаунт", Username = "old", Email = "old@mail.com", Role = UserRole.User, CreatedAt = null });
                context.Recipes.AddRange(
                    new Recipe { Id = 1, Title = "Борщ", UserId = 1 },
                    new Recipe { Id = 2, Title = "Вареники", UserId = 1 },
                    new Recipe { Id = 3, Title = "Деруни", UserId = 2 });
                context.SaveChanges();
            }

            return new UserRepository(factory);
        }

        [Fact]
        public async Task SearchUsersAsync_ShouldReturnAllUsers_AdminsFirstThenNewest()
        {
            var users = await CreateRepository().SearchUsersAsync(null, UserSearchFilter.All);

            Assert.Equal(new[] { 3, 2, 1, 4 }, users.ConvertAll(u => u.Id));
            Assert.True(users[0].IsAdmin);
            Assert.False(users[1].IsAdmin);
        }

        [Fact]
        public async Task SearchUsersAsync_ShouldCountRecipesPerUser()
        {
            var users = await CreateRepository().SearchUsersAsync(null, UserSearchFilter.All);

            Assert.Equal(2, users.Find(u => u.Id == 1)!.RecipesCount);
            Assert.Equal(1, users.Find(u => u.Id == 2)!.RecipesCount);
            Assert.Equal(0, users.Find(u => u.Id == 3)!.RecipesCount);
        }

        [Theory]
        [InlineData("коваль", UserSearchFilter.Name, 1)]
        [InlineData("@bohdan", UserSearchFilter.Username, 2)]
        [InlineData("4TASTE", UserSearchFilter.Email, 3)]
        [InlineData("anna", UserSearchFilter.All, 1)]
        public async Task SearchUsersAsync_ShouldFindUserByFilter(string query, UserSearchFilter filter, int expectedId)
        {
            var users = await CreateRepository().SearchUsersAsync(query, filter);

            var user = Assert.Single(users);
            Assert.Equal(expectedId, user.Id);
        }

        [Fact]
        public async Task SearchUsersAsync_ShouldNotMatchOtherFields_WhenFilterIsSpecific()
        {
            var users = await CreateRepository().SearchUsersAsync("gmail", UserSearchFilter.Name);

            Assert.Empty(users);
        }

        [Fact]
        public async Task CountAdminsAsync_ShouldCountOnlyAdmins()
        {
            Assert.Equal(1, await CreateRepository().CountAdminsAsync());
        }

        private sealed class TestDbContextFactory : IDbContextFactory<AppDbContext>
        {
            private readonly DbContextOptions<AppDbContext> options;

            public TestDbContextFactory(DbContextOptions<AppDbContext> options) => this.options = options;

            public AppDbContext CreateDbContext() => new AppDbContext(this.options);
        }
    }

    public class UserServiceRoleTests
    {
        private readonly Mock<IUserRepository> userRepository = new Mock<IUserRepository>();

        private UserService CreateService() => new UserService(this.userRepository.Object, new Mock<ILogger>().Object);

        [Fact]
        public async Task RegisterAsync_ShouldAlwaysCreateRegularUserWithRegistrationDate()
        {
            User? saved = null;
            this.userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
            this.userRepository.Setup(r => r.AddAsync(It.IsAny<User>())).Callback<User>(u => saved = u).Returns(Task.CompletedTask);
            var before = DateTime.UtcNow;

            var result = await this.CreateService().RegisterAsync("new_user", "new@mail.com", "secret", "Нова");

            Assert.True(result.IsSuccess);
            Assert.NotNull(saved);
            Assert.Equal(UserRole.User, saved!.Role);
            Assert.NotNull(saved.CreatedAt);
            Assert.InRange(saved.CreatedAt!.Value, before, DateTime.UtcNow);
        }

        [Fact]
        public async Task DeleteAccountAsync_ShouldRefuse_WhenUserIsTheOnlyAdmin()
        {
            var admin = new User { ID = 3, Role = UserRole.Admin };
            this.userRepository.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(admin);
            this.userRepository.Setup(r => r.CountAdminsAsync()).ReturnsAsync(1);

            var result = await this.CreateService().DeleteAccountAsync(3);

            Assert.True(result.IsFailure);
            Assert.Equal(UserService.LastAdminError, result.Error);
            this.userRepository.Verify(r => r.DeleteAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAccountAsync_ShouldDeleteAdmin_WhenAnotherAdminExists()
        {
            var admin = new User { ID = 3, Role = UserRole.Admin };
            this.userRepository.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(admin);
            this.userRepository.Setup(r => r.CountAdminsAsync()).ReturnsAsync(2);

            var result = await this.CreateService().DeleteAccountAsync(3);

            Assert.True(result.IsSuccess);
            this.userRepository.Verify(r => r.DeleteAsync(admin), Times.Once);
        }

        [Fact]
        public async Task DeleteAccountAsync_ShouldDeleteRegularUser_WithoutCountingAdmins()
        {
            var user = new User { ID = 4, Role = UserRole.User };
            this.userRepository.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(user);

            var result = await this.CreateService().DeleteAccountAsync(4);

            Assert.True(result.IsSuccess);
            this.userRepository.Verify(r => r.DeleteAsync(user), Times.Once);
            this.userRepository.Verify(r => r.CountAdminsAsync(), Times.Never);
        }
    }

    public class CurrentUserServiceRoleTests
    {
        [Theory]
        [InlineData(UserRole.Admin, true)]
        [InlineData(UserRole.User, false)]
        public void IsAdmin_ShouldReflectRoleOfSignedInUser(UserRole role, bool expected)
        {
            var service = new CurrentUserService();
            service.SignIn(new User { ID = 1, Role = role });

            Assert.Equal(expected, service.IsAdmin);
        }

        [Fact]
        public void IsAdmin_ShouldBeFalse_WhenNobodySignedIn()
        {
            Assert.False(new CurrentUserService().IsAdmin);
        }
    }

    public class UserCardViewModelTests
    {
        [Theory]
        [InlineData(0, "рецептів")]
        [InlineData(1, "рецепт")]
        [InlineData(3, "рецепти")]
        [InlineData(5, "рецептів")]
        [InlineData(11, "рецептів")]
        [InlineData(12, "рецептів")]
        [InlineData(21, "рецепт")]
        [InlineData(24, "рецепти")]
        public void RecipeWord_ShouldUseCorrectUkrainianPlural(int count, string expected)
        {
            Assert.Equal(expected, UserCardViewModel.RecipeWord(count));
        }

        [Fact]
        public void Card_ShouldShowDashForUnknownRegistrationDate()
        {
            var card = new UserCardViewModel(new UserSummaryDto { Name = "", Username = "old", CreatedAt = null });

            Assert.Equal("дата реєстрації —", card.RegisteredText);
            Assert.Equal("old", card.DisplayName);
            Assert.Equal("O", card.Initial);
        }
    }
}
