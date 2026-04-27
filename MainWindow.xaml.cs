using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.Media.Imaging;
using Avalonia.Interactivity;
using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using System.Runtime.InteropServices;
using System.Reactive.Linq;
// ImageSharp removed: using Avalonia WriteableBitmap instead

namespace PixelLife
{
    public partial class MainWindow : Window
    {
        private WriteableBitmap universeBitmap;
        private byte[] pixelBuffer;
        Random random = new Random();
        int stateCounter = 0;
        bool pausedState = true;
        Color defaultColor;
        DispatcherTimer dispatcherTimer = new DispatcherTimer();
        decimal PopulationDensityPercentage = 8;
        private bool _setupComplete = false;
        int Underpopulation = 1;
        int Overpopulation = 5;
        int Birth = 3;

        const int WIDTH = 400;
        const int HEIGHT = 400;

        Cell[,] LifeMatrix, PrevLifeMatrix;

        // UI controls - auto-generated from x:Name in XAML
        private TextBlock? textBlock1;
        private Button? ResetButton;
        private Button? PauseButton;
        private Slider? SpeedSlider;
        private TextBox? TextBoxSeed;
        private TextBox? TextBoxUnderPop;
        private TextBox? TextBoxOverPop;
        private TextBox? TextBoxProcPop;
        private Avalonia.Controls.Image? mainImage;
        private TextBlock? textBlockStatus;

        public MainWindow()
        {
            InitializeComponent();

            // Get references to named controls from XAML
            textBlock1 = this.FindControl<TextBlock>("textBlock1");
            ResetButton = this.FindControl<Button>("ResetButton");
            PauseButton = this.FindControl<Button>("PauseButton");
            SpeedSlider = this.FindControl<Slider>("SpeedSlider");
            TextBoxSeed = this.FindControl<TextBox>("TextBoxSeed");
            TextBoxUnderPop = this.FindControl<TextBox>("TextBoxUnderPop");
            TextBoxOverPop = this.FindControl<TextBox>("TextBoxOverPop");
            TextBoxProcPop = this.FindControl<TextBox>("TextBoxProcPop");
            mainImage = this.FindControl<Avalonia.Controls.Image>("mainImage");
            textBlockStatus = this.FindControl<TextBlock>("textBlockStatus");

            // Wire up event handlers
            ResetButton?.Click += ResetState;
            PauseButton?.Click += PauseState;

            // subscribe to value/text changes via PropertyChanged to avoid ambiguous extension overloads
            SpeedSlider?.PropertyChanged += (s, e) => { if (e.Property == Avalonia.Controls.Primitives.RangeBase.ValueProperty) SpeedChanged(null); };
            TextBoxSeed?.PropertyChanged += (s, e) => { if (e.Property == TextBox.TextProperty) SeedTextChanged(); };
            TextBoxOverPop?.PropertyChanged += (s, e) => { if (e.Property == TextBox.TextProperty) OverPopulationChanged(); };
            TextBoxUnderPop?.PropertyChanged += (s, e) => { if (e.Property == TextBox.TextProperty) UnderPopulationChanged(); };
            TextBoxProcPop?.PropertyChanged += (s, e) => { if (e.Property == TextBox.TextProperty) BirthChanged(); };

            defaultColor = Color.FromRgb(245, 222, 179);
            LifeMatrix = new Cell[WIDTH, HEIGHT];
            PrevLifeMatrix = new Cell[WIDTH, HEIGHT];
            resetMatrix();

            pixelBuffer = new byte[WIDTH * HEIGHT * 4];
            universeBitmap = new WriteableBitmap(new PixelSize(WIDTH, HEIGHT), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Unpremul);
            
            // Set the bitmap source
            if (mainImage != null)
                mainImage.Source = universeBitmap;
            
            updateBitmap();

            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = TimeSpan.FromMilliseconds(1000);
            dispatcherTimer.Start();
            _setupComplete = true;
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        private void resetMatrix()
        {
            for (int i = 0; i < WIDTH; i++)
            for (int j = 0; j < HEIGHT; j++)
            {
                int randInt = random.Next(1, 1000);
                if ((decimal)(randInt / 10) <= PopulationDensityPercentage)
                    LifeMatrix[i, j] = new Cell(1);
                else
                    LifeMatrix[i, j] = new Cell(0);
            }
            stateCounter = 0;
            textBlock1?.SetValue(TextBlock.TextProperty, "State Counter = " + stateCounter.ToString());

            // Ensure PrevLifeMatrix is initialized to avoid null dereferences
            for (int i = 0; i < WIDTH; i++)
            for (int j = 0; j < HEIGHT; j++)
                PrevLifeMatrix[i, j] = new Cell(LifeMatrix[i, j].State, LifeMatrix[i, j].Color, LifeMatrix[i, j].RemainingLifeSpan, LifeMatrix[i, j].MaximumLifeSpan);
        }

        private void updateBitmap()
        {
            // Fill BGRA buffer
            for (int j = 0; j < HEIGHT; j++)
            {
                for (int i = 0; i < WIDTH; i++)
                {
                    var c = (LifeMatrix[i, j].State == 1) ? LifeMatrix[i, j].Color : defaultColor;
                    int idx = (j * WIDTH + i) * 4;
                    pixelBuffer[idx + 0] = c.B; // B
                    pixelBuffer[idx + 1] = c.G; // G
                    pixelBuffer[idx + 2] = c.R; // R
                    pixelBuffer[idx + 3] = c.A; // A
                }
            }

            // Update the bitmap
            using (var fb = universeBitmap.Lock())
            {
                Marshal.Copy(pixelBuffer, 0, fb.Address, pixelBuffer.Length);
            }
            
            // Force visual invalidation for Linux rendering
            if (mainImage != null)
                mainImage.InvalidateVisual();
        }

        private void dispatcherTimer_Tick(object? sender, EventArgs e)
        {
            if (pausedState) return;
            // reuse existing bitmap and buffer; just clear buffer
            Array.Clear(pixelBuffer, 0, pixelBuffer.Length);

            AgeMaxtrix(1);

            for (int i = 0; i < WIDTH; i++)
            for (int j = 0; j < HEIGHT; j++)
                PrevLifeMatrix[i, j] = new Cell(LifeMatrix[i, j].State, LifeMatrix[i, j].Color, LifeMatrix[i, j].RemainingLifeSpan, LifeMatrix[i, j].MaximumLifeSpan);

            LifeMethod1(Underpopulation, Overpopulation, Birth);

            stateCounter++;
            textBlock1?.SetValue(TextBlock.TextProperty, "State Counter = " + stateCounter.ToString());
            updateBitmap();
        }

        private void AgeMaxtrix(int deathRate)
        {
            if (deathRate > 0)
            {
                for (int i = 0; i < WIDTH; i++)
                for (int j = 0; j < HEIGHT; j++)
                    LifeMatrix[i, j].Age(1);
            }
        }

        private void LifeMethod1(int lonelyDeathTouches, int overpopulationDeathTouches, int procreationTouches)
        {
            if (textBlockStatus != null)
                textBlockStatus.Text = string.Format("PopDensity: {0}, UnderPop: {1}, OverPop: {2}, Birth: {3}", PopulationDensityPercentage, lonelyDeathTouches.ToString(), overpopulationDeathTouches.ToString(), procreationTouches.ToString());

            int touchCount;
            for (int i = 1; i < WIDTH - 1; i++)
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

                if (PrevLifeMatrix[i, j].State == 1)
                {
                    if (touchCount <= lonelyDeathTouches) LifeMatrix[i, j].State = 0;
                    if (touchCount >= overpopulationDeathTouches) LifeMatrix[i, j].State = 0;
                }
                else
                {
                    if (touchCount == procreationTouches && touches.Count > 0)
                    {
                        int parent = random.Next(0, touches.Count);
                        LifeMatrix[i, j] = PrevLifeMatrix[touches[parent].Item1, touches[parent].Item2].HaveChild();
                    }
                }
            }
        }

