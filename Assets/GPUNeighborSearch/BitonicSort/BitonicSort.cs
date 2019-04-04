using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Seiro.GPUSandbox.NS
{

    public sealed class BitonicSort
    {
        // バイトニックソートを行う基本単位
        // スレッドグループごとに共有メモリ上にロードしてからソート処理を行うので、
        // その時のシェーダモデル、APIに依存して使用可能量の上限は決めたほうが良い。
        // https://docs.microsoft.com/ja-jp/windows/desktop/direct3dhlsl/dx-graphics-hlsl-variable-syntax
        public static readonly int BITONIC_BLOCK_SIZE = 512;
        public static readonly int TRANSPOSE_BLOCK_SIZE = 16;

        // Shader関連のパラメータはここで一旦IDと紐づけておく、
        // 毎回文字列を入力するのは間違いが起きやすいのと、
        // シェーダ自体にそれらのプロパティが含まれているか？の確認にもなり、毎回プロパティIDへの参照がなくなるので処理も早くなる。
        // 一石十鳥ぐらいの効果がある。
        public class ShaderParameters
        {
            public readonly int bitonicSortKernelId;
            public readonly int matrixTransposeKernelId;

            public readonly int levelId;
            public readonly int levelMaskId;
            public readonly int widthId;
            public readonly int heightId;

            public readonly int inputBufId;
            public readonly int dataBufId;

            public ShaderParameters(ref ComputeShader bitonicCS)
            {
                bitonicSortKernelId = bitonicCS.FindKernel("BitonicSort");
                matrixTransposeKernelId = bitonicCS.FindKernel("MatrixTranspose");

                levelId = Shader.PropertyToID("_Level");
                levelMaskId = Shader.PropertyToID("_LevelMask");
                widthId = Shader.PropertyToID("_Width");
                heightId = Shader.PropertyToID("_Height");

                inputBufId = Shader.PropertyToID("_Input");
                dataBufId = Shader.PropertyToID("_Data");
            }
        }

        private int _numElements;
        private ComputeShader _bitonicCS;

        private ShaderParameters _params;

        public BitonicSort(int numElements, ComputeShader bitonicCS)
        {
            _numElements = numElements;
            _bitonicCS = bitonicCS;

            _params = new ShaderParameters(ref bitonicCS);
        }

        public void Sort(ref ComputeBuffer inBuffer, ref ComputeBuffer tempBuffer)
        {
            // バイトニックソート時の比較対象がBITONIC_SORT_SIZEの範囲内に存在するレベルの場合は
            // そのままバイトニックソートを行う。(比較対象がすべて共有メモリ上に読み込めるので)
            for (int l = 2; l <= BITONIC_BLOCK_SIZE; l <<= 1)
            {
                // levelとlevel maskは同じ値を使用する
                SetGPUSortConstants(_bitonicCS, l, l, 0, 0);
                // 計算用のバッファを設定してそのままソートする。
                _bitonicCS.SetBuffer(_params.bitonicSortKernelId, _params.dataBufId, inBuffer);
                _bitonicCS.Dispatch(_params.bitonicSortKernelId, _numElements / BITONIC_BLOCK_SIZE, 1, 1);

				// LogForDebug(inBuffer);
            }

            // ソートを行う範囲が、BITONIC_BLOCK_SIZEよりも大きい場合はここから先の処理を行う。
            // そのままソートのための比較を行うことができないので、一旦転置処理を行い、
            // メモリ空間上で対象の値を近くに配置し直してから、一定の範囲を共有メモリに乗せつつ処理を行う。

            int matrixWidth = BITONIC_BLOCK_SIZE;
            int matrixHeight = _numElements / BITONIC_BLOCK_SIZE;

            for (int l = (BITONIC_BLOCK_SIZE << 1); l <= _numElements; l <<= 1)
            {
                int level = l / BITONIC_BLOCK_SIZE;
                // level maskはlevelがnumElementsと同じときに0になる。
                int levelMask = (l & ~_numElements) / BITONIC_BLOCK_SIZE;

                // 転置した状態でのソート
                SetGPUSortConstants(_bitonicCS, level, levelMask, matrixWidth, matrixHeight);
                _bitonicCS.SetBuffer(_params.matrixTransposeKernelId, _params.inputBufId, inBuffer);
                _bitonicCS.SetBuffer(_params.matrixTransposeKernelId, _params.dataBufId, tempBuffer);
                _bitonicCS.Dispatch(_params.matrixTransposeKernelId, matrixWidth / TRANSPOSE_BLOCK_SIZE, matrixHeight / TRANSPOSE_BLOCK_SIZE, 1);

                // tempBufferのソート
                _bitonicCS.SetBuffer(_params.bitonicSortKernelId, _params.dataBufId, tempBuffer);
                _bitonicCS.Dispatch(_params.bitonicSortKernelId, _numElements / BITONIC_BLOCK_SIZE, 1, 1);

                // もう一度転置して転置の必要のない範囲のソートを行う。
                SetGPUSortConstants(_bitonicCS, BITONIC_BLOCK_SIZE, l, matrixHeight, matrixWidth);
                _bitonicCS.SetBuffer(_params.matrixTransposeKernelId, _params.inputBufId, tempBuffer);
                _bitonicCS.SetBuffer(_params.matrixTransposeKernelId, _params.dataBufId, inBuffer);
                _bitonicCS.Dispatch(_params.matrixTransposeKernelId, matrixHeight / TRANSPOSE_BLOCK_SIZE, matrixWidth / TRANSPOSE_BLOCK_SIZE, 1);

                // inBufferのソート
                _bitonicCS.SetBuffer(_params.bitonicSortKernelId, _params.dataBufId, inBuffer);
                _bitonicCS.Dispatch(_params.bitonicSortKernelId, _numElements / BITONIC_BLOCK_SIZE, 1, 1);
            }
        }

        public void SetGPUSortConstants(ComputeShader cs, int level, int levelMask, int width, int height)
        {
            cs.SetInt(_params.levelId, level);
            cs.SetInt(_params.levelMaskId, levelMask);
            cs.SetInt(_params.widthId, width);
            cs.SetInt(_params.heightId, height);
        }

		void LogForDebug(ComputeBuffer inBuffer)
		{
			if (inBuffer.stride != sizeof(uint) * 2)
			{
				return;
			}

			int count = inBuffer.count;
			uint[] data = new uint[count * 2];
			inBuffer.GetData(data);

			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < count; ++i)
			{
				sb.AppendFormat("data[{0}] : grid = {1}, id = {2}", i, data[i * 2], data[i * 2 + 1]);
				sb.AppendLine();
			}

			Debug.Log(sb.ToString());
		}
    }
}