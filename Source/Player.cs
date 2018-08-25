using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Rewired;
using UnityEngine.SceneManagement;

public enum Surface { None, Ground, Player };
public enum BopType { KICKED, SLAMMED, INKED, NOPED };

public class Player : MonoBehaviour {
    [HideInInspector] public bool inGame = false;
    
    // Customizable
    [HideInInspector] public string PlayerName;
    [HideInInspector] public Color headColor = Color.white;
    [SerializeField] private LayerMask dashCollidable;

    public int rewiredPlayerID;

    public bool alive = true;

    private Transform _transform;

    // Component grabs
    private SmoothCamera cam;
    [HideInInspector] public Rigidbody2D rbody;
    [HideInInspector] public AudioClipPlayer _audio;
    [HideInInspector] public Face face;
    [HideInInspector] public Hat hat;
    private Feet feet;
    private Leg[] legs;
    private CapsuleCollider2D _collider;
    private SquishHitbox squishHitbox;
    public Rigidbody2D hatAnchor;
    public Rigidbody2D giftAnchor;

    [SerializeField] private Transform stunHalo;
    [SerializeField] private Head head;
    [SerializeField] private PhysicsMaterial2D physicsMaterial;

    [Header("Animations")]
    [SerializeField] private SVGAnimationController tiredAnimation;

    // Misc
    [HideInInspector] public int sortingOrder = -1;

    // Collider tracking
    [HideInInspector] public Surface surface;
    [HideInInspector] public List<Collider2D> ledgeBodyTriggers = new List<Collider2D>();
    [HideInInspector] public List<Collider2D> ledgeHeadTriggers = new List<Collider2D>();
    [HideInInspector] public bool ledgeBody = false;
    [HideInInspector] public bool ledgeHead = false;

    // Gameplay tracking
    [HideInInspector] public bool hasGift = false;
    [HideInInspector] public int currentRoomIndex = 0;
    
    // Basic movement vars
    [HideInInspector] public bool canControl = true;
    private int direction = 1;
    public int Direction { get { return direction; } }
    private bool canFlipDir = false;
    private Vector3 lastPlace;
    private bool ledgeCondition = false;
    private int moveX = 0;
    [HideInInspector] public bool canMoveHorizontal;
    Vector3 encourage = Vector3.zero;

    public Rewired.Player player_controller;
    bool can_jump_prev_frame = false;

    // Jump conditions passed to FixedUpdate from Update
    bool jc_0 = false;
    bool jc_1 = false;
    bool jc_2 = false;
    bool jc_3 = false;

    // Trap affectors
    public int flatgrasses_occupied = 0;

    // Movement constants
    private Vector3 cachedVelocity;
    private const float jumpSpeed        = 14f;
    private const float slamSpeed        = 14f;
    private const float bopSpeed        = 11f;
    private const float runSpeedGround    = 2f;
    private const float runSpeedAir        = 0.4f;
    private const float runSpeedBoost    = 0.3f;
    private const float maxVelX            = 15;
    private const float maxDistBeforeFlip = 0.8f;
    private const float maxJumpTime = 0.5f; // seconds
    private const float maxLedgeGrabTime = 0.25f; // seconds
    private const float maxLedgePushTime = 0.125f; // seconds
    private float jumpTime = 0;
    private float ledgeGrabTime = 0;
    private float ledgePushAccumulator = 0;
    public float BopSpeed    { get { return bopSpeed; } }
    public float MaxVelX    { get { return maxVelX; } }

    // Friction
    private float fric;
    private const float fricGround        = 0.07f;
    private const float fricSlippy        = 0.02f;
    private const float fricAir            = 0.00f;
    private const float fricDash        = 0.0f;
    private const float fricHighSpeed    = 3;
    private const float dashFricSmooth    = 0.1f;

    // Slamming, slipping, boosting
    [HideInInspector] public bool Slipping = false;
    [HideInInspector] public List<Slippy> slipTriggers = new List<Slippy>();
    public int slippyInSoles = 0;
    [HideInInspector] public bool canSlam = false;
    [HideInInspector] public bool slamming = false;
    [HideInInspector] public bool slammingInAir = false;
    [HideInInspector] public bool Boosting = false;
    [HideInInspector] public bool Transitioning = false;
    [HideInInspector] public bool isInked = false;

