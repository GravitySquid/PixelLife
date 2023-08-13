using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Drawing;
using System.Runtime.InteropServices;
using System.IO;
using System.ComponentModel;
using Color = System.Drawing.Color;
using System.Drawing.Drawing2D;

namespace PixelLife
{
    /// <summary>
    /// Pixel Life Simulator
    /// </summary>

    public partial class MainWindow : Window
    {
        BitmapSource universeSource;
        Bitmap universe;
        Random random = new Random();
        int stateCounter = 0;
        bool pausedState = true;
        System.Drawing.Color defaultColor;
        Graphics bitmapGraphics;
        System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
        decimal PopulationDensityPercentage = 8;
        private bool _setupComplete = false;
        int Underpopulation = 1;
        int Overpopulation = 5;
        int Birth = 3;

        // CONSTANTS
        const int WIDTH = 400;
        const int HEIGHT = 400;

        // Representations
        Cell[,] LifeMatrix, PrevLifeMatrix;

        public MainWindow()
        {
            InitializeComponent();

            defaultColor = Color.Wheat;
            LifeMatrix = new Cell[WIDTH, HEIGHT];
            PrevLifeMatrix = new Cell[WIDTH, HEIGHT];
            resetMatrix();

            universe = new System.Drawing.Bitmap(WIDTH, HEIGHT);
            updateBitmap();

            // Timer to update universe
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 1000);
            dispatcherTimer.Start();
            _setupComplete = true;
            this.Top = 0;
            this.Left = 100;
        }

        private void resetMatrix()
        {
            // RESET the PixelLife matrix with new cells
            // - Use Population Density Percentage to determine life
            // - Set random colour for each life pixel 
            int randInt, lifeExpectancy;
            for (int i = 0; i < WIDTH; i++)
            {
                for (int j = 0; j < HEIGHT; j++)
                {
                    randInt = random.Next(1, 1000);
                    if ((decimal)(randInt / 10) <= PopulationDensityPercentage)
                    {
                        LifeMatrix[i, j] = new Cell(1);
                    }
                    else
                        LifeMatrix[i, j] = new Cell(0);
                }
            }
            stateCounter = 0;
            textBlock1.Text = "State Counter = " + stateCounter.ToString();
        }


        private void updateBitmap()
        {
            // Update Bitmap from PixelLife Matrix
            for (int i = 0; i < WIDTH; i++)
            {
                for (int j = 0; j < HEIGHT; j++)
                {
                    if (LifeMatrix[i, j].State == 1)
                        universe.SetPixel(i, j, LifeMatrix[i, j].Color);
                    else
                        universe.SetPixel(i, j, defaultColor);
                }
            }
            // Update impage
            universeSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                universe.GetHbitmap(),
                IntPtr.Zero,
                System.Windows.Int32Rect.Empty,
                BitmapSizeOptions.FromWidthAndHeight(WIDTH, HEIGHT));
            mainImage.Source = universeSource;
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (pausedState == true) return;
            universe.Dispose();
            universe = new System.Drawing.Bitmap(WIDTH, HEIGHT);

            // APPLY DEATH BY AGING
            AgeMaxtrix(1);

            // SAVE CURRENT STATE
            for (int i = 0; i < WIDTH; i++)
            {
                for (int j = 0; j < HEIGHT; j++)
                {
                    PrevLifeMatrix[i, j] = new Cell(LifeMatrix[i, j].State, LifeMatrix[i, j].Color, LifeMatrix[i, j].RemainingLifeSpan, LifeMatrix[i, j].MaximumLifeSpan);
                }
            }

            // UPDATE CELLS
            LifeMethod1(Underpopulation, Overpopulation, Birth);

