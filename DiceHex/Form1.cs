using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Timers;
using System.Windows.Forms;
using static DiceHex.Library;
using static DiceHex.PointExtensions;

namespace DiceHex
{
    public partial class Form1 : Form
    {
        Graphics graphics;
        Thread thread;
        GameSettings settings = new GameSettings();

        Point p, oldp, _oldp, cmp, pCount, eCount, nCount;
        int mouse; // 0 = outside of grid, 1 = inside of grid
        
        private readonly Point NULL_POINT = new Point(-1, -1);
        
        int hex_size;
        Point[] hexagon;
        int frames, fps, row, col, lx, ly;
        System.Timers.Timer fpsTimer;
        List<Point> selection;
        Point consumedSelection;

        Tile[][] tiles;

        bool debug;
        bool contextMenuOpen; // TODO: prevent things like mouse hover highlights and clicks from occuring while the context menu is open
        TurnState turnState;

        Queue<History> history;

        public Form1()
        {
            InitializeComponent();
            Location = new Point(0, 0);
            graphics = Grid.CreateGraphics();
            init();
        }

        public void init()
        {
            // Initialize the hexagon shape
            hex_size = 30;
            hexagon = new Point[] {
                new Point(0, (int)hex_size),
                new Point((int)(hex_size / 2), 0),
                new Point((int)(hex_size * 1.8), 0),
                new Point((int)(hex_size * 2.3), (int)hex_size),
                new Point((int)(hex_size * 1.8), (int)(hex_size * 2)),
                new Point((int)(hex_size / 2), (int)(hex_size * 2)) };

            // Build the map
            tiles = new Tile[settings.GridWidth][];
            for (int i = 0; i < settings.GridWidth; i++)
            {
                tiles[i] = new Tile[settings.GridHeight];
                for (int j = 0; j < settings.GridHeight; j++)
                {
                    tiles[i][j] = new Tile
                    {
                        Player = GameSettings.MapStandard_PlayerStart[i][j],
                        Piece = GameSettings.MapStandard_PieceStart[i][j],
                        TileType = GameSettings.MapStandard_TileType[i][j],
                        Highlight = HighlightType.None
                    };
                }
            }

            // Initialize the selected hexagon to (0,0)
            oldp = new Point(settings.GridOriginWidth, settings.GridOriginHeight);

            // Reset selection variables
            selection = new List<Point>();
            consumedSelection = NULL_POINT;

            frames = 0; fps = 0;
            fpsTimer = new System.Timers.Timer(1000);
            fpsTimer.Elapsed += fpsTimer_Elapsed;

            // Begin game with player 1
            settings.CurrentPlayer = Player.Player1;

            getCounts();

            // Set flags
            debug = false;
            contextMenuOpen = false;
            turnState = TurnState.NoSelection;

            thread = new Thread(new ThreadStart(render));
            thread.Start();
        }

        private void fpsTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            fps = frames;
            frames = 0;
        }

        public void stop()
        {
            thread.Abort();
        }

        public void render()
        {
            //int framesRendered = 0;
            long startTime = Environment.TickCount;

            while (true)
            {
                drawCanvas(hexagon);
            }
        }

