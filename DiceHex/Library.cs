using System.Drawing;
using static DiceHex.Library.TileType;
using static DiceHex.Library.Player;

namespace DiceHex
{
    class Library
    {
        public enum TurnState
        {
            NoSelection,
            PieceSelected,
            ConsumeSelectedMultipleChoice,
            ConsumeSelectedPromotePawn,
            ConsumeSelectedCreatePawn,
            GameOver
        }

        public enum Piece
        {
            None,
            Pawn,
            Elite,
            Noble
        }

        public enum TileType
        {
            OutOfBounds,
            Consumed,
            Playable
        }

        public enum HighlightType
        {
            None,
            Moveable,
            Attackable
        }

        public enum Player
        {
            None,
            Player1,
            Player2
        }

        public enum Action
        {
            Move,
            PromotePawn,
            CreatePawn,
            Kill
        }

        public class Tile
        {
            public Player Player { get; set; }
            public TileType TileType { get; set; }
            public HighlightType Highlight { get; set; }
            public Piece Piece { get; set; }
        }

        public class History
        {
            public Player Player { get; set; }
            public Action Action { get; set; }
            public Piece PieceType { get; set; }
            public Piece EnemyPieceType { get; set; }
            public Point StartingSpace { get; set; }
            public Point EndingSpace { get; set; }
            public Point ConsumedSpace { get; set; }
        }

        public class GameSettings
        {
            public Player CurrentPlayer;

            public int CanvasWidth = 600;
            public int CanvasHeight = 600;
            public int GridWidth = 9;
            public int GridHeight = 9;
            public int GridOriginWidth = 10;
            public int GridOriginHeight = 10;
            public Point Grid;

            public Point Player1InfoPosition = new Point(70, 50);
            public Point Player2InfoPosition = new Point(400, 50);
            public Point PlayerTurnInfoPosition = new Point(240, 10);
            public Point DebugInfoPosition = new Point(0, 0);

            public int MaxPawns = 5;
            public int MaxElites = 3;
            public int MaxNobles = 3;

            public Font Font = new Font("Arial", 8);
            public Brush BrushBlack = new SolidBrush(Color.Black);
            public Brush BrushPlayer1 = new SolidBrush(Color.Indigo);
            public Brush BrushPlayer2 = new SolidBrush(Color.IndianRed);
            public Pen PenOutline = new Pen(new SolidBrush(Color.Black));
            public Brush BrushBackground = new SolidBrush(Color.Aqua);
            public Brush BrushPlayable = new SolidBrush(Color.LawnGreen);
            public Brush BrushConsumed = new SolidBrush(Color.Tomato);
            public Brush PLAYER3_COLOR = new SolidBrush(Color.Violet);
            public Brush PLAYER4_COLOR = new SolidBrush(Color.Khaki);
            public Brush BrushOutOfBounds = new SolidBrush(Color.Indigo);
            public Brush BrushSelection = new SolidBrush(Color.Yellow);
            public Brush BrushHighlightMove = new SolidBrush(Color.Gold);
            public Brush BrushHighlightAttack = new SolidBrush(Color.Red);
            public Brush BrushMouseHover = new SolidBrush(Color.Beige);

            public Point nullPoint = new Point(-1, -1);
            public Point LineSpace = new Point(0, 11);

            public static TileType[][] MapStandard_TileType = new TileType[][] 
            {
                new TileType[] { OutOfBounds,   OutOfBounds,    OutOfBounds,    OutOfBounds,    OutOfBounds,    OutOfBounds,    OutOfBounds,    OutOfBounds,    OutOfBounds },
                new TileType[] { OutOfBounds,   OutOfBounds,    Playable,       Playable,       Playable,       Playable,       Playable,       OutOfBounds,    OutOfBounds },
                new TileType[] { OutOfBounds,   OutOfBounds,    Playable,       Playable,       Playable,       Playable,       Playable,       Playable,       OutOfBounds },
                new TileType[] { OutOfBounds,   Playable,       Playable,       Playable,       Playable,       Playable,       Playable,       Playable,       OutOfBounds },
                new TileType[] { OutOfBounds,   Playable,       Playable,       Playable,       Playable,       Playable,       Playable,       Playable,       Playable },
                new TileType[] { OutOfBounds,   Playable,       Playable,       Playable,       Playable,       Playable,       Playable,       Playable,       OutOfBounds },
                new TileType[] { OutOfBounds,   OutOfBounds,    Playable,       Playable,       Playable,       Playable,       Playable,       Playable,       OutOfBounds },
                new TileType[] { OutOfBounds,   OutOfBounds,    Playable,       Playable,       Playable,       Playable,       Playable,       OutOfBounds,    OutOfBounds },
                new TileType[] { OutOfBounds,   OutOfBounds,    OutOfBounds,    OutOfBounds,    OutOfBounds,    OutOfBounds,    OutOfBounds,    OutOfBounds,    OutOfBounds }
            };

            public static Player[][] MapStandard_PlayerStart = new Player[][]
            {
                new Player[] { None,   None,    None,    None,    None,    None,    None,    None,    None },
                new Player[] { None,   None,    Player1, Player1, Player1, Player1, Player1, None,    None },
                new Player[] { None,   None,    None,    None,    None,    None,    None,    None,    None },
                new Player[] { None,   None,    None,    None,    None,    None,    None,    None,    None },
                new Player[] { None,   None,    None,    None,    None,    None,    None,    None,    None },
                new Player[] { None,   None,    None,    None,    None,    None,    None,    None,    None },
                new Player[] { None,   None,    None,    None,    None,    None,    None,    None,    None },
                new Player[] { None,   None,    Player2, Player2, Player2, Player2, Player2, None,    None },
                new Player[] { None,   None,    None,    None,    None,    None,    None,    None,    None }
            };

            public static Piece[][] MapStandard_PieceStart = new Piece[][]
            {
                new Piece[] { Piece.None,   Piece.None,    Piece.None,    Piece.None,    Piece.None,    Piece.None,    Piece.None,    Piece.None,    Piece.None },
                new Piece[] { Piece.None,   Piece.None,    Piece.Pawn,    Piece.Pawn,    Piece.Pawn,    Piece.Pawn,    Piece.Pawn,    Piece.None,    Piece.None },
                new Piece[] { Piece.None,   Piece.None,    Piece.None,    Piece.None,    Piece.None,    Piece.None,    Piece.None,    Piece.None,    Piece.None },
                new Piece[] { Piece.None,   Piece.None,    Piece.None,    Piece.None,    Piece.None,    Piece.None,    Piece.None,    Piece.None,    Piece.None },
                new Piece[] { Piece.None,   Piece.None,    Piece.None,    Piece.None,    Piece.None,    Piece.None,    Piece.None,    Piece.None,    Piece.None },
                new Piece[] { Piece.None,   Piece.None,    Piece.None,    Piece.None,    Piece.None,    Piece.None,    Piece.None,    Piece.None,    Piece.None },
                new Piece[] { Piece.None,   Piece.None,    Piece.None,    Piece.None,    Piece.None,    Piece.None,    Piece.None,    Piece.None,    Piece.None },
                new Piece[] { Piece.None,   Piece.None,    Piece.Pawn,    Piece.Pawn,    Piece.Pawn,    Piece.Pawn,    Piece.Pawn,    Piece.None,    Piece.None },
                new Piece[] { Piece.None,   Piece.None,    Piece.None,    Piece.None,    Piece.None,    Piece.None,    Piece.None,    Piece.None,    Piece.None }
            };

            public GameSettings()
            {
                Grid = new Point(GridWidth, GridHeight);
            }
        }
    }
}
