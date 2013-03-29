using System;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;


namespace GraphiteInv
{
	class Program
	{
		static void Main(string[] args)
		{
			var _appSettings = ConfigurationManager.AppSettings;
			var _metricsHostName = _appSettings["metricsHostName"];
			var _metricsPort = int.Parse(_appSettings["metricsPort"]);
			var _metricsKeyPrefix = _appSettings["metricsKeyPrefix"];

			var _metricClient = new Metric.Client.Recorder(new Metric.Client.StatsdPipe(_metricsHostName, _metricsPort), _metricsKeyPrefix);

			int _increment = 1;
			while (true)
			{
				System.Threading.Thread.Sleep(100);

				Random r = new Random();
				var _randomNumber = r.Next(5);

				if (_randomNumber >= 2)
				{
					_metricClient.Increment("200");
				}
				if (_randomNumber == 3)
				{
					_metricClient.Increment("400");
				}
				if (_randomNumber == 4)
				{
					_metricClient.Increment("500");
				}

				Console.WriteLine(_increment);
				_increment++;

				if (_increment % 10 == 0)
				{
					Console.WriteLine("{0}.deploy");
					_metricClient.Increment("deploy");
				}

				using (var _timer = _metricClient.StartTimer("timer"))
				{
					Thread.Sleep(100 + _randomNumber);
				}
			}
		}

		static void MainOld2(string[] args)
		{
			var _appSettings = ConfigurationManager.AppSettings;
			var _metricsHostName = _appSettings["metricsHostName"];
			var _metricsPort = int.Parse(_appSettings["metricsPort"]);
			var _metricsKeyPrefix = _appSettings["metricsKeyPrefix"];

			var _statsdClient = new Statsd.StatsdPipe(_metricsHostName, _metricsPort);

			int _increment = 1;
			while (true)
			{
				System.Threading.Thread.Sleep(100);

				Random r = new Random();
				var _randomNumber = r.Next(15);

				if (_randomNumber >= 2)
				{
					_statsdClient.Increment(string.Format("{0}.{1}", _metricsKeyPrefix, 200));
				}
				if (_randomNumber == 3)
				{
					_statsdClient.Increment(string.Format("{0}.{1}", _metricsKeyPrefix, 400));
				}
				if (_randomNumber == 4)
				{
					_statsdClient.Increment(string.Format("{0}.{1}", _metricsKeyPrefix, 500));
				}

				Console.WriteLine(_increment);
				_increment++;

				if (_increment % 10 == 0)
				{
					Console.WriteLine(string.Format("{0}.deploy", _metricsKeyPrefix));
					_statsdClient.Increment(string.Format("{0}.deploy", _metricsKeyPrefix));
				}


				_statsdClient.Timing(string.Format("{0}.timer", _metricsKeyPrefix), 300 + _randomNumber);
			}
		}


		static void MainOLD(string[] args)
		{

			var cpuCounter = new PerformanceCounter();

			cpuCounter.CategoryName = "Processor";
			cpuCounter.CounterName = "% Processor Time";
			cpuCounter.InstanceName = "_Total";

			while (true)
			{
				Console.WriteLine(cpuCounter.NextValue()+"%");
				System.Threading.Thread.Sleep(2000);
			}

			var _statsdClient = new Statsd.StatsdPipe("ec2-54-234-97-29.compute-1.amazonaws.com", 8125);

			int _increment = 1;
			while (true)
			{
				System.Threading.Thread.Sleep(100);

				Random r = new Random();
				var _randomNumber = r.Next(15);

				if (_randomNumber >= 2)
				{
					_statsdClient.Increment("myapp.server1.200");
				}
				if (_randomNumber == 3)
				{
					_statsdClient.Increment("myapp.server1.400");
				}
				if (_randomNumber == 4)
				{
					_statsdClient.Increment("myapp.server1.500");
				}

				Console.WriteLine(_increment);
				_increment++;

				if (_increment % 100 == 0)
				{
					Console.WriteLine("event");
					_statsdClient.Increment("events.deploy.myapp");
				}


				_statsdClient.Timing("noel.timmer2", 300 + _randomNumber);
			}
		}
	}
	//class Program
	//{
	//	static void Main(string[] args)
	//	{
	//		var _statsdClient = new Statsd.StatsdPipe("ec2-54-242-38-117.compute-1.amazonaws.com", 8125);