        private void drawCanvas(Point[] hexagon)
        {
            using (Bitmap frame = new Bitmap(settings.CanvasWidth, settings.CanvasHeight))
            using (Graphics frameGraphics = Graphics.FromImage(frame))
            {
                // Draw Canvas
                frameGraphics.FillRectangle(settings.BrushBackground, 0, 0, settings.CanvasWidth, settings.CanvasHeight);

                // Get mouse position in respect to the canvas
                this.Invoke(new System.Action(() => p = PointToClient(Cursor.Position)));

                // Calculate the mouse position on a square grid offset by HEX_SIZE / 2
                col = p.X < settings.GridOriginWidth ? -1 : (int)((p.X - settings.GridOriginWidth) / (hex_size * 1.8));
                row = p.Y < (settings.GridOriginHeight + (int)((col % 2) * hex_size)) ? -1 : (int)((p.Y - settings.GridOriginHeight - (col % 2) * hex_size) / (hex_size * 2));

                // Calculate the corner of that specific square on the grid in pixels
                lx = p.X - settings.GridOriginWidth - (int)(hex_size * 1.8 * col);
                ly = p.Y - settings.GridOriginHeight - (int)(hex_size * 2 * row) - (int)((col % 2) * hex_size);

                // Test to see if the mouse is inside one of other hexagons on the left side
                if ((int)(lx + (ly / 2)) < hex_size / 2) // Top hexagon
                    p.X -= hex_size / 2;
                if ((int)(lx - (ly / 2)) < (hex_size / 2) * -1) // Bottom hexagon
                    p.X -= hex_size / 2;

                // Recalculate the grid position based on the updated mouse position
                col = p.X < settings.GridOriginWidth ? -1 : (int)((p.X - settings.GridOriginWidth) / (hex_size * 1.8));
                row = p.Y < (settings.GridOriginHeight + (int)((col % 2) * hex_size)) ? -1 : (int)((p.Y - settings.GridOriginHeight - (col % 2) * hex_size) / (hex_size * 2));

                // (Re)Set the current position if the mouse is within grid bounds
                if (col < settings.GridWidth && row < settings.GridHeight && col > -1 && row > -1)
                {
                    oldp = new Point((int)(settings.GridOriginWidth + col * hex_size * 1.8), (int)(((col % 2) * (hex_size)) + settings.GridOriginHeight + row * hex_size * 2));
                    _oldp = new Point(col, row);
                    mouse = 1;
                }
                else
                    mouse = 0;

                // Draw tile colors
                for (int i = 0; i < settings.GridWidth; i++)
                {
                    for (int j = 0; j < settings.GridHeight; j++)
                    {
                        Brush hexFill;

                        if (tiles[i][j].Highlight == HighlightType.Moveable)
                            hexFill = settings.BrushHighlightMove;
                        else if (tiles[i][j].Highlight == HighlightType.Attackable)
                            hexFill = settings.BrushHighlightAttack;
                        else
                        {
                            switch (tiles[i][j].TileType)
                            {
                                case TileType.Consumed:
                                    hexFill = settings.BrushConsumed;
                                    break;
                                case TileType.Playable:
                                    hexFill = settings.BrushPlayable;
                                    break;
                                default:
                                    hexFill = settings.BrushBackground;
                                    continue;
                            }
                        }

                        if (selection.Contains(new Point(i, j)))
                            hexFill = settings.BrushSelection;

                        if (mouse == 1 && i == col && j == row)
                            hexFill = settings.BrushMouseHover;

                        var hexPosition = CalculateHexPosition(i, j);

                        // Draw the current hexagon
                        frameGraphics.FillPolygon(
                            hexFill,
                            add(
                                hexagon,
                                hexPosition));

                        frameGraphics.DrawPolygon(
                            settings.PenOutline,
                            add(
                                hexagon,
                                hexPosition));

                        if (tiles[i][j].Piece != Piece.None)
                        {
                            frameGraphics.DrawString(
                                tiles[i][j].Piece == Piece.Pawn ? "P" : tiles[i][j].Piece == Piece.Elite ? "E" : tiles[i][j].Piece == Piece.Noble ? "N" : "", 
                                settings.Font,
                                tiles[i][j].Player == Player.Player1 ? settings.BrushPlayer1 : settings.BrushPlayer2,
                                new Point((int)settings.GridOriginWidth + (int)(i * hex_size * 1.8) + hex_size,
                                        settings.GridOriginHeight + j * hex_size * 2 + (i % 2) * hex_size + (int)(hex_size * .8)));
                        }
                    }
                }

                frameGraphics.DrawString(
                    "Player " + (settings.CurrentPlayer == Player.Player1 ? "1" : "2"), settings.Font, settings.CurrentPlayer == Player.Player1 ? settings.BrushPlayer1 : settings.BrushPlayer2,
                    settings.PlayerTurnInfoPosition);

                frameGraphics.DrawString(
                    "Available pieces for Player 1", settings.Font, settings.BrushPlayer1, settings.Player1InfoPosition);
                frameGraphics.DrawString(
                    "(" + pCount.X.ToString() + ") Pawns", settings.Font, settings.BrushPlayer1, add(settings.Player1InfoPosition, settings.LineSpace, 1));
                frameGraphics.DrawString(
                    "(" + eCount.X.ToString() + ") Elites", settings.Font, settings.BrushPlayer1, add(settings.Player1InfoPosition, settings.LineSpace, 2));
                frameGraphics.DrawString(
                    "(" + nCount.X.ToString() + ") Nobles", settings.Font, settings.BrushPlayer1, add(settings.Player1InfoPosition, settings.LineSpace, 3));

                frameGraphics.DrawString(
                    "Avaliable pieces for Player 2", settings.Font, settings.BrushPlayer2, settings.Player2InfoPosition);
                frameGraphics.DrawString(
                    "(" + pCount.Y.ToString() + ") Pawns", settings.Font, settings.BrushPlayer2, add(settings.Player2InfoPosition, settings.LineSpace, 1));
                frameGraphics.DrawString(
                    "(" + eCount.Y.ToString() + ") Elites", settings.Font, settings.BrushPlayer2, add(settings.Player2InfoPosition, settings.LineSpace, 2));
                frameGraphics.DrawString(
                    "(" + nCount.Y.ToString() + ") Nobles", settings.Font, settings.BrushPlayer2, add(settings.Player2InfoPosition, settings.LineSpace, 3));

                // Debug
                if (debug)
                {
                    frames++;
                    frameGraphics.DrawString(fps.ToString() + "fps", settings.Font, settings.BrushBlack, settings.DebugInfoPosition);
                    frameGraphics.DrawString(col.ToString() + "x, " + row.ToString() + "y", settings.Font, settings.BrushBlack, add(settings.DebugInfoPosition, settings.LineSpace, 1));
                    frameGraphics.DrawString(lx.ToString() + "x, " + ly.ToString() + "y", settings.Font, settings.BrushBlack, add(settings.DebugInfoPosition, settings.LineSpace, 2));
                }

                /* Debug Grid
                for (int i = 0; i < GRID_WIDTH; i++)
                    for (int j = 0; j < GRID_HEIGHT; j++)
                    {
                        frameGraphics.DrawRectangle(bp, (int)(settings.GridOriginWidth + i * HEX_SIZE * 1.8), (int)(((i % 2) * (HEX_SIZE)) + settings.GridOriginHeight + j * HEX_SIZE * 2), (int)(HEX_SIZE * 1.8), (int)(HEX_SIZE * 2));
                        //frameGraphics.DrawRectangle(, 0, 0, 10, 10);
                    }
                */

                // Paint the frame
                graphics.DrawImage(frame, 0, 0);
            }
        }

