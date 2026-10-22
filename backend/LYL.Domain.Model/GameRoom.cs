namespace LYL.Domain.Model;

public class GameRoom
{
    public const int DefaultTableCount = 5; 

    private readonly object tablesLock = new();
    public GameRoom(Supervisor host, string roomCode)
    {
        Host = host;
        RoomCode = roomCode;
        
        for (int i = 1; i <= DefaultTableCount; i++) //changed to five tables to get the demo able to accomodate 25 students
        {
            Tables.Add(new Table { TableNumber = i });
        }
    }
    public Guid GameRoomId { get; set; }

    public string Name { get; set; }

    public string RoomCode { get; set; }

    public bool IsActive { get; set; }

    public Supervisor Host { get; set; }

    public List<Table> Tables { get; set; } = new();

    public Table AddTable(List<Character> characters)
    {
        lock (tablesLock)
        {
            var table = new Table
            {
                TableNumber = Tables.Max(t => t.TableNumber) + 1,
                Characters = characters
            };

            Tables = new List<Table>(Tables) { table };
            return table;
        }
    }
}