    // Dashing
    [HideInInspector] public bool Dashing = false;
    Coroutine dashCoroutine;
    private    int dashDir;
    private const float dashSpeed = 25;
    private const float dashBounceBack = 0.5f;
    private const float dashDrainFactor = 0.3f; // seconds
    private const float dashRechargeFactor_Game = 4.5f; // seconds
    private const float dashRechargeFactor_Tutorial = 1.5f; // seconds
    private float dashRechargeFactor;
    private bool adjustedDashFriction = false;
    private bool tired = false;
    public bool Tired { get { return tired; } }
    private int DashLayerMask;

    // Bopping / disabling variables
    private const float unBopDelay = 0.6f; // seconds
    private const float unBopInkDelay = 1.2f; // seconds
    private const float unBopNopeDelay = 1.25f; // seconds
    private bool invincible = false;
    private const float invincibilityTime = 0.6f;

    // Kicking
    private bool justKicked = false;
    public bool JustKicked { get { return justKicked; } }
    [SerializeField] private LayerMask kickMask;
    [HideInInspector] public Vector2 kickOffset { get; set; }
    [HideInInspector] public int kickTentacleIndex = 0;
    private const float kickRadius = 2.5f;
    private const float kickExtendTime = 0.15f;
    private const float kickRetractTime = 0.5f;
    private const float kickHitRetractTime = 0.85f;

    // Dynamically instantiated objects
    [SerializeField] private GameObject playerStomp;
    [SerializeField] private GameObject playerShadow;

    // Tweens
    private GoTweenConfig bopTweenConfig;
    private GoTween unBopDelayTween;
    private GoTween kickRetractTween;
    private GoTweenConfig kickRetractConfig;

    [HideInInspector] public Vector2 sizeJiggleOffset { get; set; }
    [HideInInspector] public Vector2 OffsetJiggle { get; set; }
    
    // Steam stuff
    public PlayerAchievementMgr achievementMgr;

    void Awake() {
        feet = GetComponentInChildren<Feet>();
        legs = GetComponentsInChildren<Leg>();
        rbody = GetComponent<Rigidbody2D>();
        face = GetComponentInChildren<Face>();
        hat = GetComponentInChildren<Hat>();
        _audio = GetComponent<AudioClipPlayer>();
        _collider = GetComponent<CapsuleCollider2D>();
        squishHitbox = GetComponentInChildren<SquishHitbox>();
        achievementMgr = GetComponent<PlayerAchievementMgr>();
        _transform = transform;
    }

    public void SignalBoosted() {
        achievementMgr.ResetAirTime();
    }
    public void SignalBoppedPlayer() {
        achievementMgr.Bop();
    }
    public void SignalFiredShot() {
        achievementMgr.FiredShot();
    }
    public void SignalHitByShot(Player who_fired_at_me) {
        achievementMgr.GotHitByShot(who_fired_at_me);
    }

    IEnumerator Start() {
        while (PlayerName == null || Time.timeScale == 0) yield return null;

        // Set dash time according to which stage someone's in
        if (UIManager.current_scene_name == GlobalStrings.SCENE_Game) {
            dashRechargeFactor = dashRechargeFactor_Game;
        } else {
            dashRechargeFactor = dashRechargeFactor_Tutorial;
        }
        
        dashDrainWfs = new WaitForSeconds(dashDrainFactor);
        dashRechargeWfs = new WaitForSeconds(dashRechargeFactor);

        cam = Camera.main.GetComponent<SmoothCamera>();

        surface = Surface.None;
        canMoveHorizontal = true;
        kickOffset = Vector2.zero;
        
        // Pode coloring
        head.SetColor(headColor);
        foreach (Leg leg in legs) {
            leg.Init(headColor);
        }

        // Spawn PlayerStomp prefab (allows correct foot positioning on other players)
        GameObject newStomper = Instantiate(playerStomp, _transform.position, Quaternion.identity) as GameObject;
        newStomper.GetComponent<PlayerStomp>().player = gameObject;
        newStomper.transform.parent = _transform.parent;

        // Spawn shadow object
        GameObject newShadow = Instantiate(playerShadow, _transform.position, Quaternion.identity) as GameObject;
        newShadow.GetComponent<PlayerShadow>().player = this;
        newShadow.transform.parent = _transform.parent;

        // Update sprite / renderer ordering
        SetOrder();
        
        stunHalo.gameObject.SetActive(false);

        // Tween initialization
        bopTweenConfig = new GoTweenConfig();

        kickRetractConfig = new GoTweenConfig()
            .vector2Prop("kickOffset", Vector2.zero)
            .setEaseType(GoEaseType.ExpoOut);

        DashLayerMask = LayerMask.NameToLayer("Level");
    }
    
