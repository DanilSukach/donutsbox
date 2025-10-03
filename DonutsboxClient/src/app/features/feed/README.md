# Feed Module

Модуль ленты подписок с интегрированным компонентом топ-авторов, реализованный с использованием современных практик Angular.

## Структура

```
feed/
├── components/
│   └── top-authors/          # Компонент отображения топ-10 авторов
│       ├── top-authors.ts
│       ├── top-authors.html
│       └── top-authors.css
├── pages/
│   └── feed-page/           # Главная страница ленты
│       ├── feed-page.ts
│       ├── feed-page.html
│       └── feed-page.css
└── services/
    ├── feed-facade.ts       # Основной facade для ленты и пользователей
    ├── authors-facade.ts    # Facade для работы с авторами
    ├── feed-facade.spec.ts  # Тесты для FeedFacade
    └── authors-facade.spec.ts # Тесты для AuthorsFacade
```

## Современные практики Angular

### Использованные технологии:
- ✅ **Standalone компоненты** - без NgModules
- ✅ **Signals** - для реактивного управления состоянием
- ✅ **OnPush Change Detection** - для оптимизации производительности
- ✅ **Inject function** - вместо constructor injection
- ✅ **Новый Control Flow** - `@if`, `@for` вместо `*ngIf`, `*ngFor`
- ✅ **Строгая типизация** - TypeScript best practices
- ✅ **Tailwind CSS** - utility-first CSS framework для стилизации
- ✅ **Facade Pattern** - инкапсуляция API взаимодействия в сервисах

## Компоненты

### TopAuthors

Компонент для отображения топ-10 авторов по количеству подписчиков.

**Современные особенности:**
```typescript
// Facade pattern - инкапсуляция API логики
private readonly feedFacade = inject(FeedFacade);

// Signals из facade сервиса
readonly topAuthors = this.feedFacade.topAuthors;
readonly loading = this.feedFacade.isLoadingTopAuthors;
readonly error = this.feedFacade.topAuthorsError;

// OnPush change detection для производительности
changeDetection: ChangeDetectionStrategy.OnPush
```

**Функции:**
- Загрузка топ-авторов через API `api/Authors/top`
- Отображение рейтинга, аватара, имени, описания и количества подписчиков
- Обработка состояний загрузки и ошибок с помощью signals
- Адаптивный дизайн
- Кнопка подписки (заготовка для будущей функциональности)

**API:**
- `GET /api/Authors/top?count=10` - получение топ авторов

### FeedPage

Главная страница ленты подписок.

**Современные особенности:**
```typescript
// Standalone компонент
imports: [TopAuthors]

// OnPush change detection
changeDetection: ChangeDetectionStrategy.OnPush
```

**Функции:**
- Интеграция компонента TopAuthors
- Заготовка для отображения контента подписок
- Адаптивный дизайн

## Facade Сервисы

### FeedFacade
Основной сервис для управления лентой и пользовательскими данными.

**Возможности:**
```typescript
// Управление состоянием пользователя
readonly userGuid = signal<string | null>(null);
readonly isLoadingUserData = signal(false);

// Управление топ авторами
readonly topAuthors = signal<AuthorRequestDto[]>([]);
readonly isLoadingTopAuthors = signal(false);
readonly topAuthorsError = signal<string | null>(null);

// Методы
loadTopAuthors(count: number): Observable<AuthorRequestDto[]>
subscribeToAuthor(authorId: string): Observable<boolean>
loadUserFeedContent(): Observable<any[]>
refreshUserData(): void
clearUserData(): void
```

### AuthorsFacade
Специализированный сервис для работы с авторами.

**Возможности:**
```typescript
// Управление состоянием авторов
readonly authors = signal<AuthorRequestDto[]>([]);
readonly selectedAuthor = signal<AuthorRequestDto | null>(null);
readonly isLoadingAuthors = signal(false);

// Методы
loadAuthors(page, pageSize, sortBy, descending): Observable<AuthorRequestDto[]>
loadAuthorById(id: string): Observable<AuthorRequestDto | null>
navigateToAuthor(authorId: string): void
searchAuthors(query: string): Observable<AuthorRequestDto[]>
getRecommendedAuthors(userId: string): Observable<AuthorRequestDto[]>
```

### Преимущества Facade Pattern:
- ✅ **Инкапсуляция** - скрытие сложности API взаимодействия
- ✅ **Переиспользование** - общая логика в одном месте
- ✅ **Тестируемость** - легко мокать facade в тестах
- ✅ **Централизованное состояние** - signals управляются в сервисах
- ✅ **Разделение ответственности** - компоненты фокусируются на UI

## Шаблоны

### Новый Control Flow синтаксис:
```html
<!-- Вместо *ngIf -->
@if (loading()) {
  <div class="loading">...</div>
}

<!-- Вместо *ngFor -->
@for (author of topAuthors(); track trackByAuthorId($index, author); let i = $index) {
  <div class="author-card">...</div>
}
```

