# Сборка образа TodoApi (Егор)

Многоэтапная сборка приложения TodoApi (ASP.NET Core, .NET 10).

Итоговый образ — docker-todo:multi, порт приложения внутри контейнера — 8080.

## Что здесь лежит

- docker/egor/Dockerfile — итоговая многоэтапная сборка, образ docker-todo:multi. Его подключает Саша в Compose.
- docker/egor/Dockerfile.simple — одностадийная сборка, образ docker-todo:simple. Только для сравнения размеров.
- .dockerignore в корне репозитория — очищает контекст сборки (без .git, bin/, obj/, .env). Контекст — 14,86 КБ.

## Команды (выполнять из корня репозитория)

```
# Итоговый образ
docker build -f docker/egor/Dockerfile -t docker-todo:multi .

# Простой вариант (для сравнения)
docker build -f docker/egor/Dockerfile.simple -t docker-todo:simple .

# Размеры
docker image ls docker-todo
```

## Результаты измерений

- docker-todo:multi — 353 МБ, первая сборка 103,6 с, повторная без изменений 1,9 с (все слои CACHED).
- docker-todo:simple — 1,36 ГБ, первая сборка 77,7 с.

Вывод: многоэтапная сборка даёт образ примерно в 4 раза меньше. SDK (~1,3 ГБ) нужен только для компиляции и в итоговый образ не попадает — в multi остаётся только runtime aspnet (~340 МБ) плюс само приложение. Меньше размер — быстрее скачивание образа и меньше поверхность атаки.

## Поведение кеша (замеры)

- Пересборка без изменений — все слои CACHED, 1,9 с.
- Изменение Program.cs — restore остаётся закеширован, пересобираются только COPY кода и publish: 26,5 с (из них publish 18,8 с).
- Изменение TodoApi.csproj — restore выполняется заново, примерно 34 с.

Поэтому в Dockerfile csproj копируется отдельным слоем до остального кода: пока зависимости не меняются, долгий restore не повторяется.

## Ручная проверка образа (без Compose)

База должна быть запущена:

```
docker compose up -d --wait db
```

Запуск контейнера с ручным тестом:

```
docker run --rm -d --name api-test --network docker-todo_default -p 8080:8080 `
  -e ConnectionStrings__TodoDb="Host=db;Port=5432;Database=tododb;Username=todouser;Password=todo_local_password" `
  docker-todo:multi
```

Проверка в браузере:

- http://localhost:8080 — интерфейс (wwwroot внутри образа)
- http://localhost:8080/todoitems — JSON API

Остановить тестовый контейнер:

```
docker rm -f api-test
```

Строка подключения передаётся переменной окружения, в самом образе её нет. В Compose Саша задаёт её так же — через ConnectionStrings__TodoDb.

## Как проверить, что задача дошла до базы

Создать задачу в интерфейсе на http://localhost:8080, затем открыть psql интерактивно:

```
docker compose exec db psql -U todouser -d tododb
```

Внутри psql (имя таблицы в кавычках, с большой буквы):

```
SELECT * FROM "Todos";
```

Выход из psql — \q.

Примечание. Одной командой запрос не отправлять: PowerShell 5.1 вырезает двойные кавычки, psql получает FROM todos в нижнем регистре и отвечает relation "todos" does not exist. Поэтому только интерактивный режим.

## Как читать Dockerfile

- Этап build (образ sdk:10.0): компиляция и публикация, в итоговый образ не попадает.
- Этап final (образ aspnet:10.0): только runtime, COPY --from=build забирает готовые файлы.
- EXPOSE 8080 — только метаданные; реальный проброс порта делают ports: в compose или -p в docker run.
- USER $APP_UID — процесс в контейнере работает не от root.
- При старте приложение само создаёт схему БД (EnsureCreatedAsync), миграции не нужны.