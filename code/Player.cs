using Sandbox;
using System;
using SpriteTools;

public sealed class Player : Component, Component.ICollisionListener
{
	[Property] SpriteComponent Sprite {get; set;}

	[Property] float WalkSpeed {get; set;} = 10f;
	[Property] float MaxWalkSpeed {get; set;} = 100f;

	[Property] float RunSpeed {get; set;} = 20f;
	[Property] float MaxRunSpeed {get; set;} = 180f;

	[Property] public float LinearDrag {get; set;} = 500f;
	[Property] public float DragMultiplier {get; set;} = 5f;

	[Property] public float JumpStrength = 400f;
	[Property] public float HardLandStrength = 200f;

	[Property] public float MaxFallSpeed = -300f;
	[Property] public float MaxAirHorizontalSpeed = 300f;
	[Property] public float AirLinearDrag {get; set;} = 300f;
	[Property] public float AirDragMultiplier {get; set;} = 5f;

	[Property] public float BubbleSpeed {get; set;} = 10f;
	[Property] public float MaxBubbleFallSpeed {get; set;} = -200f;
	[Property] public float MaxBubbleFlySpeed {get; set;} = 500f;
	[Property] public float BubbleFlapStrength {get; set;} = 200f;
	[Property] public float BubbleFlapBuffer {get; set;} = 0.5f;
	[Property] public float MaxBubbleGroundSpeed {get; set;} = 100f;

	[Property] public bool HasInput = false;

	[ReadOnly] [Property] Vector2 WishVelocity = Vector2.Zero;
	[ReadOnly] [Property] Vector2 Velocity = Vector2.Zero;
	[ReadOnly] [Property] Vector2 PastVelocity = Vector2.Zero;

	[ReadOnly] [Property] bool OnGround {get; set;} = false;

	[Property] float BubbleGravity = -100f;
	[Property] float BubbleGravityMultiplier = 2f;
	[Property] float Gravity = -200f;
	[Property] float GravityMultiplier {get; set;} = 5f;

	[Property] public KirbyState CurrentState = KirbyState.Idle;
	[Property] public Direction FaceDirection = Direction.Right;

	[Property] [ReadOnly] TimeSince TimeSinceBlink;
	[Property] public float MaxBlinkBuffer {get; set;} = 3f;
	[Property] [ReadOnly] public float CurrentBlinkBuffer = 1f;
	[Property] [ReadOnly] public bool HasBlinked = false;

	[Property] [ReadOnly] TimeSince TimeSinceLastFlap;
	[Property] [ReadOnly] TimeSince TimeSinceLastTurn;
	[Property] [ReadOnly] TimeSince TimeSinceJump;
	[Property] [ReadOnly] TimeSince TimeSinceFall;
	[Property] [ReadOnly] TimeSince TimeSinceLowVel;

	[Property] [ReadOnly] public bool HitLowVel = false;
	[Property] [ReadOnly] public bool ForcedStop = false;

	[Property] [ReadOnly] public bool HasPeaked = false;

	[Property] [ReadOnly] public bool IsBubble = false;

	[Property] [ReadOnly] public bool IsFalling = false;
	[Property] [ReadOnly] public bool IsFlapping = false;
	[Property] [ReadOnly] public bool IsJumping = false;
	[Property] [ReadOnly] public bool IsTurning = false;
	[Property] [ReadOnly] public bool IsDucking = false;
	[Property] [ReadOnly] public bool IsRunning = false;
	[Property] [ReadOnly] public bool IsWalking = false;
	[Property] [ReadOnly] public bool IsIdle = false;

	[Property] public SoundEvent JumpSound {get; set;}
	[Property] public SoundEvent LandSound {get; set;}
	[Property] public SoundEvent HardLandSound {get; set;}
	[Property] public SoundEvent RunSound {get; set;}
	[Property] public SoundEvent TurnSound {get; set;}
	[Property] public SoundEvent DuckSound {get; set;}

	[Property] public SoundEvent BubbleStartSound {get; set;}
	[Property] public SoundEvent BubbleFlapSound {get; set;}
	[Property] public SoundEvent BubbleEndSound {get; set;}

	[Property] public GameObject DustPrefab {get; set;}
	[Property] public GameObject StarPrefab {get; set;}

