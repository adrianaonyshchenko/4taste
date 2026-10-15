# 📘 Документація реалізації: Проєкт FourTaste (4taste)

Цей документ містить повний опис проєкту **FourTaste**, його архітектури, поставленого завдання (Use Case №4 «Керування обліковим записом» та «Фіксація харчових алергій») і детальний розбір усіх змін у коді по кожному шару.

---

## 1. Загальний опис проєкту

**FourTaste (4taste — «Спільний простір для нових смаків»)** — це десктопний застосунок на платформі **WPF (.NET)** для управління кулінарними рецептами, планування раціону, підбору страв за наявними вдома продуктами з урахуванням профілю харчових алергій, а також ведення спільних списків покупок у віртуальних кімнатах.

### Стек технологій
- **Платформа:** .NET (`net10.0` / `net10.0-windows`)
- **UI-фреймворк:** WPF (Windows Presentation Foundation, XAML + Code-Behind)
- **База даних:** Хмарна реляційна БД **PostgreSQL** на хостингу **Neon.tech** (`neondb`)
- **ORM:** Entity Framework Core (`Microsoft.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL`, версія `8.0.11`)
- **Логування:** Структуроване логування через **Serilog** + веб-панель **Seq** (`http://localhost:5341` та командний тунель `ngrok`)
- **Безпека паролів:** Хешування через `BCrypt.Net-Next`
- **Якість коду:** Аналізатор `StyleCop.Analyzers` із увімкненим правилом `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`
- **Тестування:** Ізольовані модульні тести на базі **xUnit** та **Moq**

---

## 2. Архітектура проєкту (Clean Architecture)

Рішення `FourTaste.slnx` поділене на 4 фізичні проєкти згідно з принципами Чистої архітектури:

```
FourTaste.WPF (UI / Presentation)
   ├── залежить від ──> FourTaste.ApplicationCore
   └── залежить від ──> FourTaste.Infrastructure ──> FourTaste.ApplicationCore

FourTaste.Tests (Unit Tests)
   └── тестує сервіси з ──> FourTaste.ApplicationCore (через Moq)
```

1. **`FourTaste.ApplicationCore` (Ядро бізнес-логіки)**
   - Не має залежностей від бази даних (EF Core) чи інтерфейсу (WPF).
   - Містить:
     - `Entities/` — доменні сутності (`User`, `Allergy`, `Ingredient`);
     - `Interfaces/` — абстракції репозиторіїв та сервісів (`IRepository<T>`, `IUserRepository`, `IAllergyService`, `IAuthService`);
     - `Services/` — бізнес-сервіси з валідацією та логуванням (`AllergyService`, `UserService`, `AuthService`, `SessionContext`).

2. **`FourTaste.Infrastructure` (Інфраструктура та доступ до даних)**
   - Залежить від `FourTaste.ApplicationCore`.
   - Містить:
     - `Data/AppDbContext.cs` — контекст бази даних EF Core (`DbSet<User> Users`, `DbSet<Allergy> Allergies`);
     - `Repositories/` — конкретні реалізації репозиторіїв (`AllergyRepository`, `UserRepository`), які виконують SQL-запити до PostgreSQL через `AppDbContext`;
     - `Migrations/` — файли міграцій EF Core, що фіксують структуру таблиць у БД.

3. **`FourTaste.WPF` (Шар представлення / UI)**
   - Залежить від `ApplicationCore` та `Infrastructure`.
   - Містить:
     - `App.xaml` та `App.xaml.cs` — точка входу, ініціалізація `Serilog` з `appsettings.json` та налаштування **Dependency Injection (DI)** контейнера (`ServiceCollection`);
     - Вікна застосунку: `RegisterWindow`, `LoginWindow`, `ManageAllergiesWindow` (вікно «Профіль користувача»), `MainWindow`;
     - `appsettings.json` — конфігурація рядка підключення до Neon PostgreSQL та адрес серверів Seq.

4. **`FourTaste.Tests` (Модульні тести)**
   - Містить unit-тести на `xUnit` + `Moq`: `AllergyServiceTests`, `RegistrationTests`, `AuthServiceTests`.

---

## 3. Суть виконаного завдання

Згідно з технічною документацією проєкту (`documents/4tasteDocumentaion.pdf`), було реалізовано:
- **Use Case «Фіксація харчових алергій» (Manage Allergies / Налаштувати профіль алергій):**
  - Додавання нового алергену до профілю користувача у БД PostgreSQL;
  - Валідація (назва алергену не може бути порожньою);
  - Перегляд списку збережених алергенів поточного користувача;
  - Видалення обраного алергену зі списку;
  - Структуроване логування кожної дії (успіх, попередження валідації, помилка) у Serilog/Seq.
