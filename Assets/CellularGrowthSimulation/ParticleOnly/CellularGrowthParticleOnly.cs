using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Seiro.GPUSandbox.CellularGrowthSimulation
{

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct CGS_Particle2D
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

        public float deltaTime = 0.016f;
        public float drag = 1f;
        public float limit = 1f;
        public float repulsion = 1f;
        public float grow = 1f;
        public float divideInterval = 0.5f;

        [Space]

        public Material renderMat = null;
        public Vector3 simulationRange = new Vector3(10f, 10f, 10f);
        public Gradient gradient;

        [Space]

        public CGS_Particle2D[] dumpParticles;
        public int[] dumpIndices;

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
            _particlesBuffer = new PingPongBuffer(particlesCount, typeof(CGS_Particle2D));

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
            _drawMesh = UtilFunc.BuildQuad();
        }

        private void Update()
        {
            if (Input.GetMouseButton(0))
            {
                EmitParticlesKernel(GetMousePoint());
            }
            UpdateParticles();
            RenderParticles();

            if (Input.GetKeyDown(KeyCode.Space))
            {
                DumpParticleData();
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
            compute.SetBuffer(kernel, "_ParticlePoolAppend", _poolBuffer);
            compute.SetInt("_ParticlesCount", particlesCount);
            Dispatch1D(compute, kernel, particlesCount);

            // _particlesBuffer.Swap();
        }

        /// <summary>
        /// 指定した数のパーティクルを生成する。
        /// </summary>
        /// <param name="point"></param>
        /// <param name="emitCount"></param>
        private void EmitParticlesKernel(Vector2 point, int emitCount = 8)
        {
            if (compute == null) return;
            // プール内の使用可能なオブジェクトの数を計算。
            emitCount = Mathf.Min(emitCount, GetRemainParticlesCount());
            Debug.Log(emitCount + " : " + GetRemainParticlesCount());
            if (emitCount <= 0) return;

            var kernel = compute.FindKernel("EmitParticles");
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
            compute.Dispatch(kernel, Mathf.CeilToInt(size / (float)x), (int)y, (int)z);
        }

        private Vector2 GetMousePoint()
        {
            Vector2 p = Input.mousePosition;
            Vector3 w = Camera.main.ScreenToWorldPoint(new Vector3(p.x, p.y, -Camera.main.transform.position.z));
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

        private void UpdateParticles()
        {
            if (compute == null) return;
            int kernel = compute.FindKernel("UpdateParticles");

            compute.SetBuffer(kernel, "_ParticlesRead", _particlesBuffer.read);
            compute.SetBuffer(kernel, "_Particles", _particlesBuffer.write);

            compute.SetFloat("_DT", deltaTime);
            compute.SetFloat("_Drag", drag);
            compute.SetFloat("_Limit", limit);
            compute.SetFloat("_Repulsion", repulsion);
            compute.SetFloat("_Grow", grow);

            Dispatch1D(compute, kernel, particlesCount);

            _particlesBuffer.Swap();
        }

        private void RenderParticles()
        {
            if (renderMat == null || _drawMesh == null)
            {
                return;
            }

            renderMat.SetPass(0);
            renderMat.SetBuffer("buf", _particlesBuffer.read);
            Graphics.DrawMeshInstancedIndirect(_drawMesh, 0, renderMat, new Bounds(Vector3.zero, simulationRange), _drawArgsBuffer);
        }

        private IEnumerator Divider()
        {
            yield return 0;
            while (true)
            {
                yield return new WaitForSeconds(divideInterval);
                DivideParticles();
            }
        }

        private void DivideParticles()
        {
            StoreDividableParticles();
        }

        private void StoreDividableParticles()
        {
            _dividablePoolBuffer.SetCounterValue(0);

            var kernel = compute.FindKernel("StoreDividableParticles");
            compute.SetBuffer(kernel, "_ParticlesRead", _particlesBuffer.read);
            compute.SetBuffer(kernel, "_DividablePoolAppend", _dividablePoolBuffer);

            Dispatch1D(compute, kernel, particlesCount);
        }

        private void DumpParticleData()
        {
            if (dumpParticles.Length != particlesCount)
            {
                dumpParticles = new CGS_Particle2D[particlesCount];
                dumpIndices = new int[particlesCount];
            }
            _particlesBuffer.read.GetData(dumpParticles);
            _poolBuffer.GetData(dumpIndices);
        }
    }
}