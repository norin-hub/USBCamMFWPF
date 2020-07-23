using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Decoder
{
	public static class Yuy2Decoder
	{
		public static byte[] DecompressYUY2(byte[] input, int width, int height)
		{
			byte[] output = new byte[width * height * sizeof(uint)];
			DecompressYUY2(input, width, height, output);
			return output;
		}

		public static void DecompressYUY2(byte[] input, int width, int height, byte[] output)
		{
			int p = 0;
			int o = 0;
			int halfWidth = width / 2;
			for (int j = 0; j < height; j++)
			{
				for (int i = 0; i < halfWidth; ++i)
				{
					int y0 = input[p++];
					int u0 = input[p++];
					int y1 = input[p++];
					int v0 = input[p++];
					int c = y0 - 16;
					int d = u0 - 128;
					int e = v0 - 128;
					output[o++] = ClampByte((298 * c + 516 * d + 128) >> 8);            // blue
					output[o++] = ClampByte((298 * c - 100 * d - 208 * e + 128) >> 8);  // green
					output[o++] = ClampByte((298 * c + 409 * e + 128) >> 8);            // red
					output[o++] = 255;
					c = y1 - 16;
					output[o++] = ClampByte((298 * c + 516 * d + 128) >> 8);            // blue
					output[o++] = ClampByte((298 * c - 100 * d - 208 * e + 128) >> 8);  // green
					output[o++] = ClampByte((298 * c + 409 * e + 128) >> 8);            // red
					output[o++] = 255;
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte ClampByte(int x)
		{
			return (byte)(byte.MaxValue < x ? byte.MaxValue : (x > byte.MinValue ? x : byte.MinValue));
		}
	}

	public static class I420Decoder
    {
		public static void DecompressI420(byte[] input, int width, int height, byte[] output)
		{
			//  y0,y1,y0_2,y1_2
			//  u0,u0_2
			//  v0,v0_2

			int p = 0;
			int p2 = 0;
			int p3 = 0;
			int o = 0;
			int halfWidth = width / 2;

			int total = (height * halfWidth * 4) / 2;
			p2 = (total/2);
			p3 = (total);

			for(int i = 0; i < (total/2); i++)
            {
				int y0 = input[p++];
				int y1 = input[p++];
				int u0 = input[p2++];
				int v0 = input[p3++];

                //		int y0 = input[p++];
                //		int y1 = input[p++];
                //		int u0 = input[p++];
                //		int v0 = input[p++];
                int c = y0 - 16;
                int d = u0 - 128;
                int e = v0 - 128;
				output[o++] = Yuy2Decoder.ClampByte((298 * c + 409 * e + 128) >> 8);            // red
				output[o++] = Yuy2Decoder.ClampByte((298 * c - 100 * d - 208 * e + 128) >> 8);  // green
				output[o++] = Yuy2Decoder.ClampByte((298 * c + 516 * d + 128) >> 8);            // blue
																								//output[o++] = 255;
				c = y1 - 16;
				output[o++] = Yuy2Decoder.ClampByte((298 * c + 409 * e + 128) >> 8);            // red
                output[o++] = Yuy2Decoder.ClampByte((298 * c - 100 * d - 208 * e + 128) >> 8);  // green
				output[o++] = Yuy2Decoder.ClampByte((298 * c + 516 * d + 128) >> 8);            // blue
																								//output[o++] = 255;

			}


			//for (int j = 0; j < height; j++)
			//{
			//	for (int i = 0; i < halfWidth; ++i)
			//	{
			//		int y0 = input[p++];
			//		int y1 = input[p++];
			//		int u0 = input[p++];
			//		int v0 = input[p++];
			//		int c = y0 - 16;
			//		int d = u0 - 128;
			//		int e = v0 - 128;
			//		output[o++] = Yuy2Decoder.ClampByte((298 * c + 516 * d + 128) >> 8);            // blue
			//		output[o++] = Yuy2Decoder.ClampByte((298 * c - 100 * d - 208 * e + 128) >> 8);  // green
			//		output[o++] = Yuy2Decoder.ClampByte((298 * c + 409 * e + 128) >> 8);            // red
			//		output[o++] = 255;
			//		c = y1 - 16;
			//		output[o++] = Yuy2Decoder.ClampByte((298 * c + 516 * d + 128) >> 8);            // blue
			//		output[o++] = Yuy2Decoder.ClampByte((298 * c - 100 * d - 208 * e + 128) >> 8);  // green
			//		output[o++] = Yuy2Decoder.ClampByte((298 * c + 409 * e + 128) >> 8);            // red
			//		output[o++] = 255;
			//	}
			//}



		}
	}
}
