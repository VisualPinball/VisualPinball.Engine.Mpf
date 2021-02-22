using System;
using VisualPinball.Engine.Mpf.Unity;

namespace VisualPinball.Engine.Mpf
{
	public static class MpfTest
	{
		public static void Main(string[] args)
		{
			if (args == null) {
				throw new ArgumentNullException(nameof(args));
			}

			Console.WriteLine("Starting...");
			var client = new MpfClient();
			client.Connect();
			client.Play();
			Console.WriteLine("Description = " + client.GetMachineDescription());
			Console.WriteLine("Done!");
		}
	}
}
