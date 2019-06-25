using UnityEngine;

namespace Seiro.GPUSandbox
{
	public sealed class Follower : MonoBehaviour
	{
		[SerializeField]
		private Transform _target;
		[SerializeField]
		private bool _followingPosition = true;
		[SerializeField]
		private Vector3 _offset;
		[SerializeField]
		private bool _followingAngles = false;

		private void Update()
		{
			if (!_target) return;
			if (_followingPosition)
			{
				Vector3 pos = _target.transform.position + _offset;
				transform.position = pos;
			}
			if (_followingAngles)
			{
				Quaternion rotation = _target.transform.rotation;
				transform.rotation = rotation;
			}
		}
	}
}