- **Use Case №4 «Керувати обліковим записом» (Manage Account) та Use Case №3 «Вийти із системи»:**
  - Завантаження даних авторизованого користувача у вікні профілю;
  - Редагування особистих даних (`Ім'я`, `Нікнейм`, `Електронна пошта`) зі збереженням у БД;
  - Зміна пароля з хешуванням через `BCrypt`;
  - Видалення облікового запису користувача («Видалити профіль»);
  - Вихід із системи («Вийти») через `AuthService.LogoutAsync` з очищенням `SessionContext.CurrentUser`.

---

## 4. Детальний розбір створених та змінених файлів

### 4.1. Шар `FourTaste.ApplicationCore`

1. **`Entities/Allergy.cs`**
   - Сутність, що представляє запис про алергію користувача:
     - `public int Id { get; set; }` — первинний ключ запису (автоінкремент у PostgreSQL);
     - `public int UserId { get; set; }` — ID користувача, якому належить алерген;
     - `public string AllergenName { get; set; } = string.Empty;` — назва алергену (напр., «Арахіс», «Лактоза»).

2. **`Interfaces/IAllergyService.cs`**
   - Інтерфейс сервісу для роботи з алергіями:
     - `Task AddAllergyAsync(int userId, string allergenName)` — додати алерген;
     - `Task<IReadOnlyList<Allergy>> GetAllergiesByUserIdAsync(int userId)` — отримати алергени конкретного користувача;
     - `Task<IReadOnlyList<Allergy>> GetAllAllergiesAsync()` — отримати всі алергени;
     - `Task DeleteAllergyAsync(int allergyId)` — видалити алерген за його ID.

3. **`Services/AllergyService.cs`**
   - Реалізація `IAllergyService`.
   - Приймає через конструктор `IRepository<Allergy> allergyRepository` та `ILogger? logger = null` (якщо логер не передано напряму, використовує глобальний `Log.Logger`).
   - **Логіка `AddAllergyAsync`:**
     1. Перевіряє `string.IsNullOrWhiteSpace(allergenName)`. Якщо назва порожня або складається з пробілів — записує лог `logger.Warning("Спроба додати порожній алерген для користувача з ID {UserId}", userId)` і викидає `ArgumentException`.
     2. Створює об'єкт `Allergy` з `UserId = userId` та `AllergenName = allergenName.Trim()`.
     3. Викликає `await allergyRepository.AddAsync(allergy)` у блоці `try/catch`, логуючи успіх через `logger.Information` або помилку через `logger.Error`.
   - **Логіка `GetAllergiesByUserIdAsync`:** отримує список через `allergyRepository.ListAsync()`, фільтрує за `a.UserId == userId` і логує кількість знайдених записів.
   - **Логіка `DeleteAllergyAsync`:** шукає запис через `GetByIdAsync(allergyId)`. Якщо запису немає — логує `Warning` і викидає `InvalidOperationException`; якщо є — видаляє через `DeleteAsync` і логує `Information`.

4. **`Interfaces/IUserRepository.cs` та `Services/UserService.cs`**
   - В `IUserRepository` додано метод `Task UpdateAsync(User entity)`.
   - У `UserService`:
     - У `RegisterAsync(string username, string email, string password, string name = "")` додано збереження поля `Name = name.Trim()`, щоб ім'я, введене при реєстрації, одразу зберігалося в БД.
     - Додано `GetUserByIdAsync(int userId)` для завантаження профілю.
     - Додано `UpdateProfileAsync(int userId, string name, string username, string email)` з перевіркою на порожні поля, перевіркою `@` в email, оновленням сутності `User` у БД та логуванням.
     - Додано `ChangePasswordAsync(int userId, string newPassword)` з валідацією, хешуванням нового пароля через `BCrypt.Net.BCrypt.HashPassword` та збереженням у БД.
     - Додано `DeleteAccountAsync(int userId)` для видалення користувача з БД.

---

### 4.2. Шар `FourTaste.Infrastructure`

1. **`Data/AppDbContext.cs`**
   - Додано властивість `public DbSet<Allergy> Allergies { get; set; }`, яка відображається на таблицю `"Allergies"` у PostgreSQL.

2. **`Repositories/AllergyRepository.cs`**
   - Реалізує `IRepository<Allergy>`:
     - `GetByIdAsync(int id)` $\rightarrow$ `_context.Allergies.FindAsync(id)`;
     - `ListAsync()` $\rightarrow$ `_context.Allergies.ToListAsync()`;
     - `AddAsync(Allergy entity)` $\rightarrow$ `_context.Allergies.AddAsync(entity)` + `_context.SaveChangesAsync()`;
     - `DeleteAsync(Allergy entity)` $\rightarrow$ `_context.Allergies.Remove(entity)` + `_context.SaveChangesAsync()`.

3. **`Repositories/UserRepository.cs`**
   - Реалізовано метод `UpdateAsync(User entity)`, який викликає `_context.Users.Update(entity)` та `await _context.SaveChangesAsync()`.

