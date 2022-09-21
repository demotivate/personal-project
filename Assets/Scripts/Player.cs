using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using Photon.Pun;
using TMPro;

// [RequireComponent(typeof(Animator))];
public class Player : MonoBehaviourPunCallbacks, IPunObservable {

    public CharacterController controller;
    public Weapon w;

    public AudioSource sfx;
    public AudioClip sound;

    public float speed = 12f;
    public float gravity = -9.81f;
    public float jumpHeight = 3f;

    public int max_health;

    public Camera weaponCam;
    public Camera mainCam;
    private float baseFOV;

    public GameObject cameraParent;
    public Transform trueCameraParent;

    public Transform weaponParent;
    public GameObject spotlight;

    private Vector3 weaponParentOrigin;
    private Vector3 cameraParentOrigin;
    //public GameObject mesh; // delete instances of this later

    public Renderer[] teamIndicators;

    private float movementCounter;
    private float idleCounter;

    private Vector3 targetWeaponBobPositon;
    private Vector3 targetCameraBobPosition;

    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    [HideInInspector] public ProfileData playerProfile;
    [HideInInspector] public bool awayTeam;
    public TextMeshPro playerUsername;

    private Transform ui_healthbar;
    private Text ui_ammo;
    private Text ui_username;
    private Text ui_team;

    private int current_health;

    private Manager manager;
    private Weapon weapon;

    private bool crouched;

    private float aimAngle;

    private float stepDelay, stepDelayReset;


