using Epx.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GameServices.GameBasic;
using GameServices;
using System.Text;
using System.Windows.Media.Animation;

namespace GoLine
{
    /// <summary>
    /// Interaction logic for GamePage.xaml
    /// </summary>
    public partial class GamePage : Page
    {
        private SolidColorBrush backTransBrush = new SolidColorBrush(Colors.DimGray) { Opacity = 0.6 };
        private IList<Player> humanPlayers;
        private Player primaryPlayer, secondaryPlayer;
        private Brush primaryBrush;
        private Brush secondaryBrush = Brushes.DimGray;
        private GameService Provider { get; set; }
        private IGameBoard<Brush, Spot> GameBoard { get; set; }
        //private IGameUI DebugConsole { get; set; } = new GameConsole();
        public GameMode GameMode { get; private set; }
        public int FirstPlayerIndex { get; set; }

        public GamePage() : this(GameMode.PVE) { }
        public GamePage(GameMode gameMode)
        {
            InitializeComponent();
            GameMode = gameMode;
            primaryBrush = (Brush)(new BrushConverter()).ConvertFromString(App.Settings.ChessBrush);
            //TODO  Wait for selected to init game service when :
            //0.Game mode is Online need to select player by player
            //1.Game mode is PVE or AIVSAI need to select AI level
            //Else init game service when load completed
            this.Loaded += OnGamePageLoaded;
        }

        private void OnGamePageLoaded(object sender, RoutedEventArgs e)
        {
            if (GameMode == GameMode.PVP) InItFirstPlayerBox();
            else InitLevelSelector();
        }

        #region --------Set first--------

        private void InItFirstPlayerBox()
        {
            if (TryInitPlayers() == false) return;
            var players = new List<SelectionItem<Player>>()
            {
                new SelectionItem<Player>(primaryPlayer.Name, primaryPlayer),
                new SelectionItem<Player>(secondaryPlayer.Name, secondaryPlayer)
            };
            FirstStartComboBox.ItemsSource = players;
            FirstStartComboBox.SelectedIndex = 0;
            FadeExtension.FadeInBoxOf(FirstSelectBox, FirstSelectBoxContent);
        }       
        private void SetFirstBtnOKClick(object sender, RoutedEventArgs e)
        {
            if (FirstStartComboBox.SelectedIndex < 0) return;
            else FirstPlayerIndex = FirstStartComboBox.SelectedIndex;
            if (TryInitPlayers() == false) return;
            FadeExtension.FadeOutBoxOf(FirstSelectBox, FirstSelectBoxContent);
            Initialize(sender, e);
        }

        #endregion

        #region --------Level select handler--------

        private string GetAIName(AIType type)
        {
            switch (type)
            {
                case AIType.Advanced:return Properties.Resources.AdvancedAI;
                case AIType.Intermediate: return Properties.Resources.IntermediateAI;
                case AIType.Elementary: return Properties.Resources.ElementaryAI;
            }
            return string.Empty;
        }

        private void InitLevelSelector()
        {
            var availbleType = new List<SelectionItem<AIType>>()
            {
                new SelectionItem<AIType>(GetAIName(AIType.Advanced), AIType.Advanced),
                new SelectionItem<AIType>(GetAIName(AIType.Intermediate), AIType.Intermediate),
                new SelectionItem<AIType>(GetAIName(AIType.Elementary), AIType.Elementary)
            };
            if (GameMode == GameMode.PVE)
            {
                SecLevelContent.Visibility = Visibility.Collapsed;
                FirstLevelComboBox.ItemsSource = availbleType;
                FirstLevelComboBox.SelectedIndex = 0;
            }
            else
            {
                FirstLevelComboBox.ItemsSource = availbleType;
                SecLevelComboBox.ItemsSource = availbleType;
                FirstLevelComboBox.SelectedIndex = 0;
                SecLevelComboBox.SelectedIndex = 0;
            }
            FadeExtension.FadeInBoxOf(LevelSelectBox, LevelBoxContent);
        }
        
        private AIType GetLevelSelected(int index)
        {
            if (index == 0 && FirstLevelComboBox.SelectedIndex >= 0)
                return (AIType)FirstLevelComboBox.SelectedValue;
            if (index != 0 && SecLevelComboBox.SelectedIndex >= 0)
                return (AIType)FirstLevelComboBox.SelectedValue;
            return AIType.None;
        }

