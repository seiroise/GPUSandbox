using UnityEngine;

namespace Seiro.GPUSandbox.Lighting2D
{
	public class MouseSpawner : MonoBehaviour
	{
		public SpriteRenderer toSpawn;

		private void Update()
		{
			if (Input.GetMouseButton(1))
			{
				Vector3 mpos = Input.mousePosition;
				mpos.z = -Camera.main.transform.position.z;
				Vector3 wpos = Camera.main.ScreenToWorldPoint(mpos);
				SpriteRenderer spawned = Instantiate(toSpawn, wpos, Quaternion.identity);
				spawned.color = Random.ColorHSV();
			}
		}
	}
}