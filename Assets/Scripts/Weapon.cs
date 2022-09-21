using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using Photon.Pun;

public class Weapon : MonoBehaviourPunCallbacks
{
    public List<Gun> loadout;
    [HideInInspector] public Gun currentGunData;

    public Transform weaponParent;
    public Transform characterParent;
    //public GameObject bulletholePrefab;
    public LayerMask canBeShot;
    public AudioSource sfx;

    private float currentCooldown;
    private int currentIndex;
    private GameObject currentWeapon;
    private GameObject currentCharacter;
    private Player playerObject;
    private GameObject canvas;

    private bool isReloading;

    public GameObject modelAnim;

    private void Start() {
        foreach(Gun a in loadout) {
            a.Initialize();
        }
        Equip(0);

        playerObject = (Player)GameObject.FindObjectOfType(typeof(Player));

        canvas = GameObject.FindGameObjectWithTag("Canvas");
        canvas.transform.Find("HUD/Ammo").gameObject.SetActive(false);
    }

    void Update()
    {
        if (Pause.paused && photonView.IsMine) return;

        if (photonView.IsMine) {
            if (Input.GetKeyDown(KeyCode.Alpha1)) {
                photonView.RPC("Equip", RpcTarget.All, 0);
            }
            if (Input.GetKeyDown(KeyCode.Alpha2) && loadout.Contains(GunLibrary.guns[1])) {
                photonView.RPC("Equip", RpcTarget.All, loadout.IndexOf(GunLibrary.guns[1]));
            }
            if(Input.GetKeyDown(KeyCode.Alpha3) && loadout.Contains(GunLibrary.guns[0])) {
                photonView.RPC("Equip", RpcTarget.All, loadout.IndexOf(GunLibrary.guns[0]));
            }

            canvas.transform.Find("HUD/Ammo").gameObject.SetActive(currentGunData.tag == 0);

            switch (currentGunData.tag) {
                case 1:
                    foreach (Transform a in canvas.transform.Find("HUD/Weapons")) {
                        a.GetComponentInChildren<Text>().fontStyle = FontStyle.Normal;
                    }
                    canvas.transform.Find("HUD/Weapons/Knife/Text").GetComponent<Text>().fontStyle = FontStyle.BoldAndItalic;
                    break;
                case 2:
                    Transform hudText = canvas.transform.Find("HUD/Weapons/Flashlight");
                    foreach (Transform a in canvas.transform.Find("HUD/Weapons")) {
                        a.GetComponentInChildren<Text>().fontStyle = FontStyle.Normal;
                    }
                    //hudText.gameObject.SetActive(true);
                    hudText.GetComponentInChildren<Text>().fontStyle = FontStyle.BoldAndItalic;
                    break;
                case 0:
                    hudText = canvas.transform.Find("HUD/Weapons/Pistol");
                    foreach (Transform a in canvas.transform.Find("HUD/Weapons")) {
                        a.GetComponentInChildren<Text>().fontStyle = FontStyle.Normal;
                    }
                    //hudText.gameObject.SetActive(true);
                    hudText.GetComponentInChildren<Text>().fontStyle = FontStyle.BoldAndItalic;
                    break;
            }

            canvas.transform.Find("HUD/Weapons/Flashlight").gameObject.SetActive(loadout.Contains(GunLibrary.guns[1]));
            canvas.transform.Find("HUD/Weapons/Pistol").gameObject.SetActive(loadout.Contains(GunLibrary.guns[0]));
        }

        bool isAiming = Input.GetMouseButton(1), isCrouching = Input.GetKey(KeyCode.LeftControl);

        if (currentWeapon != null) {
            if (photonView.IsMine) {
                if (Input.GetMouseButtonDown(0) && currentCooldown <= 0) {
                    if (!isReloading) {
                        if (loadout[currentIndex].FireBullet()) {
                            photonView.RPC("Shoot", RpcTarget.All, isAiming, isCrouching);
                        }
                        else StartCoroutine(Reload(loadout[currentIndex].reload));
                    }
                }

                if (Input.GetKeyDown(KeyCode.R)) {
                    StartCoroutine(Reload(loadout[currentIndex].reload));
                }

                // cooldown
                if (currentCooldown > 0) currentCooldown -= Time.deltaTime;
            }
            // weapon pos elasticity
            currentWeapon.transform.localPosition = Vector3.Lerp(currentWeapon.transform.localPosition, Vector3.zero, Time.deltaTime * 4f);
        }
    }

