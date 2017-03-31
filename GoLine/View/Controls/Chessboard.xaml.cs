using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Input;
using GameServices.GameBasic;
using System.Collections.Generic;

namespace GoLine
{
    /// <summary>
    /// Interaction logic for ChessBg.xaml
    /// </summary>
    public partial class Chessboard : UserControl, IGameBoard<Brush, Spot>
    {
        public Brush BasicLineBrush
        {
            get { return (Brush)GetValue(BasicLineBrushProperty); }
            set { SetValue(BasicLineBrushProperty, value); }
        }
        public static readonly DependencyProperty BasicLineBrushProperty =
            DependencyProperty.Register("BasicLineBrush", typeof(Brush), typeof(Chessboard), new PropertyMetadata(Brushes.SkyBlue, OnBasicLineBrushChanged));
        
        public bool AutoNoticeNewest
        {
            get { return (bool)GetValue(AutoNoticeNewestProperty); }
            set { SetValue(AutoNoticeNewestProperty, value); }
        }
        
        public static readonly DependencyProperty AutoNoticeNewestProperty =
            DependencyProperty.Register("AutoNoticeNewest", typeof(bool), typeof(Chessboard), new PropertyMetadata(false));
        
        public double HighLightWaitTime
        {
            get { return (double)GetValue(HighLightWaitTimeProperty); }
            set { SetValue(HighLightWaitTimeProperty, value); }
        }
        
        public static readonly DependencyProperty HighLightWaitTimeProperty =
            DependencyProperty.Register("HighLightWaitTime", typeof(double), typeof(Chessboard), new PropertyMetadata(5d));

        public bool RefuseNewChess
        {
            get { return (bool)GetValue(RefuseNewChessProperty); }
            set { SetValue(RefuseNewChessProperty, value); }
        }
        
        public static readonly DependencyProperty RefuseNewChessProperty =
            DependencyProperty.Register("RefuseNewChess", typeof(bool), typeof(Chessboard), new PropertyMetadata(false));
        
        public double RadiuLength { get; private set; }
        public double LatticeLength { get; private set; }
        public bool CalculateLattice { get; set; } = true;
        private double chessLength;
        public double ChessLength
        {
            get { return chessLength; }
            set {
                if (chessLength != value)
                {
                    chessLength = value;
                    RadiuLength = value / 2;
                    chessMargin = new Thickness(-value / 2);
                }
            }
        }
        //Private
        private Ellipse[,] ellipseArray;
        private Thickness chessMargin;
        private Ellipse newestEllipse;
        private Size newerSize;

        public Chessboard()
        {
            InitializeComponent();
            RegisterHandler();
        }

        private void RegisterHandler()
        {
            SizeChangedEventHandler sizeHandler = null;
            RoutedEventHandler unloadHandler = null, ucLoadHandler = null, ucUnloadHandler = null;
            sizeHandler = (sender, e) =>
            {
                newerSize = e.NewSize;
                LatticeLength = e.NewSize.Height / 6;
            };
            unloadHandler = (sender, e) =>
            {
                Path.Unloaded -= unloadHandler;
                Path.SizeChanged -= sizeHandler;
            };
            Path.Unloaded += unloadHandler;
            Path.SizeChanged += sizeHandler;

            ucLoadHandler = (sender, e) =>
            {
                this.Loaded -= ucLoadHandler;
                //Initilize basic data for ellipse
                ChessLength = 48d;
                ellipseArray = new Ellipse[7, 7];
                scaleStoryboard = this.FindResource("ScaleStoryboard") as Storyboard;
                //scaleBackStoryboard = this.FindResource("ScaleBackStoryboard") as Storyboard;
                removingStoryboard = this.FindResource("RemovingStoryboard") as Storyboard;
            };
            ucUnloadHandler = (sender, e) =>
            {
                this.Unloaded -= ucUnloadHandler;
                EnsureReleaseTimer();
            };
            this.Loaded += ucLoadHandler;
            this.Unloaded += ucUnloadHandler;
        }

        private static void OnBasicLineBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var board = d as Chessboard;
            if (board != null)
            {
                board.UpdateLineBrush();
            }
        }

        private void UpdateLineBrush()
        {
            if (BasicLineBrush != null)
                Path.Stroke = BasicLineBrush;
        }
        
        #region Chess Add/Remove/Update

        private void DrawChess(Brush brush, int col, int row)
        {
            var ellipse = new Ellipse()
            {
                Width = ChessLength,
                Height = ChessLength,
                Margin = chessMargin,
                Fill = brush
            };
            Grid.SetColumn(ellipse, col);
            Grid.SetRow(ellipse, row);

            ChessLayer.Items.Add(ellipse);
            ellipseArray[col, row] = ellipse;
            newestEllipse = ellipse;
            if (AutoNoticeNewest)
            {
                scaleStoryboard.Stop();
                EnsureTimer();
                ResetTimer();
            }
        }

