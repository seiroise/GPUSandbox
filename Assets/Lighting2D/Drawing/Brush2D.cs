using UnityEngine;
using System;

namespace Seiro.GPUSandbox.Lighting2D
{
	[Serializable]
	public struct BrushMaterial
	{
		public Color color;
		public Vector4 substance;
	}

	public sealed class Brush2D : MonoBehaviour
	{
		new public Renderer2D renderer;

		public Material sphereBrush;

		public BrushMaterial brush;
		public float radius = 10f;

		private void Update()
		{
			DrawLineByFreeHand(ref brush);
		}

		private void DrawLineByFreeHand(ref BrushMaterial mat)
		{
			if (renderer == null) return;
			if (Input.GetMouseButton(0))
			{
				DrawSphere(renderer.baseColor.read, renderer.baseColor.write, ref mat, Input.mousePosition, radius);
				renderer.baseColor.Swap();
			}
		}

		private void DrawSphere(RenderTexture src, RenderTexture dst, ref BrushMaterial mat, Vector2 position, float radius)
		{
			if (sphereBrush == null || src == null || dst == null) return;

			sphereBrush.SetVector("_Screen", new Vector2(Screen.width, Screen.height));

			sphereBrush.SetTexture("_BaseColor", renderer.baseColor.read);
			sphereBrush.SetTexture("_Substance", renderer.substance.read);

			sphereBrush.SetColor("_C", mat.color);
			sphereBrush.SetVector("_S", mat.substance);
			sphereBrush.SetVector("_Position", position);
			sphereBrush.SetFloat("_Radius2", radius * radius);

			Graphics.SetRenderTarget(

			Graphics.Blit(src, sphereBrush, 0);
		}
	}
}