	protected override void OnAwake()
	{
		Sprite.OnAnimationComplete += OnAnimComplete;
	}

	void OnAnimComplete(string s)
	{
		Log.Info(s);
		if(s == "blink")
		{
			Sprite.PlayAnimation("default");
			ResetBlink();
		}
		else if(s == "duck_blink")
		{
			Sprite.PlayAnimation("duck");
			ResetBlink();			
		}
		else if(s == "peak")
		{
			SetState(KirbyState.Falling);
			HasPeaked = true;
		}
		else if(s == "land")
		{
			SetState(KirbyState.Idle);
		}
		else if(s == "hardstart")
		{
			Sprite.PlayAnimation("hardfall");
		}
		else if(s == "breathein")
		{
			SetState(KirbyState.BubbleFall);
		}
		else if(s == "bubbleflap")
		{
			SetState(KirbyState.BubbleFall);
		}
		else if(s =="breatheout")
		{
			SetState(KirbyState.Peak);
			IsBubble = false;
		}
	}
	protected override void OnStart()
	{
		SetState(KirbyState.Idle);
		TimeSinceBlink = 0f;
		ResetBlink();
	}

	protected override void OnUpdate()
	{
		switch(CurrentState)
		{
			case KirbyState.Idle:
				HandleBlink();
				break;
			case KirbyState.Ducking:
				HandleBlink();
				break;
			default:
				break;
		}

	}
	protected override void OnFixedUpdate()
	{
		Move();
		BuildWishVelocity();
		CheckGrounded();
		ManageDirection(Velocity);

	}

	void HandleBlink()
	{
		if(TimeSinceBlink >= CurrentBlinkBuffer && !HasBlinked)
		{
			Blink();
		}
	}

	void Blink()
	{
		HasBlinked = true;
		TimeSinceBlink = 0f;

		if(CurrentState == KirbyState.Idle)
		{
			Sprite.PlayAnimation("blink");
		}
		else if(CurrentState == KirbyState.Ducking)
		{
			Sprite.PlayAnimation("duck_blink");
		}

	}
	void ResetBlink()
	{
		HasBlinked = false;
		CurrentBlinkBuffer = Game.Random.Float(1,MaxBlinkBuffer);
	}

	void BuildWishVelocity()
	{
		WishVelocity = 0;
		if(Input.Pressed("Use"))
		{
			if(IsBubble)
			{
				if(CurrentState == KirbyState.BubbleFlap || CurrentState == KirbyState.BubbleFall || CurrentState == KirbyState.BubbleGround)
				{
					SetState(KirbyState.BubbleEnd);
				}
				return;
			}
			else if(!IsBubble)
			{
				if(CurrentState == KirbyState.Jumping || CurrentState == KirbyState.Peak || CurrentState == KirbyState.Falling || CurrentState == KirbyState.HardFall)
				{
					WishVelocity += new Vector2(0, BubbleFlapStrength);
					Velocity += WishVelocity;
					SetState(KirbyState.BubbleStart);
					return;
				}
			}

		}
		if(Input.Released("Backward"))
		{
			if(IsBubble) return;
			if(CurrentState == KirbyState.Ducking)
			{
				SetState(KirbyState.Idle);
			}

		}
		if(Input.Down("Backward"))
		{
			if(IsBubble) return;
			if(CurrentState == KirbyState.Jumping || CurrentState == KirbyState.Falling || CurrentState == KirbyState.Peak) return;
			WishVelocity = new Vector2(0,0);
			SetState(KirbyState.Ducking);
			return;
		}
		if(Input.Down("Left"))
		{
			WishVelocity += new Vector2(-WalkSpeed, 0);
		}
		if(Input.Down("Right"))
		{
			WishVelocity += new Vector2(WalkSpeed, 0);
		}
		if(Input.Down("Forward"))
		{
			//WishVelocity = new Vector2(0, Speed);
		}
		if(Input.Down("Jump"))
		{
			HasInput = true;
			if(IsBubble)
			{
				if(TimeSinceLastFlap < BubbleFlapBuffer) return;
				WishVelocity += new Vector2(0, BubbleFlapStrength);
				Velocity += WishVelocity;
				SetState(KirbyState.BubbleFlap);
				return;				
			}
			if(OnGround)
			{
				if(IsJumping) return;
				if(CurrentState == KirbyState.Walking || CurrentState == KirbyState.Running || CurrentState == KirbyState.Ducking || CurrentState == KirbyState.Idle)
				{
					WishVelocity += new Vector2(0, JumpStrength);
					Velocity += WishVelocity;
					SetState(KirbyState.Jumping);
					return;
				}

			}

		}
		var input = Input.AnalogMove;
		if(input.y != 0 && Input.Down("Run") && (CurrentState != KirbyState.Jumping && CurrentState != KirbyState.Falling && CurrentState != KirbyState.Peak && CurrentState != KirbyState.Land) && !IsFalling)
		{
			HasInput = true;
			if(!IsTurning)
			{
				SetState(KirbyState.Running);
			}

			Velocity += new Vector2(-input.y,0) * RunSpeed;
		}
		else if(input.y != 0 && (CurrentState != KirbyState.Land))
		{
			HasInput = true;
			if(!IsBubble)
			{
				if(!IsTurning)
				{

					if(OnGround)
					{
					if(CurrentState == KirbyState.Peak) return;
					SetState(KirbyState.Walking);
					}

				}
			}


			Velocity += new Vector2(-input.y,0) * WalkSpeed;
		}
		else
		{
			HasInput = false;
		}

	}

