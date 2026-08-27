using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Media; // 상단에 반드시 추가
using System.IO;

namespace BombDropper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            bombTimer.Tick += bombTimer_Tick;
            // 매 프레임마다 파편을 움직여줄 루프 이벤트를 연결합니다.
            CompositionTarget.Rendering += UpdateParticles;
        }

        private void canvasBackground_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RectangleGeometry rect = new RectangleGeometry();
            rect.Rect = new Rect(0, 0, canvasBackground.ActualWidth, canvasBackground.ActualHeight);
            canvasBackground.Clip = rect;
        }

        // "Adjustments" happen periodically, increasing the speed of bomb
        // falling and shortening the time between bombs.
        private DateTime lastAdjustmentTime = DateTime.MinValue;

        // Perform an adjustment every 15 seconds.
        private double secondsBetweenAdjustments = 15;

        // Initially, bombs fall every 1.3 seconds, and hit the ground after 3.5 seconds.
        private double initialSecondsBetweenBombs = 1.3;
        private double initialSecondsToFall = 3.5;
        private double secondsBetweenBombs;
        private double secondsToFall;

        // After every adjustment, shave 0.1 seconds off both.
        private double secondsBetweenBombsReduction = 0.1;
        private double secondsToFallReduction = 0.1;

        // Make it possible to look up a storyboard based on a bomb.
        private Dictionary<Bomb, Storyboard> storyboards = new Dictionary<Bomb, Storyboard>();
        // 현재 재생 중인 사운드 플레이어들을 보관하는 리스트
        private List<MediaPlayer> activePlayers = new List<MediaPlayer>();
        // Fires events on the user interface thread.
        private DispatcherTimer bombTimer = new DispatcherTimer();
        // 💡 배경음악 전용 플레이어 전역 선언 (참조 유지 및 메모리 누수 방지)
        private MediaPlayer bgmPlayer = new MediaPlayer();
        Brush[] particleBrush = new Brush[]
        {
            Brushes.Orange, Brushes.Yellow, Brushes.Red
        };

        // 💡 파편들을 담아둘 리스트 생성
        private List<ParticleInfo> particles = new List<ParticleInfo>();
        // Start the game.
        private void cmdStart_Click(object sender, RoutedEventArgs e)
        {
            cmdStart.IsEnabled = false;

            // Reset the game.
            droppedCount = 0;
            savedCount = 0;
            secondsBetweenBombs = initialSecondsBetweenBombs;
            secondsToFall = initialSecondsToFall;

            // Start bomb dropping events.            
            bombTimer.Interval = TimeSpan.FromSeconds(secondsBetweenBombs);
            bombTimer.Start();

            // 💡 [추가] 게임 시작 시 배경음악 켜기
            StartBGM();
        }

        // Drop a bomb.
        private void bombTimer_Tick(object sender, EventArgs e)
        {
            // Perform an "adjustment" when needed.
            if ((DateTime.Now.Subtract(lastAdjustmentTime).TotalSeconds >
                secondsBetweenAdjustments))
            {
                lastAdjustmentTime = DateTime.Now;

                secondsBetweenBombs -= secondsBetweenBombsReduction;
                secondsToFall -= secondsToFallReduction;

                // (Technically, you should check for 0 or negative values.
                // However, in practice these won't occur because the game will
                // always end first.)

                // Set the timer to drop the next bomb at the appropriate time.
                bombTimer.Interval = TimeSpan.FromSeconds(secondsBetweenBombs);

                // Update the status message.
                lblRate.Text = String.Format("A bomb is released every {0} seconds.",
                    secondsBetweenBombs);
                lblSpeed.Text = String.Format("Each bomb takes {0} seconds to fall.",
                    secondsToFall);
            }

            // Create the bomb.
            Bomb bomb = new Bomb();
            bomb.IsFalling = true;

            // Position the bomb.            
            Random random = new Random();
            bomb.SetValue(Canvas.LeftProperty,
                (double)(random.Next(0, (int)(canvasBackground.ActualWidth - 50))));
            bomb.SetValue(Canvas.TopProperty, -100.0);

            // Attach mouse click event (for defusing the bomb).
            bomb.MouseLeftButtonDown += bomb_MouseLeftButtonDown;

            // Create the animation for the falling bomb.
            Storyboard storyboard = new Storyboard();
            DoubleAnimation fallAnimation = new DoubleAnimation();
            fallAnimation.To = canvasBackground.ActualHeight;
            fallAnimation.Duration = TimeSpan.FromSeconds(secondsToFall);

            Storyboard.SetTarget(fallAnimation, bomb);
            Storyboard.SetTargetProperty(fallAnimation, new PropertyPath("(Canvas.Top)"));
            storyboard.Children.Add(fallAnimation);

            // Create the animation for the bomb "wiggle."
            DoubleAnimation wiggleAnimation = new DoubleAnimation();
            wiggleAnimation.From = -30; // 좌측 최대 각도
            wiggleAnimation.To = 30;
            wiggleAnimation.Duration = TimeSpan.FromSeconds(0.2);
            wiggleAnimation.RepeatBehavior = RepeatBehavior.Forever;
            wiggleAnimation.AutoReverse = true;

            // 💡 [수정] 타겟을 객체가 아닌 'bomb' 컨트롤 자체로 지정합니다.
            Storyboard.SetTarget(wiggleAnimation, bomb);

            // 💡 [수정] 속성 경로를 RenderTransform의 첫 번째 자식(Index 0)의 Angle 속성으로 정확히 지정합니다.
            Storyboard.SetTargetProperty(wiggleAnimation,
                new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(RotateTransform.Angle)"));

            storyboard.Children.Add(wiggleAnimation);

            // Add the bomb to the Canvas.
            canvasBackground.Children.Add(bomb);

            // Add the storyboard to the tracking collection.            
            storyboards.Add(bomb, storyboard);

            // Configure and start the storyboard.
            storyboard.Duration = fallAnimation.Duration;
            storyboard.Completed += storyboard_Completed;
            storyboard.Begin();
        }

        private void bomb_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Get the bomb.
            Bomb bomb = (Bomb)sender;
            bomb.IsFalling = false;

            // 💡 [수정] 강한 참조 연결을 끊기 위해 이벤트 핸들러를 가장 먼저 해제합니다.
            bomb.MouseLeftButtonDown -= bomb_MouseLeftButtonDown;

            // Get the bomb's current position.
            Storyboard storyboard = storyboards[bomb];
            double currentTop = Canvas.GetTop(bomb);

            // Stop the bomb from falling.
            storyboard.Stop();

            // Reuse the existing storyboard, but with new animations.
            // Send the bomb on a new trajectory by animating Canvas.Top
            // and Canvas.Left.
            storyboard.Children.Clear();

            // 1. 클릭된 폭탄의 현재 위치 가져오기
            double bombX = Canvas.GetLeft(bomb) + (bomb.ActualWidth / 2);
            double bombY = Canvas.GetTop(bomb) + (bomb.ActualHeight / 2);

            // 2. 폭발 이펙트 생성 (파편 20개 생성)
            CreateExplosion(bombX, bombY);

            // 3. 캔버스에서 폭탄 제거 및 관련 스토리보드 중지
            canvasBackground.Children.Remove(bomb);
            if (storyboards.ContainsKey(bomb))
            {
                storyboards[bomb].Stop();
                storyboards.Remove(bomb);
            }
        }
        private void CreateExplosion(double centerX, double centerY)
        {
            Random rand = new Random();
            int particleCount = 20; // 생성할 파편 개수

            PlayExplosionSoundWav();

            for (int i = 0; i < particleCount; i++)
            {
                // 원형 파편 UI 생성
                Ellipse p = new Ellipse
                {
                    Width = rand.Next(4, 10),  // 크기 무작위
                    Height = rand.Next(4, 10),
                    Fill = particleBrush[rand.Next(particleBrush.Length)]   // 폭발 색상 (노랑, 주황 등 섞어도 좋습니다)
                };

                // 초기 위치 설정 (폭탄 중심)
                Canvas.SetLeft(p, centerX);
                Canvas.SetTop(p, centerY);
                canvasBackground.Children.Add(p);

                // 무작위 각도와 속도 계산 (원형으로 퍼지도록)
                double angle = rand.NextDouble() * Math.PI * 2;
                double speed = rand.NextDouble() * 8 + 2; // 속도 2~10 사이

                particles.Add(new ParticleInfo
                {
                    Element = p,
                    VelocityX = Math.Cos(angle) * speed,
                    VelocityY = Math.Sin(angle) * speed,
                    Life = 1.0,
                    FadeSpeed = rand.NextDouble() * 0.03 + 0.02 // 서서히 사라지는 속도
                });
            }
        }
        private void UpdateParticles(object sender, EventArgs e)
        {
            // 리스트를 역순으로 순회해야 삭제 시 인덱스가 꼬이지 않습니다.
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                var p = particles[i];

                // 1. 수명 차감
                p.Life -= p.FadeSpeed;

                if (p.Life <= 0)
                {
                    // 2. 수명이 다하면 화면과 리스트에서 완전히 제거
                    canvasBackground.Children.Remove(p.Element);
                    particles.RemoveAt(i);
                }
                else
                {
                    // 3. 파편 이동 위치 계산
                    double nextX = Canvas.GetLeft(p.Element) + p.VelocityX;
                    // 중력 효과를 주고 싶다면 여기에 약간의 가속도를 더할 수 있습니다 (예: p.VelocityY += 0.2;)
                    double nextY = Canvas.GetTop(p.Element) + p.VelocityY;

                    Canvas.SetLeft(p.Element, nextX);
                    Canvas.SetTop(p.Element, nextY);

                    // 4. 투명도 적용 (서서히 흐려짐)
                    p.Element.Opacity = p.Life;
                }
            }
        }


        // Keep track of how many are dropped and stopped.
        private int droppedCount = 0;
        private int savedCount = 0;

        // End the game at maxDropped.
        private int maxDropped = 5;

        private void storyboard_Completed(object sender, EventArgs e)
        {
            ClockGroup clockGroup = (ClockGroup)sender;

            // Get the first animation in the storyboard, and use it to find the
            // bomb that's being animated.
            DoubleAnimation completedAnimation = (DoubleAnimation)clockGroup.Children[0].Timeline;
            Bomb completedBomb = (Bomb)Storyboard.GetTarget(completedAnimation);

            // 💡 [수정] 파괴되기 전 이벤트 해제
            if (completedBomb != null)
            {
                completedBomb.MouseLeftButtonDown -= bomb_MouseLeftButtonDown;
            }

            // Determine if a bomb fell or flew off the Canvas after being clicked.
            if (completedBomb.IsFalling)
            {
                // Get the bomb's current position.
                Storyboard storyboard = storyboards[completedBomb];
                double currentTop = Canvas.GetTop(completedBomb);

                // Stop the bomb from falling.
                storyboard.Stop();

                // Reuse the existing storyboard, but with new animations.
                // Send the bomb on a new trajectory by animating Canvas.Top
                // and Canvas.Left.
                storyboard.Children.Clear();

                // 1. 클릭된 폭탄의 현재 위치 가져오기
                double bombX = Canvas.GetLeft(completedBomb) + (completedBomb.ActualWidth / 2);
                double bombY = Canvas.GetTop(completedBomb) + (completedBomb.ActualHeight / 2);
                CreateExplosion(bombX, bombY);
                droppedCount++;
            }
            else
            {
                savedCount++;
            }

            // Update the display.
            lblStatus.Text = String.Format("You have dropped {0} bombs and saved {1}.",
                droppedCount, savedCount);

            // Check if it's game over.
            if (droppedCount >= maxDropped)
            {
                bombTimer.Stop();
                lblStatus.Text += "\r\n\r\nGame over.";

                // 💡 [추가] 게임 종료 시 배경음악 끄기
                StopBGM();

                // Find all the storyboards that are underway.
                foreach (KeyValuePair<Bomb, Storyboard> item in storyboards)
                {
                    Storyboard storyboard = item.Value;
                    Bomb bomb = item.Key;

                    storyboard.Stop();
                    canvasBackground.Children.Remove(bomb);
                }
                // Empty the tracking collection.
                storyboards.Clear();

                // 💡 [추가] 남아있는 모든 파편 UI 제거 및 데이터 초기화
                foreach (var p in particles)
                {
                    canvasBackground.Children.Remove(p.Element);
                }
                particles.Clear();

                // Allow the user to start a new game.
                cmdStart.IsEnabled = true;
            }
            else
            {
                Storyboard storyboard = (Storyboard)clockGroup.Timeline;
                storyboard.Stop();

                storyboards.Remove(completedBomb);
                canvasBackground.Children.Remove(completedBomb);
            }
        }

        private void PlayExplosionSound()
        {
            try
            {
                MediaPlayer soundPlayer = new MediaPlayer();
                string soundPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Bomb.mp3");

                soundPlayer.Open(new Uri(soundPath, UriKind.Absolute));

                // 💡 중요: GC가 수거하지 못하도록 전역 리스트에 담아 참조를 유지합니다.
                activePlayers.Add(soundPlayer);

                soundPlayer.MediaEnded += (s, e) =>
                {
                    MediaPlayer mp = s as MediaPlayer;
                    if (mp != null)
                    {
                        mp.Stop();
                        mp.Close();

                        // 💡 재생이 끝나면 리스트에서 안전하게 제거 (메모리 해제)
                        activePlayers.Remove(mp);
                    }
                };

                // WPF MediaPlayer 버그 방지를 위해 위치를 처음으로 초기화 후 재생
                soundPlayer.Position = TimeSpan.Zero;
                soundPlayer.Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("사운드 재생 실패: " + ex.Message);
            }
        }


        private void PlayExplosionSoundWav()
        {
            try
            {
                string soundPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Bomb.wav");

                // SoundPlayer는 비동기로 다중 재생(Play)해도 메모리 관리가 안정적입니다.
                SoundPlayer player = new SoundPlayer(soundPath);
                player.Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }


        private void StartBGM()
        {
            try
            {
                string bgmPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bgm.mp3");
                bgmPlayer.Open(new Uri(bgmPath, UriKind.Absolute));

                // 볼륨 설정 (0.0 ~ 1.0) -> 효과음보다 조금 작게 설정하는 것이 좋습니다.
                bgmPlayer.Volume = 0.5;

                // 💡 무한 반복 재생 설정
                bgmPlayer.MediaEnded -= BgmPlayer_MediaEnded; // 중복 등록 방지
                bgmPlayer.MediaEnded += BgmPlayer_MediaEnded;

                bgmPlayer.Position = TimeSpan.Zero;
                bgmPlayer.Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("BGM 재생 실패: " + ex.Message);
            }
        }

        private void BgmPlayer_MediaEnded(object sender, EventArgs e)
        {
            // 음악이 끝나면 다시 처음으로 돌려서 재생
            bgmPlayer.Position = TimeSpan.Zero;
            bgmPlayer.Play();
        }

        private void StopBGM()
        {
            // BGM 정지 및 리소스 해제
            bgmPlayer.Stop();
            bgmPlayer.Close();
        }


    }

    public class ParticleInfo
    {
        public Ellipse Element { get; set; }  // 화면에 그려질 원형 파편
        public double VelocityX { get; set; } // 가로 이동 속도
        public double VelocityY { get; set; } // 세로 이동 속도 (중력 효과 가능)
        public double Life { get; set; }      // 남은 수명 (1.0에서 시작해 0이 되면 삭제)
        public double FadeSpeed { get; set; } // 사라지는 속도
    }
}