	//		int _increment = 1;
	//		while (true)
	//		{
	//			System.Threading.Thread.Sleep(100);

	//			Random r = new Random();
	//			var _randomNumber = r.Next(10);

	//			if (_randomNumber > 3)
	//			{
	//				_statsdClient.Increment("myapp.server1");
	//			}
	//			else
	//			{
	//				_statsdClient.Decrement("myapp.server1");
	//			}

	//			Console.WriteLine(_increment);
	//			_increment++;

	//			_statsdClient.Timing("noel.timmer2", 100 + _randomNumber);
	//		}
	//	}
	//}


	namespace Statsd
	{
		public class StatsdPipe : IDisposable
		{
			private readonly UdpClient udpClient;

			[ThreadStatic]
			private static Random random;

			private static Random Random
			{
				get
				{
					return random ?? (random = new Random());
				}
			}

			public StatsdPipe(string host, int port)
			{
				udpClient = new UdpClient(host, port);
			}

			public bool Gauge(string key, int value)
			{
				return Gauge(key, value, 1.0);
			}

			public bool Gauge(string key, int value, double sampleRate)
			{
				return Send(sampleRate, String.Format("{0}:{1:d}|g", key, value));
			}

			public bool Timing(string key, int value)
			{
				return Timing(key, value, 1.0);
			}

			public bool Timing(string key, int value, double sampleRate)
			{
				return Send(sampleRate, String.Format("{0}:{1:d}|ms", key, value));
			}

			public bool Decrement(string key)
			{
				return Increment(key, -1, 1.0);
			}

			public bool Decrement(string key, int magnitude)
			{
				return Decrement(key, magnitude, 1.0);
			}

			public bool Decrement(string key, int magnitude, double sampleRate)
			{
				magnitude = magnitude < 0 ? magnitude : -magnitude;
				return Increment(key, magnitude, sampleRate);
			}

			public bool Decrement(params string[] keys)
			{
				return Increment(-1, 1.0, keys);
			}

			public bool Decrement(int magnitude, params string[] keys)
			{
				magnitude = magnitude < 0 ? magnitude : -magnitude;
				return Increment(magnitude, 1.0, keys);
			}

			public bool Decrement(int magnitude, double sampleRate, params string[] keys)
			{
				magnitude = magnitude < 0 ? magnitude : -magnitude;
				return Increment(magnitude, sampleRate, keys);
			}

			public bool Increment(string key)
			{
				return Increment(key, 1, 1.0);
			}

			public bool Increment(string key, int magnitude)
			{
				return Increment(key, magnitude, 1.0);
			}

			public bool Increment(string key, int magnitude, double sampleRate)
			{
				string stat = String.Format("{0}:{1}|c", key, magnitude);
				return Send(stat, sampleRate);
			}

			public bool Increment(int magnitude, double sampleRate, params string[] keys)
			{
				return Send(sampleRate, keys.Select(key => String.Format("{0}:{1}|c", key, magnitude)).ToArray());
			}

			protected bool Send(String stat, double sampleRate)
			{
				return Send(sampleRate, stat);
			}

			protected bool Send(double sampleRate, params string[] stats)
			{
				var retval = false; // didn't send anything
				if (sampleRate < 1.0)
				{
					foreach (var stat in stats)
					{
						if (Random.NextDouble() <= sampleRate)
						{
							var statFormatted = String.Format("{0}|@{1:f}", stat, sampleRate);
							if (DoSend(statFormatted))
							{
								retval = true;
							}
						}
					}
				}
				else
				{
					foreach (var stat in stats)
					{
						if (DoSend(stat))
						{
							retval = true;
						}
					}
				}

				return retval;
			}

			protected bool DoSend(string stat)
			{
				//var data = Encoding.Default.GetBytes(stat + "\n");
				var data = Encoding.Default.GetBytes(stat);

				udpClient.Send(data, data.Length);
				return true;
			}

			#region IDisposable Members

			public void Dispose()
			{
				try
				{
					if (udpClient != null)
					{
						udpClient.Close();
					}
				}
				catch
				{
				}
			}

			#endregion
		}
	}
}