	void Move()
	{
		if(IsBubble)
		{
			if(!OnGround)
			{
				Velocity += new Vector2(0, BubbleGravity * Time.Delta * BubbleGravityMultiplier);

				if(Velocity.y < MaxBubbleFallSpeed)
				{
					Velocity = new Vector2(Velocity.x, MaxBubbleFallSpeed);					
				}
				if(Velocity.y > MaxBubbleFlySpeed)
				{
					Velocity = new Vector2(Velocity.x, MaxBubbleFlySpeed);
				}
				if(Velocity.x > MaxAirHorizontalSpeed)
				{
					Velocity = new Vector2(MaxAirHorizontalSpeed, Velocity.y);
				}
				else if(Velocity.x < -MaxAirHorizontalSpeed)
				{
					Velocity = new Vector2(-MaxAirHorizontalSpeed, Velocity.y);
				}
				if(!HasInput)
				{
					if(Velocity.x < 0)
					{
						Velocity += new Vector2(AirLinearDrag * Time.Delta * AirDragMultiplier, 0);
					}
					if(Velocity.x > 0)
					{
						Velocity += new Vector2(-AirLinearDrag * Time.Delta * AirDragMultiplier, 0);
					}
				}
			}
			else if(OnGround)
			{
				if(MathF.Abs(Velocity.x) <= 20 && Velocity.x != 0)
				{
					if(!HitLowVel)
					{
						HitLowVel = true;
						TimeSinceLowVel = 0f;
					}
					else if(HitLowVel && TimeSinceLowVel >= 0.5f)
					{
						Velocity = 0;
						HitLowVel = false;
						Log.Info("ForceStop");
					}
				}
				else
				{
					HitLowVel = false;
				}
				if(Velocity.x > MaxBubbleGroundSpeed)
				{
					Velocity = new Vector2(MaxBubbleGroundSpeed, Velocity.y);
				}
				else if(Velocity.x < -MaxBubbleGroundSpeed)
				{
					Velocity = new Vector2(-MaxBubbleGroundSpeed, Velocity.y);
				}
				if(!HasInput)
				{
					if(Velocity.x < 0)
					{
						Velocity += new Vector2(LinearDrag * Time.Delta * DragMultiplier, 0);
					}
					if(Velocity.x > 0)
					{
						Velocity += new Vector2(-LinearDrag * Time.Delta * DragMultiplier, 0);
					}
				}							
			}


		}
		else
		{
			if(!OnGround)
			{
				Velocity += new Vector2(0, Gravity * Time.Delta * GravityMultiplier);
				if(Velocity.x > MaxAirHorizontalSpeed)
				{
					Velocity = new Vector2(MaxAirHorizontalSpeed, Velocity.y);
				}
				else if(Velocity.x < -MaxAirHorizontalSpeed)
				{
					Velocity = new Vector2(-MaxAirHorizontalSpeed, Velocity.y);
				}
				if(!IsFalling && PastVelocity.y == 0)
				{
					SetState(KirbyState.Peak);
				}
				if(!IsFalling && PastVelocity.y > 0f && Velocity.y <= 0f)
				{
					SetState(KirbyState.Peak);
				}
				if( Velocity.y < MaxFallSpeed)
				{
					Velocity = new Vector2(Velocity.x, MaxFallSpeed);
				}
				if(CurrentState != KirbyState.HardFall && IsFalling && TimeSinceFall >= 0.6f && HasPeaked)
				{
					SetState(KirbyState.HardFall);
				}
				if(!HasInput)
				{
					if(Velocity.x < 0)
					{
						Velocity += new Vector2(AirLinearDrag * Time.Delta * AirDragMultiplier, 0);
					}
					if(Velocity.x > 0)
					{
						Velocity += new Vector2(-AirLinearDrag * Time.Delta * AirDragMultiplier, 0);
					}
				}
			}
			if(OnGround)
			{
				if(MathF.Abs(Velocity.x) <= 20 && Velocity.x != 0)
				{
					if(!HitLowVel)
					{
						HitLowVel = true;
						TimeSinceLowVel = 0f;
					}
					else if(HitLowVel && TimeSinceLowVel >= 0.5f)
					{
						Velocity = 0;
						HitLowVel = false;
						Log.Info("ForceStop");
					}
				}
				else
				{
					HitLowVel = false;
				}
				if(CurrentState == KirbyState.Walking)
				{
					if(Velocity.x > MaxWalkSpeed)
					{
						Velocity = new Vector2(MaxWalkSpeed, Velocity.y);
					}
					else if(Velocity.x < -MaxWalkSpeed)
					{
						Velocity = new Vector2(-MaxWalkSpeed, Velocity.y);
					}
				}
				if(CurrentState == KirbyState.Running)
				{
					if(Velocity.x > MaxWalkSpeed)
					{
						Velocity = new Vector2(MaxRunSpeed, Velocity.y);
					}
					else if(Velocity.x < -MaxRunSpeed)
					{
						Velocity = new Vector2(-MaxRunSpeed, Velocity.y);
					}
				}
				if(Velocity.y == 0 && Velocity.x == 0 && CurrentState != KirbyState.Idle && CurrentState != KirbyState.Ducking && CurrentState != KirbyState.Land)
				{
					SetState(KirbyState.Idle);
				}
				if(!HasInput)
				{
					if(Velocity.x < 0)
					{
						Velocity += new Vector2(LinearDrag * Time.Delta * DragMultiplier, 0);
					}
					if(Velocity.x > 0)
					{
						Velocity += new Vector2(-LinearDrag * Time.Delta * DragMultiplier, 0);
					}
				}
				else if(MathF.Abs(Velocity.x) > 150f && WishVelocity.x * Velocity.x < 0f && CurrentState == KirbyState.Running && TimeSinceLastTurn > 0.5f)
				{
					SetState(KirbyState.Turning);
				}
				if(IsTurning && WishVelocity.x * Velocity.x > 0f)
				{
					IsTurning = false;
				}
				if(IsTurning && Velocity.x == 0)
				{
					IsTurning = false;
				}
			}
		}
		
		PastVelocity = Velocity;
		WorldPosition += new Vector3(Velocity.x, Velocity.y, 0) * Time.Delta;
	}