        private void LevelOkBtnClick(object sender, RoutedEventArgs e)
        {
            FadeExtension.FadeOutBoxOf(LevelSelectBox, LevelBoxContent);
            //Active first player selection when selected level
            InItFirstPlayerBox();
        }

        private void CancelBtnClick(object sender, RoutedEventArgs e)
        {
            if(this.NavigationService.CanGoBack)this.NavigationService.GoBack();
        }

        #endregion

        private void Initialize(object sender, RoutedEventArgs e)
        {
            if(GameBoard == null)
            {
                System.ComponentModel.PropertyChangedEventHandler settingsChangedHandler = null;
                settingsChangedHandler = (obj, args) =>
                {
                    var name = nameof(App.Settings.ChessBrush);
                    var converter = new BrushConverter();
                    if (args.PropertyName == name)
                    {
                        var newBrush = (Brush)converter.ConvertFromString(App.Settings.ChessBrush);
                        chessboard.UpdateChessFill(primaryBrush, newBrush);
                        System.Diagnostics.Debug.WriteLine($"{primaryBrush.ToString()} {newBrush.ToString()}");
                    }
                };
                App.Settings.PropertyChanged += settingsChangedHandler;
                //Chessboard
                GameBoard = chessboard;
                //Unsubsribe handler
                RoutedEventHandler unsubscribeAllHandler = null;
                unsubscribeAllHandler = (obj, args) =>
                {
                    this.Unloaded -= unsubscribeAllHandler;
                    App.Settings.PropertyChanged -= settingsChangedHandler;
                    chessboard.PreviewMouseDown -= OnChessboardPreviewMouseDown;
                    ReleaseGameService();
                    App.BGMService.Stop();
                };
                this.Unloaded += unsubscribeAllHandler;
                chessboard.PreviewMouseDown += OnChessboardPreviewMouseDown;
                startBtn.IsEnabled = true;
                wdBtn.IsEnabled = false;
            }
            //Init and join player
            player01TB.Text = primaryPlayer.Name;
            player02TB.Text = secondaryPlayer.Name;
            player01Rect.Fill = primaryBrush;
            player02Rect.Fill = secondaryBrush;
            RegisterNewGameService();
            App.BGMService.Play();
            //App.BGMService.AddDirectory();
        }

        private bool TryInitPlayers()
        {
            humanPlayers = new List<Player>();
            switch (GameMode)
            {
                case GameMode.PVE:
                    //Create human player
                    if (App.LoginedAccount == null)
                    //TODO Ask for name of guest 0
                    {
                        if(PlayerService.TryCreateGuest("Guest", out primaryPlayer))
                        {
                            primaryPlayer.Name = Properties.Resources.Guest;
                        }
                    }
                    else
                        PlayerService.TryCreatePlayer(App.LoginedAccount, out primaryPlayer);
                    //Create AI player
                    if(PlayerService.TryCreateAI(GetLevelSelected(0), out secondaryPlayer))
                    {
                        secondaryPlayer.Name = GetAIName(GetLevelSelected(0));
                    }
                    break;
                case GameMode.AIvsAI:
                    if (PlayerService.TryCreateAI(GetLevelSelected(0), out primaryPlayer))
                    {
                        primaryPlayer.Name = GetAIName(GetLevelSelected(0));
                    }
                    if (PlayerService.TryCreateAI(GetLevelSelected(1), out secondaryPlayer))
                    {
                        secondaryPlayer.Name = GetAIName(GetLevelSelected(1));
                    }
                    break;
                case GameMode.PVP:
                default:
                    //Create human player 0
                    if (App.LoginedAccount == null)
                    //TODO Ask for name of guest 0
                    {
                        if (PlayerService.TryCreateGuest("Guest", out primaryPlayer))
                        {
                            primaryPlayer.Name = Properties.Resources.Guest;
                        }
                    }
                    else
                        PlayerService.TryCreatePlayer(App.LoginedAccount, out primaryPlayer);
                    //Create human player 1
                    //TODO Ask for name of guest 1
                    if (PlayerService.TryCreateGuest("Guest2", out secondaryPlayer))
                    {
                        secondaryPlayer.Name = Properties.Resources.Guest2;
                    }
                    break;
            }
            if (primaryPlayer == null || secondaryPlayer == null) return false;
            if (!primaryPlayer.IsAI) humanPlayers.Add(primaryPlayer);
            if (!secondaryPlayer.IsAI) humanPlayers.Add(secondaryPlayer);
            return true;
        }

        #region --------Game service op--------

