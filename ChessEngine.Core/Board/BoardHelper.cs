namespace ChessEngine.Core.Board;

public static class BoardHelper
{
    public static int SquareIndex(int file, int rank) => rank * 8 + file;
    public static int FileOf(int square) => square & 0b111;
    public static int RankOf(int square) => square >> 3;

    public static bool IsValidSquare(int square) => square >= 0 && square < 64;

    public static string SquareName(int square)
    {
        char file = FileOf(square) switch
        {
            0 => 'a',
            1 => 'b',
            2 => 'c',
            3 => 'd',
            4 => 'e',
            5 => 'f',
            6 => 'g',
            7 => 'h',
            _ => '?'
        };

        char rank = RankOf(square) switch
        {
            0 => '1',
            1 => '2',
            2 => '3',
            3 => '4',
            4 => '5',
            5 => '6',
            6 => '7',
            7 => '8',
            _ => '?'
        };
        return $"{file}{rank}";
    }

    public static int SquareFromName(string name)
    {
        int file = name[0] switch
        {
            'a' => 0,
            'b' => 1,
            'c' => 2,
            'd' => 3,
            'e' => 4,
            'f' => 5,
            'g' => 6,
            'h' => 7,
            _ => -1
        };

        int rank = name[1] switch
        {
            '1' => 0,
            '2' => 1,
            '3' => 2,
            '4' => 3,
            '5' => 4,
            '6' => 5,
            '7' => 6,
            '8' => 7,
            _ => -1
        };

        return SquareIndex(file, rank);
    }
}