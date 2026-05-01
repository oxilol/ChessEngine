namespace ChessEngine.Core.MoveGen;

//https://www.chessprogramming.org/Perft

// Perft, (performance test, move path enumeration) is a test to validate that your move generation doesn't have any bug,
// it counts the number of legal positions reachable from a given position at a certain depth.
public class Perft
{
    private readonly MoveGenerator _moveGen = new();

    public long Run(Board.Board board, int depth)
    {
        if (depth == 0)
        {
            // once we reach depth 0, we count the node as 1, and return back up the tree
            return 1;
        }

        var moves = _moveGen.GenerateLegalMoves(board);
        long nodes = 0;

        foreach (var move in moves)
        {
            board.MakeMove(move);
            nodes += Run(board, depth - 1);
            board.UnmakeMove(move);
        }

        return nodes;
    }

    //public void Divide(Board.Board board, int depth)
    //{
    //    var moves = _moveGen.GenerateLegalMoves(board);
    //    long total = 0;

    //    // Sort alphabetically
    //    //var sorted = moves.OrderBy(m => m.ToString()).ToList();

    //    foreach (var move in moves)
    //    {
    //        board.MakeMove(move);
    //        long nodes = Run(board, depth - 1);
    //        board.UnmakeMove(move);

    //        Console.WriteLine($"{move}: {nodes}");
    //        total += nodes;
    //    }

    //    Console.WriteLine();
    //    Console.WriteLine($"Total: {total}");
    //}

    public void RunTestSuite()
    {
        var positions = new (string name, string fen)[]
        {
            ("Starting Position",
             "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1"),

            ("Kiwipete",
             "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1"),

            ("Position 3",
             "8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1"),

            ("Position 4",
             "r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1"),

            ("Position 5",
             "rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8"),
        };

        // Known correct node counts [position][depth]
        var expected = new long[][]
        {
            //               d1   d2      d3        d4          d5
            new long[] { 0,  20,  400,   8902,   197281,   4865609 },
            new long[] { 0,  48, 2039,  97862,  4085603,  193690690 },
            new long[] { 0,  14,  191,   2812,    43238,    674624 },
            new long[] { 0,   6,  264,   9467,   422333,  15833292 },
            new long[] { 0,  44, 1486,  62379,  2103487,  89941194 },
        };

        int maxDepth = 5;

        Console.WriteLine("=================================================");
        Console.WriteLine("  PERFT TEST SUITE");
        Console.WriteLine("=================================================");
        Console.WriteLine();

        bool allPassed = true;

        for (int p = 0; p < positions.Length; p++)
        {
            var (name, fen) = positions[p];
            var board = new Board.Board(fen);

            Console.WriteLine($"Position {p + 1}: {name}");
            Console.WriteLine($"FEN: {fen}");
            Console.WriteLine();

            for (int depth = 1; depth <= maxDepth; depth++)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                long nodes = Run(board, depth);
                sw.Stop();

                long exp = expected[p][depth];
                bool passed = nodes == exp;
                allPassed = allPassed && passed;

                string result = passed ? "PASS ✓" : $"FAIL ✗  (expected {exp})";
                string nps = sw.ElapsedMilliseconds > 0
                              ? $"{nodes * 1000 / sw.ElapsedMilliseconds:N0} nps"
                              : "< 1ms";

                Console.WriteLine($"  depth {depth}: {nodes,12:N0}  {result,-30} {sw.ElapsedMilliseconds}ms  {nps}");
            }

            Console.WriteLine();
        }

        Console.WriteLine("=================================================");
        Console.WriteLine(allPassed ? "  ALL TESTS PASSED ✓" : "  SOME TESTS FAILED ✗");
        Console.WriteLine("=================================================");
    }
}