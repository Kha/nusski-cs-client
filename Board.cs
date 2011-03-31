using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Client
{
	/// <summary>
	/// Stellt einen Vektor in den Brett-Koordinaten dar.
	/// Geht es um absolute Koordinaten, ist (0,0) die linke obere Ecke.
	/// Diese Klasse ist immutable.
	/// </summary>
	[DebuggerDisplay("{X};{Y}")]
	class Position
	{
		/// <summary>
		/// Auflistung der Vektoren zu benachbarten Feldern
		/// </summary>
		public static ReadOnlyCollection<Position> Directions = new ReadOnlyCollection<Position>(
			(from x in new[] { -1, 0, 1 }
			 from y in new[] { -1, 0, 1 }
			 where x != 0 || y != 0
			 select new Position(x, y)).ToList()
		);

		/// <summary>
		/// Auflistung der Vektoren gültiger Züge
		/// </summary>
		public static ReadOnlyCollection<Position> MoveVectors = new ReadOnlyCollection<Position>(
			(from x in new[] { -2, -1, 0, 1, 2 }
			 from y in new[] { -2, -1, 0, 1, 2 }
			 where x != 0 || y != 0
			 select new Position(x, y)).ToList()
		);

		public int X { get; private set; }
		public int Y { get; private set; }

		public Position(int x, int y)
		{
			X = x;
			Y = y;
		}

		public static Position operator +(Position p1, Position p2)
		{
			return new Position(p1.X + p2.X, p1.Y + p2.Y);
		}

		public static Position operator -(Position p1, Position p2)
		{
			return new Position(p1.X - p2.X, p1.Y - p2.Y);
		}

		/// <summary>
		/// Parst einen Position-Vektor in Schach-Notation ("a1" bis "i9")
		/// </summary>
		public static Position Parse(string s)
		{
			Debug.Assert(s.Length == 2);
			return new Position(s[0] - 'a', s[1] - '1');
		}

		/// <summary>
		/// Schach-Notation des Vektors
		/// </summary>
		public override string ToString()
		{
			return (char)('a' + X) + (Y + 1).ToString();
		}

		public override bool Equals(object obj)
		{
			if (obj is Position)
			{
				var other = (Position)obj;
				return X == other.X && Y == other.Y;
			}
			else
				return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			return unchecked(X + Y << 16);
		}

		/// <summary>
		/// True gdw. der Vektor einen gültigen Zug darstellt.
		/// </summary>
		public bool IsMoveVector
		{
			get
			{
				int x = Math.Abs(X);
				int y = Math.Abs(Y);
				if (x % 2 == 0 && y % 2 == 0)
				{
					x /= 2;
					y /= 2;
				}
				return (x == 1 && y <= 1) || (x <= 1 && y == 1);
			}
		}
	}

	/// <summary>
	/// Stellt ein Spielbrett inklusive Steine, Löcher und aktuellem und wartendem Spieler dar.
	/// Alle Zug-Methoden beziehen sich auf den aktuellen Spieler.
	/// Diese Klasse ist immutable.
	/// </summary>
	class Board
	{
		IPlayer[] board;
		HashSet<Position> holes;

		public Board(int size, IPlayer player0, IPlayer player1, IEnumerable<Position> holes)
		{
			Debug.Assert(size >= 2);
			Debug.Assert(player0 != null);
			Debug.Assert(player1 != null);
			Debug.Assert(player0 != player1);

			Size = size;
			CurrentPlayer = player0;
			OpposingPlayer = player1;
			this.holes = new HashSet<Position>(holes);

			// Standardaufstellung
			board = new IPlayer[size * size];
			this[new Position(0, 0)] = this[new Position(size - 1, size - 1)] = player1;
			this[new Position(0, size - 1)] = this[new Position(size - 1, 0)] = player0;
		}

		private Board(Board board)
		{
			CurrentPlayer = board.OpposingPlayer;
			OpposingPlayer = board.CurrentPlayer;
			this.Size = board.Size;
			this.board = board.board.ToArray();
			this.holes = board.holes;
		}

		public IPlayer CurrentPlayer { get; private set; }
		public IPlayer OpposingPlayer { get; private set; }

		public int Size { get; private set; }

		/// <summary>
		/// Auflistung aller Felder des Bretts.
		/// </summary>
		public IEnumerable<Position> Positions
		{
			get
			{
				return
					from x in Enumerable.Range(0, Size)
					from y in Enumerable.Range(0, Size)
					let pos = new Position(x, y)
					select pos;
			}
		}

		/// <summary>
		/// Spieler des Steines auf dem angegebenen Feld oder null, falls es unbesetzt oder ein Loch ist.
		/// </summary>
		public IPlayer this[Position pos]
		{
			get { return board[pos.Y * Size + pos.X]; }
			private set { board[pos.Y * Size + pos.X] = value; }
		}

		public Boolean IsHole(Position pos) { return holes.Contains(pos); }

		/// <summary>
		/// True gdw. das angegebene Feld auf dem Brett liegt und kein Loch ist.
		/// </summary>
		public bool IsIndexLegal(Position pos)
		{
			return pos.X >= 0 && pos.X < Size && pos.Y >= 0 && pos.Y < Size && !IsHole(pos);
		}

		public bool IsMoveLegal(Position from, Position to)
		{
			return IsIndexLegal(from) && IsIndexLegal(to) && this[from] == CurrentPlayer && this[to] == null && (from - to).IsMoveVector;
		}

		/// <summary>
		/// True gdw. alle Felder von Spielern oder Löchern besetzt sind.
		/// </summary>
		public bool IsFilled { get { return Positions.All(pos => IsHole(pos) || this[pos] != null); } }

		bool HasLegalMoves { get { return Positions.Any(pos => Position.MoveVectors.Any(move => IsMoveLegal(pos, pos + move))); } }

		/// <summary>
		/// Gibt das Brett zurück, das durch den angegebenen Zug entsteht. Der aktuelle Spieler wechselt, falls der Gegner nicht aussetzen muss.
		/// </summary>
		public Board MovePiece(Position from, Position to)
		{
			if (!IsMoveLegal(from, to))
				throw new ArgumentException("Ungültiger Zug!");

			Board newBoard = new Board(this);
			if (Math.Abs((from - to).X) == 2 || Math.Abs((from - to).Y) == 2)
				newBoard[from] = null;
			newBoard[to] = CurrentPlayer;

			// Flächenschaden
			foreach (Position dir in Position.Directions)
				if (IsIndexLegal(to + dir) && newBoard[to + dir] == OpposingPlayer)
					newBoard[to + dir] = CurrentPlayer;

			// OpposingPlayer muss aussetzen?
			if (!newBoard.HasLegalMoves)
			{
				newBoard.CurrentPlayer = CurrentPlayer;
				newBoard.OpposingPlayer = OpposingPlayer;
			}

			return newBoard;
		}

		/// <summary>
		/// Auflistung aller Felder, die vom angegebenen Spieler besetzt sind.
		/// </summary>
		public IEnumerable<Position> GetPositions(IPlayer player) { return Positions.Where(pos => this[pos] == player); }

		/// <summary>
		/// Anzahl der Felder, die vom angegebenen Spieler besetzt sind.
		/// </summary>
		public int GetPoints(IPlayer player) { return board.Count(b => b == player); }

		/// <summary>
		/// Punktzahl, die der angegebene Spieler vorne liegt (ggf. negativ).
		/// </summary>
		public int GetPointDelta(IPlayer player) { return GetPoints(player) - GetPoints(player == CurrentPlayer ? OpposingPlayer : CurrentPlayer); }
	}
}