## Роутинг и Авторизация

Страница доступна по адресу `/feed` только для авторизованных пользователей:
```typescript
{
  path: 'feed',
  loadComponent: () => import('./features/feed/pages/feed-page/feed-page').then((c) => c.FeedPage),
  canActivate: [authGuard], // 🔒 Только для авторизованных пользователей
}
```

### AuthGuard логика:
- ✅ **Проверка токена** - наличие access token в localStorage
- ✅ **Валидация GUID** - извлечение и проверка GUID пользователя из JWT
- ✅ **Обработка новых создателей** - перенаправление на `/profile/setup`
- ✅ **Перенаправления**:
  - Нет токена → `/auth/login`
  - Невалидный токен → очистка + `/auth/login`
  - Новый создатель → `/profile/setup`
  - Авторизован → доступ к `/feed`

## Использование

### Для неавторизованных пользователей:
1. Попытка доступа к `/feed` → автоматическое перенаправление на `/auth/login`

### Для авторизованных пользователей:
1. **Авторизуйтесь** через `/auth/login`
2. **Перейдите на `/feed`** - страница загрузится с вашим GUID
3. **Просмотрите топ-10 авторов** по подписчикам
4. **Персональный контент** будет загружаться на основе вашего GUID
5. Нажмите на карточку автора для перехода к профилю (функция в разработке)
6. Используйте кнопку "Подписаться" для подписки на автора (функция в разработке)

### Персонализация:
- 🔑 **GUID пользователя** извлекается из JWT токена
- 📊 **Персональный контент** загружается на основе GUID
- 🎯 **Подписки пользователя** будут влиять на отображаемый контент
- ✅ **Статус авторизации** отображается в интерфейсе

## Дизайн-система с Tailwind CSS

### Цветовая палитра:
- **Основные цвета**: Amber (янтарный) - `amber-400`, `amber-600`, `amber-900`
- **Градиенты**: `from-pink-50 via-yellow-50 to-amber-50` для фонов
- **Акценты**: Green для кнопок действий, Red для ошибок
- **Нейтральные**: Gray для текста и границ

### Layout структура:
```html
<!-- Основной Grid Layout -->
<main class="grid grid-cols-1 lg:grid-cols-4 gap-8">
  <!-- Центральная лента (3/4 ширины) -->
  <section class="lg:col-span-3 order-2 lg:order-1">
    <!-- Основной контент -->
  </section>
  
  <!-- Боковая панель справа (1/4 ширины) -->
  <aside class="lg:col-span-1 order-1 lg:order-2">
    <div class="lg:sticky lg:top-8">
      <!-- Топ авторы -->
    </div>
  </aside>
</main>
```

### Компоненты UI:
```html
<!-- Карточки -->
<div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6 sm:p-8">

<!-- Компактные карточки авторов -->
<div class="flex items-start p-3 border border-gray-200 rounded-lg hover:border-amber-300">

<!-- Кнопки -->
<button class="px-4 py-2 bg-amber-600 text-white rounded-lg hover:bg-amber-700 transition-colors duration-200">

<!-- Аватары -->
<div class="w-10 h-10 rounded-full overflow-hidden ring-2 ring-gray-200 group-hover:ring-amber-300">

<!-- Состояния -->
<div class="animate-spin rounded-full h-6 w-6 border-b-2 border-amber-600">
```

## Преимущества современного подхода

### Производительность:
- **OnPush Change Detection** - компоненты обновляются только при изменении inputs или signals
- **Signals** - автоматическое отслеживание зависимостей и оптимизированные обновления
- **Standalone компоненты** - меньший bundle size, tree-shaking
- **Tailwind CSS** - оптимизированный CSS, purging неиспользуемых стилей

### Разработка:
- **Строгая типизация** - меньше ошибок во время выполнения
- **Новый Control Flow** - лучшая производительность и читаемость шаблонов
- **Inject function** - более простое тестирование и dependency injection
- **Utility-first CSS** - быстрая разработка, консистентный дизайн

### UX/UI:
- **Единый дизайн** - согласованная цветовая схема и компоненты
- **Адаптивность** - responsive дизайн с Tailwind breakpoints
- **Интерактивность** - плавные переходы и hover эффекты
- **Доступность** - focus states и семантическая разметка
- **Оптимальный Layout** - центральная лента + боковая панель
- **Sticky позиционирование** - топ авторы остаются видимыми при скролле

## Будущие улучшения

- [ ] Реализация функции подписки на авторов с signals
- [ ] Навигация к профилю автора при клике на карточку
- [ ] Отображение контента от подписок в ленте с signals
- [ ] Фильтрация и сортировка контента
- [ ] Бесконечная прокрутка для большого количества контента
- [ ] Кэширование данных топ-авторов с computed signals
- [ ] Добавление input/output functions для компонентов
- [ ] Реализация reactive forms для фильтров