        private Point CalculateHexPosition(int i, int j)
        {
            return new Point((int)settings.GridOriginWidth + (int)(i * hex_size * 1.8),
                        settings.GridOriginHeight + j * hex_size * 2 + (i % 2) * hex_size);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            stop();
        }

        private void Grid_MouseClick(object sender, MouseEventArgs e)
        {
            getCounts();
            if (e.Button == MouseButtons.Right)
            {
                if (mouse == 1 && tiles[_oldp.X][_oldp.Y].TileType != TileType.OutOfBounds)
                {
                    if (selection.Contains(_oldp))
                    {
                        // Deselect
                        if (tiles[_oldp.X][_oldp.Y].Piece == Piece.Pawn)
                            foreach (Point p in getAdjacent(_oldp, settings.Grid))
                                tiles[p.X][p.Y].Highlight = HighlightType.None;
                        else if (tiles[_oldp.X][_oldp.Y].Piece == Piece.Elite)
                            foreach (Point p in getOuterAdjacent(_oldp, settings.Grid))
                                tiles[p.X][p.Y].Highlight = HighlightType.None;
                        else if (tiles[_oldp.X][_oldp.Y].Piece == Piece.Noble)
                            foreach (Point[] pa in getLineAdjacent(_oldp, settings.Grid))
                                foreach (Point p in pa)
                                    tiles[p.X][p.Y].Highlight = HighlightType.None;

                        resetSelection();
                    }
                    cmp = _oldp;
                    var cm = new ContextMenuStrip();
                    cm.Items.Add("Clear", null, clearPos);
                    cm.Items.Add("Consume", null, consumePos);
                    cm.Items.Add("(" + (settings.CurrentPlayer == Player.Player1 ? pCount.X.ToString() : pCount.Y.ToString()) + ") Pawn", null, pawnPos);
                    cm.Items.Add("(" + (settings.CurrentPlayer == Player.Player1 ? eCount.X.ToString() : eCount.Y.ToString()) + ") Elite", null, elitePos);
                    cm.Items.Add("(" + (settings.CurrentPlayer == Player.Player1 ? nCount.X.ToString() : nCount.Y.ToString()) + ") Noble", null, noblePos);
                    cm.Closed += new ToolStripDropDownClosedEventHandler(leavingContextMenu);
                    contextMenuOpen = true;
                    cm.Show(this, p);
                }
            }
            else if (e.Button == MouseButtons.Left)
            {
                if (mouse == 1 && tiles[_oldp.X][_oldp.Y].TileType != TileType.OutOfBounds)
                {
                    
                    // Clicking on a piece you own
                    if (turnState == TurnState.NoSelection && 
                        tiles[_oldp.X][_oldp.Y].Piece != Piece.None && 
                        tiles[_oldp.X][_oldp.Y].Player == settings.CurrentPlayer)
                    {
                        // Select
                        selection.Add(_oldp);
                        if (tiles[_oldp.X][_oldp.Y].Piece == Piece.Pawn)
                        {
                            foreach (Point p in getAdjacent(_oldp, settings.Grid))
                            {
                                if (tiles[p.X][p.Y].TileType == TileType.Playable && tiles[p.X][p.Y].Piece == Piece.None)
                                    tiles[p.X][p.Y].Highlight = HighlightType.Moveable;
                                if ((tiles[_oldp.X][_oldp.Y].Player == Player.Player1 && tiles[p.X][p.Y].Player == Player.Player2) 
                                    || (tiles[_oldp.X][_oldp.Y].Player == Player.Player2 && tiles[p.X][p.Y].Player == Player.Player1))
                                    tiles[p.X][p.Y].Highlight = HighlightType.Attackable;
                            }
                        }
                        else if (tiles[_oldp.X][_oldp.Y].Piece == Piece.Elite)
                        {
                            foreach (Point p in getOuterAdjacent(_oldp, settings.Grid))
                            {
                                if (tiles[p.X][p.Y].TileType == TileType.Playable && tiles[p.X][p.Y].Piece == Piece.None)
                                    tiles[p.X][p.Y].Highlight = HighlightType.Moveable;
                                if ((tiles[_oldp.X][_oldp.Y].Player == Player.Player1 && tiles[p.X][p.Y].Player == Player.Player2)
                                    || (tiles[_oldp.X][_oldp.Y].Player == Player.Player2 && tiles[p.X][p.Y].Player == Player.Player1))
                                    tiles[p.X][p.Y].Highlight = HighlightType.Attackable;
                            }
                        }
                        else if (tiles[_oldp.X][_oldp.Y].Piece == Piece.Noble)
                        {
                            foreach (Point[] pa in getLineAdjacent(_oldp, settings.Grid))
                            {
                                foreach (Point p in pa)
                                {
                                    if (tiles[p.X][p.Y].TileType == TileType.Playable && tiles[p.X][p.Y].Piece == Piece.None)
                                        tiles[p.X][p.Y].Highlight = HighlightType.Moveable;
                                    else if ((tiles[_oldp.X][_oldp.Y].Player == Player.Player1 && tiles[p.X][p.Y].Player == Player.Player2) 
                                        || (tiles[_oldp.X][_oldp.Y].Player == Player.Player2 && tiles[p.X][p.Y].Player == Player.Player1))
                                    {
                                        tiles[p.X][p.Y].Highlight = HighlightType.Attackable;
                                        break;
                                    }
                                    else if (tiles[_oldp.X][_oldp.Y].Player == tiles[p.X][p.Y].Player) break;
                                    else
                                        continue;
                                }
                            }

                            foreach (Point p in getAdjacent(_oldp, settings.Grid))
                                if (tiles[p.X][p.Y].Highlight == HighlightType.Attackable)
                                    tiles[p.X][p.Y].Highlight = HighlightType.None;
                        }

                        turnState = TurnState.PieceSelected;
                    }
                    // Clicking on a valid tile to move the selected piece to
                    else if (turnState == TurnState.PieceSelected && tiles[_oldp.X][_oldp.Y].Highlight != HighlightType.None)
                    {
                        // Move
                        if (tiles[_oldp.X][_oldp.Y].TileType == TileType.Playable)
                        {
                            if (tiles[selection[0].X][selection[0].Y].Piece == Piece.Pawn)
                                foreach (Point p in getAdjacent(selection[0], settings.Grid))
                                    tiles[p.X][p.Y].Highlight = HighlightType.None;
                            else if (tiles[selection[0].X][selection[0].Y].Piece == Piece.Elite)
                                foreach (Point p in getOuterAdjacent(selection[0], settings.Grid))
                                    tiles[p.X][p.Y].Highlight = HighlightType.None;
                            else if (tiles[selection[0].X][selection[0].Y].Piece == Piece.Noble)
                                foreach (Point[] pa in getLineAdjacent(selection[0], settings.Grid))
                                    foreach (Point p in pa)
                                        tiles[p.X][p.Y].Highlight = HighlightType.None;

                            tiles[_oldp.X][_oldp.Y].Piece = tiles[selection[0].X][selection[0].Y].Piece;
                            tiles[_oldp.X][_oldp.Y].Player = tiles[selection[0].X][selection[0].Y].Player;
                            tiles[selection[0].X][selection[0].Y].Piece = Piece.None;
                            tiles[selection[0].X][selection[0].Y].Player = Player.None;
                            resetSelection();
                            nextPlayer();
                        }
                    }
                    // Clicking on the previous selection or anywhere else
                    else if (turnState == TurnState.PieceSelected)
                    {
                        // Deselect
                        if (tiles[selection[0].X][selection[0].Y].Piece == Piece.Pawn)
                            foreach (Point p in getAdjacent(selection[0], settings.Grid))
                                tiles[p.X][p.Y].Highlight = HighlightType.None;
                        else if (tiles[selection[0].X][selection[0].Y].Piece == Piece.Elite)
                            foreach (Point p in getOuterAdjacent(selection[0], settings.Grid))
                                tiles[p.X][p.Y].Highlight = HighlightType.None;
                        else if (tiles[selection[0].X][selection[0].Y].Piece == Piece.Noble)
                            foreach (Point[] pa in getLineAdjacent(selection[0], settings.Grid))
                                foreach (Point p in pa)
                                    tiles[p.X][p.Y].Highlight = HighlightType.None;
                        getLineAdjacent(selection[0], settings.Grid).Select(pa => pa.Select(p => tiles[p.X][p.Y].Highlight = HighlightType.None));

                        resetSelection();
                    }
                    // Choosing a place to put a new pawn
                    else if (turnState == TurnState.ConsumeSelectedCreatePawn)
                    {
                        if (selection.Contains(_oldp))
                        {
                            cmp = _oldp;
                            selection.Select(p => tiles[p.X][p.Y].Highlight = HighlightType.None);
                            pawnPos(this, null);
                        }
                        else
                            resetConsumedSelection();

                        resetSelection();
                    }
                    // Choosing which piece to promote
                    else if (turnState == TurnState.ConsumeSelectedMultipleChoice)
                    {
                        if (selection.Contains(_oldp))
                        {
                            resetSelection();
                            promotePiece(_oldp);
                        }
                        else
                            cancelPromote(this, null);
                    }
                    // Clicking an empty tile with no other TurnStates active
                    else if (tiles[_oldp.X][_oldp.Y].TileType == TileType.Playable)
                    {
                        // Look for nearby pieces that can be promoted
                        var adjacentPlayerPieces = getAdjacent(_oldp, settings.Grid).Where(p => tiles[p.X][p.Y].Player == settings.CurrentPlayer);

                        // Single piece go ahead and begin promotion
                        if (adjacentPlayerPieces.Count() == 1)
                        {
                            consumedSelection = _oldp;
                            tiles[_oldp.X][_oldp.Y].TileType = TileType.Consumed;
                            promotePiece(adjacentPlayerPieces.First());
                        }
                        // More than one possible option, highlight all options
                        else
                        {
                            // Elites/Nobles that can be selected must have a valid loction to put a pawn
                            selection.AddRange(adjacentPlayerPieces.Where(pa => (tiles[pa.X][pa.Y].Piece != Piece.Pawn &&
                                                                            getAdjacent(pa, settings.Grid).Where(pb => tiles[pb.X][pb.Y].TileType == TileType.Playable && 
                                                                            tiles[pb.X][pb.Y].Piece == Piece.None).Count() > 0 &&
                                                                            (settings.CurrentPlayer == Player.Player1 ? pCount.X : pCount.Y) > 0) ||
                                                                            tiles[pa.X][pa.Y].Piece == Piece.Pawn));
                            if (selection.Count() > 0)
                            {
                                consumedSelection = _oldp;
                                tiles[_oldp.X][_oldp.Y].TileType = TileType.Consumed;
                                turnState = TurnState.ConsumeSelectedMultipleChoice;
                            }
                        }
                    }
                }
            }

            getCounts();
        }

