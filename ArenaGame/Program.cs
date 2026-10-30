namespace ArenaGame;

class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        Game game = new();
        game.Run();
    }
}