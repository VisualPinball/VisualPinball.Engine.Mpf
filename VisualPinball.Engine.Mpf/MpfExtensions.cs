using Mpf.Vpe;

namespace VisualPinball.Engine.Mpf
{
	public static class MpfExtensions
	{
		public static byte[] FrameData(this SetDmdFrameRequest req)
		{
			return req.Frame.ToByteArray();
		}
	}
}
