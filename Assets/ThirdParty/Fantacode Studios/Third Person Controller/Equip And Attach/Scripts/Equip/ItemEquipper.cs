using FS_Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FS_ThirdPerson
{
    [RequireComponent(typeof(BoneMapper))]
    public class ItemEquipper : MonoBehaviour
    {
        [field: HideInInspector] public List<EquippableItem> equippableItems = new List<EquippableItem>();

        public EquippableItem EquippedItem { get; set; }

        public bool IsCurrentItemUnusable { get; private set; }
        public EquippableItemHolder LeftHandHolder { get; private set; }
        public EquippableItemHolder RightHandHolder { get; private set; }

        public EquippableItemObject EquippedItemRight => RightHandHolder?.currentItem;
        public EquippableItemObject EquippedItemLeft => LeftHandHolder?.currentItem;
        public EquippableItemObject EquippedItemObject => EquippedItem?.holderBone == HumanBodyBones.RightHand? RightHandHolder.currentItem : LeftHandHolder.currentItem;
            
       

        public bool IsChangingItem => IsEquippingItem || IsUnEquippingItem;
        public bool IsEquippingItem { get; set; }
        public bool IsUnEquippingItem { get; set; }
        public bool PreventItemSwitching { get; set; } = false;
        public bool PreventItemUnEquip { get; set; } = false;
        public bool InterruptItemSwitching { get; set; } = false;

        /// <summary>
        /// Invoked when the item becomes completely unusable and can no longer be used (e.g., out of ammo, broken, disabled by game state).
        /// </summary>
        public Action<EquippableItem> OnItemBecameUnusable;

        /// <summary>
        /// Invoked when the item becomes restricted and cannot be used anymore (e.g., due to conditions like being stunned, disarmed, or out of bounds).
        /// </summary>
        public Action<EquippableItem> OnItemBecameRestricted;

        /// <summary>
        /// Invoked when a previously restricted item becomes usable again (e.g., restriction lifted, re-enabled).
        /// </summary>
        public Action<EquippableItem> OnItemBecameUnrestricted;

        public Action<EquippableItem> OnEquip;
        public Action OnUnEquip;

        public Action<EquippableItem> OnEquipComplete;
        public Action<EquippableItem> OnBeforeItemDisable;
        public Action OnUnEquipComplete;


        AnimGraph animGraph;
        ItemAttacher itemAttacher;
        BoneMapper boneMapper;

        public Animator Animator { get; private set; }
        public CharacterID CharacterId { get; private set; }


        public void Awake()
        {
            Animator = GetComponent<Animator>();
            animGraph = GetComponent<AnimGraph>();
            itemAttacher = GetComponent<ItemAttacher>();
            boneMapper = GetComponent<BoneMapper>();
            CharacterId = GetComponent<CharacterID>();

            if (CharacterId == null)
                CharacterId = gameObject.AddComponent<CharacterID>();

            LeftHandHolder = boneMapper.GetBone(BoneType.LeftHand).GetComponentInChildren<EquippableItemHolder>();
            RightHandHolder = boneMapper.GetBone(BoneType.RightHand).GetComponentInChildren<EquippableItemHolder>();

        }

        /// <summary>
        /// Adds an equippable item to the equippable items list if it is not already present.
        /// </summary>
        /// <param name="item">The equippable item to add.</param>
        public void AddItem(EquippableItem item)
        {
            if (!equippableItems.Contains(item))
                equippableItems.Add(item);
        }

        public void RemoveItem(EquippableItem item)
        {
            if (equippableItems.Contains(item))
            {
                equippableItems.Remove(item);
            }
        }

        #region Equip

        public void EquipItem(EquippableItem itemData, bool playEquipAnimation = true, Action onItemEnabled = null, Action onPrevItemDisabled = null, EquippableItemObject itemObject = null)
        {
            StartCoroutine(Equip(itemData, playEquipAnimation, onItemEnabled, onPrevItemDisabled, itemObject));
        }


        /// <summary>
        /// Handles the process of equipping a new item, including unequipping the current item if necessary,
        /// playing equip animations, attaching the item to the correct hand(s), and invoking relevant callbacks.
        /// Supports equipping based on item slot, interruption, and optional item instantiation.
        /// <param name="dropItem">If true, drops the item after equipping (used during slot replacement).</param>
        /// </summary>
        private IEnumerator Equip(EquippableItem itemData, bool playEquipAnimation = true, Action onItemEnabled = null, Action onPrevItemDisabled = null, EquippableItemObject itemObject = null, bool dropItem = false)
        {
            // Exit if the item is null, the same as the currently equipped item, or if switching is prevented
            if (itemData == null || EquippedItem == itemData || PreventItemSwitching || IsChangingItem)
                yield break;

            if (EquipSettings.i.EquipItemsBasedOnSlot && !dropItem)
            {
                var currentItemWithSameSlot = equippableItems.FirstOrDefault(i => i.equipmentSlotIndex == itemData.equipmentSlotIndex);
                if (currentItemWithSameSlot != null && itemData != currentItemWithSameSlot)
                {
                    if (EquippedItem != currentItemWithSameSlot)
                    {
                        yield return Equip(currentItemWithSameSlot, false, dropItem: true);
                    }
                    DropItem(currentItemWithSameSlot, false);
                    itemAttacher.DettachItem(currentItemWithSameSlot);
                    yield return new WaitUntil(() => !IsUnEquippingItem);
                }

            }
            itemAttacher.AttachItem(itemData, disableItemAfterAttach: true);

            if (EquippedItem != null)
            {
                // Unequip the currently equipped item before equipping the new one
                UnEquipItem(playEquipAnimation, onItemDisabled: onPrevItemDisabled);

                // Wait until the item is completely unequipped before proceeding
                yield return new WaitUntil(() => !IsChangingItem);
            }

            // Exit if an interruption occurs during item switching
            if (InterruptItemSwitching)
                yield break;

            IsEquippingItem = true;

            // Attach the new item to the character (returns whether the attachment was successful)
            var hasItem = SetItem(itemData, itemObject);

            bool weaponEnabled = false;
            // Create an action to enable the item at the correct animation timing
            var itemEnableAction = new ActionData()
            {
                normalizeTime = itemData.itemEnableTime,
                action = () =>
                {
                    EnableItem(itemData, playEquipAnimation);
                    onItemEnabled?.Invoke();
                    weaponEnabled = true;
                }
            };

            EquippedItem = itemData;
            OnEquip?.Invoke(itemData);

            // If an equip animation exists, play it and trigger the enable action at the right moment
            if (playEquipAnimation && itemData.equipClip.clip != null)
                yield return animGraph.CrossfadeAsync(null, itemData.equipClip, mask: Mask.Arm, actions: itemEnableAction);
            else
                itemEnableAction.action?.Invoke(); // Immediately enable the item if no animation is played

            if (!weaponEnabled)
            {
                itemEnableAction.action?.Invoke();
            }

            OnEquipComplete?.Invoke(itemData);
            IsEquippingItem = false;
        }
        bool SetItem(EquippableItem equippableItemData, EquippableItemObject itemObject = null)
        {
            var hasItem = false;

            if (equippableItemData.isDualItem)
            {
                hasItem = SpawnItem(equippableItemData, RightHandHolder, equippableItemData.LocalPositionR(CharacterId, Animator.avatar), equippableItemData.LocalRotationR(CharacterId, Animator.avatar), HumanBodyBones.RightHand, itemObject);
                hasItem = SpawnItem(equippableItemData, LeftHandHolder, equippableItemData.LocalPositionL(CharacterId, Animator.avatar), equippableItemData.LocalRotationL(CharacterId, Animator.avatar), HumanBodyBones.LeftHand,itemObject);
            }
            else if (equippableItemData.holderBone == HumanBodyBones.RightHand)
            {
                hasItem = SpawnItem(equippableItemData, RightHandHolder, equippableItemData.LocalPositionR(CharacterId, Animator.avatar), equippableItemData.LocalRotationR(CharacterId, Animator.avatar), HumanBodyBones.RightHand,itemObject);
            }
            else if (equippableItemData.holderBone == HumanBodyBones.LeftHand)
            {
                hasItem = SpawnItem(equippableItemData, LeftHandHolder, equippableItemData.LocalPositionL(CharacterId, Animator.avatar), equippableItemData.LocalRotationL(CharacterId, Animator.avatar), HumanBodyBones.LeftHand,itemObject);
            }

            if (hasItem)
                animGraph.PlayLoopingAnimation(equippableItemData.itemEquippedIdleClip, mask: equippableItemData.itemEquippedIdleClipMask, isActAsAnimatorOutput: true);
            else
                Debug.LogWarning("Cannot equip item because the ItemObject is null.");


            return hasItem;
        }
        bool SpawnItem(EquippableItem itemData, EquippableItemHolder itemHolder, Vector3 localPosition, Vector3 localRotation, HumanBodyBones bodyBone, EquippableItemObject itemObject = null)
        {
            GameObject weaponObject = null;
            if (itemObject != null)
            {
                weaponObject = itemObject.gameObject;
                weaponObject.transform.parent = itemHolder.transform;
                weaponObject.transform.localPosition = localPosition;
                weaponObject.transform.localRotation = Quaternion.Euler(localRotation);
                weaponObject.SetActive(false);
            }

            EquippableItemObject currentItem = weaponObject?.GetComponent<EquippableItemObject>();

            if (currentItem == null && Animator.isHuman)
            {
                var items = Animator.GetBoneTransform(bodyBone).GetComponentsInChildren<EquippableItemObject>(true);
                currentItem = items.FirstOrDefault(i => i.itemData == itemData && i.GetComponentInParent<EquippableItemHolder>() == itemHolder);
            }

            if (currentItem == null)
            {
                if (itemData.modelPrefab != null)
                {
                    weaponObject = Instantiate(itemData.modelPrefab, itemHolder.transform);
                    weaponObject.transform.localPosition = localPosition;
                    weaponObject.transform.localRotation = Quaternion.Euler(localRotation);
                    weaponObject.SetActive(false);
                }
                else
                {
                    // For Hand Combat
                    weaponObject = boneMapper.GetBone(BoneMapper.HumanBodyBoneToBoneType(itemData.holderBone))?.transform.Find(itemData.holderBone.ToString() + "Collider")?.gameObject;
                }

                if (weaponObject == null) return false;

                currentItem = weaponObject.GetComponent<EquippableItemObject>();
                currentItem.itemData = itemData;
                currentItem.defaultLocalPos = currentItem.transform.localPosition;
                currentItem.defaultLocalRot = currentItem.transform.localRotation;
            }

            AddItem(currentItem.itemData);

            itemHolder.currentItem = currentItem;

            return true;
        }
        void EnableItem(EquippableItem itemData, bool playEquipAnimation)
        {
            if (itemData.isDualItem)
            {
                EquippedItemRight?.gameObject?.SetActive(true);
                EquippedItemLeft?.gameObject?.SetActive(true);
            }
            else
            {
                EquippedItemObject?.gameObject?.SetActive(true);
            }
            if (itemData.overrideController)
                animGraph.UpdateOverrideController(itemData.overrideController, playEquipAnimation);

            itemAttacher.EquipItem(itemData);
        }

        #endregion

        #region UnEquip

        public void UnEquipItem(bool playUnEquipAnimation = true, bool disableItem = true, Action onItemDisabled = null)
        {
            StartCoroutine(UnEquipItemAsync(playUnEquipAnimation, disableItem, onItemDisabled));
        }

        private IEnumerator UnEquipItemAsync(bool playUnEquipAnimation = true, bool disableItem = true, Action onItemDisabled = null)
        {
            // Exit if there is no currently equipped item, an item switch is already in progress, 
            // or if unequipping is prevented
            if (EquippedItem == null || IsChangingItem || PreventItemUnEquip)
                yield break;

            // Store the currently equipped item reference before unequipping
            var itemData = EquippedItem;

            // Mark that an item is currently being unequipped
            IsUnEquippingItem = true;
            bool weaponDisabled = false;
            // Define an action that disables the weapon once the unequip animation reaches a certain point
            var weaponDisableAction = new ActionData()
            {
                normalizeTime = EquippedItem.itemDisableTime,
                action = () =>
                {
                    // Invoke the completion callback for unequipping
                    OnBeforeItemDisable?.Invoke(itemData);

                    // Disable the currently equipped weapon
                    DisableCurrentItem(disableItem);
                    onItemDisabled?.Invoke();

                    // Clear the currently equipped item reference
                    EquippedItem = null;

                    // Remove item references from both hand handlers
                    RightHandHolder.currentItem = null;
                    LeftHandHolder.currentItem = null;

                    // Stop any looping animations that were playing
                    animGraph.StopLoopingAnimations(true);

                    // Reset the animation override controller
                    animGraph.UpdateOverrideController(null, playUnEquipAnimation);
                    weaponDisabled = true;
                    itemAttacher.UnEquipItem(itemData);
                }
            };

            OnUnEquip?.Invoke();

            if (playUnEquipAnimation && itemData.unEquipClip.clip != null)
            {
                yield return animGraph.CrossfadeAsync(null, itemData.unEquipClip, mask: Mask.Arm, actions: weaponDisableAction);
            }
            else
            {
                // If no animation is played, immediately invoke the disable action
                yield return null;      // Unity seems to crash if we do not wait for a frame
                yield return null;      // Unity seems to crash if we do not wait for a frame
                weaponDisableAction.action?.Invoke();
            }
            if (!weaponDisabled)
            {
                weaponDisableAction.action?.Invoke();
            }
            OnUnEquipComplete?.Invoke();

            IsUnEquippingItem = false;
        }
        void DisableCurrentItem(bool disableItem = true)
        {
            if (EquippedItem.isDualItem)
            {
                if (disableItem)
                {
                    EquippedItemRight?.gameObject.SetActive(false);
                    EquippedItemLeft?.gameObject.SetActive(false);
                }
            }
            else
            {
                if (disableItem)
                {
                    EquippedItemObject?.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Drop the specified equippable item from the equippable items list.
        /// </summary>
        /// <param name="item">The equippable item to be removed.</param>
        /// <param name="destroyItem">
        /// If true, the item will be destroyed. 
        /// If false, the item will remain but physics will be enabled, causing it to fall by gravity.
        public void DropItem(EquippableItem item = null, bool destroyItem = true)
        {
            if(item == null)
            {
                item = EquippedItem;
                if(item == null) return;
            }
            
            if (EquippedItem == item)
            {
                if (equippableItems.Contains(item))
                {
                    equippableItems.Remove(item);
                }
                if (!destroyItem)
                {
                    var equipHotspot = Instantiate(EquipSettings.i.EquipHotspot, EquippedItemObject.transform);
                    equipHotspot.transform.localRotation = Quaternion.identity;
                    equipHotspot.transform.localPosition = Vector3.zero;
                    equipHotspot.item = EquippedItemObject.itemData;
                    equipHotspot.itemObject = EquippedItemObject;
                    var itemCollider = EquippedItemObject.GetComponent<Collider>();
                }
                UnEquipItem(false, destroyItem);
                if (item.isDualItem)
                {
                    //var itemCloneR = Instantiate(EquippedItemRight, EquippedItemRight.transform.parent);
                    //itemCloneR.transform.SetPositionAndRotation(EquippedItemRight.transform.position, EquippedItemRight.transform.rotation);
                    //itemCloneR.gameObject.SetActive(false);
                    //var itemCloneL = Instantiate(EquippedItemLeft, EquippedItemLeft.transform.parent);
                    //itemCloneL.transform.SetPositionAndRotation(EquippedItemLeft.transform.position, EquippedItemLeft.transform.rotation);
                    //itemCloneL.gameObject.SetActive(false);
                    EquippedItemLeft.transform.parent = null;
                    EquippedItemRight.transform.parent = null;

                   

                    if (destroyItem)
                    {
                        Destroy(EquippedItemRight.gameObject);
                        Destroy(EquippedItemLeft.gameObject);
                    }
                    else
                    {
                        EquippedItemRight.HandlePhysics(true);
                        EquippedItemLeft.HandlePhysics(true);
                    }
                }
                else
                {
                    //var itemClone = Instantiate(EquippedItemObject, EquippedItemObject.transform.parent);
                    //itemClone.transform.SetPositionAndRotation(EquippedItemObject.transform.position, EquippedItemObject.transform.rotation);
                    //itemClone.gameObject.SetActive(false);

                    EquippedItemObject.transform.parent = null;

                    if (destroyItem)
                    {
                        Destroy(EquippedItemObject.gameObject);
                    }
                    else
                    {
                        EquippedItemObject.HandlePhysics(true);
                    }
                }
                OnItemBecameUnusable?.Invoke(item);
            }
        }

        #endregion

        #region Utilities

        public void ResumeIdleAnimation()
        {
            if (IsCurrentItemUnusable)
            {
                PlayIdleAnimation();
                IsCurrentItemUnusable = false;
                OnItemBecameUnrestricted?.Invoke(EquippedItem);
            }
        }

        public void StopIdleAnimation()
        {
            animGraph.StopLoopingAnimations(true);
            IsCurrentItemUnusable = true;
            OnItemBecameRestricted?.Invoke(EquippedItem);
        }

        public void PlayIdleAnimation()
        {
            animGraph.PlayLoopingAnimation(EquippedItem.itemEquippedIdleClip, mask: EquippedItem.itemEquippedIdleClipMask, isActAsAnimatorOutput: true);
        }

        #endregion
    }



    public class CustomAction
    {
        public Action action = null;
    }
}
