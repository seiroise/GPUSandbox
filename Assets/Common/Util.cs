using UnityEngine;
using System;
using System.Runtime.InteropServices;

namespace Seiro.GPUSandbox
{

	public sealed class PingPongBuffer : IDisposable
	{
		ComputeBuffer[] _buffers;
		int _read = 0, _write = 1;

		public PingPongBuffer(int count, Type type)
		{
			_buffers = new ComputeBuffer[2];
			_buffers[0] = new ComputeBuffer(count, Marshal.SizeOf(type), ComputeBufferType.Default);
			_buffers[1] = new ComputeBuffer(count, Marshal.SizeOf(type), ComputeBufferType.Default);
		}

		public ComputeBuffer read { get { return _buffers[_read]; } }
		public ComputeBuffer write { get { return _buffers[_write]; } }

		public void Dispose()
		{
			UtilFunc.ReleaseBuffer(ref _buffers[0]);
			UtilFunc.ReleaseBuffer(ref _buffers[1]);
			_buffers = null;
		}

		public void Swap() { var tmp = _read; _read = _write; _write = tmp; }
	}

    public static class UtilFunc
    {
        public static void ReleaseBuffer(ref ComputeBuffer buffer)
        {
            if (buffer != null)
            {
                buffer.Release();
                buffer = null;
            }
        }

        public static void ReleaseRT(ref RenderTexture rt)
        {
            if (rt != null)
            {
                rt.Release();
                rt = null;
            }
        }

        public static void SwapBuffer(ref ComputeBuffer ping, ref ComputeBuffer pong)
        {
            ComputeBuffer temp = ping;
            ping = pong;
            pong = temp;
        }

		public static void Swap<T>(ref T ping, ref T pong)
		{
			T temp = ping;
			ping = pong;
			pong = temp;
		}
    }
}