        private void promotePiece(Point selectedPiece)
        {
            if (tiles[selectedPiece.X][selectedPiece.Y].Piece == Piece.Pawn)
            {
                turnState = TurnState.ConsumeSelectedPromotePawn;
                selection.Add(selectedPiece);
                cmp = selectedPiece;
                var cm = new ContextMenuStrip();
                cm.Items.Add("Cancel", null, cancelPromote);
                if ((settings.CurrentPlayer == Player.Player1 ? eCount.X : eCount.Y) > 0)
                    cm.Items.Add("Promote to Elite", null, elitePos);
                if ((settings.CurrentPlayer == Player.Player1 ? nCount.X : nCount.Y) > 0)
                    cm.Items.Add("Promote to Noble", null, noblePos);
                contextMenuOpen = true;
                cm.Closed += new ToolStripDropDownClosedEventHandler(cm_Closed);
                cm.Show(this, p);
            }
            else
            {
                turnState = TurnState.ConsumeSelectedCreatePawn;
                selection.AddRange(getAdjacent(selectedPiece, settings.Grid)
                    .Where(p => tiles[p.X][p.Y].TileType == TileType.Playable && 
                            tiles[p.X][p.Y].Piece == Piece.None && 
                            (settings.CurrentPlayer == Player.Player1 ? pCount.X : pCount.Y) > 0));
                if (selection.Count == 0)
                    cancelPromote(this, null);
            }
        }