	void SetState(KirbyState state)
	{
		switch(state)
		{
			case KirbyState.Idle:
				if(CurrentState == state) return;
				CurrentState = state;
				IsIdle = true;
				IsFalling = false;
				IsJumping = false;
				IsDucking = false;
				IsWalking = false;
				IsRunning = false;
				Sprite.PlayAnimation("default");
				ResetBlink();
				break;
			case KirbyState.Walking:
				if(CurrentState == state) return;
				CurrentState = state;
				IsWalking =true;
				IsRunning = false;
				IsIdle = false;
				Sprite.PlayAnimation("walk");
				break;
			case KirbyState.Running:
				if(CurrentState == state) return;
				CurrentState = state;
				IsRunning = true;
				IsIdle = false;
				IsWalking = false;
				Sprite.PlayAnimation("run");
				Sound.Play(RunSound);
				break;
			case KirbyState.Ducking:
				if(CurrentState == state) return;
				CurrentState = state;
				IsDucking = true;
				IsFalling = false;
				IsRunning = false;
				IsIdle = false;
				IsWalking = false;
				Velocity = 0;
				Sprite.PlayAnimation("duck");
				Sound.Play(DuckSound);
				ResetBlink();
				break;
			case KirbyState.Jumping:
				if(CurrentState == state) return;
				CurrentState = state;
				IsJumping = true;
				IsTurning = false;
				IsFalling = false;
				IsDucking = false;
				IsRunning = false;
				IsIdle = false;
				IsWalking = false;
				TimeSinceJump = 0f;
				Sprite.PlayAnimation("jump");
				Sound.Play(JumpSound);
				break;
			case KirbyState.Peak:
				if(CurrentState == state) return;
				CurrentState = state;
				HasPeaked = false;
				IsFalling = true;
				IsJumping = false;
				IsDucking = false;
				IsRunning = false;
				IsIdle = false;
				IsWalking = false;	
				Sprite.PlayAnimation("peak");
				break;	
			case KirbyState.Falling:
				if(CurrentState == state) return;
				CurrentState = state;
				IsFalling = true;
				IsJumping = false;
				IsDucking = false;
				IsRunning = false;
				IsIdle = false;
				IsWalking = false;
				TimeSinceFall = 0f;
				Sprite.PlayAnimation("fall");
				break;
			case KirbyState.HardFall:
				if(CurrentState == state) return;
				CurrentState = state;
				IsFalling = true;
				IsJumping = false;
				IsDucking = false;
				IsRunning = false;
				IsIdle = false;
				IsWalking = false;
				Sprite.PlayAnimation("hardstart");
				break;
			case KirbyState.Land:
				if(CurrentState == state) return;
				CurrentState = state;
				Sprite.PlayAnimation("land");
				break;	
			case KirbyState.Turning:
				if(CurrentState == state) return;
				CurrentState = state;
				IsTurning = true;
				TimeSinceLastTurn = 0f;
				Sprite.PlayAnimation("turn");
				Sound.Play(TurnSound);
				SpawnDust();
				break;
			case KirbyState.BubbleStart:
				if(CurrentState == state) return;
				CurrentState = state;
				IsBubble = true;
				IsFalling = true;
				IsJumping = false;
				IsDucking = false;
				IsRunning = false;
				IsIdle = false;
				IsWalking = false;	
				Sprite.PlayAnimation("breathein");
				Sound.Play(BubbleStartSound);
				break;
			case KirbyState.BubbleFall:
				if(CurrentState == state) return;
				CurrentState = state;
				IsBubble = true;
				IsFalling = true;
				IsJumping = false;
				IsDucking = false;
				IsRunning = false;
				IsIdle = false;
				IsWalking = false;	
				TimeSinceFall = 0f;
				Sprite.PlayAnimation("bubblefall");
				break;
			case KirbyState.BubbleFlap:
				if(CurrentState == state) return;
				CurrentState = state;
				IsBubble = true;
				TimeSinceLastFlap = 0f;
				Sprite.PlayAnimation("bubbleflap");
				Sound.Play(BubbleFlapSound);
				break;
			case KirbyState.BubbleGround:
				if(CurrentState == state) return;
				CurrentState = state;
				IsBubble = true;
				Sprite.PlayAnimation("bubbleground");
				break;	
			case KirbyState.BubbleEnd:
				if(CurrentState == state) return;
				CurrentState = state;
				IsBubble = false;
				IsFalling = false;
				TimeSinceFall = 0f;
				Sprite.PlayAnimation("breatheout");
				Sound.Play(BubbleEndSound);
				break;										 
			default:
				break;
		}

		Log.Info($"State: {state}");
	}