    public void RefreshAmmo(Text p_text) {
        int t_clip = loadout[currentIndex].GetClip();
        int t_stash = loadout[currentIndex].GetStash();

        p_text.text = t_clip.ToString("D2") + " / " + t_stash.ToString("D2");
    }

    IEnumerator Reload(float p_wait) {
        if (isReloading) yield break;
        isReloading = true;
        GameObject modelAnim = currentWeapon.transform.GetChild(0).GetChild(0).GetChild(0).gameObject;
        if (modelAnim.GetComponent<Animator>()) {
            modelAnim.GetComponent<Animator>().Play("metarig|reloadArm", 0, 0);
        }
        else {
            currentWeapon.SetActive(false);
        }

        yield return new WaitForSeconds(p_wait);

        loadout[currentIndex].Reload();
        currentWeapon.SetActive(true);
        isReloading = false;
    }

    [PunRPC]
    void Equip(int p_ind){
        if(currentWeapon != null) {
            StopCoroutine("Reload");
            Destroy(currentWeapon);
        }

        currentIndex = p_ind;

        GameObject t_newWeapon = Instantiate(loadout[p_ind].prefab, weaponParent.position, weaponParent.rotation, weaponParent) as GameObject;
        t_newWeapon.transform.localPosition = Vector3.zero;
        t_newWeapon.transform.localEulerAngles = Vector3.zero;
        t_newWeapon.GetComponent<Sway>().isMine = photonView.IsMine;

        //GameObject t_newCharacter = Instantiate(loadout[p_ind].character, characterParent.position, characterParent.rotation, characterParent) as GameObject;
        //t_newCharacter.transform.localPosition = Vector3.zero;
        //t_newCharacter.transform.localEulerAngles = Vector3.zero;

        GameObject t_newCharacter = modelAnim.transform.Find(loadout[p_ind].character.name).gameObject;
        foreach (Transform subModels in modelAnim.transform) {
            subModels.gameObject.SetActive(false);
        }
        t_newCharacter.SetActive(true);

        if (photonView.IsMine) {
            ChangeLayersRecursively(t_newWeapon, 9);
            ChangeLayersRecursively(t_newCharacter, 7);
        }
        else {
            ChangeLayersRecursively(t_newWeapon, 0);
            ChangeLayersRecursively(t_newCharacter, 8);
        }

        currentWeapon = t_newWeapon;
        currentCharacter = t_newCharacter;
        currentGunData = loadout[p_ind];
    }

    public bool CheckLoadoutForWeapon(string name) {
        return loadout.Contains(GunLibrary.FindGun(name));
    }
    
    [PunRPC]
    void PickupWeapon(string name) {
        // find weapon from library
        Gun newWeapon = GunLibrary.FindGun(name);
        newWeapon.Initialize();

        if(loadout.Count >= 3) {
            // replace curr weapon
            loadout[currentIndex] = newWeapon;
            Equip(currentIndex);
        }
        else {
            // add weapon
            loadout.Add(newWeapon);
            Equip(loadout.Count - 1);
        }
    }

    private void ChangeLayersRecursively(GameObject p_target, int p_layer) {
        p_target.layer = p_layer;
        foreach(Transform a in p_target.transform) {
            ChangeLayersRecursively(a.gameObject, p_layer);
        }
    }