    void Update() {
        // Don't execute if paused
        if (Time.timeScale != 0) {
            HandleMovement ();
        } else {
            // Turn off slippy audio
            if (_audio.continualSourceIsPlaying) _audio.DisableContinual(); 
        }
    }

    void HandleMovement() {
        if (!ReInput.isReady) return;
        
        player_controller = ReInput.players.GetPlayer(rewiredPlayerID);
        if (player_controller == null) return;
        
        justKicked = false;
        
        float axis = player_controller.GetAxis(RewiredConsts.Action.Run);
        moveX = (axis == 0) ? 0 : (int)Mathf.Sign(axis);
        
        if (moveX != 0) FestivalTimeOutManager.Reset();

        if (!canControl || !canMoveHorizontal || Dashing) moveX = 0;
        
        // Slippy surface audio
        if (Slipping) {
            float velFactor = Mathf.Min(maxVelX, rbody.velocity.x) / maxVelX;
            velFactor *= velFactor;
            float slipVol = AudioManager.sfxVol * velFactor * 0.6f;

            if (_audio.continualSourceIsPlaying && !_audio.continualSourceIsTweening) {
                _audio.continualSource.volume = slipVol;
            }
            if (surface != Surface.None && !_audio.continualSourceIsPlaying) {
                _audio.EnableContinual(slipVol);
            }
        }
        if ((surface == Surface.None || !Slipping) && _audio.continualSourceIsPlaying) {
            _audio.DisableContinual();
        }
        
        if (canControl) {
            bool can_jump_this_frame =  (ledgeBody && !ledgeHead && surface == Surface.None);
            
            // Jump logic
            bool jumpCondition = player_controller.GetButton(RewiredConsts.Action.Jump);
            if (jumpCondition) {
                // keep racking up time
                jumpTime += Time.deltaTime;
            }

            if (ledgeCondition && surface == Surface.None && jumpCondition) {
                ledgeGrabTime = maxLedgeGrabTime;
            }
            if (ledgePushAccumulator > 0) ledgePushAccumulator -= Time.deltaTime;

            bool jumpTimeCondition = (player_controller.GetButtonDown(RewiredConsts.Action.Jump) || jumpTime > maxJumpTime);
            bool surfaceCondition = (surface != Surface.None || ledgeGrabTime > 0);
            
            bool end_of_ledge_grab = !can_jump_this_frame && can_jump_prev_frame;
            if (jumpCondition && ((jumpTimeCondition && surfaceCondition) || end_of_ledge_grab)) {
                // Regular jump
                jc_0 = true;
                
                // Quickfeet achievement
                if (surface == Surface.Player && squishHitbox.last_touched_player != null && squishHitbox.last_touched_player.Dashing) {
                    achievementMgr.GetQuickFeet();
                }

                // allow shunt in direction of ledge
                if (ledgeGrabTime > 0) ledgePushAccumulator = maxLedgePushTime;

                jumpTime = 0;

                Squish(SquishType.JUMP);
            }

            can_jump_prev_frame = can_jump_this_frame;

            // Hopping up ledges autoassist
            if (ledgePushAccumulator > 0) {
                ledgePushAccumulator -= Time.deltaTime;
                jc_1 = true;
            }

            // Slowing down to allow variable jump height
            jumpCondition = player_controller.GetButtonUp(RewiredConsts.Action.Jump);
            if (jumpCondition && rbody.velocity.y > 0 && !Boosting) {
                jc_2 = true;

                // reset jump time
                jumpTime = 0;
            }

            // Slamming
            bool slamCondition = player_controller.GetButtonDown(RewiredConsts.Action.Kick);
            if (slamCondition && canSlam) {
                slamming = true;
                if (surface == Surface.None) {
                    slammingInAir = true;
                }

                // Kicking when on ground
                if (surface == Surface.Ground) {
                    Kick();
                } else {
                    // Add slam speed to whatever downward motion we already have
                    jc_3 = true;
                }
                
                // Allow multiple player slams, but disable constant slamming in the air
                if (surface != Surface.Player) canSlam = false;

                // Audiovisual update
                if (surface == Surface.None) Squish(SquishType.SLAM);
                //_audio.PlayClipOnce("slam", AudioManager.sfxVol * 0.5f, Random.Range(2.8f, 3f));
            }

            // Dashing
            bool dashCondition = player_controller.GetButtonDown(RewiredConsts.Action.Dash);
            if (dashCoroutine == null && !tired && !Dashing && dashCondition && canMoveHorizontal) {
                dashCoroutine = StartCoroutine("InitDash");
            }

            // Switch direction of sprite with movement
            if (!Transitioning) {
                UpdateOrientation(moveX);
            }
        }
    }

