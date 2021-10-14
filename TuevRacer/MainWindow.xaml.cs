using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TuevRacer
{
    public partial class MainWindow : Window
    {
        private double playerDistanceKm;
        private double playerSpeedKmh;
        private double playerX;
        private double playerY = 10;
        private double carDistanceKm = 0.1;
        private double carX;
        private double carY;
        private Image[] cars;
        private Image car;
        private long lastTicks;
        private bool gameover;
        private double explosionTime;

        const double screenWidth = 800;
        const double borderWidth = 148;
        const double playerWidth = 70;
        const double carSpeedKmh = 50;

        public MainWindow()
        {
            InitializeComponent();
            cars = new[] { imageDekra, imageGtu, imageTuvsud };
            CompositionTarget.Rendering += CompositionTarget_Rendering;
            Reset();
        }

        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            long ticks = DateTime.Now.Ticks;
            long deltaTicks = ticks - lastTicks;
            lastTicks = ticks;

            TimeSpan deltaTime = TimeSpan.FromTicks(deltaTicks);
            Update(deltaTime);
        }

        private void Reset()
        {
            playerDistanceKm = 0;
            playerSpeedKmh = 80;

            playerX = 0.5 * (screenWidth - imagePlayer.Width);
            Canvas.SetLeft(imagePlayer, playerX);
            playerTransform.Angle = 0;
            imagePlayer.Visibility = Visibility.Visible;
            
            if (car != null) // move off screen
            {
                Canvas.SetBottom(car, -car.Height);
                car.Visibility = Visibility.Visible;
            }
            
            SpawnCar();
            
            lastTicks = DateTime.Now.Ticks;
            gameover = false;
        }

        private void Update(TimeSpan deltaTime)
        {
            if (gameover)
            {
                explosionTime += deltaTime.TotalSeconds;
                var frame = (long)(explosionTime * 12);
                if (frame < 4)
                {
                    rectExplosion.Visibility = Visibility.Visible;
                    brushExplosion.Viewbox = new Rect { Y = 0.25 * frame, Width = 1, Height = 0.25 };
                }
                else
                {
                    rectExplosion.Visibility = Visibility.Hidden;
                }
                if (explosionTime > 1)
                {
                    var res = MessageBox.Show("Nochmal?", "Game Over", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (res == MessageBoxResult.Yes)
                        Reset();
                    else
                        Close();
                }
            }
            else
            {
                playerDistanceKm += playerSpeedKmh * deltaTime.TotalHours;
                carDistanceKm += carSpeedKmh * deltaTime.TotalHours;

                playerSpeedKmh += 0.5 * deltaTime.TotalSeconds; // increase by 1 km/h every two seconds

                playerTransform.Angle = 0;
                if (Keyboard.IsKeyDown(Key.Left)) { playerX -= 10 * (50 + 0.1 * playerSpeedKmh) * deltaTime.TotalSeconds; playerTransform.Angle = -6; }
                if (Keyboard.IsKeyDown(Key.Right)) { playerX += 10 * (50 + 0.1 * playerSpeedKmh) * deltaTime.TotalSeconds; playerTransform.Angle = 6; }
                if (playerX < borderWidth) { playerX = borderWidth; playerTransform.Angle = 0; }
                if (playerX > screenWidth - borderWidth - playerWidth) { playerX = screenWidth - borderWidth - playerWidth; playerTransform.Angle = 0; }

                const double avgCarLengthKm = 4.5 / 1000.0;
                const double playerHeight = 121;
                const double kmToPx = playerHeight / avgCarLengthKm;
                var distancePx = playerDistanceKm * kmToPx; // scroll value

                // scroll background
                brushGrass.Viewport = new Rect { X = 0, Y = distancePx, Width = 128, Height = 128 };
                brushRoad.Viewport = new Rect { X = 0, Y = distancePx, Width = 544, Height = 128 };

                var deltaY = (carDistanceKm - playerDistanceKm) * kmToPx;
                carY = playerY + deltaY;

                // position cars
                Canvas.SetLeft(imagePlayer, playerX);
                Canvas.SetBottom(car, playerY);
                Canvas.SetLeft(car, carX);
                Canvas.SetBottom(car, carY);

                if (CheckCollision())
                {
                    OnGameOver();
                    return;
                }

                if (carY < -car.Height) SpawnCar();

                // update UI
                textSpeed.Text = $"{playerSpeedKmh:F1}";
                textDistance.Text = $"{playerDistanceKm:F2}";
            }
        }

        private void SpawnCar()
        {
            var r = new Random();
            carDistanceKm = playerDistanceKm + 0.03;
            car = cars[r.Next(cars.Length)];
            carX = borderWidth + r.NextDouble() * (screenWidth - 2 * borderWidth - car.Width);
        }

        private bool CheckCollision()
        {
            var playerRect = new Rect { X = playerX, Y = playerY, Width = imagePlayer.Width, Height = imagePlayer.Height };
            var carRect = new Rect { X = carX, Y = carY, Width = car.Width, Height = car.Height };
            return playerRect.IntersectsWith(carRect);
        }

        private void OnGameOver()
        {
            imagePlayer.Visibility = Visibility.Hidden;
            car.Visibility = Visibility.Hidden;
            rectExplosion.Visibility = Visibility.Visible;
            explosionTime = 0;
            Canvas.SetLeft(rectExplosion, 0.5 * (playerX + carX + 0.5 * (imagePlayer.Width + car.Width) - rectExplosion.Width));
            Canvas.SetBottom(rectExplosion, 0.5 * (playerY + carY + 0.5 * (imagePlayer.Height + car.Height) - rectExplosion.Height));
            gameover = true;
        }
    }
}
