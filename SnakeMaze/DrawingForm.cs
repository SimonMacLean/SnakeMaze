using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using SnakeMaze.Properties;

namespace SnakeMaze
{
    public partial class DrawingForm : Form
    {
        public static int TargetTotalPoints;
        public static int TotalPoints;
        public static int Points;
        public static Point Offset;
        public static bool CanloopX = false;
        public static bool MakingMaze;
        public static bool MazeMade;
        public static Point GridSize;
        public static bool UseTaxiCab = false;
        public static double PixelSize;
        public static int StartingSpotSize;
        public static int SpotSize;
        public static Random R = new Random();
        public static Spot[][] Grid;
        public static Stack<Spot> SpotStack;
        public static Snake PlayerSnake;
        public static int GameState;
        public static List<CustomButton> Buttons;
        public static Graphics G;
        public static Rectangle BoundsRectangle;
        public static Menu MainMenu;
        public static string LevelScores;
        public static Point MouseLocation;
        private static Spot _current;
        public static Store MainStore;
        public static double DirectDistanceBetween(bool useTaxiCab, int ax, int ay, int bx, int by)
        {
            if (useTaxiCab)
                return Math.Abs(ax - bx) + Math.Abs(ay - by);
            return Math.Sqrt(Math.Pow(ax - bx, 2) + Math.Pow(ay - by, 2));
        }
        public static double DistanceBetween(bool useTaxiCab, int ax, int ay, int bx, int by, int limitSizeX)
        {
            if (!CanloopX)
                return DirectDistanceBetween(useTaxiCab, ax, ay, bx, by);
            double normal = DirectDistanceBetween(useTaxiCab, ax, ay, bx, by);
            double loopedA = DirectDistanceBetween(useTaxiCab, ax + limitSizeX, ay, bx, by);
            double loopedB = DirectDistanceBetween(useTaxiCab, ax, ay, bx + limitSizeX, by);
            return loopedA < loopedB ? (loopedA < normal ? loopedA : normal) : (loopedB < normal ? loopedB : normal);
        }
        public static int RealPixels(int pixels)
        {
            return (int) (pixels * PixelSize);
        }
        public DrawingForm()
        {
            InitializeComponent();
            BoundsRectangle = ClientRectangle;
            PixelSize = BoundsRectangle.Width < BoundsRectangle.Height
                ? BoundsRectangle.Width / 600.0
                : BoundsRectangle.Height / 600.0;
            StartingSpotSize = RealPixels(150);
            SpotSize = RealPixels(150);
            LevelScores = Settings.Default.levelScores;
            GameState = 0;
            G = CreateGraphics();
            int i = 1;
            while (LevelScores[i - 1].ToString() != "c") i++;
            typeof(DrawingForm).InvokeMember("DoubleBuffered",
                BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic, null, this,
                new object[] {true});
            Buttons = new List<CustomButton>();
            CustomButton startButton =
                new CustomButton("startButton", Point.Empty, "START", Color.White, Color.DarkSlateBlue, Color.SlateBlue,
                        new Font(FontFamily.GenericMonospace, RealPixels(50)), G)
                    {Enabled = false};
            startButton.OnClick += OnStart;
            Buttons.Add(startButton);
            CustomButton retryButton =
                new CustomButton("retryButton", Point.Empty, "RETRY", Color.White, Color.DarkSlateBlue, Color.SlateBlue,
                    new Font(FontFamily.GenericMonospace, RealPixels(50)), G) {Enabled = false};
            retryButton.OnClick += OnDeath;
            Buttons.Add(retryButton);
            CustomButton nextButton =
                new CustomButton("nextButton", Point.Empty, "NEXT", Color.White, Color.DarkSlateBlue, Color.SlateBlue,
                    new Font(FontFamily.GenericMonospace, RealPixels(50)), G) {Enabled = false};
            nextButton.OnClick += NextLevel;
            Buttons.Add(nextButton);
            CustomButton levelButton =
                new CustomButton("levelButton", Point.Empty, "LEVELS", Color.White, Color.DarkSlateBlue,
                        Color.SlateBlue,
                        new Font(FontFamily.GenericMonospace, RealPixels(50)), G)
                    { Enabled = false };
            levelButton.OnClick += Levels;
            Buttons.Add(levelButton);
            CustomButton backLevelButton =
                new CustomButton("backLevelButton", Point.Empty, "BACK TO LEVELS", Color.White, Color.DarkSlateBlue,
                        Color.SlateBlue,
                        new Font(FontFamily.GenericMonospace, RealPixels(20)), G)
                    { Enabled = false };
            backLevelButton.OnClick += Levels;
            Buttons.Add(backLevelButton);
            CustomButton storeButton =
                new CustomButton("storeButton", Point.Empty, "STORE", Color.White, Color.DarkSlateBlue,
                        Color.SlateBlue,
                        new Font(FontFamily.GenericMonospace, RealPixels(20)), G)
                    { Enabled = false };
            storeButton.OnClick += GoToStore;
            Buttons.Add(storeButton);
            var t = new Timer
            {
                Enabled = true,
                Interval = 1
            };
            t.Tick += T_Tick;
            TotalPoints = Settings.Default.TotalPoints;
            TargetTotalPoints = TotalPoints;
            MainMenu = new Menu(i, BoundsRectangle);
            MainStore = new Store(BoundsRectangle);
        }
        public static void Init()
        {
            GridSize = new Point((BoundsRectangle.Width - RealPixels(20)) / SpotSize,
                (BoundsRectangle.Height - RealPixels(70)) / SpotSize);
            Offset = new Point(RealPixels(10), RealPixels(60));
            Grid = new Spot[GridSize.Y][];
            MakingMaze = true;
            for (int i = 0; i < Grid.Length; i++)
            {
                Grid[i] = new Spot[GridSize.X];
                for (int j = 0; j < Grid[i].Length; j++)
                    Grid[i][j] = new Spot(i, j);
            }
            foreach (Spot[] t in Grid)
            foreach (Spot t1 in t)
                t1.AddNeighbors(Grid);
            _current = Grid[0][0];
            SpotStack = new Stack<Spot>();
            MazeMade = false;
            Points = 0;
            while (!MazeMade)
            {
                if (MakingMaze && !MazeMade)
                {
                    Grid[_current.Y][_current.X].Visited = true;
                    Grid[_current.Y][_current.X].Current = false;
                    Spot randomNeighbor = _current.GetRandomNeighbor(Grid);
                    if (randomNeighbor != null)
                    {
                        SpotStack.Push(_current);
                        Spot[] cellsWithRemovedWalls =
                            Spot.ChangeWalls(_current, randomNeighbor, Grid[0].Length, Grid.Length, false);
                        Spot[] cellsWithAddedWalls = {null, null};
                        int xDif = _current.X - randomNeighbor.X;
                        int yDif = _current.Y - randomNeighbor.Y;
                        if (yDif != 0)
                            for (int i = -1; i < 2; i += 2)
                                if (_current.X + i >= 0 && _current.X + i < Grid[0].Length && randomNeighbor.Y >= 0 &&
                                    _current.Y - yDif < Grid[0].Length)
                                    if (Grid[_current.Y][_current.X].CanReach(Grid[_current.Y][_current.X + i]) &&
                                        Grid[randomNeighbor.Y][_current.X]
                                            .CanReach(Grid[randomNeighbor.Y][_current.X + i]) &&
                                        Grid[_current.Y][_current.X + i]
                                            .CanReach(Grid[randomNeighbor.Y][_current.X + i]))
                                        switch (R.Next(3))
                                        {
                                            case 0:
                                                cellsWithAddedWalls =
                                                    Spot.ChangeWalls(_current, Grid[_current.Y][_current.X + i],
                                                        Grid[0].Length, Grid.Length, true);
                                                break;
                                            case 1:
                                                cellsWithAddedWalls =
                                                    Spot.ChangeWalls(Grid[randomNeighbor.Y][_current.X],
                                                        Grid[randomNeighbor.Y][_current.X + i], Grid[0].Length,
                                                        Grid.Length,
                                                        true);
                                                break;
                                            case 2:
                                                cellsWithAddedWalls = Spot.ChangeWalls(Grid[_current.Y][_current.X + i],
                                                    Grid[randomNeighbor.Y][_current.X + i], Grid[0].Length, Grid.Length,
                                                    true);
                                                break;
                                        }
                        if (xDif != 0)
                            for (int i = -1; i < 2; i += 2)
                                if (_current.Y + i >= 0 && _current.Y + i < Grid.Length && randomNeighbor.X >= 0 &&
                                    randomNeighbor.X < Grid[0].Length)
                                    if (Grid[_current.Y][_current.X].CanReach(Grid[_current.Y + i][_current.X]) &&
                                        Grid[_current.Y][randomNeighbor.X]
                                            .CanReach(Grid[_current.Y + i][randomNeighbor.X]) &&
                                        Grid[_current.Y + i][_current.X]
                                            .CanReach(Grid[_current.Y + i][randomNeighbor.X]))
                                        switch (R.Next(3))
                                        {
                                            case 0:
                                                cellsWithAddedWalls =
                                                    Spot.ChangeWalls(_current, Grid[_current.Y + i][_current.X],
                                                        Grid[0].Length, Grid.Length, true);
                                                break;
                                            case 1:
                                                cellsWithAddedWalls =
                                                    Spot.ChangeWalls(Grid[_current.Y][randomNeighbor.X],
                                                        Grid[_current.Y + i][randomNeighbor.X], Grid[0].Length,
                                                        Grid.Length,
                                                        true);
                                                break;
                                            case 2:
                                                cellsWithAddedWalls = Spot.ChangeWalls(Grid[_current.Y + i][_current.X],
                                                    Grid[_current.Y + i][randomNeighbor.X], Grid[0].Length, Grid.Length,
                                                    true);
                                                break;
                                        }
                        Grid[cellsWithRemovedWalls[0].Y][cellsWithRemovedWalls[0].X] = cellsWithRemovedWalls[0];
                        Grid[cellsWithRemovedWalls[1].Y][cellsWithRemovedWalls[1].X] = cellsWithRemovedWalls[1];
                        if (cellsWithAddedWalls[0] != null && cellsWithAddedWalls[1] != null)
                        {
                            Grid[cellsWithAddedWalls[0].Y][cellsWithAddedWalls[0].X] = cellsWithAddedWalls[0];
                            Grid[cellsWithAddedWalls[1].Y][cellsWithAddedWalls[1].X] = cellsWithAddedWalls[1];
                        }
                        _current = Grid[randomNeighbor.Y][randomNeighbor.X];
                        Grid[_current.Y][_current.X].Current = true;
                    }
                    else if (SpotStack.Count != 0)
                    {
                        _current = SpotStack.Pop();
                        Grid[_current.Y][_current.X].Current = true;
                    }
                    else
                        MazeMade = true;
                }
                else
                    MakingMaze = true;
            }
            PlayerSnake = Grid[0][0].CanReach(Grid[0][1])
                ? new Snake(
                    new Point((SpotSize - Snake.SnakeSize) / 2 + Offset.X, (SpotSize - Snake.SnakeSize) / 2 + Offset.Y),
                    Snake.Direction.Right)
                : new Snake(
                    new Point((SpotSize - Snake.SnakeSize) / 2 + Offset.X, (SpotSize - Snake.SnakeSize) / 2 + Offset.Y),
                    Snake.Direction.Down);
            Snake.Colors = Store.AwardList[Settings.Default.CurrentColor];
        }
        private void T_Tick(object sender, EventArgs e)
        {
            Invalidate();
            if (TotalPoints < TargetTotalPoints)
                TotalPoints += 1;
            if (TotalPoints > TargetTotalPoints)
                TotalPoints -= 10;
            if (GameState != 2) return;
            PlayerSnake.Update();
            if (!Grid[Grid.Length - 1][Grid[0].Length - 1].WasEaten || GameState == 4) return;
            GameState = 4;
            TargetTotalPoints += 50;
        }
        private void DrawingForm_Paint(object sender, PaintEventArgs e)
        {
            if (GameState == 2)
            {
                PlayerSnake.Draw(e.Graphics);
                foreach (Spot[] t in Grid)
                foreach (Spot t1 in t)
                    t1.Draw(e.Graphics);
            }
            e.Graphics.DrawString("POINTS: " + TotalPoints,
                new Font(FontFamily.GenericMonospace, RealPixels(40), FontStyle.Bold),
                Brushes.White, RealPixels(5),
                RealPixels(5));
            if (GameState != 2)
            {
                Rectangle container = new Rectangle(BoundsRectangle.X + Offset.X, BoundsRectangle.Y + Offset.Y,
                    BoundsRectangle.Width - Offset.X * 2, BoundsRectangle.Height - Offset.Y - Offset.X);
                int betweenTextPadding = RealPixels(120);
                int topPadding = container.Height / 4;
                e.Graphics.DrawRectangle(new Pen(Color.White, RealPixels(6)), container);
                Point topLeft = new Point(container.Location.X, container.Location.Y);
                e.Graphics.FillRectangle(Brushes.SlateBlue,
                    new Rectangle(topLeft.X + 1, topLeft.Y + 1, container.Width - 1, container.Height - 1));
                if (GameState == 0)
                {
                    SizeF sizeLarge = G.MeasureString("MAZE SNAKE!",
                        new Font(FontFamily.GenericMonospace, RealPixels(80)));
                    SizeF sizeSmall = G.MeasureString("You have " + TotalPoints + " points",
                        new Font(FontFamily.GenericMonospace, RealPixels(40)));
                    e.Graphics.DrawString("MAZE SNAKE!", new Font(FontFamily.GenericMonospace, RealPixels(80)),
                        Brushes.White,
                        topLeft.X + (container.Width - sizeLarge.Width) / 2, topLeft.Y + topPadding);
                    string pointsScoredString = "You have " + TotalPoints + " points";
                    e.Graphics.DrawString(pointsScoredString, new Font(FontFamily.GenericMonospace, RealPixels(40)),
                        Brushes.White,
                        topLeft.X + (container.Width - sizeSmall.Width) / 2,
                        topLeft.Y + topPadding + betweenTextPadding);
                    EnableButton("startButton",
                        new Point(topLeft.X + container.Width / 2, topLeft.Y + topPadding + betweenTextPadding * 2));
                }
                if (GameState == 1)
                {
                    MainMenu.Draw(e.Graphics);
                    SizeF sizeLarge = G.MeasureString("STORE",
                        new Font(FontFamily.GenericMonospace, RealPixels(25)));
                    EnableButton("storeButton", new Point((int)(container.Width - topLeft.X - sizeLarge.Width/2), topLeft.Y));
                }
                if (GameState == 3)
                {
                    SizeF sizeLarge = G.MeasureString("GAME OVER",
                        new Font(FontFamily.GenericMonospace, RealPixels(80)));
                    SizeF sizeSmall = G.MeasureString("You Scored " + Points + " Points",
                        new Font(FontFamily.GenericMonospace, RealPixels(40)));
                    e.Graphics.DrawString("GAME OVER", new Font(FontFamily.GenericMonospace, RealPixels(80)),
                        Brushes.White,
                        topLeft.X + (container.Width - sizeLarge.Width) / 2, topLeft.Y + topPadding);
                    string pointsScoredString = "You Scored " + Points + " Points";
                    e.Graphics.DrawString(pointsScoredString, new Font(FontFamily.GenericMonospace, RealPixels(40)),
                        Brushes.White,
                        topLeft.X + (container.Width - sizeSmall.Width) / 2,
                        topLeft.Y + topPadding + betweenTextPadding);
                    EnableButton("retryButton",
                        new Point(topLeft.X + container.Width / 3, topLeft.Y + topPadding + betweenTextPadding * 2));
                    EnableButton("levelButton",
                        new Point(topLeft.X + 2 * container.Width / 3,
                            topLeft.Y + topPadding + betweenTextPadding * 2));
                }
                if (GameState == 4)
                {
                    int star = Grid.Length * Grid[0].Length * 10 / 3;
                    string pointsScoredString = string.Empty;
                    int stars = 0;
                    for (int i = 0; i < Points + 1 - star; i += star)
                    {
                        pointsScoredString += "★";
                        stars++;
                    }
                    while (pointsScoredString.Length < 3)
                    {
                        pointsScoredString += "☆";
                    }
                    SizeF sizeLarge = G.MeasureString("COMPLETED!",
                        new Font(FontFamily.GenericMonospace, RealPixels(80)));
                    SizeF sizeSmall = G.MeasureString(pointsScoredString,
                        new Font(FontFamily.GenericMonospace, RealPixels(40)));
                    e.Graphics.DrawString("COMPLETED!", new Font(FontFamily.GenericMonospace, RealPixels(80)),
                        Brushes.White,
                        topLeft.X + (container.Width - sizeLarge.Width) / 2, topLeft.Y + topPadding);
                    e.Graphics.DrawString(pointsScoredString, new Font(FontFamily.GenericMonospace, RealPixels(40)),
                        Brushes.White,
                        topLeft.X + (container.Width - sizeSmall.Width) / 2,
                        topLeft.Y + topPadding + betweenTextPadding);
                    EnableButton("retryButton",
                        new Point(topLeft.X + container.Width / 4,
                            topLeft.Y + topPadding + betweenTextPadding * 2));
                    EnableButton("nextButton",
                        new Point(topLeft.X + container.Width / 2, topLeft.Y + topPadding + betweenTextPadding * 2));
                    EnableButton("levelButton",
                        new Point(topLeft.X + 3 * container.Width / 4,
                            topLeft.Y + topPadding + betweenTextPadding * 2));
                    Settings.Default.levelScores =
                        LevelScores.Substring(0, (StartingSpotSize - SpotSize)/2) + stars +
                        (LevelScores.Substring((StartingSpotSize - SpotSize) / 2 + 1, 1) == "x"
                            ? "c"
                            : LevelScores.Substring((StartingSpotSize - SpotSize) / 2 + 1, 1)) +
                        LevelScores.Substring((StartingSpotSize - SpotSize) / 2 + 2);
                }
                if (GameState == 5)
                {
                    MainStore.Draw(e.Graphics);
                    SizeF sizeLarge = G.MeasureString("BACK TO LEVELS",
                        new Font(FontFamily.GenericMonospace, RealPixels(20)));
                    EnableButton("backLevelButton", new Point((int)(container.Width + topLeft.X - sizeLarge.Width/2 - RealPixels(25)), topLeft.Y));
                }
            }
            foreach (CustomButton c in Buttons)
            {
                if (c.Enabled)
                    c.Draw(e.Graphics);
            }
        }
        private static void NextLevel(object sender, EventArgs eventArgs)
        {
            foreach (CustomButton c in Buttons)
                c.Enabled = false;
            SpotSize -= 1;
            GameState = 2;
            Points = 0;
            Settings.Default.Save();
            LevelScores = Settings.Default.levelScores;
            Init();
        }
        public void OnDeath(object sender, EventArgs eventArgs)
        {
            //TotalPoints -= Points;
            Points = 0;
            foreach (CustomButton c in Buttons)
                c.Enabled = false;
            PlayerSnake = Grid[0][0].CanReach(Grid[0][1])
                ? new Snake(
                    new Point((SpotSize - Snake.SnakeSize) / 2 + Offset.X, (SpotSize - Snake.SnakeSize) / 2 + Offset.Y),
                    Snake.Direction.Right)
                : new Snake(
                    new Point((SpotSize - Snake.SnakeSize) / 2 + Offset.X, (SpotSize - Snake.SnakeSize) / 2 + Offset.Y),
                    Snake.Direction.Down);
            foreach (Spot[] s1 in Grid)
            foreach (Spot s in s1)
                s.WasEaten = false;
            GameState = 2;
        }
        public void OnStart(object sender, EventArgs eventArgs)
        {
            foreach (CustomButton c in Buttons)
                c.Enabled = false;
            Init();
            GameState = 1;
        }
        private static void Levels(object sender, EventArgs eventArgs)
        {
            foreach (CustomButton c in Buttons)
                c.Enabled = false;
            GameState = 1;
            Settings.Default.Save();
            LevelScores = Settings.Default.levelScores;
        }
        private static void GoToStore(object sender, EventArgs eventArgs)
        {
            foreach (CustomButton c in Buttons)
                c.Enabled = false;
            GameState = 5;
            Settings.Default.Save();
        }
        private void DrawingForm_KeyDown(object sender, KeyEventArgs e)
        {
            switch (GameState)
            {
                case 2:
                    switch (e.KeyCode)
                    {
                        case Keys.Up:
                            if (PlayerSnake.MovingDirection != Snake.Direction.Down)
                                PlayerSnake.MovingDirection = Snake.Direction.Up;
                            break;
                        case Keys.Right:
                            if (PlayerSnake.MovingDirection != Snake.Direction.Left)
                                PlayerSnake.MovingDirection = Snake.Direction.Right;
                            break;
                        case Keys.Down:
                            if (PlayerSnake.MovingDirection != Snake.Direction.Up)
                                PlayerSnake.MovingDirection = Snake.Direction.Down;
                            break;
                        case Keys.Left:
                            if (PlayerSnake.MovingDirection != Snake.Direction.Right)
                                PlayerSnake.MovingDirection = Snake.Direction.Left;
                            break;
                    }
                    break;
                case 1:
                    switch (e.KeyCode)
                    {
                        case Keys.Right:
                            if (MainMenu.Level < LevelScores.Length)
                                MainMenu.Level++;
                            break;
                        case Keys.Left:
                            if (MainMenu.Level > 1)
                                MainMenu.Level--;
                            break;
                    }
                    break;
                case 5:
                    switch (e.KeyCode)
                    {
                        case Keys.Right:
                            if (MainStore.CurrentItem < Store.AwardList.Count - 1)
                                MainStore.CurrentItem++;
                            break;
                        case Keys.Left:
                            if (MainStore.CurrentItem >= 1)
                                MainStore.CurrentItem--;
                            break;
                    }
                    break;
            }
        }
        private static void EnableButton(string name, Point center)
        {
            foreach (CustomButton c in Buttons)
            {
                if (c.Name != name) continue;
                c.Enabled = true;
                SizeF measureString = G.MeasureString(c.Text, c.Font);
                c.Location = center - new Size((int) measureString.Width / 2, (int) measureString.Height / 2);
            }
        }
        private void DrawingForm_MouseMove(object sender, MouseEventArgs e)
        {
            foreach (CustomButton c in Buttons)
            {
                if (!c.Enabled) continue;
                c.Update(e.Location);
            }
            MouseLocation = e.Location;
        }
        private void DrawingForm_Click(object sender, EventArgs e)
        {
            if (GameState == 1)
            {
                MainMenu.OnClick(MouseLocation);
            }
            if (GameState == 5)
            {
                MainStore.OnClick(MouseLocation);
            }
            foreach (CustomButton c in Buttons)
            {
                if (!c.Enabled || !c.Hovering) continue;
                c.OnClick.Invoke(sender, e);
            }
        }
        private void DrawingForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Settings.Default.TotalPoints = TotalPoints;
            Settings.Default.Save();
        }
    }

    public class Spot
    {
        public bool WasEaten;
        private const int WallSize = 2;
        private const int CanGoThroughWallsChance = 1000;
        public int X;
        public int Y;
        public double BestDistance;
        public double DistanceMoved;
        public double DistanceFromEnd;
        public Spot Previous;
        public Rectangle FillingRectangle = new Rectangle();
        public static readonly Random R = new Random();
        public bool Visited;
        public bool Current;
        private readonly bool[] _wallsActive = {true, true, true, true}; //Top right bottom left
        public readonly Point[] DrawVerticies;
        public Point[] FillVerticies { get; }
        public List<Spot> Neighbors;
        public Point CenterPoint;
        public Spot(int i, int j)
        {
            X = j;
            Y = i;
            int sideLength = DrawingForm.SpotSize;
            Point offset = DrawingForm.Offset;
            DrawVerticies = new[]
            {
                new Point(X * sideLength + offset.X, Y * sideLength + offset.Y),
                new Point(X * sideLength + offset.X, (Y + 1) * sideLength + offset.Y),
                new Point((X + 1) * sideLength + offset.X, (Y + 1) * sideLength + offset.Y),
                new Point((X + 1) * sideLength + offset.X, Y * sideLength + offset.Y)
            };
            FillVerticies = new[]
            {
                new Point(X * sideLength + 1 + offset.X, Y * sideLength + 1 + offset.Y),
                new Point(X * sideLength + 1 + offset.X, (Y + 1) * sideLength + offset.Y),
                new Point((X + 1) * sideLength + offset.X, (Y + 1) * sideLength + offset.Y),
                new Point((X + 1) * sideLength + offset.X, Y * sideLength + 1 + offset.Y)
            };
            CenterPoint = new Point(DrawVerticies[0].X + DrawingForm.SpotSize / 2,
                DrawVerticies[0].Y + DrawingForm.SpotSize / 2);
        }
        public void AddNeighbors(Spot[][] grid)
        {
            Neighbors = new List<Spot>();
            if (DrawingForm.CanloopX)
            {
                if (Y < grid.Length - 1)
                {
                    Neighbors.Add(grid[Y + 1][X]);
                    if (!DrawingForm.UseTaxiCab)
                    {
                        Neighbors.Add(grid[Y + 1][(X - 1 + grid[0].Length) % grid[0].Length]);
                        Neighbors.Add(grid[Y + 1][(X + 1) % grid[0].Length]);
                    }
                }
                if (Y > 0)
                {
                    Neighbors.Add(grid[Y - 1][X]);
                    if (!DrawingForm.UseTaxiCab)
                    {
                        Neighbors.Add(grid[Y - 1][(X - 1 + grid[0].Length) % grid[0].Length]);
                        Neighbors.Add(grid[Y - 1][(X + 1) % grid[0].Length]);
                    }
                }
                Neighbors.Add(grid[Y][(X + 1) % grid[0].Length]);
                Neighbors.Add(grid[Y][(X - 1 + grid[0].Length) % grid[0].Length]);
            }
            else
            {
                if (Y < grid.Length - 1)
                    Neighbors.Add(grid[Y + 1][X]);
                if (Y > 0)
                    Neighbors.Add(grid[Y - 1][X]);
                if (X < grid[Y].Length - 1)
                    Neighbors.Add(grid[Y][X + 1]);
                if (X > 0)
                    Neighbors.Add(grid[Y][X - 1]);
                if (DrawingForm.UseTaxiCab) return;
                if (Y < grid.Length - 1 && X < grid[Y].Length - 1)
                    Neighbors.Add(grid[Y + 1][X + 1]);
                if (Y > 0 && X < grid[Y].Length - 1)
                    Neighbors.Add(grid[Y - 1][X + 1]);
                if (Y < grid.Length - 1 && X > 0)
                    Neighbors.Add(grid[Y + 1][X - 1]);
                if (Y > 0 && X > 0)
                    Neighbors.Add(grid[Y - 1][X - 1]);
            }
        }
        public Spot GetRandomNeighbor(Spot[][] grid)
        {
            Neighbors = new List<Spot>();
            if (DrawingForm.CanloopX)
            {
                if (!grid[Y][(X + grid[Y].Length - 1) % grid[Y].Length].Visited || R.Next(CanGoThroughWallsChance) == 0)
                    Neighbors.Add(grid[Y][(X + grid[Y].Length - 1) % grid[Y].Length]);
                if (Y < grid.Length - 1 && (!grid[(Y + 1) % grid.Length][X].Visited ||
                                            R.Next(CanGoThroughWallsChance) == 0))
                    Neighbors.Add(grid[(Y + 1) % grid.Length][X]);
                if (!grid[Y][(X + 1) % grid[Y].Length].Visited || R.Next(CanGoThroughWallsChance) == 0)
                    Neighbors.Add(grid[Y][(X + 1) % grid[Y].Length]);
                if (Y > 0 && (!grid[(Y + grid.Length - 1) % grid.Length][X].Visited ||
                                R.Next(CanGoThroughWallsChance) == 0))
                    Neighbors.Add(grid[(Y + grid.Length - 1) % grid.Length][X]);
                return Neighbors.Count > 0 ? Neighbors[R.Next(Neighbors.Count)] : null;
            }
            if (X > 0 && (!grid[Y][X - 1].Visited || R.Next(CanGoThroughWallsChance) == 0))
                Neighbors.Add(grid[Y][X - 1]);
            if (Y < grid.Length - 1 && (!grid[Y + 1][X].Visited || R.Next(CanGoThroughWallsChance) == 0))
                Neighbors.Add(grid[Y + 1][X]);
            if (X < grid[Y].Length - 1 && (!grid[Y][X + 1].Visited || R.Next(CanGoThroughWallsChance) == 0))
                Neighbors.Add(grid[Y][X + 1]);
            if (Y > 0 && (!grid[Y - 1][X].Visited || R.Next(CanGoThroughWallsChance) == 0))
                Neighbors.Add(grid[Y - 1][X]);
            return Neighbors.Count > 0 ? Neighbors[R.Next(Neighbors.Count)] : null;
        }
        public void Draw(Graphics canvas)
        {
            for (int i = 0; i < _wallsActive.Length; i++)
            {
                if (_wallsActive[i])
                    canvas.DrawLine(new Pen(Color.White, WallSize),
                        DrawVerticies[i] + new Size(1, 1),
                        DrawVerticies[(i + 1) % DrawVerticies.Length] + new Size(1, 1));
            }
            foreach (Point t in DrawVerticies)
                canvas.FillRectangle(Brushes.White, t.X, t.Y, WallSize, WallSize);
            if (!WasEaten)
            {
                canvas.FillEllipse(
                    X == DrawingForm.Grid[DrawingForm.Grid.Length - 1][DrawingForm.Grid[0].Length - 1].X && Y ==
                    DrawingForm.Grid[DrawingForm.Grid.Length - 1][DrawingForm.Grid[0].Length - 1].Y
                        ? Brushes.Yellow
                        : Brushes.White, CenterPoint.X - Snake.SnakeSize / 2,
                    CenterPoint.Y - Snake.SnakeSize / 2, Snake.SnakeSize, Snake.SnakeSize);
            }
        }
        public static Spot[] ChangeWalls(Spot a, Spot b, int gridSizeY, int gridSizeX, bool wallSetting)
        {
            int xDif = a.X - b.X;
            int yDif = a.Y - b.Y;
            if (yDif != 0)
                if (yDif < 2 && yDif > -2)
                {
                    a._wallsActive[2 + yDif] = wallSetting;
                    b._wallsActive[2 - yDif] = wallSetting;
                }
                else if (yDif == -1 * gridSizeY + 1)
                {
                    a._wallsActive[3] = wallSetting;
                    b._wallsActive[1] = wallSetting;
                }
                else if (yDif == gridSizeY - 1)
                {
                    a._wallsActive[1] = wallSetting;
                    b._wallsActive[3] = wallSetting;
                }
            if (xDif != 0)
                if (xDif < 2 && xDif > -2)
                {
                    a._wallsActive[1 - xDif] = wallSetting;
                    b._wallsActive[1 + xDif] = wallSetting;
                }
                else if (xDif == -1 * gridSizeX + 1)
                {
                    a._wallsActive[0] = wallSetting;
                    b._wallsActive[2] = wallSetting;
                }
                else if (xDif == gridSizeX - 1)
                {
                    a._wallsActive[2] = wallSetting;
                    b._wallsActive[0] = wallSetting;
                }
            Spot[] result = {a, b};
            return result;
        }
        public bool CanReach(Spot other)
        {
            if (DrawingForm.UseTaxiCab &&
                DrawingForm.DistanceBetween(true, X, Y, other.X, other.Y, DrawingForm.GridSize.X) > 1)
                return false;
            if (!DrawingForm.UseTaxiCab &&
                DrawingForm.DistanceBetween(false, X, Y, other.X, other.Y, DrawingForm.GridSize.X) > Math.Sqrt(2))
                return false;
            int xDif = X - other.X;
            int yDif = Y - other.Y;
            if (yDif != 0)
            {
                if (yDif < 2 && yDif > -2 && !_wallsActive[2 + yDif] && !other._wallsActive[2 - yDif])
                    return true;
                if (yDif == -1 * DrawingForm.GridSize.Y + 1 && !_wallsActive[3] &&
                    !other._wallsActive[1])
                    return true;
                if (yDif == DrawingForm.GridSize.Y - 1 && !_wallsActive[1] && !other._wallsActive[3])
                    return true;
            }
            if (xDif == 0) return false;
            if (Math.Abs(xDif) < 2 && !_wallsActive[1 - xDif] && !other._wallsActive[1 + xDif])
                return true;
            if (xDif == -1 * DrawingForm.GridSize.Y + 1 && !_wallsActive[0] && !other._wallsActive[2])
                return true;
            return xDif == DrawingForm.GridSize.Y - 1 && !_wallsActive[2] && !other._wallsActive[0];
        }
    }

    public class PointD
    {
        public double X;
        public double Y;
        public Point Point => new Point((int) X, (int) Y);
        public PointD(double x, double y)
        {
            X = x;
            Y = y;
        }
        public static PointD operator +(PointD p, PointD p2)
        {
            return new PointD(p.X + p2.X, p.Y + p2.Y);
        }
    }

    public class Snake
    {
        public enum Direction
        {
            Up,
            Right,
            Down,
            Left
        }
        public Direction MovingDirection;
        public int Speed = 2;
        public int TicksPassed;
        public static List<PointD> Segments;
        public static List<Color> Colors;
        public static int SnakeSize = 12;
        public Spot CurrentSpot;
        public int MovementDistance = 5;
        public Snake(Point location, Direction startingDirection)
        {
            Segments = new List<PointD> {new PointD(location.X, location.Y)};
            Colors = new List<Color>();
            MovingDirection = startingDirection;
            for (int i = 1; i <= 48 / MovementDistance; i++)
            {
                switch (MovingDirection)
                {
                    case Direction.Down:
                        Segments.Add(Segments[0] + new PointD(0, -1 * i * DrawingForm.PixelSize * MovementDistance));
                        break;
                    case Direction.Left:
                        Segments.Add(Segments[0] + new PointD(i * DrawingForm.PixelSize * MovementDistance, 0));
                        break;
                    case Direction.Right:
                        Segments.Add(Segments[0] + new PointD(-1 * i * DrawingForm.PixelSize * MovementDistance, 0));
                        break;
                    case Direction.Up:
                        Segments.Add(Segments[0] + new PointD(0, i * DrawingForm.PixelSize * MovementDistance));
                        break;
                }
            }
            Inside(DrawingForm.Grid[0][0]);
            CurrentSpot = DrawingForm.Grid[0][0];
            foreach (PointD dummy in Segments)
            {
                Colors.Add(Color.FromArgb(192, 192, 255));
            }
        }
        public void Draw(Graphics g)
        {
            for (int i = 0; i < Segments.Count; i++)
            {
                PointD p = Segments[Segments.Count - 1 - i];
                g.FillEllipse(new SolidBrush(Colors[(Segments.Count - 1 - i) % Colors.Count]), p.Point.X - 1,
                    p.Point.Y - 1,
                    SnakeSize + 1,
                    SnakeSize + 1);
            }
        }
        public void Update()
        {
            TicksPassed = (TicksPassed + 1) % Speed;
            if (TicksPassed != 0) return;
            switch (MovingDirection)
            {
                case Direction.Down:
                    Segments.Remove(Segments[Segments.Count - 1]);
                    Segments.Insert(0, Segments[0] + new PointD(0, DrawingForm.PixelSize * MovementDistance));
                    break;
                case Direction.Left:
                    Segments.Remove(Segments[Segments.Count - 1]);
                    Segments.Insert(0, Segments[0] + new PointD(-1 * DrawingForm.PixelSize * MovementDistance, 0));
                    break;
                case Direction.Right:
                    Segments.Remove(Segments[Segments.Count - 1]);
                    Segments.Insert(0, Segments[0] + new PointD(DrawingForm.PixelSize * MovementDistance, 0));
                    break;
                case Direction.Up:
                    Segments.Remove(Segments[Segments.Count - 1]);
                    Segments.Insert(0, Segments[0] + new PointD(0, -1 * DrawingForm.PixelSize * MovementDistance));
                    break;
            }
            bool inNewSpot = false;
            Spot q = CurrentSpot;
            if (!FullyInside(CurrentSpot))
            {
                foreach (Spot[] s1 in DrawingForm.Grid)
                {
                    foreach (Spot s in s1)
                    {
                        if (!Inside(s) || s.X == CurrentSpot.X && s.Y == CurrentSpot.Y || s.X == q.X && s.Y == q.Y) continue;
                        if (!CurrentSpot.CanReach(s))
                        {
                            DrawingForm.GameState = 3;
                        }
                        inNewSpot = true;
                        CurrentSpot = s;
                    }
                }
                if (!inNewSpot)
                    DrawingForm.GameState = 3;
            }
            if (Overlapping())
                DrawingForm.GameState = 3;
            if (!(DrawingForm.DirectDistanceBetween(false, (int) Segments[0].X + SnakeSize / 2,
                        (int) Segments[0].Y + SnakeSize / 2,
                        CurrentSpot.CenterPoint.X, CurrentSpot.CenterPoint.Y) <= SnakeSize) ||
                CurrentSpot.WasEaten) return;
            DrawingForm.Grid[CurrentSpot.Y][CurrentSpot.X].WasEaten = true;
            CurrentSpot.WasEaten = true;
            DrawingForm.Points += 10;
            DrawingForm.TargetTotalPoints += 10;
            bool adding = false;
            Color lastColor = Colors[Colors.Count - 1];
            for (int i = 6; Colors[i - 6] != lastColor; i++)
            {
                if (adding)
                {
                    Colors.Add(Colors[i]);
                }
                if (Colors[i].ToArgb() == lastColor.ToArgb())
                {
                    adding = true;
                }
            }
            for (int i = 1; i <= 5 / MovementDistance; i++)
            {
                Segments.Add(Segments[Segments.Count - 1] +
                                new PointD(Segments[Segments.Count - 2].X - Segments[Segments.Count - 1].X,
                                    Segments[Segments.Count - 2].Y - Segments[Segments.Count - 1].Y));
            }
        }
        public bool Inside(Spot s)
        {
            return !(Segments[0].X + SnakeSize <= s.DrawVerticies[0].X || s.DrawVerticies[2].X <= Segments[0].X ||
                        Segments[0].Y + SnakeSize <= s.DrawVerticies[0].Y || s.DrawVerticies[2].Y <= Segments[0].Y);
        }
        public bool FullyInside(Spot s)
        {
            Rectangle r = new Rectangle(s.DrawVerticies[0].X, s.DrawVerticies[0].Y, DrawingForm.SpotSize,
                DrawingForm.SpotSize);
            return r.Contains(Segments[0].Point) &&
                    r.Contains(Segments[0].Point + new Size(SnakeSize, SnakeSize));
        }
        public bool Overlapping()
        {
            for (int i = 1; i < Segments.Count; i++)
            {
                if (Segments[0].X == Segments[i].X && Segments[0].Y == Segments[i].Y)
                {
                    return true;
                }
            }
            return false;
        }
    }

    public class CustomButton
    {
        public string Name;
        public Point Location;
        public readonly string Text;
        private readonly Color _outside;
        private readonly Color _inside;
        public readonly Font Font;
        private SizeF _textSize;
        public bool Hovering;
        public EventHandler OnClick;
        private readonly Color _insideHover;
        public bool Enabled;
        public CustomButton(string name, int x, int y, string text, Color outerRim, Color insideFill, Font font,
            Graphics createdGraphics)
        {
            Name = name;
            Location = new Point(x, y);
            Text = text;
            _outside = outerRim;
            _inside = insideFill;
            Font = font;
            SizeF measureString = createdGraphics.MeasureString(Text, Font);
            _textSize = measureString;
            _insideHover = insideFill;
        }
        public CustomButton(string name, Point center, string text, Color outerRim, Color insideFill, Font font,
            Graphics createdGraphics)
        {
            Name = name;
            Font = font;
            Text = text;
            _outside = outerRim;
            _inside = insideFill;
            SizeF measureString = createdGraphics.MeasureString(Text, Font);
            Location = center - new Size((int) measureString.Width / 2, (int) measureString.Height / 2);
            _textSize = measureString;
            _insideHover = insideFill;
        }
        public CustomButton(string name, int x, int y, string text, Color outerRim, Color insideFill, Color insideHover,
            Font font, Graphics createdGraphics)
        {
            Name = name;
            Location = new Point(x, y);
            Text = text;
            _outside = outerRim;
            _inside = insideFill;
            Font = font;
            SizeF measureString = createdGraphics.MeasureString(Text, Font);
            _textSize = measureString;
            _insideHover = insideHover;
        }
        public CustomButton(string name, Point center, string text, Color outerRim, Color insideFill, Color insideHover,
            Font font, Graphics createdGraphics)
        {
            Name = name;
            Font = font;
            Text = text;
            _outside = outerRim;
            _inside = insideFill;
            SizeF measureString = createdGraphics.MeasureString(Text, Font);
            Location = center - new Size((int) measureString.Width / 2, (int) measureString.Height / 2);
            _textSize = measureString;
            _insideHover = insideHover;
        }
        public void Draw(Graphics g)
        {
            g.FillRectangle(Hovering ? new SolidBrush(_insideHover) : new SolidBrush(_inside), Location.X - 5,
                Location.Y - 5, _textSize.Width + 10, _textSize.Height + 10);
            g.DrawRectangle(new Pen(_outside, 5), Location.X - 7, Location.Y - 7, _textSize.Width + 14,
                _textSize.Height + 14);
            g.DrawString(Text, Font, new SolidBrush(_outside), Location);
        }
        public void Update(Point mouseLocation)
        {
            Rectangle r = new Rectangle(Location.X - 7, Location.Y - 7, (int) _textSize.Width + 14,
                (int) _textSize.Height + 14);
            Hovering = r.Contains(mouseLocation);
        }
    }

    public class Menu
    {
        public int Level;
        private readonly Rectangle _levelRectangle;
        public Menu(int level, Rectangle bounds)
        {
            Level = level;
            var boundsRectangle = bounds;
            _levelRectangle = new Rectangle(boundsRectangle.Width / 4,
                boundsRectangle.Height / 4 + DrawingForm.Offset.Y / 2, boundsRectangle.Width / 2,
                boundsRectangle.Height / 2);
        }
        public void Draw(Graphics g)
        {
            g.DrawRectangle(new Pen(Color.White, 5), _levelRectangle);
            SizeF sizeLarge = DrawingForm.G.MeasureString("Level " + Level, new Font(FontFamily.GenericMonospace, 40));
            switch (DrawingForm.LevelScores[Level - 1].ToString())
            {
                case "x":
                    g.FillRectangle(Brushes.Gray, _levelRectangle);
                    break;
                case "c":
                    g.FillRectangle(Brushes.DarkSlateBlue, _levelRectangle);
                    break;
                default:
                    g.FillRectangle(Brushes.SlateBlue, _levelRectangle);
                    break;

            }
            g.DrawString("Level " + Level, new Font(FontFamily.GenericMonospace, 40), Brushes.White,
                _levelRectangle.X + (_levelRectangle.Width - sizeLarge.Width) / 2, _levelRectangle.Y + 100);
            int stars;
            string pointsScoredString = string.Empty;
            if (int.TryParse(DrawingForm.LevelScores[Level - 1].ToString(), out stars))
            {
                for (int i = 0; i < stars; i += 1)
                {
                    pointsScoredString += "★";
                }
                while (pointsScoredString.Length < 3)
                {
                    pointsScoredString += "☆";
                }
            }
            SizeF sizeSmall =
                DrawingForm.G.MeasureString(pointsScoredString, new Font(FontFamily.GenericMonospace, 40));
            g.DrawString(pointsScoredString, new Font(FontFamily.GenericMonospace, 40), Brushes.White,
                _levelRectangle.X + (_levelRectangle.Width - sizeSmall.Width) / 2, _levelRectangle.Y + 160);
        }
        public void OnClick(Point mousePoint)
        {
            if (!_levelRectangle.Contains(mousePoint) || DrawingForm.LevelScores[Level - 1].ToString() == "x") return;
            DrawingForm.SpotSize = DrawingForm.StartingSpotSize + 2 - 2 * Level;
            DrawingForm.GameState = 2;
            DrawingForm.Init();
        }
    }

    public class Store
    {
        public static List<List<Color>> AwardList;
        public static List<bool> EnabledList;
        private static Rectangle _storeRectangle;
        public int BestItem;
        public int CurrentItem;
        public static List<int> CostList;
        public Store(Rectangle bounds)
        {
            EnabledList = new List<bool>();
            AwardList = ColorsFrom(Settings.Default.RewardString);
            foreach (char c in Settings.Default.EnabledString)
            {
                EnabledList.Add(c.ToString() == "1");
            }
            var boundsRectangle = bounds;
            _storeRectangle = new Rectangle(boundsRectangle.Width / 4,
                boundsRectangle.Height / 4 + DrawingForm.Offset.Y / 2, boundsRectangle.Width / 2,
                boundsRectangle.Height / 2);
            BestItem = Settings.Default.CurrentColor;
            CurrentItem = BestItem;
            string costString = Settings.Default.Costs;
            costString = costString.Substring(1);
            CostList = new List<int>();
            while (costString.Length > 0)
            {
                string s = string.Empty;
                while (costString[0] != ' ')
                {
                    s += costString[0].ToString();
                    costString = costString.Substring(1);
                }
                costString = costString.Substring(1);
                CostList.Add(int.Parse(s));
            }
        }
        public static List<List<Color>> ColorsFrom(string colorText)
        {
            List<List<Color>> result = new List<List<Color>>();
            List<string> intermediateStep = new List<string>();
            while (colorText.Length > 0)
            {
                colorText = colorText.Substring(1);
                string s = string.Empty;
                while (colorText[0] != ']')
                {
                    s += colorText[0].ToString();
                    colorText = colorText.Substring(1);
                }
                colorText = colorText.Substring(2);
                intermediateStep.Add(s);
            }
            foreach (string b in intermediateStep)
            {
                result.Add(new List<Color>());
                string c = b;
                c = c.Substring(1);
                while (c.Length > 0)
                {
                    string s = string.Empty;
                    while (c[0] != ' ')
                    {
                        s += c[0].ToString();
                        c = c.Substring(1);
                    }
                    c = c.Substring(1);
                    result[result.Count - 1].Add(Color.FromArgb(int.Parse(s)));
                }
            }
            return result;
        }
        public void Draw(Graphics g)
        {
            g.DrawRectangle(new Pen(Color.White, DrawingForm.RealPixels(10)), _storeRectangle);
            SizeF sizeLarge = DrawingForm.G.MeasureString(CostList[CurrentItem].ToString(),
                new Font(FontFamily.GenericMonospace, DrawingForm.RealPixels(80)));
            g.FillRectangle(EnabledList[CurrentItem] ? CurrentItem == Settings.Default.CurrentColor? Brushes.DarkSlateBlue: Brushes.SlateBlue : Brushes.Gray, _storeRectangle);
            if (!EnabledList[CurrentItem])
                g.DrawString(CostList[CurrentItem].ToString(),
                    new Font(FontFamily.GenericMonospace, DrawingForm.RealPixels(80)), Brushes.White,
                    _storeRectangle.X + (_storeRectangle.Width - sizeLarge.Width) / 2, _storeRectangle.Y + 100);
            Point exampleStart = new Point(_storeRectangle.X + _storeRectangle.Width/2 - 12 * DrawingForm.PlayerSnake.MovementDistance - Snake.SnakeSize, _storeRectangle.Y + _storeRectangle.Height / 2 - Snake.SnakeSize - (EnabledList[CurrentItem]  ? 0 : DrawingForm.RealPixels(60)));
            for (int i = 0; i < 24; i++)
            {
                g.FillEllipse(
                    new SolidBrush(AwardList[CurrentItem][AwardList[CurrentItem].Count - 1 - i % AwardList[CurrentItem].Count]),
                    exampleStart.X + i * DrawingForm.PlayerSnake.MovementDistance, exampleStart.Y,
                    Snake.SnakeSize * 2 + 1,
                    Snake.SnakeSize * 2 + 1);
            }
        }
        public void OnClick(Point mousePoint)
        {
            if (!_storeRectangle.Contains(mousePoint) || CostList[CurrentItem] > DrawingForm.TargetTotalPoints) return;
            BestItem = CurrentItem;
            if(!EnabledList[CurrentItem])
                DrawingForm.TargetTotalPoints -= CostList[BestItem];
            EnabledList[CurrentItem] = true;
            Settings.Default.CurrentColor = BestItem;
            Settings.Default.EnabledString = EnabledList.Aggregate(string.Empty, (current, b) => current + (b ? "1" : "0"));
            Snake.Colors = AwardList[Settings.Default.CurrentColor];
        }
    }
}