    WaitForSeconds dashDrainWfs;
    WaitForSeconds dashRechargeWfs;
    private IEnumerator InitDash() {
        Dashing = true;

        // Set dash direction based on where player is facing, 
        // but prioritise held key (not if going from CCU, to prevent falling off map)
        dashDir = direction;
        if (moveX != 0 && !Transitioning) dashDir = moveX;

        // Update visuals
        face.SetFace(Face.FaceType.Dashing);

        // Spin our head if we're in mid-air
        if (surface == Surface.None) {
            head.Spin(dashDrainFactor * 3.5f);
        }
        
        _audio.PlayClipOnce("dash", AudioManager.sfxVol * 6f, Random.Range(1f, 1.2f));

        yield return dashDrainWfs;

        EndDash(true);
        
        yield return dashRechargeWfs;
        
        // Revert to normal state
        tired = false;
        tiredAnimation.EndAnimation();
        if (face.ActiveFace == Face.FaceType.Tired) {
            face.SetFace(Face.FaceType.Default);
        }
    }
    
    void DashTick() {
        if (!Dashing) return;

        // Shoot up tangent of surface feet are colliding with
        RaycastHit2D footRay = feet.FootRay;
        Vector2 normPerp = footRay.normal;
        var tx = normPerp.x;
        normPerp.x = -normPerp.y;
        normPerp.y = tx;

        // Don't dash up anything too steep
        Vector2 dir = Vector2.right * Mathf.Sign(rbody.velocity.x);
        RaycastHit2D wallRay = Physics2D.Raycast(_transform.position, dir, 1f, dashCollidable);
        if (wallRay && !wallRay.collider.isTrigger && !wallRay.collider.CompareTag("PartyDoor")) {
            Vector2 wallNormal = wallRay.normal;
            float normDot = Vector2.Dot(wallNormal, Vector2.up);

            // We've hit a vertical wall - cancel EVERYTHING dash-related
            if (normDot == 0) {
                // bounce off backwards
                cachedVelocity = rbody.velocity;
                cachedVelocity.x *= -dashBounceBack;
                rbody.velocity = cachedVelocity;

                EndDash(true);
                return;
            }
        }
        
        if (surface == Surface.None) {
            cachedVelocity.x = dashDir * dashSpeed;
            cachedVelocity.y *= 0.95f;
        } else {
            // Scale down player-player dash-sailing. It's fun though, so only scale it by a bit
            if (footRay.collider.gameObject.layer != DashLayerMask) {
                normPerp.y *= 0.7f;
            }
            // Shoot player forwards along normal
            cachedVelocity = -normPerp * (dashDir * dashSpeed);
        }
        rbody.velocity = cachedVelocity;

        fric = fricDash;
    }
    
    public void EndDash(bool isTired, bool reset = false) {
        if (!Dashing) return;

        if (reset) {
            if (dashCoroutine != null) StopCoroutine(dashCoroutine);
        }

        dashCoroutine = null;
        if (isTired) {
            tired = true;
            tiredAnimation.BeginAnimation();
            face.SetFace(Face.FaceType.Tired);
        } else {
            tiredAnimation.EndAnimation();
            tired = false;
        }

        Dashing = false;
        adjustedDashFriction = true;
    }

