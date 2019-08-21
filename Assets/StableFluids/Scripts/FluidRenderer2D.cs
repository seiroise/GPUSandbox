using UnityEngine;

namespace Seiro.GPUSandbox.StableFluids
{
	public sealed class FluidRenderer2D : MonoBehaviour
	{
		public FluidSimulator2D simulator;
		public View view = View.Texture;
		public Material material;
		public Camera mainCamera;

		private void Update()
		{
			if (mainCamera && Input.GetMouseButton(0))
			{
				Vector3 mPos = Input.mousePosition;
				mPos.z = -mainCamera.transform.position.z;
				Vector2 vPos = WorldToSimulatorViewport(mainCamera.ScreenToWorldPoint(mPos));
				Debug.Log(vPos);
				InteractSimulation(vPos);
			}
		}

		private void LateUpdate()
		{
			Draw();
		}

		private void Draw()
		{
			if (!simulator || !material) return;

			RenderTexture simTex = simulator.GetSimulationTexture();
			material.SetTexture("_MainTex", simTex);
		}

		public Vector2 WorldToSimulatorViewport(Vector3 worldPosition)
		{
			Vector3 localPosition = transform.worldToLocalMatrix.MultiplyPoint3x4(worldPosition);
			localPosition.z = 0f;
			Vector2 viewportPosition = localPosition;
			viewportPosition.x = localPosition.x + 0.5f;
			viewportPosition.y = localPosition.y + 0.5f;
			return viewportPosition;
		}

		// ちょっとしたテスト。
		public void InteractSimulation(Vector2 viewportPos)
		{
			if (!simulator) return;
			simulator.Interact(viewportPos, 0.1f, Vector2.right);
		}
	}
}