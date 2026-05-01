using ChessEngine.Core.MoveGen;
using ChessEngine.Core.UCI;

if (args.Length > 0 && args[0] == "perft")
{
    var perft = new Perft();
    perft.RunTestSuite();
}
else
{
    var handler = new UciHandler();
    handler.Loop();
}