        private void cm_Closed(object sender, ToolStripDropDownClosedEventArgs e)
        {
            if (e.CloseReason == ToolStripDropDownCloseReason.ItemClicked)
            {
                resetSelection();
                return;
            }
            cancelPromote(sender, e);
        }

        private void resetSelection()
        {
            selection.Clear();
            turnState = TurnState.NoSelection;
        }

        private void resetConsumedSelection()
        {
            if (consumedSelection == NULL_POINT) return;

            tiles[consumedSelection.X][consumedSelection.Y].TileType = TileType.Playable;
            consumedSelection = NULL_POINT;
        }

        private void leavingContextMenu(object sender, EventArgs e)
        {
            contextMenuOpen = false;
        }

        private void cancelPromote(object sender, EventArgs e)
        {
            tiles[consumedSelection.X][consumedSelection.Y].TileType = TileType.Playable;
            consumedSelection = NULL_POINT;
            resetSelection();
            leavingContextMenu(sender, e);
        }

        private void nextPlayer()
        {
            settings.CurrentPlayer = settings.CurrentPlayer == Player.Player1 ? Player.Player2 : Player.Player1;
            getCounts();
        }

        private void noblePos(object sender, EventArgs e)
        {
            if ((settings.CurrentPlayer == Player.Player1 ? nCount.X : nCount.Y) > 0)
            {
                tiles[cmp.X][cmp.Y].Player = settings.CurrentPlayer;
                tiles[cmp.X][cmp.Y].Piece = Piece.Noble;
                tiles[cmp.X][cmp.Y].TileType = TileType.Playable;
                cmp = NULL_POINT;
            }
            leavingContextMenu(sender, e);
            nextPlayer();
        }