    void FixedUpdate() {
        // Only allow ledge-grabs when our body's hitting something(s) and our head's in the clear
        ledgeCondition = (ledgeBody && !ledgeHead);
        ledgeGrabTime -= Time.fixedDeltaTime;

        // Handles slippy surfaces. Here rather than Slippy script to save on OnTriggerStay calls / GetComponent calls etc
        Slipping = false;
        if (surface != Surface.None && slippyInSoles > 0) {
            Slipping = true;
        }

        // Horizontal movement
        float runSpeed = runSpeedGround;
        if (surface == Surface.None) runSpeed = runSpeedAir;
        if (Boosting) runSpeed = runSpeedBoost;

        // if we're not going too fast, we're trying to counteract movement, or we're slipping, we can go faster
        if ((Mathf.Abs(rbody.velocity.x) < maxVelX) ||
             Mathf.Sign(moveX) != Mathf.Sign(rbody.velocity.x) ||
             Slipping) {

            encourage.x = moveX;
            encourage.y = 0;
            
            // help up hills
            if (surface == Surface.Ground && moveX != 0 && slippyInSoles <= 0) {
                Vector2 help = feet.help_vec;
                float absed = Mathf.Abs(help.x);
                if (Mathf.Sign(help.x) != moveX && absed > 0.25f) {                
                    encourage.y = absed * 0.2f;
                }
            }

            rbody.AddForce(encourage * runSpeed, ForceMode2D.Impulse);
        }

        if (jc_0) {
            cachedVelocity = rbody.velocity;
            cachedVelocity.y = jumpSpeed;
            rbody.velocity = cachedVelocity;
        }

        if (jc_1) {
            cachedVelocity = rbody.velocity;
            cachedVelocity.x += direction * 2;
            // scale down y influence over time - initial shove
            cachedVelocity.y += 1f * (1f - ledgePushAccumulator / maxLedgePushTime);
            rbody.velocity = cachedVelocity;
        }

        if (jc_2) {
            cachedVelocity = rbody.velocity;
            cachedVelocity.y *= 0.5f;
            rbody.velocity = cachedVelocity;
        }

        if (jc_3) {
            float baseVelY = Mathf.Min(rbody.velocity.y, 0);
            cachedVelocity = rbody.velocity;
            cachedVelocity.y = baseVelY - slamSpeed;
            rbody.velocity = cachedVelocity;
        }

        jc_0 = jc_1 = jc_2 = jc_3 = false;

        DashTick();

        // Apply friction
        cachedVelocity = rbody.velocity;
        if (!Boosting && !Dashing) {
            float newFric = (surface == Surface.None) ? fricAir : fricGround;
            if (Slipping && surface == Surface.Ground) {
                newFric = fricSlippy;
            }
            if (adjustedDashFriction) {
                // Interpolate back to old friction
                fric += (newFric - fric) * dashFricSmooth;
                if (Mathf.Abs(fric - newFric) < 0.001f) {
                    fric = newFric;
                    adjustedDashFriction = false;
                }
            } else {
                fric = newFric;
            }

            cachedVelocity.x *= 1 - fric;

            // Velocity capping when on non-slippy surface
            if (Mathf.Abs(cachedVelocity.x) > maxVelX && !Slipping) {
                if (surface == Surface.None) {
                    // reduce down back to normal speed
                    cachedVelocity.x += Mathf.Sign(cachedVelocity.x) * (maxVelX - Mathf.Abs(cachedVelocity.x)) * fricHighSpeed * (1f / 60);
                } else {
                    // directly cap it
                    cachedVelocity.x = Mathf.Sign(cachedVelocity.x) * maxVelX;
                }
            }
        }

        // Flatgrass affliction
        if (flatgrasses_occupied < 0) flatgrasses_occupied = 0;
        if (flatgrasses_occupied > 0) {
            cachedVelocity.x *= (1 - 0.2f);
        }

        rbody.velocity = cachedVelocity;
    }

    public void UpdateOrientation(int nextDir, bool forceUpdate = false) {
        if (nextDir != direction && nextDir != 0 && !canFlipDir) {
            canFlipDir = true;
            lastPlace = rbody.transform.position;   
        }

        // We need to flip the sprite
        if (forceUpdate || (canFlipDir && Vector3.SqrMagnitude(lastPlace - rbody.transform.position) > maxDistBeforeFlip * maxDistBeforeFlip)) {
            direction *= -1;
            canFlipDir = false;

            head.Flip();
            hat.Flip(direction);

            // Flip tired puff
            Transform puffTrans = tiredAnimation.transform;
            Vector3 eulerAngles = puffTrans.localEulerAngles;
            eulerAngles.z *= -1;
            puffTrans.localRotation = Quaternion.Euler(eulerAngles);
            Vector3 pos = puffTrans.localPosition;
            pos.x *= -1;
            Vector3 scale = puffTrans.localScale;
            scale.x *= -1;
            puffTrans.localScale = scale;
            puffTrans.localPosition = pos;
        }
    }

    public void Squish(SquishType type) {
        head.Squish(type, cachedVelocity);

        // Play squish noise
        float pitch = Random.Range(1f, 2.5f);
        float volume = 1.0f * Mathf.Min(1.0f, rbody.velocity.magnitude / 30);
        if (MusicController.PLAYER_SQUISHIES_PLAYING < 4) _audio.PlayClipOnce("squish", AudioManager.sfxVol * volume, pitch);
    }
    
