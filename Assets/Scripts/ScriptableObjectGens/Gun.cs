using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName="New Gun", menuName="Gun")]
public class Gun : ScriptableObject
{
    public string name;
    // 0 = gun, 1 = knife, 2 = flashlight
    public short tag;
    public int damage;
    public int ammo;
    public int clipsize;
    public float firerate;
    public float raycastLength;
    public float bloom;
    public float recoil;
    public float kickback;
    public float aimSpeed;
    public float reload;
    [Range(0, 1)] public float mainFOV;
    [Range(0, 1)] public float weaponFOV;
    public AudioClip gunshotSound;
    public float pitchRandomization;
    public float shotVolume;
    public GameObject prefab;
    public GameObject bulletholePrefab;
    public GameObject character;
    public GameObject display;

    //public GameObject modelAnim;
    private Animator anim;
    private int stash; //current ammo
    private int clip; //current clip

    public void Initialize() {
        anim = character.GetComponent<Animator>();
        stash = ammo;
        clip = clipsize;
    }

    public bool FireBullet() {
        if (clip > 0) {
            clip -= 1;
            return true;
        }
        return false;
    }

    public void Reload() {
        //stash += clip;
        //clip = Mathf.Min(clipsize, stash);
        //stash -= clip;
        clip = Mathf.Min(clipsize, stash + clip);
    }

    public int GetStash() {
        return stash;
    }

    public int GetClip() {
        return clip;
    }

    //public void SetLeanAnim(int leanSetting, float leanSpeed) {
    //    anim.gameObject.SetActive(true);
    //    //if (anim.runtimeAnimatorController != null) return;
    //    anim.SetFloat("Lean", Mathf.Lerp(anim.GetFloat("Lean"), (float)leanSetting / 2f, leanSpeed * Time.deltaTime));
    //}

    //public void SetCrouchAnim(bool isCrouching) {
    //    anim.gameObject.SetActive(true);
    //    //if (anim.runtimeAnimatorController != null) return;
    //    anim.SetBool("IsCrouching", isCrouching);
    //}

    //public void SetVel(float t_anim_vertical, float t_anim_horizontal) {
    //    anim.gameObject.SetActive(true);
    //    //if (anim.runtimeAnimatorController != null) return;
    //    anim.SetFloat("Vertical", t_anim_vertical);
    //    anim.SetFloat("Horizontal", t_anim_horizontal);
    //}
    //// thinking to run these methods from the other scripts
}
