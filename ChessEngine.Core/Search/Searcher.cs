using ChessEngine.Core.Board;
using ChessEngine.Core.Evaluation;
using ChessEngine.Core.MoveGen;

namespace ChessEngine.Core.Search;

public class Searcher
{
    private readonly Board.Board _board;
    private readonly MoveGenerator _moveGenerator;
    private readonly Evaluator _evaluator;

    public Searcher(Board.Board board)
    {
        _board = board;
        _moveGenerator = new MoveGenerator();
        _evaluator = new Evaluator();
    }

    public Move FindBestMove(int depth)
    {

        List<Move> legalMoves = _moveGenerator.GenerateLegalMoves(_board);

        return legalMoves[Random.Shared.Next(legalMoves.Count)]; // TODO: implement search lol
    }
}