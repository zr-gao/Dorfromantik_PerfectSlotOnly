using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace PerfectSlotOnly
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class PerfectSlotOnlyBase : BaseUnityPlugin
    {
        private const string modGUID = "JG.PerfectSlotOnly";
        private const string modName = "Perfect Slot";
        private const string modVersion = "1.0.0";

        private static readonly Color DarkerBackgroundColor = new Color(0.4f, 0.4f, 0.4f, 1.0f);

        private readonly Harmony harmony = new Harmony(modGUID);

        private static PerfectSlotOnlyBase Instance;

        private static bool IsAllowKeyHeld => Input.GetKey(KeyCode.LeftShift);

        [System.Runtime.InteropServices.DllImport("USER32.dll")] public static extern short GetKeyState(int nVirtKey);
        private static bool IsCapsLockOn => (GetKeyState(0x14) & 1) > 0;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            harmony.PatchAll(typeof(PerfectSlotOnlyBase));
        }

        private static bool IsSlotPerfectRot(TileSlot slot, Tile tile, int rot)
        {
            int num = 0;
            for (int i = 0; i < 6; i++)
            {
                int directionIndex = (i - rot + 6) % 6;
                Tile neighbor = slot.NeighborTiles[i];
                if (neighbor == null)
                {
                    num++;
                    continue;
                }
                GroupType groupType = tile.GetElementGroup(directionIndex, Space.World)?.GroupType;
                GroupType groupType2 = neighbor.GetElementGroup((i + 3) % 6, Space.World)?.GroupType;
                if (groupType == groupType2)
                {
                    num++;
                }
                else if (groupType != null && groupType2 != null && (groupType == neighbor.GetElementGroup((i + 3) % 6, Space.World, groupType)?.GroupType || groupType2 == tile.GetElementGroup(directionIndex, Space.World, groupType2)?.GroupType))
                {
                    num++;
                }
                else if ((tile.GetHybridEdges(directionIndex, Space.World).Count > 0 && groupType2 == null) || (neighbor.GetHybridEdges((i + 3) % 6, Space.World).Count > 0 && groupType == null))
                {
                    num++;
                }
            }
            return num == 6;
        }
        private static bool IsSlotPerfect(TileSlot slot, Tile tile)
        {
            for (int i = 0; i < 6; i++)
            {
                if (IsSlotPerfectRot(slot, tile, i))
                {
                    return true;
                }
            }
            return false;
        }


        [HarmonyPatch(typeof(TileSlotPreviewer), "UpdateTileSlotValidity")]
        [HarmonyPostfix]
        private static void ShowPerfectSlotOnly(Tile newTile, ref Dictionary<Vector2Int, TileSlot> ___tileSlots)
        {
            foreach (TileSlot value in ___tileSlots.Values)
            {
                if (IsCapsLockOn)
                // if (!IsAllowKeyHeld)
                {
                    if (!IsSlotPerfect(value, newTile))
                    {
                        value.SetState(TileSlotState.Invalid);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ChangeCameraBackgroundBasedOnFocus), "Update")]
        [HarmonyPostfix]
        static void UpdatePostfix(ref Material ___skyboxGradientMat)
        {

            if (IsCapsLockOn)
            {
                Color overideColor = DarkerBackgroundColor;
                Color.RGBToHSV(overideColor, out var H, out var S, out var V);
                Vector3 hsvOffsetColor2 = new Vector3(0f, -20f, 7f);
                Vector3 vector = new Vector3(H + hsvOffsetColor2.x / 100f, S + hsvOffsetColor2.y / 100f, V + hsvOffsetColor2.z / 100f);
                Color color2 = Color.HSVToRGB(vector.x, vector.y, vector.z);
                ___skyboxGradientMat.SetColor("_Color1", overideColor);
                ___skyboxGradientMat.SetColor("_Color2", color2);
            }
        }
    }
}