        private bool RegisterNewGameService()
        {
            ReleaseGameService();
            if (primaryPlayer == null || secondaryPlayer == null) return false;
            Provider = new GameService();
            Provider.CurrentMode = GameMode;
            SubscribeGameHandler();
            Provider.JoinPlayer(primaryPlayer);
            Provider.JoinPlayer(secondaryPlayer);
            Provider.SetFirstPlayer(FirstPlayerIndex > 0 ? secondaryPlayer : primaryPlayer);
            //Ensure if there is inneed to calculate lattice location
            //(Only when there is a human player)
            if (GameMode == GameMode.AIvsAI) chessboard.CalculateLattice = false;
            else
            {
                chessboard.CalculateLattice = true;
                chessboard.LatticeClick += OnLatticeClick;
            }
            //When AI player is the first start game
            if (Provider.StartPlayer.IsAI)
            {
                Provider.StartNewGame();
            }
            //Notice the first player
            else
            {
                OnActivePlayerChanged(this, new GameEventArgs(Provider.StartPlayer));
            }
            return true;
        }

        private void SubscribeGameHandler()
        {
            //Subsribe GameMachine even handler
            Provider.GameStarting += OnGameStarting;
            Provider.GameDataUpdated += OnGameDataUpdated;
            Provider.GameEnded += OnGameEnded;
            Provider.ActivePlayerChanged += OnActivePlayerChanged;
        }

        private void ReleaseGameService()
        {
            EnsureReleaseTimer();
            chessboard.LatticeClick -= OnLatticeClick;
            if (Provider == null) return;
            Provider.GameDataUpdated -= OnGameDataUpdated;
            Provider.GameStarting -= OnGameStarting;
            Provider.GameEnded -= OnGameEnded;
            Provider.ActivePlayerChanged -= OnActivePlayerChanged;
            Provider.Detach();
            Provider = null;
            Debug.Log("ReleaseGameService");
        }

        #endregion

        #region --------Game event handler--------

        private void OnGameStarting(object sender, EventArgs e)
        {
            Debug.Log("Started");
            EnsureTimer();
            chessboard.CalculateLattice = GameMode != GameMode.AIvsAI;
            chessboard.RefuseNewChess = false;
            chessboard.Clear();
            startBtn.IsEnabled = false;
            wdBtn.IsEnabled = true;
        }

        private void OnGameDataUpdated(object sender, GameEventArgs e)
        {
            Brush brush = null;
            if (e.Player != null) brush = e.Player == primaryPlayer ? primaryBrush : secondaryBrush;
            switch (e.Type)
            {
                case GameEventType.NewInput:
                    GameBoard.Hand(brush, e.Spot);
                    App.ClickEffectPlay();
                    break;
                case GameEventType.Withdraw:
                    GameBoard.UndoHand(brush, e.Spot);
                    break;
                case GameEventType.Redo:
                    GameBoard.Hand(brush, e.Spot);
                    break;
                default: break;
            }
        }

        private void OnActivePlayerChanged(object sender, GameEventArgs e)
        {
            if (e.Player == primaryPlayer)
            {
                player01TB.Foreground = Brushes.SkyBlue;
                player02TB.Foreground = Brushes.DimGray;
            }
            else
            {
                player02TB.Foreground = Brushes.SkyBlue;
                player01TB.Foreground = Brushes.DimGray;
            }
        }
        
        private void OnGameReseted(object sender, EventArgs e)
        {
            EnsureReleaseTimer();
            chessboard.Clear();
        }

        private async void OnGameEnded(object sender, GameEndedEventArgs e)
        {
            Debug.Log("Ended");
            EnsureReleaseTimer();
            if (e.IsInterrupt) return;
            chessboard.RefuseNewChess = true;
            chessboard.CalculateLattice = false;
            //Show ended message
            var dialog = new MessageDialog()
            {
                TopTitle = Properties.Resources.Message,
                Title = Properties.Resources.Message,
                PrimaryButtonText = Properties.Resources.OK,
                SecondaryButtonText = Properties.Resources.Cancel,
                Background = backTransBrush
            };
            var time01 = Provider.GetInfoOf(primaryPlayer).TimeSpan.ToString("hh':'mm':'ss");
            var time02 = Provider.GetInfoOf(secondaryPlayer).TimeSpan.ToString("hh':'mm':'ss");
            StringBuilder builder = new StringBuilder();
            builder.Append(e.HasWinner ?  $"{Provider.Winner.Name} {Properties.Resources.WinTheGame}" : Properties.Resources.TieEnded);
            builder.Append($"\n\n{primaryPlayer.Name} ");
            builder.Append($"{Properties.Resources.Score} : {primaryPlayer.Score?.Score:0}, {Properties.Resources.Time} : {time01}");
            builder.Append($"\n{secondaryPlayer.Name} ");
            builder.Append($"{Properties.Resources.Score} : {secondaryPlayer.Score?.Score:0}, {Properties.Resources.Time} : {time02}");
            dialog.Content = builder.ToString();
            await Task.Delay(200);
            await dialog.ShowAsync();
            ReleaseGameService();
            startBtn.IsEnabled = true;
            wdBtn.IsEnabled = false;
        }