    Vector3 velocity;
    bool isGrounded;
    public GameObject modelAnim;
    private Animator anim;

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) {
        if (stream.IsWriting) {
            stream.SendNext((int)(weaponParent.transform.localEulerAngles.x * 100f));
        }
        else {
            aimAngle = (int)stream.ReceiveNext() / 100f;
        }
    }

    void Start() {
        manager = GameObject.Find("Manager").GetComponent<Manager>();
        weapon = GetComponent<Weapon>();
        current_health = max_health;

        cameraParent.SetActive(photonView.IsMine);
        if (!photonView.IsMine) {
            gameObject.layer = 8;
            ChangeLayerRecursively(modelAnim.transform, 8);
        }

        weaponParentOrigin = weaponParent.localPosition;
        cameraParentOrigin = trueCameraParent.localPosition;
        baseFOV = weaponCam.fieldOfView;


        if (photonView.IsMine) {
            ui_healthbar = GameObject.Find("HUD/Health/Bar").transform;
            if(GameObject.Find("HUD/Ammo/Text") != null) ui_ammo = GameObject.Find("HUD/Ammo/Text").GetComponent<Text>();
            ui_username = GameObject.Find("HUD/Username/Text").GetComponent<Text>();
            ui_team = GameObject.Find("HUD/Team/Text").GetComponent<Text>();

            RefreshHealthBar();
            ui_username.text = Launcher.myProfile.username;

            photonView.RPC("SyncProfile", RpcTarget.All, Launcher.myProfile.username, Launcher.myProfile.level, Launcher.myProfile.xp);

            if(GameSettings.GameMode == GameMode.TDM) {
                photonView.RPC("SyncTeam", RpcTarget.All, GameSettings.IsAwayTeam);

                if (GameSettings.IsAwayTeam) {
                    ui_team.text = "RED TEAM";
                    ui_team.color = Color.red;
                }
                else {
                    ui_team.text = "BLUE TEAM";
                    ui_team.color = Color.blue;
                }
            }
            else {
                ui_team.gameObject.SetActive(false);
            }

            //anim = modelAnim.GetComponent<Animator>();
            anim = modelAnim.GetComponentInChildren<Animator>();
            sfx.clip = sound;
            stepDelay = 0f;
            stepDelayReset = 0f;

            spotlight.SetActive(false);
        }
    }

    private void ColorTeamIndicators(Color p_color) {
        foreach(Renderer renderer in teamIndicators) {
            renderer.material.color = p_color;
        }
    }

    [PunRPC]
    private void SyncProfile(string p_username, int p_level, int p_xp) {
        playerProfile = new ProfileData(p_username, p_level, p_xp);
        playerUsername.text = playerProfile.username;
    }

    [PunRPC]
    private void SyncTeam(bool p_awayTeam) {
        awayTeam = p_awayTeam;
        if (awayTeam) {
            ColorTeamIndicators(Color.red);
        }
        else {
            ColorTeamIndicators(Color.blue);
        }
    }

    private void ChangeLayerRecursively(Transform p_trans, int p_layer) {
        p_trans.gameObject.layer = p_layer;
        foreach (Transform t in p_trans) ChangeLayerRecursively(t, p_layer);
    }

    // Update is called once per frame
    void Update() {
        if (!photonView.IsMine) {
            RefreshMultiplayerState();
            return;
        }

        anim = modelAnim.GetComponentInChildren<Animator>();

        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0) {
            velocity.y = -2f;
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        bool jump = Input.GetKeyDown(KeyCode.Space);
        bool pause = Input.GetKeyDown(KeyCode.Escape);
        bool aim = Input.GetMouseButton(1);

        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && isGrounded && GameObject.Find("CamCenterPiv").GetComponent<cameraMovementCenteredPivot>().leanSetting == 1 && !Input.GetMouseButton(1);
        bool isCrouching = Input.GetKey(KeyCode.LeftControl);
        bool isWalking = Input.GetKey(KeyCode.LeftAlt);

        // Pause
        if (pause) {
            GameObject.Find("Pause").GetComponent<Pause>().TogglePause();
        }
        if (Pause.paused) {
            x = 0f;
            z = 0f;
            jump = false;
            isCrouching = false;
            pause = false;
            isGrounded = false;
            isSprinting = false;
            isWalking = false;
            aim = false;
        }

        Vector3 t_direction = new Vector3(x, 0, z);
        float t_adjustedSpeed = speed;

        Vector3 move = transform.right * x + transform.forward * z;
        if (isCrouching) t_adjustedSpeed = 6f;
        else if (isWalking) t_adjustedSpeed = 8f;
        else if (isSprinting) {
            t_adjustedSpeed = 24f;
            stepDelayReset = 0.25f;
        }
        else {
            stepDelayReset = .5f;
        }
        controller.Move(move * t_adjustedSpeed * Time.deltaTime);

        if((isSprinting || (!isWalking && !isCrouching)) && (x != 0f || z != 0f)) {
            if (stepDelay == 0) {
                photonView.RPC("PlayStepSound", RpcTarget.All);
            }
        }
        //else {
        //    sfx.Stop();
        //}

        stepDelay = Mathf.Max(stepDelay - Time.deltaTime, 0);


        if (crouched != isCrouching) {
            crouched = isCrouching;
            photonView.RPC("SetCrouch", RpcTarget.All);
        }
        anim.SetBool("IsCrouching", isCrouching);

        if (jump && isGrounded) {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // TEST
        //if (Input.GetKeyDown(KeyCode.U)) {
        //    TakeDamage(100);
        //}

        // aim
        aim = weapon.Aim(aim);
        if (aim) {
            weaponCam.fieldOfView = Mathf.Lerp(weaponCam.fieldOfView, baseFOV * weapon.currentGunData.weaponFOV, Time.deltaTime * 8f);
        }
        else {
            weaponCam.fieldOfView = Mathf.Lerp(weaponCam.fieldOfView, baseFOV, Time.deltaTime * 8f);
        }

        //if (Input.GetKeyDown(KeyCode.U)) TakeDamage(100, -1);

        // head bob
        if (aim) {
            HeadBob(idleCounter, 0f, 0f);
            weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPositon, Time.deltaTime * 10f);
            CameraBob(idleCounter, 0f, 0f);
            trueCameraParent.localPosition = Vector3.Lerp(trueCameraParent.localPosition, targetCameraBobPosition, Time.deltaTime * 10f);
        }
        else if (!isGrounded) {
            HeadBob(idleCounter, 0f, 0f);
            weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPositon, Time.deltaTime * 2f);
            CameraBob(idleCounter, 0f, 0f);
            trueCameraParent.localPosition = Vector3.Lerp(trueCameraParent.localPosition, targetCameraBobPosition, Time.deltaTime * 2f);
        }
        else if (x == 0 && z == 0) {
            HeadBob(idleCounter, 0.025f, 0.025f);
            CameraBob(idleCounter, 0f, 0f);
            idleCounter += Time.deltaTime;
            weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPositon, Time.deltaTime * 2f);
            trueCameraParent.localPosition = Vector3.Lerp(trueCameraParent.localPosition, targetCameraBobPosition, Time.deltaTime * 2f);
        }
        else if (!isSprinting) {
            HeadBob(movementCounter, 0.15f, 0.075f);
            CameraBob(idleCounter, 0f, 0f);
            movementCounter += Time.deltaTime * 5f;
            weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPositon, Time.deltaTime * 10f);
            trueCameraParent.localPosition = Vector3.Lerp(trueCameraParent.localPosition, targetCameraBobPosition, Time.deltaTime * 10f);
        }
        else if (isCrouching || isWalking) {
            HeadBob(movementCounter, 0.015f, 0.075f);
            CameraBob(idleCounter, 0f, 0f);
            movementCounter += Time.deltaTime * 1f;
            weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPositon, Time.deltaTime * 5f);
            trueCameraParent.localPosition = Vector3.Lerp(trueCameraParent.localPosition, targetCameraBobPosition, Time.deltaTime * 5f);
        }
        else {
            HeadBob(movementCounter, 0.2f, 0.08f);
            CameraBob(movementCounter, 1f, .3f);
            movementCounter += Time.deltaTime * 8f;
            weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPositon, Time.deltaTime * 20f);
            trueCameraParent.localPosition = Vector3.Lerp(trueCameraParent.localPosition, targetCameraBobPosition, Time.deltaTime * 20f);
        }

        //anim
        float t_anim_horizontal = 0f;
        float t_anim_vertical = 0f;

        //if (isGrounded) {
        t_anim_vertical = z;
        t_anim_horizontal = x;
        //}

        anim.SetFloat("Vertical", t_anim_vertical);
        anim.SetFloat("Horizontal", t_anim_horizontal);

        //UI Refresh
        RefreshHealthBar();
        if (GameObject.Find("HUD/Ammo/Text") != null && ui_ammo == null) ui_ammo = GameObject.Find("HUD/Ammo/Text").GetComponent<Text>();
        if(ui_ammo != null) weapon.RefreshAmmo(ui_ammo);
    }

    [PunRPC]
    void PlayStepSound() {
        sfx.Stop();
        sfx.Play();
        stepDelay = stepDelayReset;
    }

    void RefreshMultiplayerState() {
        float cacheEulY = weaponParent.localEulerAngles.y;

        Quaternion targetRotation = Quaternion.identity * Quaternion.AngleAxis(aimAngle, Vector3.right);
        weaponParent.rotation = Quaternion.Slerp(weaponParent.rotation, targetRotation, Time.deltaTime * 8f);

        Vector3 finalRotation = weaponParent.localEulerAngles;
        finalRotation.y = cacheEulY;

        weaponParent.localEulerAngles = finalRotation;
    }

    void HeadBob(float p_z, float p_x_intensity, float p_y_intensity) {
        targetWeaponBobPositon = weaponParentOrigin + new Vector3(Mathf.Cos(p_z) * p_x_intensity, Mathf.Sin(p_z * 2) * p_y_intensity, 0);
    }
    
    void CameraBob(float p_z, float p_x_intensity, float p_y_intensity) {
        targetCameraBobPosition = cameraParentOrigin + new Vector3(Mathf.Cos(p_z) * p_x_intensity, Mathf.Sin(p_z * 2) * p_y_intensity, 0);
    }

    void RefreshHealthBar() {
        float t_health_ratio = (float)current_health / (float)max_health;
        ui_healthbar.localScale = Vector3.Lerp(ui_healthbar.localScale, new Vector3(t_health_ratio, 1, 1), Time.deltaTime * 8f);
    }

    public void TrySync() {
        if (!photonView.IsMine) return;

        photonView.RPC("SyncProfile", RpcTarget.All, Launcher.myProfile.username, Launcher.myProfile.level, Launcher.myProfile.xp);

        if(GameSettings.GameMode == GameMode.TDM) {
            photonView.RPC("SyncTeam", RpcTarget.All, GameSettings.IsAwayTeam);
        }
    }

    [PunRPC]
    void SetCrouch() {
        if (crouched) {
            controller.height = 2.8f;
            controller.center = new Vector3(0f, -0.5f, 0f);
        }
        else {
            controller.height = 3.8f;
            controller.center = Vector3.zero;
        }
    }

    public void TakeDamage(int p_damage, int p_actor) {
        if (photonView.IsMine) {
            current_health -= p_damage;
            RefreshHealthBar();

            if (current_health <= 0) {
                manager.Spawn();
                manager.ChangeStat_S(PhotonNetwork.LocalPlayer.ActorNumber, 1, 1);

                if(p_actor >= 0) {
                    manager.ChangeStat_S(p_actor, 0, 1);
                }

                PhotonNetwork.Destroy(gameObject);
            }
        }
    }
}