            // State Counter - tick over
            stateCounter++;
            textBlock1.Text = "State Counter = " + stateCounter.ToString();
            updateBitmap();
        }

        private void AgeMaxtrix(int deathRate)
        {
            // APPLY AGING
            if (deathRate > 0)
            {
                for (int i = 0; i < WIDTH; i++)
                {
                    for (int j = 0; j < HEIGHT; j++)
                    {
                        LifeMatrix[i, j].Age(1);
                    }
                }
            }
        }

        private void LifeMethod1(int lonelyDeathTouches, int overpopulationDeathTouches, int procreationTouches)
        {
            // UPDATE STATUS BAR
            textBlockStatus.Text = string.Format("PopDensity: {0}, UnderPop: {1}, OverPop: {2}, Birth: {3}", PopulationDensityPercentage, lonelyDeathTouches.ToString(), overpopulationDeathTouches.ToString(), procreationTouches.ToString());

            // UPDATE CELLS
            int touchCount = 0;
            for (int i = 1; i < WIDTH - 1; i++)
            {
                for (int j = 1; j < HEIGHT - 1; j++)
                {

                    touchCount = 0;
                    List<Tuple<int, int>> touches = new List<Tuple<int, int>>();
                    if (PrevLifeMatrix[i - 1, j].State == 1) { touchCount++; touches.Add(Tuple.Create(i - 1, j)); };
                    if (PrevLifeMatrix[i - 1, j - 1].State == 1) { touchCount++; touches.Add(Tuple.Create(i - 1, j - 1)); };
                    if (PrevLifeMatrix[i - 1, j + 1].State == 1) { touchCount++; touches.Add(Tuple.Create(i - 1, j + 1)); };
                    if (PrevLifeMatrix[i + 1, j].State == 1) { touchCount++; touches.Add(Tuple.Create(i + 1, j)); };
                    if (PrevLifeMatrix[i + 1, j - 1].State == 1) { touchCount++; touches.Add(Tuple.Create(i + 1, j - 1)); };
                    if (PrevLifeMatrix[i + 1, j + 1].State == 1) { touchCount++; touches.Add(Tuple.Create(i + 1, j + 1)); };
                    if (PrevLifeMatrix[i, j - 1].State == 1) { touchCount++; touches.Add(Tuple.Create(i, j - 1)); };
                    if (PrevLifeMatrix[i, j + 1].State == 1) { touchCount++; touches.Add(Tuple.Create(i, j + 1)); };

                    // ALIVE CELL, check for death
                    if (PrevLifeMatrix[i, j].State == 1)
                    {
                        // Too few touches, die of lonelyness
                        if (touchCount <= lonelyDeathTouches) LifeMatrix[i, j].State = 0;
                        // Too many touches, die of overpopulation
                        if (touchCount >= overpopulationDeathTouches) LifeMatrix[i, j].State = 0;
                        // Else Lives on
                    }
                    else // EMPTY CELL, check for birth
                    {
                        // if just the right population for procreation, make a baby
                        // Inherit colour from one of the parent pixels
                        if (touchCount == procreationTouches)
                        {
                            int parent = random.Next(0, touches.Count - 1);
                            //LifeMatrix[i, j] = new Cell(1, PrevLifeMatrix[touches[parent].Item1, touches[parent].Item2].Color);
                            LifeMatrix[i, j] = PrevLifeMatrix[touches[parent].Item1, touches[parent].Item2].HaveChild();
                        }
                    }
                }
            }
        }

        private void PauseState(object sender, RoutedEventArgs e)
        {
            if (pausedState == true) pausedState = false;
            else pausedState = true;
        }

        private void ResetState(object sender, RoutedEventArgs e)
        {
            pausedState = true;
            resetMatrix();
            updateBitmap();
        }

        private void SpeedChanged(object sender, RoutedEventArgs e)
        {
            int speed = (int)this.SpeedSlider.Value;
            speed = 2 * 1000 / speed;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, speed);
        }

        private void SeedTextChanged(object sender, RoutedEventArgs e)
        {
            if (!_setupComplete) { return; }
            pausedState = true;
            try
            {
                PopulationDensityPercentage = decimal.Parse(TextBoxSeed.Text);
            }
            catch { PopulationDensityPercentage = 8; }
            resetMatrix();
            updateBitmap();
        }

        private void OverPopulationChanged(object sender, TextChangedEventArgs e)
        {
            if (!_setupComplete) { return; }
            try
            {
                Overpopulation = int.Parse(TextBoxOverPop.Text);
            }
            catch { Overpopulation = 5; }
            resetMatrix();
            updateBitmap();
        }

        private void UnderPopulationChanged(object sender, TextChangedEventArgs e)
        {
            if (!_setupComplete) { return; }
            try
            {
                Underpopulation = int.Parse(TextBoxUnderPop.Text);
            }
            catch { Underpopulation = 1; }
            resetMatrix();
            updateBitmap();
        }

        private void BirthChanged(object sender, TextChangedEventArgs e)
        {
            if (!_setupComplete) { return; }
            try
            {
                Birth = int.Parse(TextBoxProcPop.Text);
            }
            catch { Birth = 3; }
            resetMatrix();
            updateBitmap();
        }
    }

    public class Cell
    {
        public int State;
        public Color Color;
        public int RemainingLifeSpan;
        public int MaximumLifeSpan;
        private Random _random = new Random();

        public Cell(int state, Color color, int life, int maxLife) { State = state; Color = color; RemainingLifeSpan = life; MaximumLifeSpan = maxLife; }

        public Cell(int state, Color color, int life) { State = state; Color = color; RemainingLifeSpan = life; MaximumLifeSpan = RemainingLifeSpan; }

        public Cell(int state, Color color) { State = state; Color = color; RemainingLifeSpan = _random.Next(10, 150); MaximumLifeSpan = RemainingLifeSpan; }
        public Cell(int state)
        {
            State = state;
            Color = Color.FromArgb(_random.Next(0, 255), _random.Next(0, 255), _random.Next(0, 255));
            RemainingLifeSpan = _random.Next(10, 100);
            MaximumLifeSpan = RemainingLifeSpan;
        }

        public void Age(int agingRate)
        {
            if (this.State == 1 && this.RemainingLifeSpan > 0) this.RemainingLifeSpan -= agingRate;
            if (this.RemainingLifeSpan <= 0) this.State = 0;
        }

        public Cell HaveChild()
        {
            Cell child = new Cell(1, this.Color, this.MaximumLifeSpan);
            return child;
        }

    }
}