    public void CycleColor(Color newColor) {
        headColor = newColor;
        head.SetColor(headColor);
        foreach (Leg leg in legs) {
            leg.SetColor(headColor);
        }
    }
    
    GoTweenConfig haloConfig = new GoTweenConfig().scale(1f).setEaseType(GoEaseType.ElasticOut);

    WaitForSeconds wfs_unBopDelay = new WaitForSeconds(unBopDelay);
    WaitForSeconds wfs_unBopInkDelay = new WaitForSeconds(unBopInkDelay);
    WaitForSeconds wfs_unBopNopeDelay = new WaitForSeconds(unBopNopeDelay);
    WaitForSeconds invincibilityWait = new WaitForSeconds(invincibilityTime);

    Vector2 bopForce = Vector2.one * 10;

    GoTween bopTween;

    public void Bop(Vector2 posSub, BopType bopType, Player otherPlayer = null) {
        if (invincible) return; // Can't get bopped if recently been bopped

        // Bop up, up and away
        if (bopType == BopType.KICKED) {
            bopForce.x = Mathf.Sign(posSub.x);
            rbody.velocity = bopForce;
        }
        // Enable stun particles
        if (bopType != BopType.INKED) {
            Go.killAllTweensWithTarget(stunHalo);
            stunHalo.gameObject.SetActive(true);
            stunHalo.localScale *= 0.7f;
            
            Go.to(stunHalo, 1.5f, haloConfig);
        }
        // Audiovisual update
        if (bopType == BopType.INKED) {
            // Ink splat
            head.EnableInkSplat();
            isInked = true;
            face.SetFace(Face.FaceType.Surprised);
            _audio.PlayClipOnce("inkhit", 1f * AudioManager.sfxVol, Random.Range(0.4f, 0.7f));
        } else if (bopType == BopType.NOPED) {
            face.SetFace(Face.FaceType.Surprised);
            _audio.PlayClipOnce("kick", 1.5f * AudioManager.sfxVol, 0.5f);
        } else {
            face.SetFace(Face.FaceType.Bopped);
            _audio.PlayClipOnce("kick", 1.5f * AudioManager.sfxVol, 1f);
        }

        invincible = true;
        canControl = false;

        Go.killAllTweensWithTarget(_collider);
        _collider.offset = new Vector2(0, 0.5f);
        _collider.size = new Vector2(1f, 1f);

        // Present-release
        if (hasGift) {
            hasGift = false;
            GameManager.gift.SetFree(posSub, otherPlayer);
        }
        
        // Time to recover depends on what hit you - ink shooter or other player
        WaitForSeconds wfs = wfs_unBopDelay;
        if (bopType == BopType.INKED) wfs = wfs_unBopInkDelay;
        if (bopType == BopType.NOPED) wfs = wfs_unBopNopeDelay;
        StartCoroutine(ThenUnbop(wfs));
    }

    private IEnumerator ThenUnbop(WaitForSeconds wfs) {
        yield return wfs;
        Unbop();
    }

    private IEnumerator InvincibilityTimeout() {
        yield return invincibilityWait;
        invincible = false;
    }

    private GoTweenConfig haloUndoConfig = new GoTweenConfig().scale(0f).setEaseType(GoEaseType.ExpoIn);
    Coroutine elasticRoutine = null;
    
    void Unbop() {
        if (canControl) return;

        StartCoroutine(InvincibilityTimeout());

        canControl = true;

        // Tween cleanup, remove stun halo
        Go.killAllTweensWithTarget(stunHalo);
        haloUndoConfig.clearEvents();
        haloUndoConfig.onComplete(c => {
            stunHalo.gameObject.SetActive(false);
        });
        Go.to(stunHalo, 0.5f, haloUndoConfig);

        if (tired) {
            face.SetFace(Face.FaceType.Tired);
        } else {
            // return to default face if we're not in 'surprised' mode
            if (face.ActiveFace != Face.FaceType.Surprised) {
                var reaction = Random.Range(0f, 1f) < 0.5f ?
                    Face.FaceType.Default :
                    Face.FaceType.Frowning;

                face.SetFace(reaction);
            }
        }
        
        isInked = false;
        head.DisableInkSplat();

        bopTweenConfig.clearEvents();
        bopTweenConfig.clearProperties();
        bopTweenConfig.setEaseType(GoEaseType.ElasticOut);

        if (elasticRoutine != null) StopCoroutine(elasticRoutine);
        elasticRoutine = StartCoroutine(ElasticStandUp());

        // Jump up from bopped position
        if (surface == Surface.Ground) {
            cachedVelocity = rbody.velocity;
            cachedVelocity.y = jumpSpeed / 1.5f;
            rbody.velocity = cachedVelocity;
        }
    }

