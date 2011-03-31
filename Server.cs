using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Windows.Threading;

namespace Client
{
	class Server : IDisposable
	{
		/*************************************************************************
		 *                       Deine Login-Daten                               *
		 *************************************************************************/
		const string ClientName = "";
		const string ClientSecret = "";


		const string ServerUri = "http://entwickler-ecke.de/nusski/nuss.php";
		const string ServerVersion = "3";

		internal class NetworkEnemy : IPlayer
		{
			public string GameId { get; private set; }
			public EnemyId EnemyId { get; private set; }

			Server server;
			BlockingCollection<Move> moves = new BlockingCollection<Move>();

			public NetworkEnemy(Server server, string gameId, EnemyId enemyId)
			{
				GameId = gameId;
				this.server = server;
				EnemyId = enemyId;
			}

			public void MakeMove(Board board, Move lastMove, Action<Move> continuation)
			{
				Move move;
				// Bricht mit false ab, falls Spiel abgebrochen wurde
				if (!moves.TryTake(out move, millisecondsTimeout: -1))
					move = null;
				continuation(move);
			}

			public void ReportEnemyMove(Move move)
			{
				string moveResponse = DownloadString("?mode=move&session={0}&game={1}&from={2}&to={3}", server.sessionId, GameId, move.From.ToString(),
					move.To.ToString());
				if (moveResponse.StartsWith("ERR:") && moveResponse != "ERR:Session")
					throw new InvalidOperationException(moveResponse);
			}

			public void PushMove(Move move)
			{
				moves.Add(move);
			}

			public void GameEnded()
			{
				moves.CompleteAdding();
			}

			public string Name
			{
				get { return EnemyId.Name; }
			}
		}

		string sessionId;
		Dictionary<string, NetworkEnemy> connectedClients = new Dictionary<string, NetworkEnemy>();

		public event Action<ServerGame> GameStarted = delegate { };

		static Server()
		{
			ServicePointManager.DefaultConnectionLimit = 50;
		}

		public Server()
		{
			Login();
		}

		[ThreadStatic]
		static WebClient client;
		static string DownloadString(string uriPart, params object[] args)
		{
			if (client == null)
				client = new WebClient();
			return client.DownloadString(ServerUri + string.Format(uriPart, args));
		}

		void Login()
		{
			string login = DownloadString("?mode=startSession&secret={0}&client={1}", ClientSecret, ClientName);
			if (login.StartsWith("ERR:"))
				throw new InvalidOperationException(login);
			string[] args = login.SplitEx(',');
			if (args[1] != ServerVersion)
				throw new InvalidOperationException("Neue Serverversion: " + args[1]);

			sessionId = args[0];
			new Thread(() => {
				while (true)
					PullActions();
			}) { IsBackground = true, Name = "PullActions loop" }.Start();
		}

		public IEnumerable<EnemyId> GetAvailableEnemies()
		{
			return DownloadString("?mode=listClients&session={0}", sessionId).SplitEx(';')
				.Select(enemyId => new EnemyId { SessionNo = enemyId.SplitEx(',')[0], Name = enemyId.SplitEx(',')[1] });
		}

		public void Challenge(EnemyId enemyId)
		{
			lock (connectedClients)
				if (!connectedClients.Values.Any(e => e.EnemyId.SessionNo == enemyId.SessionNo))
				{
					Debug.WriteLine("Challenged " + enemyId.SessionNo + "," + enemyId.Name);
					DownloadString("?mode=startGame&sessionNo={0}&session={1}", enemyId.SessionNo, sessionId);
				}
		}

		public void PullActions()
		{
			string response;
			do {
				response = DownloadString("?mode=getActionsLong&session={0}", sessionId);
			} while (string.IsNullOrEmpty(response));

			foreach (string action in response.SplitEx(';'))
				lock (connectedClients)
				{
					string[] args = action.SplitEx(':');
					string[] argsargs = args[1].SplitEx(',');
					string gameId = argsargs[0];
					switch (args[0])
					{
						case "gs":
							var enemy = new NetworkEnemy(this, gameId, new EnemyId { Name = argsargs[2], SessionNo = argsargs[3] });
							connectedClients.Add(gameId, enemy);
							GameStarted(new ServerGame { Holes = argsargs.Skip(4).Select(Position.Parse).ToArray(), ServerStarts = argsargs[1] == "w", RemotePlayer = enemy });
							break;
						case "mv":
							connectedClients[gameId].PushMove(new Move(Position.Parse(argsargs[1]), Position.Parse(argsargs[2])));
							break;
						case "ge":
						case "ga":
							connectedClients[gameId].GameEnded();
							connectedClients.Remove(gameId);
							break;
					}
				}
		}

		/// <summary>
		/// Beendet die Session und bricht damit alle Spiele ab.
		/// </summary>
		public void Dispose()
		{
			DownloadString("?mode=endSession&session={0}", sessionId);
		}
	}

	class EnemyId
	{
		public string SessionNo { get; set; }
		public string Name { get; set; }
	}

	class ServerGame
	{
		public IEnumerable<Position> Holes { get; set; }
		public bool ServerStarts { get; set; }
		public IPlayer RemotePlayer { get; set; }
	}

	static class StringEx
	{
		public static string[] SplitEx(this string s, params char[] sps)
		{
			return string.IsNullOrEmpty(s) ? new string[0] : s.Split(sps);
		}
	}
}
