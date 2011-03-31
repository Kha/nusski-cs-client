using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Client
{
	abstract class Bot : IPlayer
	{
		/// <summary>
		/// Maximale Zugzeit, kann sich im Verlauf des Turniers noch ändern.
		/// </summary>
		public static readonly TimeSpan MaxThinkingTime = TimeSpan.FromSeconds(10);

		/// <summary>
		/// Überschreibe diese Methode, um die Bot-Logik zu implementieren: Gib abhängig vom angegebenen Brettzustand
		/// einen gültigen Zug zurück
		/// </summary>
		public abstract Move MakeMove(Board board);

		void IPlayer.MakeMove(Board board, Move lastMove, Action<Move> continuation)
		{
			continuation(MakeMove(board));
		}

		public string Name
		{
			get { return GetType().Name; }
		}
	}

	/// <summary>
	/// Ein höchst harmloser Beispiel-Bot.
	/// </summary>
	class Randomy : Bot
	{
		Random rand = new Random();

		public override Move MakeMove(Board board)
		{
			Position[] myPieces = board.GetPositions(this).ToArray();
			Position randomPiece = myPieces[rand.Next(myPieces.Length)];
			Position randomMove = Position.MoveVectors[rand.Next(Position.MoveVectors.Count)];

			if (board.IsMoveLegal(randomPiece, randomPiece + randomMove))
				return new Move(randomPiece, randomPiece + randomMove);
			else
				return MakeMove(board); // better luck next time
		}
	}
}
