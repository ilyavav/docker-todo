public class TodoItemDTO
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool IsComplete { get; set; }

    public TodoItemDTO()
    {
    }

    public TodoItemDTO(Todo todo)
    {
        Id = todo.Id;
        Name = todo.Name;
        IsComplete = todo.IsComplete;
    }
}