	async void SpawnDust()
	{
		for(int i = 0; i <= 3; i++)
		{
			var d = DustPrefab.Clone(this.WorldPosition);
			var comp = d.GetComponent<Dust>();
			if(FaceDirection == Direction.Left)
			{
				d.WorldPosition += new Vector3(Sprite.Bounds.Size.x, Sprite.Bounds.Size.y /2 ,0);
				if(comp.IsValid())
				{
					comp.FlipSprite();
				}
			}
			else
			{
					d.WorldPosition += new Vector3(-Sprite.Bounds.Size.x, Sprite.Bounds.Size.y /2 ,0);	
			}
			await GameTask.DelaySeconds(0.05f);
		}
	}
	void CheckGrounded()
	{
		var trace = Scene.Trace.Ray(this.WorldPosition, this.WorldPosition + Vector3.Right * 1f)
					.Run();

		DebugOverlay.Line(trace.StartPosition, trace.EndPosition, Color.Blue, 1f);
		if(trace.Hit)
		{
			if(!IsBubble)
			{
				if(!OnGround)
				{
					Velocity = new Vector2(Velocity.x, 0);
					if(CurrentState == KirbyState.Falling || CurrentState == KirbyState.Peak)
					{

						OnLand(trace.HitPosition, trace.Normal);
					}
					else if(CurrentState == KirbyState.HardFall)
					{

						OnHardLand(trace.HitPosition, trace.Normal);
					}
				}
				OnGround = true;				
			}
			else if(IsBubble)
			{
				if(!OnGround)
				{
					Velocity = new Vector2(Velocity.x, 0);
					if(CurrentState == KirbyState.BubbleFall)
					{
						SetState(KirbyState.BubbleGround);
					}
				}
				OnGround = true;
			}
		}
		else
		{
			if(OnGround)
			{
				OnGround = false;
			}
			Log.Info("Nothing Hit");
		}
	}

