using System.Numerics;
using Raylib_cs;

namespace ArenaGame;

class Program
{
    [System.STAThread]
    private static void Main(string[] args)
    {
        Game game = new();
        game.Run();
    }
}