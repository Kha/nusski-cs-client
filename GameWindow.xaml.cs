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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Client
{
	public partial class GameWindow : Window
	{
		internal class HumanPlayer : IPlayer
		{
			public void MakeMove(Board board, Move lastMove, Action<Move> continuation)
			{
				// Logik wird von GameWindow ge-highjacked.
				throw new NotImplementedException();
			}

			public string Name
			{
				get { return "Kohlenstoffeinheit"; }
			}
		}

		class PieceVM
		{
			public Position Position { get; set; }
			public SolidColorBrush PieceColor { get; set; }
			public double StrokeThickness { get; set; }
		}

		public string Result { get; private set; }

		Board board;
		IPlayer player0, player1;
		ObservableCollection<Point> p0Points = new ObservableCollection<Point>(), p1Points = new ObservableCollection<Point>();
		TimeSpan player0MaxMove, player1MaxMove;
		Stopwatch moveWatch;
		int moves;

		static object lockObj = new object();

		public GameWindow()
		{
			// Anscheinend nicht thread-safe...
			lock (lockObj)
				InitializeComponent();
		}

		internal GameWindow(Board board) : this()
		{
			this.board = board;
			player0 = board.CurrentPlayer;
			player1 = board.OpposingPlayer;

			p0Series.ItemsSource = p0Points;
			p0Series.Title = player0.Name;
			p1Series.ItemsSource = p1Points;
			p1Series.Title = player1.Name;

			ShowBoard();
			moveWatch = Stopwatch.StartNew();
			if (!(board.CurrentPlayer is HumanPlayer))
				player0.MakeMove(board, null, Move);
		}

		void Move(Move move)
		{
			TimeSpan elapsed = moveWatch.Elapsed;
			if (board.CurrentPlayer == player0)
			{
				if (elapsed > player0MaxMove)
					p0Series.Title = string.Format("{0} ({1})", player0.Name, player0MaxMove = elapsed);
			} else
				if (elapsed > player1MaxMove)
					p1Series.Title = string.Format("{0} ({1})", player1.Name, player1MaxMove = elapsed);

			if (move == null)
			{
				Result = string.Format("{0} hat aufgegeben!", board.CurrentPlayer.Name);
				CheckForTimeout(player0, player0MaxMove);
				CheckForTimeout(player1, player1MaxMove);
				Close();
				return;
			}

			Debug.WriteLine(board.CurrentPlayer.Name + " : " + move.From + " - " + move.To);
			if (board.OpposingPlayer is Server.NetworkEnemy)
				((Server.NetworkEnemy)board.OpposingPlayer).ReportEnemyMove(move);

			IPlayer current = board.CurrentPlayer;
			board = board.MovePiece(move.From, move.To);
			p0Points.Add(new Point(moves, board.GetPoints(player0)));
			p1Points.Add(new Point(moves, board.GetPoints(player1)));
			moves++;
			ShowBoard();
			Delay(TimeSpan.FromMilliseconds(800) - elapsed, () => {
				if (!board.IsFilled && board.GetPoints(player0) > 0 && board.GetPoints(player1) > 0)
				{
					moveWatch.Restart();
					if (!(board.CurrentPlayer is HumanPlayer))
						board.CurrentPlayer.MakeMove(board, board.CurrentPlayer != current ? move : null, Move);
				}
				else
				{
					Result = string.Format("{0} hat gewonnen! {1}", (board.GetPointDelta(player0) > 0 ? player0 : player1).Name, Title);
					CheckForTimeout(player0, player0MaxMove);
					CheckForTimeout(player1, player1MaxMove);
					Close();
				}
			});
		}

		static void Delay(TimeSpan span, Action continuation)
		{
			var timer = new DispatcherTimer { Interval = span > TimeSpan.Zero ? span : TimeSpan.Zero };
			timer.Tick += delegate
			{
				continuation();
				timer.Stop();
			};
			timer.Start();
		}

		void CheckForTimeout(IPlayer player, TimeSpan maxMove)
		{
			if (maxMove > Bot.MaxThinkingTime)
				Result += string.Format("{0}Warnung: {1} hat die max. Bedenkzeit überschritten: {2}",
					Environment.NewLine, player.Name, maxMove);
		}

		void ShowBoard()
		{
			this.boardControl.ItemsSource = board.Positions.Select(pos => new PieceVM
			{
				Position = pos,
				PieceColor = board.IsHole(pos) ? Brushes.Gray : board[pos] == null ? null : board[pos] == player0 ? Brushes.Black : Brushes.White,
				StrokeThickness = board[pos] != null ? 1.0 : 0.0
			});
			Title = board.GetPoints(player0) + " - " + board.GetPoints(player1);
		}

		Position dragStart;
		void Border_MouseDown(object sender, MouseButtonEventArgs e)
		{
			var obj = (PieceVM)((FrameworkElement)sender).DataContext;
			dragStart = obj.Position;
		}

		void Border_MouseUp(object sender, MouseButtonEventArgs e)
		{
			var obj = (PieceVM)((FrameworkElement)sender).DataContext;

			if (board.CurrentPlayer is HumanPlayer && board.IsMoveLegal(dragStart, obj.Position))
				Move(new Move(dragStart, obj.Position));
			else
				Console.Beep();
		}
	}
}