	Vector2 RandomOnUnitCircle()
	{
		float angle = 2 * MathF.PI * Game.Random.Float();
		float x = MathF.Cos(angle);
		float y = MathF.Sin(angle);
		return new Vector2(x, y);
	}

	void OnLand(Vector3 hitpos, Vector3 normal)
	{
		Log.Info("Landed");
		HasPeaked = false;
		SetState(KirbyState.Land);
		Sound.Play(LandSound);
		SpawnStar(hitpos);
	}

	void OnHardLand(Vector3 hitpos, Vector3 normal)
	{
		Log.Info("HardLanded");
		HasPeaked = false;
		WishVelocity += new Vector2(0, HardLandStrength);
		Velocity += WishVelocity;
		SetState(KirbyState.Peak);
		Sprite.CurrentFrameIndex = 3;
		Sound.Play(HardLandSound);
		SpawnStar(hitpos);
	}

	void SpawnStar(Vector3 pos)
	{

		float LinePos = Game.Random.Float(-20, 20);
		Vector2 cNorm = new Vector2(0,1);
		Vector3 SpawnPos = pos + new Vector3(LinePos, 0,0);
		Vector2 EndVec2 = RandomOnUnitCircle();

		float dot = Vector2.Dot(EndVec2, cNorm);
		Log.Info($"Dot: {dot}");
		if( dot < 0f )
		{
			EndVec2 = -EndVec2;
		}
		Log.Info($"EndVec:{EndVec2}");

		EndVec2 *= Game.Random.Float(Sprite.Bounds.Size.x, 150f);

		Vector3 EndPos = SpawnPos + new Vector3(EndVec2.x, EndVec2.y,2);

		var s = StarPrefab.Clone(SpawnPos);
		var comp = s.GetComponent<Star>();
		if(comp.IsValid())
		{
			comp.SetEndPosition(EndPos);
		}
	}
	void OnFall()
	{

	}

	void ManageDirection(Vector2 velocity)
	{
		if(IsTurning)
		{
			Log.Info("No Switch. Is Turning");
			return;
		} 
		else if(velocity.x < 0)
		{

			if(Sprite.SpriteFlags != SpriteFlags.HorizontalFlip)
			{
				Sprite.SpriteFlags = SpriteFlags.HorizontalFlip;
				FaceDirection = Direction.Left;
				Log.Info("Flip");
			}

		}
		else if(velocity.x > 0)
		{
			if(Sprite.SpriteFlags != SpriteFlags.None)
			{
				Sprite.SpriteFlags = SpriteFlags.None;	
				FaceDirection = Direction.Right;
			}	
		}
	}
}

public enum KirbyState
{
	Idle,
	Ducking,
	Walking,
	Running,
	Turning,
	Jumping,
	Peak,
	Falling,
	HardFall,
	Land,
	BubbleStart,
	BubbleFlap,
	BubbleFall,
	BubbleGround,
	BubbleEnd,
}

public enum Direction
{
	Left,
	Right,
}
