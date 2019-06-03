using UnityEngine;
using System;
using System.Runtime.InteropServices;

namespace Seiro.GPUSandbox
{
    [Serializable]
    public sealed class PingPongBuffer : IDisposable
    {
        [SerializeField]
        ComputeBuffer[] _buffers;
        int _r = 0, _w = 1;

        public PingPongBuffer(int count, Type type)
        {
            _buffers = new ComputeBuffer[2];
            _buffers[0] = new ComputeBuffer(count, Marshal.SizeOf(type), ComputeBufferType.Default);
            _buffers[1] = new ComputeBuffer(count, Marshal.SizeOf(type), ComputeBufferType.Default);
        }

        public ComputeBuffer read { get { return _buffers[_r]; } }
        public ComputeBuffer write { get { return _buffers[_w]; } }

        public void Dispose()
        {
            UtilFunc.ReleaseBuffer(ref _buffers[0]);
            UtilFunc.ReleaseBuffer(ref _buffers[1]);
            _buffers = null;
        }

        public void Swap() { var tmp = _r; _r = _w; _w = tmp; }
    }

    [Serializable]
    public sealed class PingPongTexture : IDisposable
    {
        [SerializeField]
        RenderTexture[] _textures;
        int _r = 0, _w = 1;

        public PingPongTexture(string name, RenderTextureDescriptor desc)
        {
            _textures = new RenderTexture[2];
            _textures[0] = RenderTexture.GetTemporary(desc);
            _textures[0].name = name;
            _textures[1] = RenderTexture.GetTemporary(desc);
            _textures[1].name = name;
        }

        public RenderTexture read { get { return _textures[_r]; } }
        public RenderTexture write { get { return _textures[_w]; } }

        public void Dispose()
        {
            UtilFunc.ReleaseRT(ref _textures[0]);
            UtilFunc.ReleaseRT(ref _textures[1]);
            _textures = null;
        }

        public void Swap() { var tmp = _r; _r = _w; _w = tmp; }
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