using System.Text.Json;

namespace Prismbar.Core.Todos;

public sealed class TodoItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Text { get; set; } = "";
    public bool Done { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class TodoStore(string path)
{
    private List<TodoItem> _items = [];

    public IReadOnlyList<TodoItem> Items => _items;

    /// <summary>Pending (newest first) followed by done items.</summary>
    public IEnumerable<TodoItem> Display =>
        _items.Where(i => !i.Done).OrderByDescending(i => i.CreatedAt)
              .Concat(_items.Where(i => i.Done).OrderByDescending(i => i.CreatedAt));

    public int PendingCount => _items.Count(i => !i.Done);

    public void Load()
    {
        try
        {
            _items = File.Exists(path) ? JsonSerializer.Deserialize<List<TodoItem>>(File.ReadAllText(path)) ?? [] : [];
        }
        catch (JsonException) { _items = []; }
    }

    public void Add(string text, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        _items.Insert(0, new TodoItem { Text = text.Trim(), CreatedAt = now });
        Save();
    }

    public void Toggle(Guid id)
    {
        var item = _items.FirstOrDefault(i => i.Id == id);
        if (item is null) return;
        item.Done = !item.Done;
        Save();
    }

    public void Remove(Guid id)
    {
        _items.RemoveAll(i => i.Id == id);
        Save();
    }

    public void ClearDone()
    {
        _items.RemoveAll(i => i.Done);
        Save();
    }

    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(_items, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(tmp, path, overwrite: true);
    }
}
