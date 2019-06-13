using System.Runtime.InteropServices;
using UnityEngine;

namespace Seiro.GPUSandbox.CellularGrowthSimulation
{

    [StructLayout(LayoutKind.Sequential)]
    public struct CellularParticle
    {
        public Vector2 position;    // 粒子の座標
        public Vector2 velocity;    // 粒子の保持している速度
        public float rdius;         // 粒子の半径
        public float threshold;     // 粒子の最大半径
        public int links;           // 接続しているエッジの数
        public uint alive;          // 活性化フラグ
    }

    public sealed class CellularGrowthParticleOnly : MonoBehaviour
    {

        public int particlesCount = 2000;
        public ComputeShader compute = null;

        public Material renderMat = null;
        public Gradient gradient;

        private Texture2D _pallete;

        private PingPongBuffer _particlesBuffer;
        private ComputeBuffer _poolBuffer, _dividablePoolBuffer;

        private int[] _countArgs = { 0, 1, 0, 0 };
        private ComputeBuffer _countBuffer;
        private uint[] _drawArgs = { 0, 0, 0, 0, 0 };
        private ComputeBuffer _drawArgsBuffer;

        private Mesh _drawMesh = null;

        private void Start()
        {
            // _poolBufferと_dividablePoolBufferがそれぞれパーティクルのオブジェクプールとして機能している。
            // それぞれのバッファには非活性なパーティクルのインデックスが格納されているので、
            // パーティクルが必要になった場合は、それぞれのプールバッファから一つポップして、その番号のパーティクルをパーティクルバッファから取り出して、アクティブにする。
            // またアクティブではなくなったパーティクルが存在する場合は、その番号を取り出して、プールバッファにpushする。
            // これでオブジェクトプールとして機能させる事ができる。

            // パーティクル本体のデータを保持するためのダブルバッファ
            _particlesBuffer = new PingPongBuffer(particlesCount, typeof(CellularParticle));

            // オブジェクトプールの初期化
            _poolBuffer = new ComputeBuffer(
                particlesCount,
                Marshal.SizeOf(typeof(int)),
                ComputeBufferType.Append        // append bufferとして作成
            );
            _poolBuffer.SetCounterValue(0);

            // append/consume buffer内の要素数を確認するために必要
            _countBuffer = new ComputeBuffer(
                4,
                Marshal.SizeOf(typeof(int)),
                ComputeBufferType.IndirectArguments
            );
            _countBuffer.SetData(_countArgs);

            // 分裂可能なオブジェクトを管理するためのオブジェクトプール
            _dividablePoolBuffer = new ComputeBuffer(
                particlesCount,
                Marshal.SizeOf(typeof(int)),
                ComputeBufferType.Append
            );
            _dividablePoolBuffer.SetCounterValue(0);

            // オブジェクトプールの初期化
            InitParticlesKernel();

            // インスタンシングを行うためのメッシュをセットアップ。
            _drawMesh = UtilFunc.BuildQuad();
            _drawArgs[0] = _drawMesh.GetIndexCount(0);
            _drawArgs[1] = (uint)particlesCount;
            _drawArgsBuffer = new ComputeBuffer(1, Marshal.SizeOf(typeof(uint)) * 5);
            _drawArgsBuffer.SetData(_drawArgs);

            // レンダリング用の各種項目の準備
            _pallete = UtilFunc.CreatePallete(gradient, 128);
            renderMat.SetTexture("_Pallete", _pallete);
        }

        private void Update()
        {
            if (Input.GetMouseButton(0))
            {
                EmitParticlesKernel(GetMousePoint());
            }
        }

        /// <summary>
        /// パーティクルバッファ及び、プールバッファの初期化を行う。
        /// </summary>
        private void InitParticlesKernel()
        {
            if (compute == null) return;
            var kernel = compute.FindKernel("InitParticles");
            compute.SetBuffer(kernel, "_Particles", _particlesBuffer.read);
            compute.SetBuffer(kernel, "_ParticlePoolAppend", _poolBuffer);  // append/consume bufferを設定する。
            Dispatch1D(compute, kernel, particlesCount);
        }

        /// <summary>
        /// 指定した数のパーティクルを生成する。
        /// </summary>
        /// <param name="point"></param>
        /// <param name="emitCount"></param>
        private void EmitParticlesKernel(Vector2 point, int emitCount = 32)
        {
            if (compute == null) return;
            // プール内の使用可能なオブジェクトの数を計算。
            emitCount = Mathf.Min(emitCount, GetRemainParticlesCount());
            if (emitCount <= 0) return;

            var kernel = compute.FindKernel("Emitparticles");
            compute.SetBuffer(kernel, "_Particles", _particlesBuffer.read);
            compute.SetBuffer(kernel, "_ParticlePoolConsume", _poolBuffer);
            compute.SetVector("_Point", point);
            compute.SetInt("_EmitCount", emitCount);

            Dispatch1D(compute, kernel, emitCount);
        }

        private void Dispatch1D(ComputeShader compute, int kernel, int size)
        {
            uint x, y, z;
            compute.GetKernelThreadGroupSizes(kernel, out x, out y, out z);
            compute.Dispatch(kernel, Mathf.CeilToInt(size / (int)x), (int)y, (int)z);
        }

        private Vector2 GetMousePoint()
        {
            Vector2 p = Input.mousePosition;
            Vector3 w = Camera.main.ScreenToWorldPoint(new Vector3(p.x, p.y, Camera.main.nearClipPlane));
            Vector3 l = transform.InverseTransformPoint(w);
            return new Vector2(l.x, l.y);
        }

        private int GetRemainParticlesCount()
        {
            _countBuffer.SetData(_countArgs);
            ComputeBuffer.CopyCount(_poolBuffer, _countBuffer, 0);
            _countBuffer.GetData(_countArgs);
            return _countArgs[0];
        }
    }
}