        private void RemoveChess(int col, int row)
        {
            if(ellipseArray[col, row] != null)
            {
                EventHandler completeHandler = null;
                completeHandler = (sender, e) =>
                {
                    removingStoryboard.Completed -= completeHandler;
                    ChessLayer.Items.Remove(ellipseArray[col, row]);
                    ellipseArray[col, row] = null;
                };
                Storyboard.SetTarget(removingStoryboard, ellipseArray[col, row]);
                removingStoryboard.Completed += completeHandler;
                removingStoryboard.Begin();
            }
        }

        private void ClearChess()
        {
            ellipseArray = new Ellipse[7, 7];
            ChessLayer.Items.Clear();
        }
        
        public void UpdateChessFill(Brush oldBrush, Brush newBrush)
        {
            if (ellipseArray == null) return;
            foreach (var ellipse in ellipseArray)
            {
                if (ellipse == null) continue;
                if (ellipse.Fill == oldBrush) ellipse.Fill = newBrush;
            }
        }

        #endregion

        #region HighLight

        private void EnsureTimer()
        {
            if (timer == null)
            {
                timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(HighLightWaitTime);
                timer.Tick += OnTimerTick;
                timer.Start();
            }
        }

        private void ResetTimer()
        {
            if (timer != null)
            {
                timer.Stop();
                timer.Start();
            }
        }

        private void EnsureReleaseTimer()
        {
            if(timer != null)
            {
                timer.Tick -= OnTimerTick;
                timer = null;
            }
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            EnsureReleaseTimer();
            if (RefuseNewChess)
            {
                return;
            }
            CircleTipsNewest();
        }
        
        public void OnceTipsNewest()
        {
            if (newestEllipse == null) return;
            HighLightChess = newestEllipse;
            EventHandler completeHandler = null;
            completeHandler = (obj, args) => 
            {
                if (!IsHighLightChanged)
                    BeginStoryboard(ref newestEllipse);
            };
            BeginStoryboard(ref newestEllipse, completeHandler);
        }

        private void CircleTipsNewest()
        {
            if (newestEllipse == null) return;
            HighLightChess = newestEllipse;

            EventHandler invokeHandler = null;
            Action circleAction = () =>
            {
                if (IsHighLightChanged || RefuseNewChess) invokeHandler = null;
                else BeginStoryboard(ref newestEllipse, invokeHandler);
            };
            invokeHandler = (obj, args) => { this.Dispatcher.Invoke(circleAction); };
            this.Dispatcher.Invoke(circleAction);
        }

        private void BeginStoryboard(ref Ellipse chess, EventHandler completeHandler = null)
        {
            Storyboard.SetTarget(scaleStoryboard, chess);
            if (completeHandler != null)
            {
                EventHandler handler = null;
                handler = (sender, e) =>
                {
                    scaleStoryboard.Completed -= handler;
                    scaleStoryboard.Completed -= completeHandler;
                };
                scaleStoryboard.Completed += handler;
                scaleStoryboard.Completed += completeHandler;
            }
            scaleStoryboard.Begin();
        }

        private Ellipse HighLightChess { get; set; }
        private bool IsHighLightChanged => HighLightChess != newestEllipse;

        private System.Windows.Threading.DispatcherTimer timer;
        private Storyboard scaleStoryboard;
        private Storyboard removingStoryboard;
        #endregion

        #region Implement IGameUI

        public bool Hand(Brush brush, Spot spot)
        {
            if (RefuseNewChess) return false;
            this.Dispatcher.Invoke(() =>
            {
                DrawChess(brush, spot.X, spot.Y);
            });
            return true;
        }

        public bool UndoHand(Brush brush, Spot spot)
        {
            this.Dispatcher.Invoke(() =>
            {
                RemoveChess(spot.X, spot.Y);
            });
            return true;
        }

        public void Clear()
        {
            this.Dispatcher.Invoke(() =>
            {
                ClearChess();
            });
        }

        #endregion

        #region MouseDown EventHandler

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (!CalculateLattice) return;
            //Get click position
            Point clickPoint = e.GetPosition(Path);
            Location location;
            if (tryGetColumnRow(clickPoint, LatticeLength, RadiuLength, out location) )
            {
                LatticeClick?.Invoke(this, new LatticeClickEventArgs(location));
            }
        }

        private bool tryGetColumnRow(Point clickPoint, double latticeLength, double radiuLength, out Location location)
        {
            location = new Location(-1,-1);
            //Calculate colmn and row
            int column = Convert.ToInt32(clickPoint.X / latticeLength), row = Convert.ToInt32(clickPoint.Y / latticeLength);
            //check if click point is in circle with radius equal to radiuLength
            if (Math.Abs(column * latticeLength - clickPoint.X) > radiuLength || Math.Abs(row * latticeLength - clickPoint.Y) > radiuLength) return false;
            location = new Location(column, row);
            return true;
        }

        public event EventHandler<LatticeClickEventArgs> LatticeClick;
        #endregion

    }

    public class LatticeClickEventArgs : EventArgs
    {
        public Location Location { get; private set; }
        public LatticeClickEventArgs()
        {
        }
        public LatticeClickEventArgs(Location location)
        {
            Location = location;
        }
    }

}
