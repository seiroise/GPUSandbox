using UnityEngine;

namespace Seiro.GPUSandbox.Lighting2D
{
	public class TrackMouse : MonoBehaviour
	{
		private void Update()
		{
			if (Input.GetMouseButton(0))
			{
				Vector3 mpos = Input.mousePosition;
				mpos.z = -Camera.main.transform.position.z;
				Vector3 wpos = Camera.main.ScreenToWorldPoint(mpos);
				transform.position = wpos;
			}
		}
	}
}