        #endregion

        #region --------Chessboard event handler--------

        private async void OnChessboardPreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (GameMode == GameMode.AIvsAI) return;
            if (Provider != null && Provider.IsGameStarted) return;
            var dialog = new MessageDialog()
            {
                TopTitle = Properties.Resources.Message,
                Title = Properties.Resources.Tips,
                Content = $"{Properties.Resources.StartNewGame}?",
                PrimaryButtonText = Properties.Resources.OK,
                SecondaryButtonText = Properties.Resources.Cancel,
                Background = backTransBrush
            };
            var result = await dialog.ShowAsync();
            if (result == MessageDialogResult.Secondary || result == MessageDialogResult.Fail) return;
            if (Provider == null)
                RegisterNewGameService();
            Provider.StartNewGame();
        }

        //Draw in game ui for human player if lattice click
        private void OnLatticeClick(object sender, LatticeClickEventArgs e)
        {
            Debug.Log("OnLatticeClick");
            if (!Provider.IsGameStarted)
            {
                Debug.Log("Game is not start");
                return;
            }
            int index = humanPlayers.IndexOf(Provider.CurrentPlayer);
            if (index < 0) { Debug.Log($"Input Fail, Current actived is '{Provider.CurrentPlayer.Name}' "); return; }
            //Try input data
            if (!humanPlayers[index].Input(e.Location)) Debug.Log($"Input Fail -> { e.Location.X }, { e.Location.Y}");
        }

        #endregion

        #region --------Timer of show time--------

        private void EnsureTimer()
        {
            if (timer == null)
            {
                timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(1d);
                timer.Tick += OnTimerTick;
                timer.Start();
            }
        }

        private void EnsureReleaseTimer()
        {
            if (timer != null)
            {
                OnTimerTick(timer, EventArgs.Empty);
                timer.Stop();
                timer.Tick -= OnTimerTick;
                timer = null;
            }
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            var ts01 = Provider.GetInfoOf(primaryPlayer).TimeSpan;
            var ts02 = Provider.GetInfoOf(secondaryPlayer).TimeSpan;
            var time02 = ts02.ToString("hh':'mm':'ss");
            var time01 = ts01.ToString("hh':'mm':'ss");
            Dispatcher.Invoke(() => 
            {
                player01Time.Text = time01;
                player02Time.Text = time02;
            });
        }

        private System.Windows.Threading.DispatcherTimer timer;
        #endregion
            
        #region --------Game control--------

        private void withdrawClick(object sender, RoutedEventArgs e)
        {
            if (humanPlayers.Count == 0) return;
            if (humanPlayers.Count == 1) humanPlayers[0].Undo();
            else
            {
                int index = humanPlayers.IndexOf(Provider.FrontPlayer);
                if (index < 0) Debug.Log($"Can't undo now");
                else humanPlayers[index].Undo();
            }
        }

        private void startBtnClick(object sender, RoutedEventArgs e)
        {
            if (Provider == null)
            {
                if (RegisterNewGameService()) Provider.StartNewGame();
            }
            else Provider.StartNewGame();
        }
        
        private void resetdBtnClick(object sender, RoutedEventArgs e)
        {
            //Provider.ResetGame();
            ReleaseGameService();
            primaryPlayer = null;
            secondaryPlayer = null;
            OnGamePageLoaded(sender, e);
        }
        
        #endregion

        private void MenuBtnClick(object sender, RoutedEventArgs e)
        {
            MainWindow.FlyoutNavigateServices.Navigate(new MenuControl());
        }                         

        private void UserBtnClick(object sender, RoutedEventArgs e)
        {
            if(App.LoginedAccount != null)
                MainWindow.FlyoutNavigateServices.Navigate(new UserPage() { LogoutEnabled = false });
        }

        private void highLightNewestBtnClick(object sender, RoutedEventArgs e)
        {
            chessboard.OnceTipsNewest();
        }
    }
}