    Vector2 startSize = new Vector2(0, 0.5f);
    Vector2 startOffset = new Vector2(1, 1);
    Vector2 finalOffset = new Vector2(0, 0.25f);
    Vector2 finalSize = new Vector2(1, 1.5f);

    private WaitForFixedUpdate wffu = new WaitForFixedUpdate();
    private IEnumerator ElasticStandUp(){ 
        float passed = 0;
        passed += Time.fixedDeltaTime;
        
        Go.killAllTweensWithTarget(_collider);
        _collider.offset = new Vector2(0, 0.5f);
        _collider.size = new Vector2(1f, 1f);

        Vector2 interpSize = startSize;
        Vector2 interpOffset = startOffset;

        while (passed < 1f) {
            if (!canControl) yield break;

            passed += Time.fixedDeltaTime * 0.85f;

            float t = passed;
              float u = Mathf.Sin(-13.0f * (t + 1.0f) * Mathf.PI * 0.5f) * Mathf.Pow(2.0f, -10.0f * t) + 1.0f;
            interpSize.x = (1 - u) * startSize.x + u * finalSize.x;
            interpSize.y = (1 - u) * startSize.y + u * finalSize.y;
            
            interpOffset.x = (1 - u) * startOffset.x + u * finalOffset.x;
            interpOffset.y = (1 - u) * startOffset.y + u * finalOffset.y;

            _collider.size = interpSize;
            _collider.offset = interpOffset;

            yield return wffu;
        }
        
        _collider.size = finalSize;
        _collider.offset = finalOffset;
    }

    private Collider2D[] kickScan = new Collider2D[16];
    private void Kick() {
        justKicked = true;
        
        bool kickedButton = false;
        
        kickOffset = new Vector2(direction, Random.Range(0.1f, 0.3f));

        Player otherPlayer = null;
        int numKicks = Physics2D.OverlapCircleNonAlloc(_transform.position, kickRadius, kickScan, kickMask);

        //Collider2D[] kickScan = Physics2D.OverlapCircleAll(_transform.position, kickRadius, kickMask);
        if (UIManager.current_scene_name == GlobalStrings.SCENE_Select || GameManager.FOUND_SECRET) {
            // Code to prioritise buttons
            for (int i = 0; i < numKicks; i++) {
                Collider2D collider = kickScan[i];

                // TODO replace getcomponent by actually naming spawned prefabs by their username
                if ((collider.CompareTag(GlobalStrings.TAG_CustomiserButton) || collider.CompareTag("SECRET")) && (collider.transform.localPosition.y <= transform.localPosition.y)) {
                    collider.GetComponent<KickButton>().KickAction(this);
                    kickedButton = true;

                    // Adjust offset
                    float jitter = 0.1f;
                    Vector2 diff = (Vector2)(collider.transform.position - _transform.position);
                    kickOffset = diff + new Vector2(-0.8f * Mathf.Sign(diff.x) + Random.Range(-jitter, jitter), 0.45f);

                    break;
                }
            }
        }

        if (!kickedButton) otherPlayer = ScanKickable(kickScan, numKicks);

        bool kickSuccess = false;

        if (otherPlayer != null) {
            Vector2 posSub = otherPlayer.transform.position - _transform.position;
            if (!kickedButton) kickOffset = new Vector2(Mathf.Sign (posSub.x) * 2, Random.Range(0.35f, 0.7f));

            otherPlayer.Bop(kickOffset, BopType.KICKED, this);

            if (otherPlayer.Dashing) {
                achievementMgr.GetAxed();
            }

            // Update sorting - bring slamming pode in front
            int i0 = sortingOrder;
            int i1 = otherPlayer.sortingOrder;
            if (i0 < i1) {
                SetOrder(i1);
                otherPlayer.SetOrder(i0);
            }

            // SCREENSHAKE YESSSSSSSSSSSSSSSSSSSSSSSSSSS
            cam.ShakeScreen(posSub.normalized * Random.Range(4f, 6f), 0.5f);

            Squish(SquishType.KICK);
            kickSuccess = true;

            _audio.PlayClipOnce("kick_contact", 6f * AudioManager.sfxVol, Random.Range(0.8f, 1.3f));
        } else {
            // Missed the kick, minor visual update
            Squish(SquishType.KICKMISS);

            _audio.PlayClipOnce("kick_nocontact", 6f * AudioManager.sfxVol, Random.Range(0.8f, 1.3f));
        }
        
        float retractTime = (kickSuccess) ? kickHitRetractTime : kickRetractTime;
        kickTentacleIndex = (Mathf.Sign(kickOffset.x) < 0) ? 0 : 2; // send out nearest tentacle
        Leg kicker = legs[kickTentacleIndex];
        kicker.kicking = true;

        // Leg tween
        kickRetractConfig.clearEvents();
        kickRetractConfig.setDelay(kickExtendTime);

        if (kickRetractTween != null) kickRetractTween.destroy();
        kickRetractTween = Go.to(this, retractTime, kickRetractConfig);

        // Set leg thickness over total kick time
        kicker.StartKicking(kickExtendTime + retractTime);
    }

