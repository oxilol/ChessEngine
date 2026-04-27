namespace ChessEngine.Core.Board;
   
// making it a struct is faster than class (no heap allocation)
public readonly struct Move
{
    /*
     *      bits:   14 13 12 | 11 10 9 8 7 6 | 5 4 3 2 1 0
     *              ---------  -------------   -----------
     *              promo (3)      to (6)        from (6)
     */
    private readonly ushort _value;

    public int From       => _value & 0x3F;         // 0b111111
    public int To         => (_value >> 6) & 0x3F;  // 0b111111 (shitfted 6 bits the the right)
    public int PromoPiece => (_value >> 12) & 0x7;  // 0b111 (shifted 12 bits to the right)

    public bool IsPromotion  => PromoPiece != Piece.None;    
    public bool IsNull       => _value == 0;    

    public static readonly Move Null = new(0);

    public Move(ushort raw) => _value = raw;

    public Move(int from, int to, int promoPiece = Piece.None)
    {
        _value = (ushort)(from | (to << 6) | (promoPiece << 12));
    }

    //ex: e2e4, e7e8q
    public override string ToString() => BoardHelper.SquareName(From) + BoardHelper.SquareName(To) 
                                         + (IsPromotion ? Piece.ToChar(PromoPiece).ToString().ToLower() : "");
}