        private void elitePos(object sender, EventArgs e)
        {
            if ((settings.CurrentPlayer == Player.Player1 ? eCount.X : eCount.Y) > 0)
            {
                tiles[cmp.X][cmp.Y].Player = settings.CurrentPlayer;
                tiles[cmp.X][cmp.Y].Piece = Piece.Elite;
                tiles[cmp.X][cmp.Y].TileType = TileType.Playable;
                cmp = NULL_POINT;
            }
            leavingContextMenu(sender, e);
            nextPlayer();
        }

        private void pawnPos(object sender, EventArgs e)
        {
            if ((settings.CurrentPlayer == Player.Player1 ? pCount.X : pCount.Y) > 0)
            {
                tiles[cmp.X][cmp.Y].Player = settings.CurrentPlayer;
                tiles[cmp.X][cmp.Y].Piece = Piece.Pawn;
                tiles[cmp.X][cmp.Y].TileType = TileType.Playable;
                cmp = NULL_POINT;
            }
            leavingContextMenu(sender, e);
            nextPlayer();
        }

        private void consumePos(object sender, EventArgs e)
        {
            tiles[cmp.X][cmp.Y].Player = Player.None;
            tiles[cmp.X][cmp.Y].Piece = Piece.None;
            tiles[cmp.X][cmp.Y].TileType = TileType.Consumed;
            cmp = NULL_POINT;
            leavingContextMenu(sender, e);
        }

