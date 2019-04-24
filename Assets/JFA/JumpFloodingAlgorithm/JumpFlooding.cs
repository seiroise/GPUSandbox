using UnityEngine;

namespace Seiro.GPUSandbox.JFA
{
	public class JumpFlooding : MonoBehaviour
	{
		public RenderTexture distanceField;
		public RenderTexture buffer;
		public Material jfMat;
		public Material copyMat;

		[Range(1, 20)]
		public int maxStep = 10;

		private void Start()
		{
		}

		private void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			// jfMat.SetTexture("_MainTex", distanceField);
			for (int i = 1; i < maxStep; ++i)
			{
				jfMat.SetFloat("_JumpStep", 1f / (2 << i));
				Graphics.Blit(distanceField, buffer, jfMat);
				Graphics.Blit(buffer, distanceField, copyMat);
			}
			/*
			jfMat.SetFloat("_JumpStep", 1f / (2 << 2));
			Graphics.Blit(distanceField, buffer, jfMat);
			Graphics.Blit(buffer, distanceField, copyMat);
			
			jfMat.SetFloat("_JumpStep", 1f / (2 << 3));
			Graphics.Blit(distanceField, buffer, jfMat);
			
			/*
			jfMat.SetFloat("_JumpStep", 1f / (2 << 4));
			Graphics.Blit(buffer, distanceField, jfMat);

			jfMat.SetFloat("_JumpStep", 1f / (2 << 5));
			Graphics.Blit(distanceField, buffer, jfMat);
			jfMat.SetFloat("_JumpStep", 1f / (2 << 6));
			Graphics.Blit(buffer, distanceField, jfMat);

			jfMat.SetFloat("_JumpStep", 1f / (2 << 7));
			Graphics.Blit(distanceField, buffer, jfMat);
			*/
			/*
			jfMat.SetFloat("_JumpStep", 0.125f);
			Graphics.Blit(distanceField, distanceField, jfMat, 0);
			jfMat.SetFloat("_JumpStep", 0.0625f);
			Graphics.Blit(distanceField, distanceField, jfMat, 0);
			*/
			// とりあえずそのままスルー
			Graphics.Blit(buffer, destination, copyMat);
		}
	}
}