    public bool Aim(bool p_isAiming) {
        if (!currentWeapon) return false;
        if (currentGunData.tag != 0) return false;
        if (isReloading) p_isAiming = false;

        //Transform t_anchor = currentWeapon.transform.Find("Anchor");
        //Transform t_state_ads = currentWeapon.transform.Find("States/ADS");
        //Transform t_state_hip = currentWeapon.transform.Find("States/Hip");

        //if (p_isAiming) {
        //    // aim
        //    t_anchor.position = Vector3.Lerp(t_anchor.position, t_state_ads.position, Time.deltaTime * loadout[currentIndex].aimSpeed);
        //}
        //else {
        //    // hip
        //    t_anchor.position = Vector3.Lerp(t_anchor.position, t_state_hip.position, Time.deltaTime * loadout[currentIndex].aimSpeed);
        //}

        GameObject modelAnim = currentWeapon.transform.GetChild(0).GetChild(0).GetChild(0).gameObject;
        modelAnim.GetComponent<Animator>().SetBool("isAiming", p_isAiming);

        return p_isAiming;
    }

    [PunRPC]
    void Shoot(bool isAiming, bool isCrouching) {
        //Debug.Log("Shot");
        if (currentGunData.tag == 2) return;

        Transform t_spawn = transform.Find("Cameras/CamCenterPiv/Camera");
        Debug.Log(t_spawn.gameObject.name);

        //bloom
        float bloomAmount = 1000f;
        if (isAiming) bloomAmount = 10000f;
        else if (isCrouching) bloomAmount = 5000f;
        Vector3 t_bloom = t_spawn.position + t_spawn.forward * bloomAmount;
        t_bloom += Random.Range(-loadout[currentIndex].bloom, loadout[currentIndex].bloom) * t_spawn.up;
        t_bloom += Random.Range(-loadout[currentIndex].bloom, loadout[currentIndex].bloom) * t_spawn.right;
        t_bloom -= t_spawn.position;
        t_bloom.Normalize();

        // cooldown
        currentCooldown = loadout[currentIndex].firerate;   

        //raycast
        RaycastHit t_hit = new RaycastHit();
        if (Physics.Raycast(t_spawn.position, t_bloom, out t_hit, currentGunData.raycastLength, canBeShot)) {
            GameObject t_newHole = Instantiate(currentGunData.bulletholePrefab, t_hit.point + t_hit.normal * 0.001f, Quaternion.identity) as GameObject;
            t_newHole.transform.LookAt(t_hit.point + t_hit.normal);
            Destroy(t_newHole, 5f);

            if (photonView.IsMine) {
                //shooting other player on network
                if (t_hit.collider.gameObject.layer == 8) {
                    bool applyDamage = false;

                    if (GameSettings.GameMode == GameMode.FFA) {
                        applyDamage = true;
                    }
                    if (GameSettings.GameMode == GameMode.TDM) {
                        if (t_hit.collider.transform.root.gameObject.GetComponent<Player>().awayTeam != GameSettings.IsAwayTeam) {
                            applyDamage = true;
                        }
                    }

                    if (applyDamage) {
                        //RPC Call to Damage Player
                        t_hit.collider.transform.root.gameObject.GetPhotonView().RPC("TakeDamage", RpcTarget.All, loadout[currentIndex].damage, PhotonNetwork.LocalPlayer.ActorNumber);
                    }
                }
            }
        }

        // sound
        sfx.Stop();
        sfx.clip = currentGunData.gunshotSound;
        sfx.pitch = 1 - currentGunData.pitchRandomization + Random.Range(-currentGunData.pitchRandomization, currentGunData.pitchRandomization);
        sfx.volume = currentGunData.shotVolume;
        sfx.Play();

        // gun fx
        currentWeapon.transform.Rotate(-loadout[currentIndex].recoil * ((isAiming) ? 0.2f : 1f), 0, 0);
        currentWeapon.transform.position -= currentWeapon.transform.forward * loadout[currentIndex].kickback;

        if (currentGunData.tag == 1) {
            GameObject modelAnim = currentWeapon.transform.GetChild(0).GetChild(0).GetChild(0).gameObject;
            modelAnim.GetComponent<Animator>().SetTrigger("Stabbed");
            StartCoroutine(Reload(loadout[currentIndex].reload));
        }
    }

    [PunRPC]
    private void TakeDamage(int p_damage, int p_actor) {
        GetComponent<Player>().TakeDamage(p_damage, p_actor);
    }

    public Gun GetCurrWeapon() {
        return currentGunData;
    }
}