4. **Міграція `Migrations/20261011140024_AddAllergiesTable.cs`**
   - Згенерована міграція EF Core, яка виконує SQL-запит створення таблиці в PostgreSQL:
     ```sql
     CREATE TABLE "Allergies" (
         "Id" integer GENERATED BY DEFAULT AS IDENTITY,
         "UserId" integer NOT NULL,
         "AllergenName" text NOT NULL,
         CONSTRAINT "PK_Allergies" PRIMARY KEY ("Id")
     );
     ```
   - Міграція застосована до хмарної БД `neondb` на **Neon.tech** і зафіксована в таблиці `__EFMigrationsHistory`.

---

### 4.3. Шар `FourTaste.WPF`

1. **`App.xaml.cs` (Dependency Injection та Serilog)**
   - У `OnStartup`:
     - Зчитується конфігурація з `appsettings.json`;
     - Ініціалізується `Log.Logger` для відправки логів у **Seq** (`http://localhost:5341` та командний `ngrok`);
     - Створюється `ServiceCollection`, викликається `ConfigureServices(services)` і будується глобальний `App.ServiceProvider`.
   - У `ConfigureServices`:
     - Реєструється `ILogger` (`Log.Logger`);
     - Реєструється `AppDbContext` з рядком підключення до Neon PostgreSQL;
     - Реєструються сервіси та репозиторії:
       - `services.AddScoped<IRepository<Allergy>, AllergyRepository>();`
       - `services.AddScoped<IAllergyService, AllergyService>();`
       - `services.AddScoped<IUserRepository, UserRepository>();`
       - `services.AddScoped<UserService>();`
       - `services.AddScoped<IAuthService, AuthService>();`
     - Реєструються вікна (`ManageAllergiesWindow`, `RegisterWindow`, `LoginWindow`, `MainWindow`).

2. **`ManageAllergiesWindow.xaml` та `ManageAllergiesWindow.xaml.cs`**
   - Вікно зверстане відповідно до мокапу **«Профіль користувача»** зі стор. 17 документації:
     - **Верхня панель:** бейдж «Мій профіль», відображення поточного імені (`DisplayNameTextBlock`) та нікнейму (`DisplayUsernameTextBlock`), кнопка **«Видалити профіль»** (`DeleteProfileButton_Click`).
     - **Ліва колонка (Картка «Алергени» та «Країни»):**
       - Поле `AllergenTextBox` з плейсхолдером «Додати алерген...»;
       - Список `AllergiesListBox` (відображає властивість `AllergenName` червоним кольором);
       - Кнопка «Видалити обраний» (`DeleteAllergyButton_Click`) та кнопка «Зберегти» (`SaveAllergyButton_Click`).
     - **Центральна колонка (Картка «Особисті дані та безпека»):**
       - Поля `ProfileNameTextBox`, `ProfileUsernameTextBox`, `ProfileEmailTextBox` та кнопка «Зберегти особисті дані» (`UpdateProfileButton_Click`);
       - Поле `NewPasswordBox` та кнопка «Змінити пароль» (`ChangePasswordButton_Click`).
     - **Права колонка:** блок меню, списків та кнопка **«Вийти»** (`LogoutButton_Click`), яка викликає `authService.LogoutAsync(CurrentUserId)`, очищує `SessionContext.CurrentUser` і повертає користувача на вікно реєстрації/входу.
   - Ідентифікатор поточного користувача береться із сесії: `private int CurrentUserId => SessionContext.CurrentUser?.ID ?? 0;`.

---

### 4.4. Шар `FourTaste.Tests` (Модульні тести)

1. **`AllergyServiceTests.cs`**
   - Використовує `xUnit` (`[Fact]`, `[Theory]`, `[InlineData]`) та `Moq` (`Mock<IRepository<Allergy>>`, `Mock<ILogger>`):
     - `AddAllergyAsync_ShouldSaveAllergyAndLogInformation_WhenAllergenNameIsValid` — перевіряє, що при валідній назві («Арахіс») викликається `AddAsync` у репозиторії рівно 1 раз і логується повідомлення рівня `Information`.
     - `AddAllergyAsync_ShouldThrowArgumentExceptionAndLogWarning_WhenAllergenNameIsEmpty` — перевіряє, що при порожньому рядку `""` або пробілах `"   "` викидається `ArgumentException`, метод `AddAsync` не викликається (`Times.Never`), а логер записує `Warning`.
     - `GetAllergiesByUserIdAsync_ShouldReturnFilteredAllergiesAndLogInformation` — перевіряє, що повертаються лише алергени вказаного `UserId`.
     - `DeleteAllergyAsync_ShouldRemoveAllergyAndLogInformation_WhenAllergyExists` — перевіряє успішне видалення існуючого алергену та запис логу.

2. **`RegistrationTests.cs`**
   - Окрім тестів реєстрації, містить тести керування профілем:
     - `UpdateProfileAsync_ShouldUpdateUser_WhenDataIsValid`;
     - `ChangePasswordAsync_ShouldThrowArgumentException_WhenNewPasswordIsEmpty`;
     - `DeleteAccountAsync_ShouldDeleteUser_WhenUserExists`.

