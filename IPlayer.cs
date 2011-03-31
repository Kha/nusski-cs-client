using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Client
{
	interface IPlayer
	{
		void MakeMove(Board board, Move lastMove, Action<Move> continuation);
		string Name { get; }
	}

	class Move
	{
		public Position From { get; private set; }
		public Position To { get; private set; }

		public Move(Position from, Position to)
		{
			From = from;
			To = to;
		}
	}
}
