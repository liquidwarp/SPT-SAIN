using System.Collections;
using EFT.InventoryLogic;
using SAIN.Components.PlayerComponentSpace.Classes.Equipment;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Info;

public class GearInfo
{
    public bool HasEarPiece { get; private set; }

    public EDeafStrength Deafness { get; private set; }

    protected SAINEquipmentClass Equipment { get; private set; }

    protected InventoryEquipment _equipment
    {
        get { return Equipment.EquipmentClass; }
    }

    public GearInfo(SAINEquipmentClass equipment)
    {
        Equipment = equipment;
    }

    private const float GEAR_UPDATE_FREQ = 2f;

    /// <summary>
    /// Having this in a coroutine makes it easier to profile performance. Thats the only reason its like that.
    /// </summary>
    /// <returns></returns>
    public IEnumerator GearUpdateLoop()
    {
        WaitForSeconds wait = new(GEAR_UPDATE_FREQ);
        while (true)
        {
            updateHeadGear();
            yield return wait;
        }
    }

    public Item GetItem(EquipmentSlot slot)
    {
        return _equipment.GetSlot(slot)?.ContainedItem;
    }

    /// <summary>
    /// Mirrors Player.UpdatePhonesReally: headphones come from the Earpiece slot, or anywhere on the headwear.
    /// Without headphones, deafness comes from an armored helmet, or failing that from the face cover's armor.
    /// </summary>
    private void updateHeadGear()
    {
        bool headphones = HasHeadphones(_equipment);
        EDeafStrength deafness = EDeafStrength.None;
        if (!headphones)
        {
            if (GetItem(EquipmentSlot.Headwear) is CompoundItem helmet && helmet.Template is ArmoredEquipmentTemplate template)
            {
                deafness = maxDeafness(template.DeafStrength, mergedArmorDeafness(helmet));
            }
            if (deafness == EDeafStrength.None && GetItem(EquipmentSlot.FaceCover) is CompoundItem faceCover)
            {
                deafness = mergedArmorDeafness(faceCover);
            }
        }
        HasEarPiece = headphones;
        Deafness = deafness;
    }

    // Replicates the headphones lookup in Player.UpdatePhonesReally
    public static bool HasHeadphones(InventoryEquipment equipment)
    {
        if (equipment.GetSlot(EquipmentSlot.Earpiece)?.ContainedItem is Headphones)
        {
            return true;
        }
        return equipment.GetSlot(EquipmentSlot.Headwear)?.ContainedItem is CompoundItem headwear && containsHeadphones(headwear);
    }

    // Replicates GetAllItemsFromCollection().OfType<Headphones>().Any(), slots only
    private static bool containsHeadphones(Item item)
    {
        if (item is Headphones)
        {
            return true;
        }
        if (item is not CompoundItem compound)
        {
            return false;
        }
        Slot[] slots = compound.Slots;
        if (slots == null)
        {
            return false;
        }
        for (int i = 0; i < slots.Length; i++)
        {
            Item contained = slots[i].ContainedItem;
            if (contained != null && containsHeadphones(contained))
            {
                return true;
            }
        }
        return false;
    }

    // Replicates the max Deaf over GetItemComponentsInChildren<CompositeArmorComponent>()
    private static EDeafStrength mergedArmorDeafness(Item item)
    {
        EDeafStrength deafness = armorDeafness(item);
        if (deafness == EDeafStrength.High || item is not CompoundItem compound || compound.Slots == null)
        {
            return deafness;
        }
        Slot[] slots = compound.Slots;
        for (int i = 0; i < slots.Length; i++)
        {
            Slot slot = slots[i];
            Item contained = slot.ContainedItem;
            if (contained == null || !slot.MergesWithChildren())
            {
                continue;
            }
            deafness = maxDeafness(deafness, mergedArmorDeafness(contained));
            if (deafness == EDeafStrength.High)
            {
                break;
            }
        }
        return deafness;
    }

    private static EDeafStrength armorDeafness(Item item)
    {
        CompositeArmorComponent armor = item.GetItemComponent<CompositeArmorComponent>();
        return armor != null ? armor.Deaf : EDeafStrength.None;
    }

    private static EDeafStrength maxDeafness(EDeafStrength a, EDeafStrength b)
    {
        return a > b ? a : b;
    }
}
