using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("TodoDb");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Не задана строка подключения TodoDb.");
}

builder.Services.AddDbContext<TodoDb>(options => options.UseNpgsql(connectionString));

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TodoDb>();
    await db.Database.EnsureCreatedAsync();
}

// Configure the HTTP request pipeline.
app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/todoitems", async (TodoDb db) =>
{
    return await db.Todos.OrderBy(todo => todo.Id).Select(todo => new TodoItemDTO(todo)).ToListAsync();
});

app.MapGet("/todoitems/complete", async (TodoDb db) =>
{
    return await db.Todos.Where(todo => todo.IsComplete).OrderBy(todo => todo.Id).Select(todo => new TodoItemDTO(todo)).ToListAsync();
});

app.MapGet("/todoitems/{id:int}", async (int id, TodoDb db) =>
{
    var todo = await db.Todos.FindAsync(id);

    if (todo is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(new TodoItemDTO(todo));
});

app.MapPost("/todoitems", async (TodoItemDTO inputTodo, TodoDb db) =>
{
    if (string.IsNullOrWhiteSpace(inputTodo.Name))
    {
        return Results.BadRequest(new { error = "Укажи название задачи." });
    }

    var todo = new Todo { Name = inputTodo.Name.Trim(), IsComplete = inputTodo.IsComplete };

    db.Todos.Add(todo);
    await db.SaveChangesAsync();

    return Results.Created($"/todoitems/{todo.Id}", new TodoItemDTO(todo));
});

app.MapPut("/todoitems/{id:int}", async (int id, TodoItemDTO inputTodo, TodoDb db) =>
{
    var todo = await db.Todos.FindAsync(id);

    if (todo is null)
    {
        return Results.NotFound();
    }

    if (string.IsNullOrWhiteSpace(inputTodo.Name))
    {
        return Results.BadRequest(new { error = "Укажи название задачи." });
    }

    todo.Name = inputTodo.Name.Trim();
    todo.IsComplete = inputTodo.IsComplete;

    await db.SaveChangesAsync();

    return Results.NoContent();
});

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