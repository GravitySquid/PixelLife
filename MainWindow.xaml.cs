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
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        BitmapSource universeSource;
        Bitmap universe;
        //Bitmap universePrev;
        Random random = new Random();
        int stateCounter = 0;
        bool pausedState = true;
        System.Drawing.Color defaultColor;
        Graphics bitmapGraphics;
        System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
        int Population = 80;
        private bool _setupComplete = false;
        int Underpopulation = 1;
        int Overpopulation = 5;
        int Birth = 3;

        // CONSTANTS
        const int WIDTH = 400;
        const int HEIGHT = 400;

        // Representations
        cell[,] LifeMatrix, PrevLifeMatrix;

        public MainWindow()
        {
            InitializeComponent();

            defaultColor = Color.Wheat;
            LifeMatrix = new cell[WIDTH, HEIGHT];
            PrevLifeMatrix = new cell[WIDTH, HEIGHT];
            resetMatrix();

            universe = new System.Drawing.Bitmap(WIDTH, HEIGHT);
            updateBitmap();

            // Timer to update universe
            //System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 1000);
            dispatcherTimer.Start();
            _setupComplete = true;
            this.Top = 0;
            this.Left = 100;
        }

        private void resetMatrix()
        {

            int randInt;
            for (int i = 0; i < WIDTH; i++)
            {
                for (int j = 0; j < HEIGHT; j++)
                {
                    Color color = Color.FromArgb(random.Next(0, 255), random.Next(0, 255), random.Next(0, 255));
                    randInt = random.Next(1, 1000);
                    if (randInt <= Population)
                    {
                        LifeMatrix[i, j] = new cell(1, color);
                    }
                    else
                        LifeMatrix[i, j] = new cell(0, color);
                }
            }
            stateCounter = 0;
            textBlock1.Text = "State Counter = " + stateCounter.ToString();
        }


        private void updateBitmap()
        {
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

            //universe.SetResolution(1290 * 4, 1080 * 4);
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

            // SAVE CURRENT STATE
            for (int i = 0; i < WIDTH; i++)
            {
                for (int j = 0; j < HEIGHT; j++)
                {
                    PrevLifeMatrix[i, j] = new cell(LifeMatrix[i, j].State, LifeMatrix[i, j].Color);
                }
            }

            // UPDATE CELLS
            LifeMethod1(Underpopulation, Overpopulation, Birth);

            // State Counter - tick over
            stateCounter++;
            textBlock1.Text = "State Counter = " + stateCounter.ToString();
            updateBitmap();
        }

        private void LifeMethod1(int lonelyDeathTouches, int overpopulationDeathTouches, int procreationTouches)
        {
            textBlockStatus.Text = string.Format("UnderPop: {0}, OverPop: {1}, Birth: {2}",lonelyDeathTouches.ToString(), overpopulationDeathTouches.ToString(), procreationTouches.ToString());
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
                        // if 0 or 1 touch, die of lonelyness
                        if (touchCount <= lonelyDeathTouches) LifeMatrix[i, j].State = 0;
                        // if 4 or more touch, die of overpopulation
                        if (touchCount >= overpopulationDeathTouches) LifeMatrix[i, j].State = 0;
                        // Else Lives on
                    }
                    else // DEAD CELL, check for birth
                    {
                        // if 3 alive, make a baby
                        if (touchCount == procreationTouches)
                        {
                            LifeMatrix[i, j].State = 1;
                            int parent = random.Next(0, touches.Count - 1);
                            LifeMatrix[i, j].Color = PrevLifeMatrix[touches[parent].Item1, touches[parent].Item2].Color;
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
                Population = int.Parse(TextBoxSeed.Text);
            }
            catch { Population = 80; }
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

    public class cell
    {
        public int State;
        public Color Color;

        public cell(int state, Color color) { State = state; Color = color; }

    }
}
