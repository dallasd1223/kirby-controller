using Sandbox;

public sealed class FollowCam : Component
{
	[Property] public GameObject Target {get; set;}

	protected override void OnUpdate()
	{
		WorldPosition = new Vector3(Target.WorldPosition.x, Target.WorldPosition.y, WorldPosition.z);
	}
}
