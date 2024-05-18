using MikanCore;

namespace MikanXR
{
	public class MikanRenderTargetDescriptor
	{
		public MikanColorBufferType color_buffer_type { get; set; }
		public MikanDepthBufferType depth_buffer_type { get; set; }
		public uint width { get; set; }
		public uint height { get; set; }
		public MikanClientGraphicsApi graphicsAPI { get; set; }
	};

	public class MikanClientFrameRendered
	{
		public ulong frame_index { get; set; }
		public float zNear { get; set; }
		public float zFar { get; set; }
	};
}