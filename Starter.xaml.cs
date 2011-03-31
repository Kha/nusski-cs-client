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
using System.Windows.Threading;
using System.Threading;
using System.Reflection;

namespace Client
{
	public partial class Starter : Window
	{
		Server server;
		DispatcherTimer challengerTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10), IsEnabled = true };

		public Starter()
		{
			InitializeComponent();

			DataContext = Assembly.GetExecutingAssembly().GetTypes()
				.Where(t => typeof(IPlayer).IsAssignableFrom(t) && !t.IsAbstract && t != typeof(Server.NetworkEnemy));

			challengerTimer.Tick += delegate
			{
				if (server == null)
					return;

				// TOTAL WAR
				foreach (var enemy in server.GetAvailableEnemies())
					server.Challenge(enemy);
			};
		}

		IPlayer Instantiate(ComboBox box)
		{
			return (IPlayer)Activator.CreateInstance((Type)box.SelectedItem);
		}

		private void connectButton_Click(object sender, RoutedEventArgs e)
		{
			if (server == null)
			{
				var localPlayer = Instantiate(remoteBotBox);
				server = new Server();
				server.GameStarted += game => {
					var thread = new Thread(() => {
						var board = new Board(9, game.ServerStarts ? game.RemotePlayer : localPlayer, game.ServerStarts ? localPlayer : game.RemotePlayer, game.Holes);
						var window = new GameWindow(board);
						window.ShowDialog();
						Dispatcher.BeginInvoke(new Action(() => logBox.Text += window.Result + Environment.NewLine));
					}) { IsBackground = true };
					thread.SetApartmentState(ApartmentState.STA);
					thread.Start();
				};

				connectButton.Content = "Trennen";
			}
			else
			{
				server.Dispose();
				server = null;
				connectButton.Content = "Verbinden";
			}
		}

		private void startLocalButton_Click(object sender, RoutedEventArgs e)
		{
			var board = new Board(9, Instantiate(localBot1Box), Instantiate(localBot2Box), holes: new[] { new Position(4, 4) });
			var thread = new Thread(() => {
				var window = new GameWindow(board);
				window.ShowDialog();
				Dispatcher.BeginInvoke(new Action(() => logBox.Text += window.Result + Environment.NewLine));
			}) { IsBackground = true };
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			if (server != null)
				server.Dispose();
			Application.Current.Shutdown();
		}
	}
}
