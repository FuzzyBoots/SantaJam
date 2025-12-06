using FS_CombatCore;
using FS_Core;
using FS_ThirdPerson;
using System.Collections;
using UnityEngine;
namespace FS_CombatSystem
{
    public class MeleeCombatEffects : MonoBehaviour
    {
        FighterCore fighter;
        MeleeFighter meleeFighter;
        PlayerController player;
        private void Awake()
        {
            fighter = GetComponent<FighterCore>();
            meleeFighter = GetComponent<MeleeFighter>();
            player = GetComponent<PlayerController>();
        }

        private void Start()
        {
            meleeFighter.OnAttack += (target) =>
            {
                var strikeSound = meleeFighter.CurrAttack?.StrikeSound;
                if (strikeSound != null)
                    PlaySfx(strikeSound);
            };

            fighter.OnGotHit += (FighterCore attacker, Vector3 hitPoint, float hittingTime, bool isBlockedHit, HitType hitType) =>
            {
                if (attacker == null || hitType != HitType.Melee) return;

                var meleeAttacker = attacker.GetComponent<MeleeFighter>();

                if (meleeAttacker?.CurrAttack == null) return;

                var attack = meleeAttacker.CurrAttack;

                if (attack == null) return;

                // Play Hit Sound
                AudioClip hitSound = fighter.IsBlocking ? attack.BlockedHitSound : attack.HitSound;

                if (fighter.IsBlocking)
                    PlaySfx(attack.BlockedHitSound);
                else if(attack.hitSounds.Count > 0)
                {
                    var audio = attack.hitSounds[Random.Range(0, attack.hitSounds.Count)];
                    PlaySfx(audio);
                }

                // Play Reaction Sound
                if (!fighter.IsBlocking && attack.reactionSounds.Count > 0)
                {
                    var audio = attack.reactionSounds[Random.Range(0, attack.reactionSounds.Count)];
                    PlaySfx(audio);
                }

                // Play Vfx
                StartCoroutine(PlayVfx(meleeAttacker, hitPoint, hittingTime));
            };

            //meleeFighter.OnWeaponEquipAction += (WeaponData weaponData) =>
            //{
            //    if (!playEquipSound) return;
            //    var equipSound = weaponData.WeaponEquipSound;
            //    if (equipSound != null)
            //        PlaySfx(equipSound);
            //};

            //meleeFighter.OnWeaponUnEquipAction += (WeaponData weaponData) =>
            //{
            //    if (!playUnEquipSound) return;
            //    var unEquipSound = weaponData?.WeaponUnEquipSound;
            //    if (unEquipSound != null)
            //        PlaySfx(unEquipSound);
            //};

            meleeFighter.OnEnableHit += (MeleeWeaponObject handler) => StartCoroutine(EnableTrail(handler));
        }

        void PlaySfx(AudioClip clip)
        {
            GameObject sfx = new GameObject();
            sfx.transform.position = fighter.transform.position;
            var audioSource = sfx.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.Play();
            Destroy(sfx, 1.5f);
        }

        IEnumerator PlayVfx(MeleeFighter attacker, Vector3 hitPoint, float hittingTime)
        {
            var curAttack = attacker.CurrAttack;
            if (hitPoint == Vector3.zero && hittingTime >= 0)
            {
                yield return new WaitForSeconds(hittingTime);
                hitPoint = meleeFighter.GetHitPoint(attacker.GetComponent<MeleeFighter>());
            }
            var effect = fighter.overrideBloodEffects ? fighter.bloodEffectPrefab : curAttack.HitEffect;
            var hitEffect = fighter.IsBlocking ? attacker.CurrAttack.BlockedHitEffect : effect;
            if (hitEffect == null)
                yield break;
            var vfxObj = Instantiate(hitEffect, hitPoint, Quaternion.identity);
            vfxObj.SetActive(false);
            vfxObj.transform.position = hitPoint;
            vfxObj.SetActive(true);
            Destroy(vfxObj, 1.5f);
        }

        IEnumerator EnableTrail(MeleeWeaponObject weapon)
        {
            if (weapon?.trail == null) yield break;

            weapon.trail.gameObject.SetActive(true);
            var impactEndTime = meleeFighter.CurrAttack.ImpactEndTime;
            float timer = 0;
            while (timer < impactEndTime)
            {
                if (fighter.Action != FighterAction.Attacking)
                    break;

                timer += Time.deltaTime;
                yield return null;
            }
            weapon.trail.gameObject.SetActive(false);
        }
    }
}