    private Player ScanKickable(Collider2D[] kickScan, int numKicks) {
        Player found = null;

        for (int i = 0; i < numKicks; i++) {
            Collider2D collider = kickScan[i];

            if (collider.CompareTag(GlobalStrings.TAG_Player)) {
                var other_player = collider.GetComponent<Player>();
                if (other_player != this) {
                    // Found our first pode to kick
                    // TODO make this prioritise kicking the one in the lead?
                    found = other_player;
                    break;
                }
            }
        }

        return found;
    }

    public void Kill() {
        if (!alive) {
            return;
        }

        alive = false;
        invincible = false;
        // Set appropriate state when off-screen
        ledgeBodyTriggers.Clear();
        ledgeHeadTriggers.Clear();
        slipTriggers.Clear();
        slippyInSoles = 0;
        squishHitbox.Kill();
        Slipping = false;
        Boosting = false;
        hasGift = false;
        isInked = false;

        flatgrasses_occupied = 0;
        
        // Reset dash
        EndDash(false, true);
        surface = Surface.None;
        head.KillTweens();
        head.DisableInkSplat();
        
        tired = false;
        tiredAnimation.EndAnimation();

        // face.SetFace(Face.FaceType.Default);
        face.DisableRenderers();
        
        _audio.DisableContinual();
    }
    public void Revive() {
        tiredAnimation.SetVisible(false);
        face.SetFace(Face.FaceType.Default);
    }

    private GoTweenConfig tweenToPosConfig = new GoTweenConfig();
    private bool tweening_to_podeium = false;
    public void TweenToPos(Vector3 goal) {
        // Avoid multi-setting
        if (tweening_to_podeium) return;

        StopAllCoroutines();

        // no puffin
        tired = false;
        tiredAnimation.EndAnimation();
        
        tweening_to_podeium = true;
        
        canControl = false;
        rbody.isKinematic = true;
        rbody.velocity = Vector2.zero;
        rbody.mass = 1000000;

        DisableColliders();
        
        var posTween = Go.to(_transform, 0.5f + Random.Range(0f, 0.3f), tweenToPosConfig
            .vector3Prop("position", goal)
            .setEaseType(GoEaseType.SineInOut)
            .onComplete(c => {
                rbody.isKinematic = false;
                EnableCollidersSlow();
                
                // Update gift visual
                if (hasGift) {
                    GameManager.gift.SetWon();
                }

                face.StartCoroutine(face.ReactToWin());
            })
        );
        posTween.autoRemoveOnComplete = true;
    }

    private GoTweenConfig colliderDisableConfig = new GoTweenConfig().vector2Prop("size", new Vector2(1f, 1.5f));

    public void DisableColliders() {
        Go.killAllTweensWithTarget(_collider);
        _collider.size = Vector2.zero;
    }
    
    public void EnableCollidersSlow() {
        Go.killAllTweensWithTarget(_collider);
        Go.to(_collider, 0.5f, colliderDisableConfig);
    }

    public void SetOrder(int baseIndex = -1) {
        // calculate based on number of players in game
        if (baseIndex == -1) {
            int numPlayers = (UIManager.current_scene_name == GlobalStrings.SCENE_Game) ?  GameManager.ActivePlayers.Count : PlayerCreator.joinedPlayers.Count;
            baseIndex = 10 + numPlayers * 3;
        }
        face.SetOrder(baseIndex);
        for (int i = 0; i < legs.Length; ++i) {
            legs[i].SetOrder(baseIndex);
        }
        sortingOrder = baseIndex;
    }
}
