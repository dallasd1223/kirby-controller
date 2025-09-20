using Sandbox;
using SpriteTools;

public sealed class Dust : Component
{
	[Property]  public  SpriteComponent Sprite {get; set;}

	protected override void OnAwake()
	{
		Sprite = GetComponent<SpriteComponent>();

		Sprite.OnAnimationComplete += OnAnimationEnd;
	}

	public void FlipSprite()
	{
		if(Sprite.SpriteFlags != SpriteFlags.None)
		{
			Sprite.SpriteFlags = SpriteFlags.None;
		}
	}
	void OnAnimationEnd(string s)
	{
		if(s == "default")
		{
			this.GameObject.Destroy();
		}
	}
}
