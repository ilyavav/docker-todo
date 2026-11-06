\# Сборка образа TodoApi (Егор)



Многоэтапная сборка приложения TodoApi (ASP.NET Core, .NET 10).



Итоговый образ — `docker-todo:multi`, порт приложения внутри контейнера — 8080.



\## Файлы



| Файл | Назначение |

|---|---|

| `docker/egor/Dockerfile` | Итоговая многоэтапная сборка → `docker-todo:multi`. Его подключает Саша в Compose |

| `docker/egor/Dockerfile.simple` | Одностадийная сборка → `docker-todo:simple`. Только для сравнения размеров |

| `.dockerignore` (корень репозитория) | Очищает контекст сборки: без `.git`, `bin/`, `obj/`, `.env`. Контекст — 14,86 КБ |



\## Команды (выполнять из корня репозитория)



```powershell

\# Итоговый образ

docker build -f docker/egor/Dockerfile -t docker-todo:multi .



\# Простой вариант (для сравнения)

docker build -f docker/egor/Dockerfile.simple -t docker-todo:simple .



\# Размеры

docker image ls docker-todo

```



\## Результаты измерений



| Образ | Размер | Первая сборка | Повторная без изменений |

|---|---|---|---|

| `docker-todo:multi` | 353 МБ | 103,6 с | 1,9 с (все слои CACHED) |

| `docker-todo:simple` | 1,36 ГБ | 77,7 с | — |



Вывод: итоговый образ в \~4 раза меньше, чем при одностадийной сборке.



SDK (\~1,3 ГБ) нужен только для компиляции и не попадает в итоговый образ —

в `multi` остаётся лишь runtime `aspnet` (\~340 МБ) плюс само приложение.



Меньше размер — быстрее загрузка образа, меньше поверхность атаки.



\## Поведение кеша (замеры)



1\. Пересборка без изменений — все слои `CACHED`, 1,9 с.

2\. Изменение `Program.cs` — слой `restore` остаётся закеширован, пересобираются только `COPY` кода и `publish`: 26,5 с (publish — 18,8 с).

3\. Изменение `TodoApi.csproj` — `dotnet restore` выполняется заново (\~34 с).



Поэтому в Dockerfile `csproj` копируется отдельным слоем до остального кода: пока зависимости не меняются, долгий restore не повторяется.



\## Ручная проверка образа (без Compose)



```powershell

docker compose up -d --wait db



docker run --rm -d --name api-test --network docker-todo\_default -p 8080:8080 `

&#x20; -e ConnectionStrings\_\_TodoDb="Host=db;Port=5432;Database=tododb;Username=todouser;Password=todo\_local\_password" `

&#x20; docker-todo:multi



\# Проверка:

\#   http://localhost:8080           — интерфейс (wwwroot внутри образа)

\#   http://localhost:8080/todoitems — JSON API

docker compose exec db psql -U todouser -d tododb -c 'SELECT \* FROM \\"Todos\\";'

docker rm -f api-test

```



Строка подключения передаётся переменной окружения, в образе её нет — в Compose Саша задаёт её так же, через `ConnectionStrings\_\_TodoDb`.



Примечание: если psql отвечает `relation "todos" does not exist` — PowerShell вырезал двойные кавычки из команды. Проверяйте через интерактивный режим:



```powershell

docker compose exec db psql -U todouser -d tododb

```



внутри него: `SELECT \* FROM "Todos";`, выход — `\\q`.



\## Как читать Dockerfile



\- Этап `build` (sdk:10.0): компиляция и публикация. В итоговый образ не попадает.

\- Этап `final` (aspnet:10.0): только runtime, `COPY --from=build` забирает готовые файлы.

\- `EXPOSE 8080` — только метаданные; реальный проброс порта делают `ports:` в compose или `-p` в docker run.

\- `USER $APP\_UID` — контейнер работает не от root.

\- При старте приложение само создаёт схему БД (`EnsureCreatedAsync`), миграции не нужны.



