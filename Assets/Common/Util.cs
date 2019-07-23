using UnityEngine;
using UnityEngine.Rendering;
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

        public PingPongTexture(string name, RenderTextureDescriptor desc, TextureWrapMode wrapMode = TextureWrapMode.Clamp)
        {
            _textures = new RenderTexture[2];

            _textures[0] = RenderTexture.GetTemporary(desc);
            _textures[0].name = name;
            _textures[0].wrapMode = wrapMode;

            _textures[1] = RenderTexture.GetTemporary(desc);
            _textures[1].name = name;
            _textures[1].wrapMode = wrapMode;
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

    /// <summary>
    /// オブジェクトプール用の基底クラス
    /// オブジェクトに対応するインデックスのプールとそれの数を把握するためのカウントバッファを保持。
    /// </summary>
    [Serializable]
    public abstract class GPUObjectPoolBase : IDisposable
    {
        ComputeBuffer _poolBuffer;
        public ComputeBuffer poolBuffer { get { return _poolBuffer; } }
        ComputeBuffer _countBuffer;
        public ComputeBuffer countBuffer { get { return _countBuffer; } }
        int[] _countArgs = { 0, 1, 0, 0 };

        public GPUObjectPoolBase(int count, Type type)
        {
            _poolBuffer = new ComputeBuffer(count, Marshal.SizeOf(typeof(int)), ComputeBufferType.Append);
            _poolBuffer.SetCounterValue(0);
            _countBuffer = new ComputeBuffer(4, Marshal.SizeOf(typeof(int)), ComputeBufferType.IndirectArguments);
        }

        public virtual void Dispose()
        {
            UtilFunc.ReleaseBuffer(ref _poolBuffer);
            UtilFunc.ReleaseBuffer(ref _countBuffer);
        }

        public int GetRemainingObjectsCount()
        {
            _countBuffer.SetData(_countArgs);
            ComputeBuffer.CopyCount(_poolBuffer, _countBuffer, 0);
            _countBuffer.GetData(_countArgs);
            return _countArgs[0];
        }
    }

    [Serializable]
    public sealed class GPUIndexPool : GPUObjectPoolBase
    {
        public GPUIndexPool(int count) : base(count, typeof(int)) { }
    }

    [Serializable]
    public sealed class GPUObjectPool : GPUObjectPoolBase
    {
        ComputeBuffer _objectBuffer;
        public ComputeBuffer objectBuffer { get { return _objectBuffer; } }

        public GPUObjectPool(int count, Type type) : base(count, type)
        {
            _objectBuffer = new ComputeBuffer(count, Marshal.SizeOf(type), ComputeBufferType.Default);
        }

        public override void Dispose()
        {
            base.Dispose();
            UtilFunc.ReleaseBuffer(ref _objectBuffer);
            _objectBuffer = null;
        }
    }

    [Serializable]
    public sealed class GPUPingPongObjectPool : GPUObjectPoolBase
    {
        ComputeBuffer[] _buffers;
        int _r = 0, _w = 1;

        public ComputeBuffer read { get { return _buffers[_r]; } }
        public ComputeBuffer write { get { return _buffers[_w]; } }

        public GPUPingPongObjectPool(int count, Type type) : base(count, type)
        {
            _buffers = new ComputeBuffer[2];
            _buffers[0] = new ComputeBuffer(count, Marshal.SizeOf(type), ComputeBufferType.Default);
            _buffers[1] = new ComputeBuffer(count, Marshal.SizeOf(type), ComputeBufferType.Default);
        }

        public override void Dispose()
        {
            base.Dispose();
            UtilFunc.ReleaseBuffer(ref _buffers[0]);
            UtilFunc.ReleaseBuffer(ref _buffers[1]);
            _buffers = null;
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

        public static Mesh BuildQuad()
        {
            var mesh = new Mesh();
            mesh.hideFlags = HideFlags.HideAndDontSave;

            mesh.vertices = new Vector3[] {
                new Vector3(-0.5f,  0.5f, 0f), new Vector3( 0.5f,  0.5f, 0f),
                new Vector3( 0.5f, -0.5f, 0f), new Vector3(-0.5f, -0.5f, 0f)
            };
            mesh.uv = new Vector2[] {
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 1f), new Vector2(0f, 1f)
            };
            mesh.SetIndices(
                new int[] {
                0, 1, 2,
                2, 3, 0
                },
                MeshTopology.Triangles,
                0
            );
            mesh.RecalculateBounds();

            return mesh;
        }

        public static RenderTextureDescriptor CreateCommonDesc()
        {
            return CreateCommonDesc(Screen.width, Screen.height);
        }

        public static RenderTextureDescriptor CreateCommonDesc(int width, int height)
        {
            var desc = new RenderTextureDescriptor();
            desc.width = width;
            desc.height = height;
            desc.autoGenerateMips = false;
            desc.depthBufferBits = 0;
            desc.volumeDepth = 1;
            desc.msaaSamples = 1;
            desc.dimension = TextureDimension.Tex2D;
            desc.sRGB = false;
            return desc;
        }

        public static Texture2D CreatePallete(Gradient grad, int width)
        {
            var tex = new Texture2D(width, 1);
            var invW = 1f / width;
            for (int x = 0; x < width; ++x)
            {
                tex.SetPixel(x, 0, grad.Evaluate(x * invW));
            }
            tex.Apply();
            return tex;
        }

        public static void Dispatch1D(ComputeShader compute, int kernel, int threads)
        {
            uint x, y, z;
            compute.GetKernelThreadGroupSizes(kernel, out x, out y, out z);
            compute.Dispatch(kernel, Mathf.CeilToInt(threads / (float)x), (int)y, (int)z);
        }
    }
}