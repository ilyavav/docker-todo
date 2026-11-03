using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TodoDb>(options =>
    options.UseInMemoryDatabase("TodoList"));

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Получить все задачи.
app.MapGet("/todoitems", async (TodoDb db) =>
{
    return await db.Todos.ToListAsync();
});

// Получить выполненные задачи.
app.MapGet("/todoitems/complete", async (TodoDb db) =>
{
    return await db.Todos
        .Where(todo => todo.IsComplete)
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

    return Results.Ok(todo);
});

// Создать задачу.
app.MapPost("/todoitems", async (Todo todo, TodoDb db) =>
{
    db.Todos.Add(todo);
    await db.SaveChangesAsync();

    return Results.Created($"/todoitems/{todo.Id}", todo);
});

// Изменить задачу.
app.MapPut("/todoitems/{id:int}", async (int id, Todo inputTodo, TodoDb db) =>
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