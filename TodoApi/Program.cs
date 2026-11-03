using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("TodoDb");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Не задана строка подключения TodoDb.");
}

builder.Services.AddDbContext<TodoDb>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddOpenApi();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TodoDb>();
    await db.Database.EnsureCreatedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Получить все задачи.
app.MapGet("/todoitems", async (TodoDb db) =>
{
    return await db.Todos
        .Select(todo => new TodoItemDTO(todo))
        .ToListAsync();
});

// Получить выполненные задачи.
app.MapGet("/todoitems/complete", async (TodoDb db) =>
{
    return await db.Todos
        .Where(todo => todo.IsComplete)
        .Select(todo => new TodoItemDTO(todo))
        .ToListAsync();
});

// Получить задачу по идентификатору.
app.MapGet("/todoitems/{id:int}", async (int id, TodoDb db) =>
{
    var todo = await db.Todos.FindAsync(id);

    if (todo is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(new TodoItemDTO(todo));
});

// Создать задачу.
app.MapPost("/todoitems", async (TodoItemDTO inputTodo, TodoDb db) =>
{
    var todo = new Todo
    {
        Name = inputTodo.Name,
        IsComplete = inputTodo.IsComplete
    };

    db.Todos.Add(todo);
    await db.SaveChangesAsync();

    return Results.Created(
        $"/todoitems/{todo.Id}",
        new TodoItemDTO(todo));
});

// Изменить задачу.
app.MapPut("/todoitems/{id:int}", async (int id, TodoItemDTO inputTodo, TodoDb db) =>
{
    var todo = await db.Todos.FindAsync(id);

    if (todo is null)
    {
        return Results.NotFound();
    }

    todo.Name = inputTodo.Name;
    todo.IsComplete = inputTodo.IsComplete;

    await db.SaveChangesAsync();

    return Results.NoContent();
});

// Удалить задачу.
app.MapDelete("/todoitems/{id:int}", async (int id, TodoDb db) =>
{
    var todo = await db.Todos.FindAsync(id);

    if (todo is null)
    {
        return Results.NotFound();
    }

    db.Todos.Remove(todo);
    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.Run();