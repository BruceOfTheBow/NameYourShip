using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace ShipToShoreCam
{
    [BepInPlugin("fixingdeer.ShipToShoreCam", "Ship to Shore Cam", "1.2.1")]
    [BepInProcess("valheim.exe")]

    public class ShipToShoreCam : BaseUnityPlugin
    {
        private readonly Harmony harmony = new Harmony("fixingdeer.ShipToShoreCam");
        static string changeZoom;
        static float zoomBeforeEntry;

        private static ConfigEntry<bool> enableMod;
        private static ConfigEntry<float> exitShipZoom;
        private static ConfigEntry<float> enterShipZoom;
        private static ConfigEntry<bool> useZoomLevelBeforeEntry;
        private static ConfigEntry<bool> showDebugMessages;

        void Awake()
        {
            enableMod = Config.Bind<bool>("General", "enableMod", true, "Enable this mod");
            exitShipZoom = Config.Bind<float>("General", "exitShipZoom", 4f, "How far to zoom in when exiting a ship *Only used if useZoomLevelBeforeEntry is false");
            enterShipZoom = Config.Bind<float>("General", "enterShipZoom", 8f, "How far to zoom out when entering a ship");
            useZoomLevelBeforeEntry = Config.Bind<bool>("General", "useZoomLevelBeforeEntry", true, "When exiting a ship, should we go back to the zoom you had when you entered?");
            showDebugMessages = Config.Bind<bool>("General", "showDebugMessages", false, "Should debug messages be sent to the console?");

            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(Ship), "OnTriggerEnter")]
        public static class Ship_OnTriggerEnter_Patch
        {
            static void Prefix(Collider collider)
            {
                if (enableMod.Value)
                { 
                    var player = collider.GetComponent<Player>();
                    if (player && player == Player.m_localPlayer)
                    {
                        if (showDebugMessages.Value) Debug.Log("### Ship.OnCollisionEnter ###");

                        changeZoom = "Enter Ship";
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Ship), "OnTriggerExit")]
        public static class Ship_OnTriggerExit_Patch
        {
            static void Prefix(Collider collider)
            {
                if (enableMod.Value)
                {

                    var player = collider.GetComponent<Player>();
                    if (player && player == Player.m_localPlayer)
                    {
                        if (showDebugMessages.Value) Debug.Log("### Ship.OnCollisionExit ###");

                        changeZoom = "Exit Ship";
                    }
                }
            }
        }

        [HarmonyPatch(typeof(GameCamera), "UpdateCamera")]
        public static class GameCamera_Update_Patch
        {
            static void Prefix(ref GameCamera __instance)
            {
                if (enableMod.Value)
                {

                    if (changeZoom == "Enter Ship")
                    {
                        if (__instance.m_distance != enterShipZoom.Value)
                        {

                            //Verify valid minimum zoom level
                            if (enterShipZoom.Value < 1)
                                enterShipZoom.Value = 1;

                            //Save pre-entry zoom level
                            zoomBeforeEntry = __instance.m_distance;

                            if (showDebugMessages.Value) Debug.Log("Current zoom level: " + __instance.m_distance);
                            if (showDebugMessages.Value) Debug.Log("Player on ship, setting zoom to " + enterShipZoom.Value);

                            //Set the zoom to the 
                            __instance.m_distance = enterShipZoom.Value;

                            if (showDebugMessages.Value) Debug.Log("Now zoomed out to: " + __instance.m_distance);
                        }
                        else
                            if (showDebugMessages.Value) Debug.Log("Player on ship, no zoom change needed");
                    }
                    else if (changeZoom == "Exit Ship")
                    {
                        float newZoomValue;

                        //Determine which value to use for the exit zoom (exitShipZoom.Value or zoomBeforeEntry)
                        if (showDebugMessages.Value) Debug.Log("useZoomeLevelBeforeEntry: " + useZoomLevelBeforeEntry.Value);

                        if (useZoomLevelBeforeEntry.Value == true)
                        {
                            newZoomValue = zoomBeforeEntry;
                        }
                        else
                        {
                            newZoomValue = exitShipZoom.Value;
                        }

                        if (showDebugMessages.Value) Debug.Log("newZoomValue: " + newZoomValue);

                        if (__instance.m_distance != newZoomValue)
                        {
                            //Verify valid minimum zoom level
                            if (newZoomValue < 1)
                                newZoomValue = 1;

                            if (showDebugMessages.Value) Debug.Log("Current zoom level: " + __instance.m_distance);
                            if (showDebugMessages.Value) Debug.Log("Player off ship, setting zoom to " + newZoomValue);

                            __instance.m_distance = newZoomValue;

                            if (showDebugMessages.Value) Debug.Log("Now zoomed in to: " + __instance.m_distance);
                        }
                        else
                            if (showDebugMessages.Value) Debug.Log("Player off ship, no zoom change needed");

                    }

                    changeZoom = "";
                }
            }
        }
    }
}