        private void clearPos(object sender, EventArgs e)
        {
            tiles[cmp.X][cmp.Y].Piece = Piece.None;
            tiles[cmp.X][cmp.Y].TileType = TileType.Playable;
            cmp = NULL_POINT;
            leavingContextMenu(sender, e);
        }

        private void getCounts()
        {
            var pawns = tiles.SelectMany(t => t.Where(p => p.Piece == Piece.Pawn));
            var elites = tiles.SelectMany(t => t.Where(p => p.Piece == Piece.Elite));
            var nobles = tiles.SelectMany(t => t.Where(p => p.Piece == Piece.Noble));

            pCount = new Point(settings.MaxPawns - pawns.Count(p => p.Player == Player.Player1), settings.MaxPawns - pawns.Count(p => p.Player == Player.Player2));
            eCount = new Point(settings.MaxElites - elites.Count(p => p.Player == Player.Player1), settings.MaxElites - elites.Count(p => p.Player == Player.Player2));
            nCount = new Point(settings.MaxNobles - nobles.Count(p => p.Player == Player.Player1), settings.MaxNobles - nobles.Count(p => p.Player == Player.Player2));
        }

        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            getCounts();
            if (e.KeyChar == 'a')
            {
                debug = !debug;
                if (fpsTimer.Enabled)
                    fpsTimer.Stop();
                else
                    fpsTimer.Start();
            }
            else if (mouse == 1 && tiles[_oldp.X][_oldp.Y].TileType != TileType.OutOfBounds)
            {
                cmp = _oldp;
                switch (e.KeyChar)
                {
                    case '1':
                        settings.CurrentPlayer = Player.Player1;
                        break;
                    case '2':
                        settings.CurrentPlayer = Player.Player2;
                        break;
                    case 'c':
                    case '`':
                        consumePos(sender, e);
                        break;
                    case 'p':
                        pawnPos(sender, e);
                        break;
                    case 'e':
                        elitePos(sender, e);
                        break;
                    case 'n':
                        noblePos(sender, e);
                        break;
                    default:
                        clearPos(sender, e);
                        break;
                }
            }
            getCounts();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Modifiers == Keys.Control && e.KeyCode == Keys.Z)
            {
                var historyItem = history.Dequeue();

                // Undo
                if (!(historyItem is null))
                {
                    switch (historyItem.Action)
                    {
                        case Library.Action.Move:
                            tiles[historyItem.StartingSpace.X][historyItem.StartingSpace.Y].Player = historyItem.Player;
                            tiles[historyItem.StartingSpace.X][historyItem.StartingSpace.Y].Piece = historyItem.PieceType;
                            tiles[historyItem.EndingSpace.X][historyItem.EndingSpace.Y].Player = Player.None;
                            tiles[historyItem.EndingSpace.X][historyItem.EndingSpace.Y].Piece = Piece.None;
                            break;
                        case Library.Action.Kill:
                            tiles[historyItem.StartingSpace.X][historyItem.StartingSpace.Y].Player = historyItem.Player;
                            tiles[historyItem.StartingSpace.X][historyItem.StartingSpace.Y].Piece = historyItem.PieceType;
                            tiles[historyItem.EndingSpace.X][historyItem.EndingSpace.Y].Player = historyItem.Player;
                            tiles[historyItem.EndingSpace.X][historyItem.EndingSpace.Y].Piece = historyItem.EnemyPieceType;
                            break;
                        case Library.Action.PromotePawn:
                            tiles[historyItem.StartingSpace.X][historyItem.StartingSpace.Y].Player = historyItem.Player;
                            tiles[historyItem.StartingSpace.X][historyItem.StartingSpace.Y].Piece = Piece.Pawn;
                            tiles[historyItem.ConsumedSpace.X][historyItem.ConsumedSpace.Y].TileType = TileType.Playable;
                            break;
                        case Library.Action.CreatePawn:
                            tiles[historyItem.EndingSpace.X][historyItem.EndingSpace.Y].Player = Player.None;
                            tiles[historyItem.EndingSpace.X][historyItem.EndingSpace.Y].Piece = Piece.None;
                            tiles[historyItem.ConsumedSpace.X][historyItem.ConsumedSpace.Y].TileType = TileType.Playable;
                            break;
                    }
                }

                e.Handled = true;
            }
        }
    }
}