        private void PauseState(object? sender, RoutedEventArgs e)
        {
            pausedState = !pausedState;
        }

        private void ResetState(object? sender, RoutedEventArgs e)
        {
            pausedState = true;
            resetMatrix();
            updateBitmap();
        }

        private void SpeedChanged(object? value)
        {
            try
            {
                var v = Convert.ToDouble(SpeedSlider?.Value ?? 1.0);
                int speed = (int)v;
                speed = 2 * 1000 / Math.Max(1, speed);
                dispatcherTimer.Interval = TimeSpan.FromMilliseconds(speed);
            }
            catch { }
        }

        private void SeedTextChanged()
        {
            if (!_setupComplete) return;
            pausedState = true;
            if (!decimal.TryParse(TextBoxSeed?.Text, out var pd)) pd = 8;
            PopulationDensityPercentage = pd;
            resetMatrix();
            updateBitmap();
        }

        private void OverPopulationChanged()
        {
            if (!_setupComplete) return;
            if (!int.TryParse(TextBoxOverPop?.Text, out var op)) op = 5;
            Overpopulation = op;
            resetMatrix();
            updateBitmap();
        }

        private void UnderPopulationChanged()
        {
            if (!_setupComplete) return;
            if (!int.TryParse(TextBoxUnderPop?.Text, out var up)) up = 1;
            Underpopulation = up;
            resetMatrix();
            updateBitmap();
        }

        private void BirthChanged()
        {
            if (!_setupComplete) return;
            if (!int.TryParse(TextBoxProcPop?.Text, out var b)) b = 3;
            Birth = b;
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
            Color = Color.FromRgb((byte)_random.Next(256), (byte)_random.Next(256), (byte)_random.Next(256));
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

