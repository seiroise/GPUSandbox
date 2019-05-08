using UnityEngine;

namespace Seiro.GPUSandbox.Lighting2D
{
	public class Motor : MonoBehaviour
	{
		public float rotationSpeed = 0f;

		private void Update()
		{
			transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
		}
	}
}