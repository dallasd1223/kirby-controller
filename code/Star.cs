using Sandbox;
using SpriteTools;
using System;

public sealed class Star : Component
{
	[Property] public SpriteComponent Sprite {get; set;}
	[Property] public float LifeTime {get; set;} = 1f;
	[Property] [ReadOnly] public float CurrentElapsed;
	[Property] Vector3 EndPosition {get; set;} = Vector3.Zero;

	Angles angs = new Angles(0,0,0);
	float x = 0f;

	protected override void OnAwake()
	{
		Sprite = GetComponent<SpriteComponent>();
	}

	protected override void OnUpdate()
	{
		CurrentElapsed += Time.Delta;

		float t = MathX.Clamp(CurrentElapsed / LifeTime, 0, 1f);
		float easedt = EaseOut(t);

		x = MathX.Lerp(0,360, easedt);
		angs = new Angles(0, x, 0);

		this.GameObject.LocalScale = LocalScale + (t * 0.005f); 
		this.WorldRotation = angs.ToRotation();
		this.WorldPosition = Vector3.Lerp(this.WorldPosition, EndPosition, t);
		if(CurrentElapsed >= LifeTime)
		{
			OnLifeTimeEnd();
		}
	}

	public void SetEndPosition(Vector3 vec)
	{
		EndPosition = vec;
	}

	float EaseOut(float t)
	{
		return MathF.Sin((t * MathF.PI) / 2);
	}

	void OnLifeTimeEnd()
	{
		this.GameObject.Destroy();
	}
}
