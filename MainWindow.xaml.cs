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

namespace PixelLife
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        BitmapSource universeSource;
        Bitmap universe;
        Bitmap universePrev;
        Random random = new Random();
        int stateCounter = 0;
        bool pausedState = true;
        System.Drawing.Color defaultColor;

        // CONSTANTS
        const int WIDTH = 160;
        const int HEIGHT = 120;
        const int POPULATION = 80;

        // Representations
        int[,] LifeMatrix, PrevLifeMatrix;

        public MainWindow()
        {
            InitializeComponent();

            LifeMatrix = new int[WIDTH, HEIGHT];
            PrevLifeMatrix = new int[WIDTH, HEIGHT];
            resetMatrix();

            universe = new System.Drawing.Bitmap(WIDTH, HEIGHT);

            updateBitmap();

            // Timer to update universe
            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 2);
            dispatcherTimer.Start();
        }

        private void resetMatrix()
        {
            int randInt;    
            for (int i = 0; i < WIDTH; i++)
            {
                for (int j = 0; j < HEIGHT; j++)
                {
                    randInt = random.Next(1, 1000);
                    if (randInt <= POPULATION)
                        LifeMatrix[i, j] = 1;
                    else
                        LifeMatrix[i, j] = 0;
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
                    if (LifeMatrix[i, j] == 1)
                        universe.SetPixel(i, j, System.Drawing.Color.DarkBlue);
                    else
                        universe.SetPixel(i, j, System.Drawing.Color.Wheat);
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
            int touchCount = 0;
            if (pausedState == true) return;
            universe.Dispose();
            universe = new System.Drawing.Bitmap(WIDTH, HEIGHT);
            for (int i = 1; i < WIDTH - 1; i++)
            {
                for (int j = 1; j < HEIGHT - 1; j++)
                {
                    PrevLifeMatrix[i,j] = LifeMatrix[i,j];
                }
            }
            for (int i = 1; i < WIDTH -1; i++)
            {
                for (int j= 1; j < HEIGHT -1; j++)
                {
                    touchCount = 0;
                    if (PrevLifeMatrix[i - 1, j] == 1) touchCount++;
                    if (PrevLifeMatrix[i - 1, j - 1] == 1) touchCount++;
                    if (PrevLifeMatrix[i - 1, j + 1] == 1) touchCount++;
                    if (PrevLifeMatrix[i + 1, j] == 1) touchCount++;
                    if (PrevLifeMatrix[i + 1, j - 1] == 1) touchCount++;
                    if (PrevLifeMatrix[i + 1, j + 1] == 1) touchCount++;
                    if (PrevLifeMatrix[i, j - 1] == 1) touchCount++;
                    if (PrevLifeMatrix[i, j + 1] == 1) touchCount++;

                    // ALIVE CELL
                    if (PrevLifeMatrix[i, j] == 1)
                    {
                        // if 0 or 1 touch, die of lonelyness
                        if (touchCount <=1) LifeMatrix[i, j] = 0;
                        // if 4 or more touch, die of overpopulation
                        if (touchCount >= 4) LifeMatrix[i, j] = 0;
                        // Else Lives on
                    }
                    else // DEAD CELL
                    {
                        // if 3 alive, make a baby
                        if (touchCount == 3) LifeMatrix[i, j] = 1; 
                    }
                }
            }

            // State Counter - tick over
            stateCounter++;
            textBlock1.Text = "State Counter = " + stateCounter.ToString();
            updateBitmap();
        }

        private void PauseState(object sender, RoutedEventArgs e)
        {
            if (pausedState == true) pausedState = false;
            else pausedState = true;
        }

        private void ResetState(object sender, RoutedEventArgs e)
        {
            resetMatrix();
            updateBitmap();
        }

    }

}
